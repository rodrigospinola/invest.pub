"""Serviço de coleta de dados de fundos de investimento via CVM (dados abertos)."""
import io
import requests
import pandas as pd
from utils.logger import get_logger

logger = get_logger(__name__)

_CVM_CAD_URL = "https://dados.cvm.gov.br/dados/FI/CAD/DADOS/cad_fi.csv"
_CVM_INF_URL = "https://dados.cvm.gov.br/dados/FI/DOC/INF_DIARIO/DADOS/inf_diario_fi_{competencia}.csv"
_TIMEOUT = 60
_ENCODING = "latin-1"


def download_fundos_cadastro() -> list[dict]:
    """
    Baixa o cadastro de fundos de investimento da CVM.

    URL: https://dados.cvm.gov.br/dados/FI/CAD/DADOS/cad_fi.csv

    Returns:
        Lista de dicts com: cnpj, denominacao_social, situacao, classe.
        Retorna lista vazia em caso de erro.
    """
    try:
        logger.info("Baixando cadastro de fundos da CVM...")
        response = requests.get(_CVM_CAD_URL, timeout=_TIMEOUT)
        response.raise_for_status()

        df = pd.read_csv(
            io.StringIO(response.content.decode(_ENCODING)),
            sep=";",
            usecols=["CNPJ_FUNDO", "DENOM_SOCIAL", "SIT", "CLASSE"],
            dtype=str,
        )
        df.columns = ["cnpj", "denominacao_social", "situacao", "classe"]
        df = df.fillna("")

        records = df.to_dict(orient="records")
        logger.info(f"Cadastro CVM: {len(records)} fundos carregados")
        return records

    except requests.RequestException as e:
        logger.error(f"Erro ao baixar cadastro CVM: {e}")
        return []
    except Exception as e:
        logger.error(f"Erro ao processar cadastro CVM: {e}")
        return []


def download_fundos_informe(competencia: str) -> list[dict]:
    """
    Baixa o informe diário de fundos de investimento da CVM para uma competência.

    Args:
        competencia: Mês de referência no formato "YYYY-MM" (ex: "2024-03").

    Returns:
        Lista de dicts com: cnpj, dt_comptc, vl_quota, captc_dia, resg_dia, nr_cotst.
        Retorna lista vazia em caso de erro.
    """
    competencia_fmt = competencia.replace("-", "")
    url = _CVM_INF_URL.format(competencia=competencia_fmt)

    try:
        logger.info(f"Baixando informe CVM para competência {competencia}...")
        response = requests.get(url, timeout=_TIMEOUT)
        response.raise_for_status()

        df = pd.read_csv(
            io.StringIO(response.content.decode(_ENCODING)),
            sep=";",
            usecols=["CNPJ_FUNDO", "DT_COMPTC", "VL_QUOTA", "CAPTC_DIA", "RESG_DIA", "NR_COTST"],
            dtype=str,
        )
        df.columns = ["cnpj", "dt_comptc", "vl_quota", "captc_dia", "resg_dia", "nr_cotst"]
        df = df.fillna("")

        records = df.to_dict(orient="records")
        logger.info(f"Informe CVM {competencia}: {len(records)} registros carregados")
        return records

    except requests.RequestException as e:
        logger.error(f"Erro ao baixar informe CVM ({competencia}): {e}")
        return []
    except Exception as e:
        logger.error(f"Erro ao processar informe CVM ({competencia}): {e}")
        return []
