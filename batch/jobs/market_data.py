"""
Job de coleta de dados de mercado: ações e FIIs via CSV.

Ações: carregadas de batch/data/ativos.csv (exportação do Fundamentus/Status Invest).
       Mapeamento de colunas em csv_service._COLUMN_MAP.
FIIs:  carregados de batch/data/fiis.csv (planilha "Rendimentos FIIs - Clube do Valor").
       Inclui DY 12M, P/VP, vacância real e liquidez diária.
       Zero dependência de yfinance ou scraping.
"""
import sys
import json
from pathlib import Path

# Garante que o diretório raiz do batch está no sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass

from services.csv_service import load_acoes_br
from services.fii_csv_service import load_fiis
from services.db import execute_command, execute_query
from utils.logger import get_logger

logger = get_logger("market_data")


_CREATE_TABLE_SQL = """
CREATE TABLE IF NOT EXISTS market_data (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ticker VARCHAR(20) NOT NULL,
    tipo VARCHAR(10) NOT NULL,
    dados JSONB NOT NULL,
    data_coleta DATE NOT NULL DEFAULT CURRENT_DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(ticker, data_coleta)
);
"""

_UPSERT_SQL = """
INSERT INTO market_data (ticker, tipo, dados, data_coleta)
VALUES (%s, %s, %s, CURRENT_DATE)
ON CONFLICT (ticker, data_coleta)
DO UPDATE SET dados = EXCLUDED.dados;
"""


def _create_table() -> None:
    """Cria a tabela market_data se ainda não existir."""
    execute_command(_CREATE_TABLE_SQL)
    logger.info("Tabela market_data verificada/criada com sucesso")


def _upsert_market_data(ticker: str, tipo: str, dados: dict) -> None:
    """
    Faz upsert de um registro na tabela market_data.

    Args:
        ticker: Ticker do ativo.
        tipo: 'acao' ou 'fii'.
        dados: Dicionário com os indicadores coletados.
    """
    execute_command(_UPSERT_SQL, (ticker, tipo, json.dumps(dados)))


def _collect_acoes() -> tuple[list[str], list[str]]:
    """
    Carrega ações brasileiras do CSV e persiste na tabela market_data.

    O ticker é normalizado para o formato yfinance (sufixo .SA) se necessário.

    Returns:
        Tupla (sucessos, falhas) com listas de tickers.
    """
    acoes = load_acoes_br()
    if not acoes:
        logger.error("CSV de ações vazio ou não encontrado em /app/data/")
        return [], ["CSV_LOAD_FAILED"]

    sucessos: list[str] = []
    falhas: list[str] = []

    for ativo in acoes:
        ticker_raw = ativo.get("ticker", "").strip()
        if not ticker_raw:
            continue

        # Normaliza para formato com sufixo .SA (padrão interno do projeto)
        ticker = ticker_raw if ticker_raw.endswith(".SA") else f"{ticker_raw}.SA"

        try:
            # Garante que o ticker normalizado consta no dict persistido
            dados = {**ativo, "ticker": ticker}
            _upsert_market_data(ticker, "acao", dados)
            sucessos.append(ticker)
        except Exception as e:
            logger.error(f"Ação {ticker}: erro ao persistir — {e}")
            falhas.append(ticker)

    logger.info(f"CSV: {len(sucessos)} ações persistidas, {len(falhas)} falhas")
    return sucessos, falhas


def _collect_fiis() -> tuple[list[str], list[str]]:
    """
    Carrega FIIs do CSV e persiste na tabela market_data.

    Usa batch/data/fiis.csv (planilha Clube do Valor) com DY 12M,
    P/VP, vacância real e liquidez diária. Zero dependência de rede.

    Returns:
        Tupla (sucessos, falhas) com listas de tickers.
    """
    fiis = load_fiis()
    if not fiis:
        logger.error("CSV de FIIs vazio ou não encontrado em /app/data/")
        return [], ["CSV_FIIS_LOAD_FAILED"]

    sucessos: list[str] = []
    falhas: list[str] = []

    for fii in fiis:
        ticker_raw = fii.get("ticker", "").strip()
        if not ticker_raw:
            continue

        ticker = ticker_raw if ticker_raw.endswith(".SA") else f"{ticker_raw}.SA"

        try:
            dados = {**fii, "ticker": ticker}
            _upsert_market_data(ticker, "fii", dados)
            sucessos.append(ticker)
        except Exception as e:
            logger.error(f"FII {ticker}: erro ao persistir — {e}")
            falhas.append(ticker)

    logger.info(f"CSV FIIs: {len(sucessos)} persistidos, {len(falhas)} falhas")
    return sucessos, falhas


def main() -> None:
    """
    Ponto de entrada do job de coleta de dados de mercado.

    Carrega ações do CSV e coleta FIIs via yfinance independentemente.
    sys.exit(1) apenas se NENHUM ticker for coletado com sucesso.
    """
    logger.info("Iniciando job market_data")

    _create_table()

    acoes_ok, acoes_fail = _collect_acoes()
    fiis_ok, fiis_fail = _collect_fiis()

    total_ok = len(acoes_ok) + len(fiis_ok)
    total_fail = len(acoes_fail) + len(fiis_fail)

    logger.info(
        f"Resumo: {total_ok} tickers coletados com sucesso, {total_fail} falhas | "
        f"Ações (CSV): {len(acoes_ok)} ok / {len(acoes_fail)} falhas | "
        f"FIIs (yfinance): {len(fiis_ok)} ok / {len(fiis_fail)} falhas"
    )

    if acoes_fail and acoes_fail != ["CSV_LOAD_FAILED"]:
        logger.warning(f"Ações com falha: {acoes_fail[:10]}")
    if fiis_fail:
        logger.warning(f"FIIs com falha: {fiis_fail}")

    if total_ok == 0:
        logger.error("Nenhum ticker coletado com sucesso. Abortando com erro.")
        sys.exit(1)

    logger.info("Job market_data concluído")


if __name__ == "__main__":
    main()
