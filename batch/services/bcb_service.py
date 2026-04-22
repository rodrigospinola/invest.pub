"""Serviço de consulta à API do Banco Central do Brasil (SGS)."""
import requests
from utils.logger import get_logger

logger = get_logger(__name__)

_BCB_SGS_BASE = "https://api.bcb.gov.br/dados/serie/bcdata.sgs.{serie}/dados/ultimos/1?formato=json"
_TIMEOUT = 10


def _get_sgs_value(serie: int, nome: str) -> float | None:
    """
    Busca o último valor de uma série do SGS/BCB.

    Args:
        serie: Código da série temporal no SGS.
        nome: Nome descritivo para logging.

    Returns:
        Valor como float ou None em caso de erro.
    """
    url = _BCB_SGS_BASE.format(serie=serie)
    try:
        response = requests.get(url, timeout=_TIMEOUT)
        response.raise_for_status()
        data = response.json()
        if not data:
            logger.warning(f"SGS série {serie} ({nome}) retornou lista vazia")
            return None
        return float(data[0]["valor"].replace(",", "."))
    except requests.RequestException as e:
        logger.error(f"Erro ao buscar SGS série {serie} ({nome}): {e}")
        return None
    except (KeyError, ValueError, IndexError) as e:
        logger.error(f"Erro ao parsear SGS série {serie} ({nome}): {e}")
        return None


def get_cdi_rate() -> float | None:
    """
    CDI anual atual via BCB SGS série 4389.

    Returns:
        Taxa CDI anual em percentual (ex: 10.65) ou None em caso de erro.
    """
    return _get_sgs_value(4389, "CDI anual")


def get_selic_rate() -> float | None:
    """
    Taxa Selic Meta via BCB SGS série 432.

    Returns:
        Taxa Selic em percentual (ex: 10.75) ou None em caso de erro.
    """
    return _get_sgs_value(432, "Selic Meta")


def get_ptax_usd() -> float | None:
    """
    Dólar PTAX (taxa de câmbio BRL/USD) via BCB SGS série 1.

    Returns:
        Cotação do dólar PTAX em reais (ex: 5.12) ou None em caso de erro.
    """
    return _get_sgs_value(1, "PTAX USD")
