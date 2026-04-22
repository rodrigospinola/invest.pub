"""
Job de coleta de benchmarks: CDI, Selic, Ibovespa, Dólar PTAX e IFIX.

Persiste os valores na tabela benchmarks com upsert diário.
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

from services import bcb_service, yfinance_service
from services.db import execute_command
from utils.logger import get_logger

logger = get_logger("benchmarks")



_UPSERT_SQL = """
INSERT INTO benchmarks ("Id", "Nome", "Valor", "Data", "VariacaoNoDia", "CreatedAt")
VALUES (gen_random_uuid(), %s, %s, CURRENT_DATE, 0, NOW())
ON CONFLICT ("Nome", "Data")
DO UPDATE SET "Valor" = EXCLUDED."Valor";
"""

# Mapeamento dos tickers de benchmark para nome canônico no banco
_YFINANCE_BENCHMARKS: dict[str, str] = {
    "^BVSP": "ibovespa",
    "IFIX11.SA": "ifix",
}



def _upsert_benchmark(nome: str, valor: float) -> None:
    """
    Faz upsert de um benchmark na tabela.

    Args:
        nome: Nome canônico do benchmark (ex: 'cdi', 'selic').
        valor: Valor do benchmark.
    """
    execute_command(_UPSERT_SQL, (nome, valor))
    logger.info(f"Benchmark '{nome}' persistido: {valor}")


def _collect_bcb() -> tuple[list[str], list[str]]:
    """
    Coleta CDI, Selic e Dólar PTAX via BCB SGS.

    Returns:
        Tupla (sucessos, falhas) com nomes dos benchmarks.
    """
    sucessos: list[str] = []
    falhas: list[str] = []

    coletas: list[tuple[str, object]] = [
        ("cdi", bcb_service.get_cdi_rate()),
        ("selic", bcb_service.get_selic_rate()),
        ("ptax_usd", bcb_service.get_ptax_usd()),
    ]

    for nome, valor in coletas:
        if valor is None:
            logger.warning(f"Benchmark '{nome}' (BCB): valor não disponível")
            falhas.append(nome)
        else:
            try:
                _upsert_benchmark(nome, float(valor))
                sucessos.append(nome)
            except Exception as e:
                logger.error(f"Benchmark '{nome}': erro ao persistir — {e}")
                falhas.append(nome)

    return sucessos, falhas


def _collect_yfinance() -> tuple[list[str], list[str]]:
    """
    Coleta Ibovespa e IFIX via yfinance.

    Returns:
        Tupla (sucessos, falhas) com nomes dos benchmarks.
    """
    sucessos: list[str] = []
    falhas: list[str] = []

    tickers = list(_YFINANCE_BENCHMARKS.keys())
    prices = yfinance_service.get_benchmark_prices(tickers)

    for ticker, nome in _YFINANCE_BENCHMARKS.items():
        valor = prices.get(ticker)
        if valor is None:
            logger.warning(f"Benchmark '{nome}' ({ticker}): preço não disponível no yfinance")
            falhas.append(nome)
        else:
            try:
                _upsert_benchmark(nome, float(valor))
                sucessos.append(nome)
            except Exception as e:
                logger.error(f"Benchmark '{nome}': erro ao persistir — {e}")
                falhas.append(nome)

    return sucessos, falhas


def main() -> None:
    """
    Ponto de entrada do job de coleta de benchmarks.

    Coleta CDI, Selic, PTAX, Ibovespa e IFIX.
    sys.exit(1) apenas se TODOS os benchmarks falharem.
    """
    logger.info("Iniciando job benchmarks")

    bcb_ok, bcb_fail = _collect_bcb()
    yf_ok, yf_fail = _collect_yfinance()

    total_ok = len(bcb_ok) + len(yf_ok)
    total_fail = len(bcb_fail) + len(yf_fail)

    logger.info(
        f"Resumo: {total_ok} benchmarks coletados com sucesso, {total_fail} falhas | "
        f"BCB: {bcb_ok} | yfinance: {yf_ok}"
    )

    if total_fail > 0:
        todos_falhos = bcb_fail + yf_fail
        logger.warning(f"Benchmarks com falha: {todos_falhos}")

    if total_ok == 0:
        logger.error("Todos os benchmarks falharam. Abortando com erro.")
        sys.exit(1)

    logger.info("Job benchmarks concluído")


if __name__ == "__main__":
    main()
