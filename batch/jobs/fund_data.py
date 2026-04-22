"""
Job de coleta de dados de fundos de investimento via CVM (dados abertos).

Coleta informações de fundos multimercados e fundos de renda fixa ativos,
combinando cadastro (classe, situação) com informe diário (cota, captações).
Persiste na tabela fund_data.

Fluxo:
1. Baixar cadastro completo de fundos da CVM
2. Filtrar fundos em funcionamento normal de classes relevantes
3. Baixar informe diário do mês atual (último disponível)
4. Cruzar cadastro + informe e persistir os dados
"""
import sys
import json
from datetime import date, timedelta
from pathlib import Path

# Garante que o diretório raiz do batch está no sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass

from services.cvm_service import download_fundos_cadastro, download_fundos_informe
from services.db import execute_command, execute_query
from utils.logger import get_logger

logger = get_logger("fund_data")

# Classes de fundos relevantes para o Invest
# (multimercados e renda fixa de longo prazo)
CLASSES_RELEVANTES: frozenset[str] = frozenset([
    "Fundo Multimercado",
    "Fundo de Renda Fixa",
    "Fundo de Ações",
])

SITUACAO_ATIVO = "EM FUNCIONAMENTO NORMAL"

_CREATE_TABLE_SQL = """
CREATE TABLE IF NOT EXISTS fund_data (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cnpj VARCHAR(20) NOT NULL,
    denominacao VARCHAR(200) NOT NULL,
    classe VARCHAR(100) NOT NULL,
    dados JSONB NOT NULL,
    competencia VARCHAR(7) NOT NULL,
    data_coleta DATE NOT NULL DEFAULT CURRENT_DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(cnpj, competencia)
);
"""

_UPSERT_SQL = """
INSERT INTO fund_data (cnpj, denominacao, classe, dados, competencia, data_coleta)
VALUES (%s, %s, %s, %s, %s, CURRENT_DATE)
ON CONFLICT (cnpj, competencia)
DO UPDATE SET dados = EXCLUDED.dados, data_coleta = EXCLUDED.data_coleta;
"""


def _create_table() -> None:
    """Cria a tabela fund_data se ainda não existir."""
    execute_command(_CREATE_TABLE_SQL)
    logger.info("Tabela fund_data verificada/criada com sucesso")


def _get_competencias() -> list[str]:
    """
    Retorna as competências para coleta: mês atual e mês anterior.
    O informe do mês atual pode ainda não estar disponível.

    Returns:
        Lista com até 2 competências no formato 'YYYY-MM'.
    """
    hoje = date.today()
    mes_atual = hoje.strftime("%Y-%m")
    mes_anterior = (hoje.replace(day=1) - timedelta(days=1)).strftime("%Y-%m")
    return [mes_atual, mes_anterior]


def _filtrar_fundos_ativos(cadastro: list[dict]) -> dict[str, dict]:
    """
    Filtra fundos em funcionamento normal e de classes relevantes.

    Args:
        cadastro: Lista completa de fundos do cadastro CVM.

    Returns:
        Dict {cnpj: {denominacao, classe}} dos fundos elegíveis.
    """
    elegíveis: dict[str, dict] = {}
    for fundo in cadastro:
        situacao = fundo.get("situacao", "").strip().upper()
        classe = fundo.get("classe", "").strip()
        cnpj = fundo.get("cnpj", "").strip()

        if not cnpj:
            continue
        if situacao != SITUACAO_ATIVO:
            continue
        if classe not in CLASSES_RELEVANTES:
            continue

        elegíveis[cnpj] = {
            "denominacao": fundo.get("denominacao_social", "").strip(),
            "classe": classe,
        }

    logger.info(f"Fundos elegíveis (ativos + classes relevantes): {len(elegíveis)}")
    return elegíveis


def _processar_informe(
    informe: list[dict],
    fundos_elegíveis: dict[str, dict],
    competencia: str,
) -> int:
    """
    Cruza informe diário com cadastro de fundos elegíveis e persiste.

    Para cada CNPJ encontrado no informe que também está nos elegíveis,
    agrega os dados disponíveis e faz upsert.

    Args:
        informe: Registros do informe diário CVM.
        fundos_elegíveis: Mapeamento CNPJ → metadados do fundo.
        competencia: Mês de referência no formato 'YYYY-MM'.

    Returns:
        Número de fundos persistidos.
    """
    # Agrupar registros por CNPJ, mantendo o mais recente por data
    por_cnpj: dict[str, dict] = {}
    for registro in informe:
        cnpj = registro.get("cnpj", "").strip()
        if cnpj not in fundos_elegíveis:
            continue

        dt = registro.get("dt_comptc", "")
        existing = por_cnpj.get(cnpj)
        if existing is None or dt > existing.get("dt_comptc", ""):
            por_cnpj[cnpj] = registro

    logger.info(
        f"Competência {competencia}: {len(por_cnpj)} fundos elegíveis com dados no informe"
    )

    count = 0
    for cnpj, registro in por_cnpj.items():
        meta = fundos_elegíveis[cnpj]
        dados = {
            "dt_comptc":  registro.get("dt_comptc", ""),
            "vl_quota":   _parse_decimal(registro.get("vl_quota", "")),
            "captc_dia":  _parse_decimal(registro.get("captc_dia", "")),
            "resg_dia":   _parse_decimal(registro.get("resg_dia", "")),
            "nr_cotst":   _parse_int(registro.get("nr_cotst", "")),
        }
        try:
            execute_command(
                _UPSERT_SQL,
                (
                    cnpj,
                    meta["denominacao"],
                    meta["classe"],
                    json.dumps(dados),
                    competencia,
                ),
            )
            count += 1
        except Exception as e:
            logger.error(f"Erro ao persistir fundo {cnpj} ({competencia}): {e}")

    return count


def _parse_decimal(value: str) -> float | None:
    """Converte string decimal com vírgula para float."""
    if not value or value.strip() == "":
        return None
    try:
        return float(value.replace(",", "."))
    except ValueError:
        return None


def _parse_int(value: str) -> int | None:
    """Converte string inteira para int."""
    if not value or value.strip() == "":
        return None
    try:
        return int(float(value.replace(",", ".")))
    except ValueError:
        return None


def main() -> None:
    """
    Ponto de entrada do job de coleta de dados de fundos.

    Baixa cadastro e informes da CVM, filtra fundos elegíveis e persiste.
    sys.exit(1) apenas se o cadastro não puder ser baixado ou nenhum dado
    for persistido após todas as tentativas.
    """
    logger.info("Iniciando job fund_data")

    _create_table()

    # 1. Baixar cadastro de fundos
    logger.info("Baixando cadastro de fundos da CVM...")
    cadastro = download_fundos_cadastro()
    if not cadastro:
        logger.error("Cadastro CVM vazio ou indisponível. Abortando.")
        sys.exit(1)

    # 2. Filtrar fundos elegíveis
    fundos_elegíveis = _filtrar_fundos_ativos(cadastro)
    if not fundos_elegíveis:
        logger.error("Nenhum fundo elegível encontrado no cadastro. Abortando.")
        sys.exit(1)

    # 3. Tentar competência atual e anterior
    competencias = _get_competencias()
    total_persistidos = 0

    for competencia in competencias:
        logger.info(f"Processando competência {competencia}...")
        informe = download_fundos_informe(competencia)

        if not informe:
            logger.warning(f"Informe vazio para competência {competencia}. Tentando próxima.")
            continue

        persistidos = _processar_informe(informe, fundos_elegíveis, competencia)
        total_persistidos += persistidos
        logger.info(f"Competência {competencia}: {persistidos} fundos persistidos")

        if persistidos > 0:
            # Sucesso na competência mais recente disponível — não precisa processar mais
            break

    if total_persistidos == 0:
        logger.error(
            "Nenhum dado de fundo persistido após todas as tentativas. Abortando."
        )
        sys.exit(1)

    logger.info(f"Job fund_data concluído — {total_persistidos} fundos persistidos")


if __name__ == "__main__":
    main()
