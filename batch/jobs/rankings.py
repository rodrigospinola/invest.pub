"""
Job de geração de rankings de ativos por sub-estratégia.

Gera top 20 para 6 sub-estratégias:
- Ações:  valor, dividendos, misto_acoes
- FIIs:   renda_fiis, valorizacao_fiis, misto_fiis

Fluxo:
1. Registra BatchRun no banco
2. Busca market_data do dia (ou mais recente)
3. Calcula score quantitativo via scoring service
4. Calcula score qualitativo via gemini_service (top 40 candidatos por tipo)
5. Combina scores e ranqueia top 20 por sub-estratégia
6. Detecta entrou_hoje/saiu_hoje comparando com ranking anterior
7. Persiste batch_rankings
8. Atualiza status do BatchRun
"""
import sys
import json
import uuid
from pathlib import Path
from typing import Any

# Garante que o diretório raiz do batch está no sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass

from services.db import execute_command, execute_query
from services import scoring, gemini_service
from utils.logger import get_logger

logger = get_logger("rankings")

TOP_N = 20
QUALITATIVE_CANDIDATES = 40  # Quantidade de candidatos que recebem avaliação qualitativa

# Sub-estratégias e suas configurações de ordenação
# chave_ordenacao: campo usado para ponderar o ranking
# tipo: 'acao' ou 'fii'
SUB_ESTRATEGIAS: dict[str, dict[str, str]] = {
    "valor":           {"tipo": "acao", "peso": "pl_score_desc"},
    "dividendos":      {"tipo": "acao", "peso": "dy_score_desc"},
    "misto_acoes":     {"tipo": "acao", "peso": "score_final_desc"},
    "renda_fiis":      {"tipo": "fii",  "peso": "dy_score_desc"},
    "valorizacao_fiis":{"tipo": "fii",  "peso": "pvpa_score_desc"},
    "misto_fiis":      {"tipo": "fii",  "peso": "score_final_desc"},
}

_INSERT_BATCH_RUN_SQL = """
INSERT INTO batch_runs (id, status, started_at, created_at)
VALUES (%s, 'running', NOW(), NOW());
"""

_UPDATE_BATCH_RUN_SQL = """
UPDATE batch_runs
SET status = %s, completed_at = NOW()
WHERE id = %s;
"""

_FETCH_MARKET_DATA_SQL = """
SELECT ticker, tipo, dados
FROM market_data
WHERE data_coleta = (
    SELECT MAX(data_coleta) FROM market_data
);
"""

_FETCH_YESTERDAY_RANKINGS_SQL = """
SELECT sub_estrategia, ticker
FROM batch_rankings
WHERE data_ranking = (
    SELECT MAX(data_ranking)
    FROM batch_rankings
    WHERE data_ranking < CURRENT_DATE
);
"""

_INSERT_RANKING_SQL = """
INSERT INTO batch_rankings (
    id, batch_run_id, sub_estrategia, ticker, nome,
    posicao, score_total, score_quantitativo, score_qualitativo,
    justificativa, indicadores, entrou_hoje, saiu_hoje,
    data_ranking, created_at
)
VALUES (
    %s, %s, %s, %s, %s,
    %s, %s, %s, %s,
    %s, %s, %s, %s,
    CURRENT_DATE, NOW()
);
"""


def _create_batch_run() -> str:
    """
    Cria um registro de BatchRun no banco com status 'running'.

    Returns:
        UUID do batch_run criado.
    """
    run_id = str(uuid.uuid4())
    execute_command(_INSERT_BATCH_RUN_SQL, (run_id,))
    logger.info(f"BatchRun criado: {run_id}")
    return run_id


def _update_batch_run_status(run_id: str, status: str) -> None:
    """
    Atualiza o status de um BatchRun.

    Args:
        run_id: UUID do batch_run.
        status: 'completed' ou 'failed'.
    """
    execute_command(_UPDATE_BATCH_RUN_SQL, (status, run_id))
    logger.info(f"BatchRun {run_id} atualizado para status '{status}'")


def _fetch_market_data() -> list[dict]:
    """
    Busca os dados de mercado mais recentes disponíveis.

    Returns:
        Lista de dicts com ticker, tipo e dados (já deserializados).
    """
    rows = execute_query(_FETCH_MARKET_DATA_SQL)
    result = []
    for row in rows:
        dados = row["dados"]
        if isinstance(dados, str):
            dados = json.loads(dados)
        result.append({
            "ticker": row["ticker"],
            "tipo": row["tipo"],
            "dados": dados,
        })
    logger.info(f"market_data carregado: {len(result)} ativos")
    return result


def _fetch_yesterday_rankings() -> dict[str, set[str]]:
    """
    Busca os rankings do dia útil anterior.

    Returns:
        Dicionário {sub_estrategia: set(tickers)} do ranking anterior.
    """
    rows = execute_query(_FETCH_YESTERDAY_RANKINGS_SQL)
    result: dict[str, set[str]] = {}
    for row in rows:
        sub = row["sub_estrategia"]
        ticker = row["ticker"]
        result.setdefault(sub, set()).add(ticker)
    logger.info(f"Rankings anteriores carregados: {len(result)} sub-estratégias")
    return result


def _score_ativos(ativos: list[dict]) -> list[dict]:
    """
    Calcula o score quantitativo para cada ativo.

    Args:
        ativos: Lista de dicts com ticker, tipo e dados.

    Returns:
        Lista enriquecida com score_quantitativo e sub-scores auxiliares.
    """
    scored: list[dict] = []
    for ativo in ativos:
        tipo = ativo["tipo"]
        dados = ativo["dados"]
        try:
            if tipo == "acao":
                score_quant = scoring.score_acao_csv(dados)
            else:
                score_quant = scoring.score_fii_csv(dados)

            scored.append({
                **ativo,
                "score_quantitativo": score_quant,
                # Sub-scores auxiliares para ordenação por sub-estratégia.
                # Ações (CSV): usa ROE como proxy de DY (CSV não tem DY para ações).
                # FIIs (CSV): usa DY 12M real e vacância real.
                "_s_pl":   scoring._score_pl(dados.get("pl")),
                "_s_dy":   (
                    scoring._score_roe_pct(dados.get("roe"))
                    if tipo == "acao"
                    else scoring._score_dy(dados.get("dy"))
                ),
                "_s_pvpa": scoring._score_pvpa(dados.get("p_vpa")),
            })
        except Exception as e:
            logger.error(f"Erro ao calcular score quantitativo de {ativo['ticker']}: {e}")

    logger.info(f"Scores quantitativos calculados: {len(scored)} ativos")
    return scored


def _enrich_with_qualitative(ativos: list[dict]) -> list[dict]:
    """
    Solicita avaliação qualitativa ao Gemini para os candidatos.

    Envia apenas os top QUALITATIVE_CANDIDATES por tipo para evitar
    custo excessivo de chamadas de API.

    Args:
        ativos: Lista de ativos já com score_quantitativo.

    Returns:
        Lista enriquecida com score_qualitativo, justificativa e score_final.
    """
    # Separar por tipo e selecionar top candidatos por score quantitativo
    acoes = sorted(
        [a for a in ativos if a["tipo"] == "acao"],
        key=lambda x: x["score_quantitativo"],
        reverse=True,
    )[:QUALITATIVE_CANDIDATES]

    fiis = sorted(
        [a for a in ativos if a["tipo"] == "fii"],
        key=lambda x: x["score_quantitativo"],
        reverse=True,
    )[:QUALITATIVE_CANDIDATES]

    candidatos = acoes + fiis
    logger.info(
        f"Avaliação qualitativa: {len(acoes)} ações e {len(fiis)} FIIs candidatos"
    )

    enriched_map: dict[str, dict] = {}
    for ativo in candidatos:
        ticker = ativo["ticker"]
        nome = ticker.replace(".SA", "")
        try:
            qual = gemini_service.get_qualitative_score(ticker, nome, ativo["dados"])
            enriched_map[ticker] = {
                "score_qualitativo": qual["score"],
                "justificativa": qual["justificativa"],
            }
        except Exception as e:
            logger.error(f"Erro na avaliação qualitativa de {ticker}: {e}")
            enriched_map[ticker] = {
                "score_qualitativo": 5.0,
                "justificativa": "Avaliação indisponível.",
            }

    # Aplicar enriquecimento e calcular score_final
    result: list[dict] = []
    for ativo in ativos:
        ticker = ativo["ticker"]
        qual_data = enriched_map.get(ticker, {
            "score_qualitativo": 5.0,
            "justificativa": "Avaliação indisponível.",
        })
        score_final = scoring.score_final(
            ativo["score_quantitativo"],
            qual_data["score_qualitativo"],
        )
        result.append({
            **ativo,
            "score_qualitativo": qual_data["score_qualitativo"],
            "justificativa": qual_data["justificativa"],
            "score_final": score_final,
        })

    return result


def _sort_key_for_sub_estrategia(ativo: dict, peso: str) -> float:
    """
    Retorna a chave de ordenação para uma sub-estratégia.

    Args:
        ativo: Dict enriquecido com scores.
        peso: Identificador da estratégia de peso.

    Returns:
        Valor float para ordenação decrescente.
    """
    if peso == "pl_score_desc":
        # Valor: prioriza ações com P/L atrativo (score P/L ponderado com score_final)
        return ativo.get("_s_pl", 0.0) * 0.5 + ativo.get("score_final", 0.0) * 0.5
    if peso == "dy_score_desc":
        # Dividendos/Renda: prioriza DY alto
        return ativo.get("_s_dy", 0.0) * 0.6 + ativo.get("score_final", 0.0) * 0.4
    if peso == "pvpa_score_desc":
        # Valorização: prioriza P/VP baixo (desconto)
        return ativo.get("_s_pvpa", 0.0) * 0.6 + ativo.get("score_final", 0.0) * 0.4
    # Padrão: misto / score_final puro
    return ativo.get("score_final", 0.0)


def _rank_sub_estrategia(
    ativos: list[dict],
    sub_estrategia: str,
    tipo: str,
    peso: str,
) -> list[dict]:
    """
    Ranqueia os ativos de um tipo para uma sub-estratégia específica.

    Args:
        ativos: Todos os ativos enriquecidos.
        sub_estrategia: Nome da sub-estratégia.
        tipo: 'acao' ou 'fii'.
        peso: Estratégia de ordenação.

    Returns:
        Lista de até TOP_N ativos ordenados com posição.
    """
    filtrados = [a for a in ativos if a["tipo"] == tipo]
    ordenados = sorted(
        filtrados,
        key=lambda a: _sort_key_for_sub_estrategia(a, peso),
        reverse=True,
    )
    top = ordenados[:TOP_N]

    return [
        {**ativo, "posicao": idx + 1, "sub_estrategia": sub_estrategia}
        for idx, ativo in enumerate(top)
    ]


def _persist_rankings(
    rankings: list[dict],
    batch_run_id: str,
    yesterday: dict[str, set[str]],
) -> int:
    """
    Persiste os rankings no banco de dados.

    Args:
        rankings: Lista de ativos ranqueados com posicao e sub_estrategia.
        batch_run_id: UUID do batch_run atual.
        yesterday: Mapa {sub_estrategia: set(tickers)} do dia anterior.

    Returns:
        Número de registros inseridos.
    """
    count = 0
    for item in rankings:
        sub = item["sub_estrategia"]
        ticker = item["ticker"]
        tickers_yesterday = yesterday.get(sub, set())

        entrou_hoje = ticker not in tickers_yesterday
        saiu_hoje = False  # Calculado separadamente para os que saíram

        try:
            execute_command(
                _INSERT_RANKING_SQL,
                (
                    str(uuid.uuid4()),
                    batch_run_id,
                    sub,
                    ticker,
                    ticker.replace(".SA", ""),
                    item["posicao"],
                    item["score_final"],
                    item["score_quantitativo"],
                    item["score_qualitativo"],
                    item["justificativa"],
                    json.dumps(item["dados"]),
                    entrou_hoje,
                    saiu_hoje,
                ),
            )
            count += 1
        except Exception as e:
            logger.error(f"Erro ao persistir ranking {sub}/{ticker}: {e}")

    return count


def main() -> None:
    """
    Ponto de entrada do job de rankings.

    Orquestra coleta, scoring, avaliação qualitativa e persistência dos rankings.
    sys.exit(1) em caso de falha crítica.
    """
    logger.info("Iniciando job rankings")

    run_id: str | None = None
    try:
        run_id = _create_batch_run()

        # 1. Buscar dados de mercado
        market_data = _fetch_market_data()
        if not market_data:
            logger.error("Nenhum dado de mercado disponível. Abortando.")
            if run_id:
                _update_batch_run_status(run_id, "failed")
            sys.exit(1)

        # 2. Buscar rankings anteriores para detectar entradas/saídas
        yesterday = _fetch_yesterday_rankings()

        # 3. Calcular scores quantitativos
        ativos_scored = _score_ativos(market_data)

        # 4. Enriquecer com avaliação qualitativa (top 40 por tipo)
        ativos_enriched = _enrich_with_qualitative(ativos_scored)

        # 5. Gerar rankings por sub-estratégia
        all_rankings: list[dict] = []
        for sub_estrategia, config in SUB_ESTRATEGIAS.items():
            ranked = _rank_sub_estrategia(
                ativos_enriched,
                sub_estrategia,
                config["tipo"],
                config["peso"],
            )
            all_rankings.extend(ranked)
            logger.info(
                f"Sub-estratégia '{sub_estrategia}': {len(ranked)} ativos ranqueados"
            )

        # 6. Persistir rankings
        total_inseridos = _persist_rankings(all_rankings, run_id, yesterday)

        logger.info(
            f"Resumo: {total_inseridos} rankings gerados para "
            f"{len(SUB_ESTRATEGIAS)} sub-estratégias"
        )

        _update_batch_run_status(run_id, "completed")
        logger.info("Job rankings concluído com sucesso")

    except Exception as e:
        logger.error(f"Erro crítico no job rankings: {e}", exc_info=True)
        if run_id:
            try:
                _update_batch_run_status(run_id, "failed")
            except Exception as update_err:
                logger.error(f"Erro ao atualizar status do BatchRun: {update_err}")
        sys.exit(1)


if __name__ == "__main__":
    main()
