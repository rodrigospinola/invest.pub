"""
Job de histórico de carteira.
Calcula valor total, rentabilidade e alocação real de cada usuário diariamente.
"""
import sys
import json
from pathlib import Path
from datetime import datetime, date

# Garante que o diretório raiz do batch está no sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from services.db import execute_query, execute_command
from services.yfinance_service import get_stock_info, get_fii_info
from utils.logger import get_logger

logger = get_logger("portfolio_history")

GOAL = 500000.0

import csv
import os

# Cache global para os CSVs
_csv_cache = {}

def _load_csv_cache():
    if _csv_cache:
        return
    
    base_dir = Path(__file__).resolve().parent.parent
    ativos_path = os.path.join(base_dir, 'data', 'ativos.csv')
    fiis_path = os.path.join(base_dir, 'data', 'fiis.csv')
    
    # Carregar Ativos (Preço aproximado = P/L * LPA)
    if os.path.exists(ativos_path):
        with open(ativos_path, 'r', encoding='utf-8-sig', errors='replace') as f:
            reader = csv.reader(f)
            next(reader, None) # Pula header
            for row in reader:
                if len(row) > 6:
                    ticker = row[1]
                    try:
                        pl = float(row[4])
                        lpa = float(row[6])
                        if pl > 0 and lpa > 0:
                            _csv_cache[ticker] = pl * lpa
                    except ValueError:
                        pass
    
    # Carregar FIIs (Preço na coluna 2 como R$ XX,XX)
    if os.path.exists(fiis_path):
        with open(fiis_path, 'r', encoding='utf-8-sig', errors='replace') as f:
            reader = csv.reader(f)
            next(reader, None) # Pula header
            for row in reader:
                if len(row) > 2:
                    ticker = row[0]
                    preco_str = row[2].replace('R$', '').replace('.', '').replace(',', '.').strip()
                    try:
                        _csv_cache[ticker] = float(preco_str)
                    except ValueError:
                        pass

def _get_ticker_price(ticker: str) -> float:
    """Busca o preço atual de um ticker. Tenta market_data primeiro, depois arquivo CSV."""
    # Busca cache do dia na market_data
    res = execute_query(
        "SELECT dados FROM market_data WHERE ticker = %s AND data_coleta = CURRENT_DATE",
        (ticker,)
    )
    if res:
        return float(res[0]['dados'].get('price', 0))
    
    # Fallback CSVs locais
    logger.info(f"Ticker {ticker} não encontrado no market_data de hoje. Buscando no CSV local...")
    _load_csv_cache()
    
    if ticker in _csv_cache:
        return _csv_cache[ticker]
        
    logger.warning(f"Ticker {ticker} também não encontrado nos arquivos CSV.")
    return 0.0

def main():
    logger.info("Iniciando job portfolio_history")
    
    # 1. Obter todos os usuários com ativos
    users = execute_query("SELECT DISTINCT user_id FROM user_assets WHERE ativo = True")
    if not users:
        logger.info("Nenhum usuário com ativos ativos encontrado.")
        return

    # Cache de preços para não repetir chamadas pro mesmo ticker
    price_cache = {}

    for user in users:
        user_id = user['user_id']
        assets = execute_query(
            "SELECT ticker, quantidade, classe, nome FROM user_assets WHERE user_id = %s AND ativo = True",
            (user_id,)
        )
        
        total_value = 0.0
        allocation_values = {}
        
        for asset in assets:
            ticker = asset['ticker']
            if ticker not in price_cache:
                price_cache[ticker] = _get_ticker_price(ticker)
            
            price = price_cache[ticker]
            value = float(asset['quantidade']) * price
            total_value += value
            
            classe = asset['classe']
            allocation_values[classe] = allocation_values.get(classe, 0.0) + value
            
            # Grava AssetHistory
            execute_command(
                """
                INSERT INTO asset_history ("Id", "Ticker", "Data", "PrecoFechamento", "DividendoNoDia", "CreatedAt")
                VALUES (gen_random_uuid(), %s, CURRENT_DATE, %s, 0, NOW())
                ON CONFLICT ("Ticker", "Data") DO UPDATE SET "PrecoFechamento" = EXCLUDED."PrecoFechamento"
                """,
                (ticker, price)
            )

        # Calcula % de alocação real
        allocation_pct = {}
        if total_value > 0:
            for classe, val in allocation_values.items():
                allocation_pct[classe] = round((val / total_value) * 100, 2)

        # 2. Obter histórico anterior para rentabilidade
        prev_history = execute_query(
            'SELECT "ValorTotal", "RentabilidadeAcumulada" FROM portfolio_history WHERE "UserId" = %s ORDER BY "Data" DESC LIMIT 1',
            (user_id,)
        )
        
        rent_dia = 0.0
        rent_acum = 0.0
        
        if prev_history:
            prev_val = float(prev_history[0]['ValorTotal'])
            if prev_val > 0:
                rent_dia = ((total_value / prev_val) - 1) * 100
                rent_acum = float(prev_history[0]['RentabilidadeAcumulada']) + rent_dia
        
        dist_meta = max(0.0, GOAL - total_value)

        # 3. Gravar PortfolioHistory
        execute_command(
            """
            INSERT INTO portfolio_history (
                "Id", "UserId", "Data", "ValorTotal", "RentabilidadeNoDia", 
                "RentabilidadeAcumulada", "DistanciaMeta", "AlocacaoRealJson", "CreatedAt"
            )
            VALUES (gen_random_uuid(), %s, CURRENT_DATE, %s, %s, %s, %s, %s, NOW())
            ON CONFLICT ("UserId", "Data") DO UPDATE SET 
                "ValorTotal" = EXCLUDED."ValorTotal",
                "RentabilidadeNoDia" = EXCLUDED."RentabilidadeNoDia",
                "RentabilidadeAcumulada" = EXCLUDED."RentabilidadeAcumulada",
                "DistanciaMeta" = EXCLUDED."DistanciaMeta",
                "AlocacaoRealJson" = EXCLUDED."AlocacaoRealJson"
            """,
            (user_id, total_value, rent_dia, rent_acum, dist_meta, json.dumps(allocation_pct))
        )
        
        logger.info(f"Histórico processado para {user_id}: R$ {total_value:.2f}")

    logger.info("Job portfolio_history concluído")

if __name__ == "__main__":
    main()
