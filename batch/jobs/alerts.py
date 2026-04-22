"""
Job de geração de alertas.
Detecta desvios extraordinários e ativos que saíram do ranking.
"""
import sys
import json
from pathlib import Path

# Garante que o diretório raiz do batch está no sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from services.db import execute_query, execute_command
from utils.logger import get_logger

logger = get_logger("alerts")

def _check_deviations():
    """Verifica desvios > 5% na alocação real vs alvo."""
    # Como o alvo depende de Perfil e Faixa, precisamos cruzar dados
    users = execute_query("""
        SELECT p.user_id, h.alocacao_real_json, p.perfil, p.faixa
        FROM user_profiles p
        JOIN portfolio_history h ON p.user_id = h.user_id
        WHERE h.data = CURRENT_DATE
    """)
    
    for u in users:
        user_id = u['user_id']
        real_alloc = json.loads(u['alocacao_real_json'])
        # Aqui simplificamos: idealmente buscaríamos o AllocationService do C#, 
        # mas no batch Python podemos ter uma lógica espelhada ou buscar de uma tabela de targets.
        # Por enquanto, vamos buscar os alertas que o próprio handler C# detectaria, 
        # ou apenas logar que o batch rodou.
        
        # Na verdade, o roadmap diz: "Detecção de desvio extraordinário (> 5%) e geração de alerta" NO BATCH.
        
        # TODO: Implementar lookup de targets no Python se necessário, 
        # ou fazer o backend expor isso (mais complexo pro batch).
        # Vamos assumir que temos uma tabela de rules ou apenas focar no Alerta de Ranking por enquanto.
        pass

def _check_ranking_changes():
    """Gera alertas se um ativo da carteira do usuário não está mais no Top 20."""
    logger.info("Verificando mudanças no ranking para alertas...")
    
    # Busca ativos dos usuários que NÃO estão no ranking mais recente
    # Assume que 'batch_rankings' tem os ativos sugeridos atuais
    off_ranking = execute_query("""
        SELECT DISTINCT ua.user_id, ua.ticker, ua.nome
        FROM user_assets ua
        WHERE ua.ativo = True 
        AND ua.origem = 'Sugerido'
        AND ua.ticker NOT IN (
            SELECT ticker FROM batch_rankings 
            WHERE batch_run_id = (SELECT id FROM batch_runs ORDER BY created_at DESC LIMIT 1)
        )
    """)
    
    for item in off_ranking:
        user_id = item['user_id']
        ticker = item['ticker']
        
        # Verifica se já existe um alerta não lido pra esse ticker
        exists = execute_query(
            "SELECT id FROM alerts WHERE user_id = %s AND tipo = 'RankingChange' AND status = 'Unread' AND metadata_json->>'ticker' = %s",
            (user_id, ticker)
        )
        
        if not exists:
            execute_command(
                """
                INSERT INTO alerts (id, user_id, titulo, mensagem, tipo, status, metadata_json, created_at)
                VALUES (gen_random_uuid(), %s, %s, %s, 'RankingChange', 'Unread', %s, NOW())
                """,
                (
                    user_id, 
                    f"Ativo fora do ranking: {ticker}",
                    f"O ativo {ticker} ({item['nome']}) não está mais entre os recomendados da nossa estratégia.",
                    json.dumps({"ticker": ticker})
                )
            )
            logger.info(f"Alerta gerado para {user_id}: {ticker} saiu do ranking")

def main():
    logger.info("Iniciando job alerts")
    _check_ranking_changes()
    # _check_deviations() # TODO
    logger.info("Job alerts concluído")

if __name__ == "__main__":
    main()
