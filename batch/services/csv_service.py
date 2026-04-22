"""
Serviço de leitura de dados de ativos via arquivo CSV.

Suporta exportações do Fundamentus, Status Invest e similares.
O arquivo deve ser colocado em /app/data/ (mapeado para batch/data/ no host).

Formato esperado das colunas:
  Empresa, Ticker, Setor, Valor de Mercado, P/L, P/VP, LPA, ROE, ROIC,
  Margem Líquida, Margem Bruta, Margem EBITDA, CAGR de Lucro,
  CAGR de Receita, Dívida Líq. / EBITDA, Liquidez Corrente
"""
import csv
import os
from pathlib import Path
from utils.logger import get_logger

logger = get_logger(__name__)

# Diretório onde os CSVs são depositados
_DATA_DIR = Path(os.environ.get("CSV_DATA_DIR", "/app/data"))

# Setores brasileiros para identificar ações BR no CSV
_SETORES_BR = {
    "Financeiro", "Energia", "Indústria", "Materiais", "Utilidade",
    "Saúde", "Consumo", "Tecnologia", "Comunicação", "Imobiliário",
    "Imobili", "Serviços",
}

# Mapeamento de colunas CSV → chaves internas
_COLUMN_MAP = {
    "Empresa":                "empresa",
    "Ticker":                 "ticker",
    "Setor":                  "setor",
    "Valor de Mercado":       "valor_mercado",
    "P/L":                    "pl",
    "P/VP":                   "p_vpa",
    "LPA":                    "lpa",
    "ROE":                    "roe",
    "ROIC":                   "roic",
    "Margem Líquida":         "margem_liquida",
    "Margem Bruta":           "margem_bruta",
    "Margem EBITDA":          "margem_ebitda",
    "CAGR de Lucro":          "cagr_lucro",
    "CAGR de Receita":        "cagr_receita",
    "Dívida Líq. / EBITDA":   "div_ebitda",
    "Liquidez Corrente":      "liquidez_corrente",
}


def _find_latest_csv() -> Path | None:
    """
    Localiza o CSV mais recente no diretório de dados.

    Procura por 'ativos.csv' primeiro (nome fixo). Se não encontrar,
    pega o .csv modificado mais recentemente.

    Returns:
        Path do arquivo encontrado, ou None se nenhum CSV disponível.
    """
    if not _DATA_DIR.exists():
        logger.error(f"Diretório de dados não encontrado: {_DATA_DIR}")
        return None

    fixed = _DATA_DIR / "ativos.csv"
    if fixed.exists():
        logger.info(f"CSV encontrado: {fixed}")
        return fixed

    csvs = sorted(_DATA_DIR.glob("*.csv"), key=lambda p: p.stat().st_mtime, reverse=True)
    if csvs:
        logger.info(f"CSV encontrado (mais recente): {csvs[0]}")
        return csvs[0]

    logger.error(f"Nenhum arquivo .csv encontrado em {_DATA_DIR}")
    return None


def _parse_float(value: str) -> float | None:
    """Converte string para float, retornando None se inválido."""
    if not value or value.strip() in ("", "-", "N/A"):
        return None
    try:
        return float(value.replace(",", ".").strip())
    except ValueError:
        return None


def _is_br_stock(row: dict) -> bool:
    """Verifica se um registro é uma ação brasileira pelo setor."""
    setor = row.get("setor", "")
    return any(s in setor for s in _SETORES_BR)


def _normalize_row(raw: dict) -> dict:
    """
    Converte uma linha do CSV para o formato interno usado pelo scoring.

    Args:
        raw: Dicionário com chaves já mapeadas pelo _COLUMN_MAP.

    Returns:
        Dicionário normalizado com valores numéricos.
    """
    return {
        "ticker":           raw.get("ticker", "").strip(),
        "empresa":          raw.get("empresa", "").strip(),
        "setor":            raw.get("setor", "").strip(),
        "valor_mercado":    _parse_float(raw.get("valor_mercado", "")),
        "pl":               _parse_float(raw.get("pl", "")),
        "p_vpa":            _parse_float(raw.get("p_vpa", "")),
        "lpa":              _parse_float(raw.get("lpa", "")),
        "roe":              _parse_float(raw.get("roe", "")),
        "roic":             _parse_float(raw.get("roic", "")),
        "margem_liquida":   _parse_float(raw.get("margem_liquida", "")),
        "margem_bruta":     _parse_float(raw.get("margem_bruta", "")),
        "margem_ebitda":    _parse_float(raw.get("margem_ebitda", "")),
        "cagr_lucro":       _parse_float(raw.get("cagr_lucro", "")),
        "cagr_receita":     _parse_float(raw.get("cagr_receita", "")),
        "div_ebitda":       _parse_float(raw.get("div_ebitda", "")),
        "liquidez_corrente":_parse_float(raw.get("liquidez_corrente", "")),
    }


def load_acoes_br() -> list[dict]:
    """
    Carrega todas as ações brasileiras do CSV mais recente.

    Returns:
        Lista de dicts normalizados com indicadores fundamentalistas.
        Retorna lista vazia em caso de erro.
    """
    csv_path = _find_latest_csv()
    if not csv_path:
        return []

    acoes: list[dict] = []
    try:
        with open(csv_path, encoding="utf-8-sig") as f:
            reader = csv.DictReader(f)

            # Mapear nomes das colunas
            fieldnames = reader.fieldnames or []
            col_map = {col: _COLUMN_MAP[col] for col in fieldnames if col in _COLUMN_MAP}
            missing = [col for col in _COLUMN_MAP if col not in fieldnames]
            if missing:
                logger.warning(f"Colunas não encontradas no CSV: {missing}")

            for raw_row in reader:
                # Renomear colunas
                row = {col_map.get(k, k): v for k, v in raw_row.items()}

                if not _is_br_stock(row):
                    continue

                ticker = row.get("ticker", "").strip()
                if not ticker:
                    continue

                normalized = _normalize_row(row)
                acoes.append(normalized)

        logger.info(f"CSV carregado: {len(acoes)} ações brasileiras")
        return acoes

    except Exception as e:
        logger.error(f"Erro ao carregar CSV de ativos: {e}")
        return []
