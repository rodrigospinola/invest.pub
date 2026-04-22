"""
Roda todos os jobs do batch em sequência.
Uso: python jobs/run_all.py
"""
import subprocess
import sys
from pathlib import Path

# Garante que o diretório raiz do batch está no sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from utils.logger import get_logger

logger = get_logger("run_all")

JOBS = [
    ("benchmarks",        "jobs/benchmarks.py"),
    ("market_data",       "jobs/market_data.py"),
    ("rankings",          "jobs/rankings.py"),
    ("portfolio_history", "jobs/portfolio_history.py"),
    ("alerts",            "jobs/alerts.py"),
]


def main() -> None:
    """
    Executa todos os jobs do batch em sequência.

    Um job com falha não interrompe os próximos — todos são tentados.
    sys.exit(1) ao final se qualquer job falhou.
    """
    failed: list[str] = []

    for name, path in JOBS:
        logger.info(f"Iniciando job: {name}")
        result = subprocess.run([sys.executable, path], capture_output=False)
        if result.returncode != 0:
            logger.error(f"Job {name} falhou com exit code {result.returncode}")
            failed.append(name)
        else:
            logger.info(f"Job {name} concluído com sucesso")

    if failed:
        logger.error(f"Jobs com falha: {failed}")
        sys.exit(1)

    logger.info("Todos os jobs concluídos com sucesso.")


if __name__ == "__main__":
    main()
