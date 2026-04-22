"""
Serviço de carregamento de dados de FIIs via CSV.

Lê o arquivo exportado da planilha "Rendimentos FIIs - Clube do Valor"
disponível em batch/data/fiis.csv.

Estrutura do CSV (sem cabeçalho, colunas por índice):
  0  Ticker
  1  Segmento
  2  Preço Atual          (ex: "R$ 72,01")
  3  Liquidez Diária      (cotas negociadas/dia — multiplica por preço para obter R$)
  4  Último Dividendo     (ex: "R$ 0,55")
  5  DY Mensal            (%)
  6  DY 3M                (%)
  7  DY 6M                (%)
  8  DY 12M               (%) ← principal
  9  DY Médio 3M mensal   (%)
  10 DY Médio 6M mensal   (%)
  11 DY Médio 12M mensal  (%)
  12 DY Anualizado 12M    (%)
  13 Vacância             (%)
  14 DY Corrente          (%)
  15 Valorização 12M      (%)
  16 Patrimônio Líquido   (ex: "R$ 1.121.743.483,26")
  17 VP por Cota          (ex: "R$ 91,92")
  18 P/VP                 (ex: "0,78")
  19-25  Outros indicadores
"""
import csv
from pathlib import Path
from utils.logger import get_logger

logger = get_logger(__name__)

_DATA_DIR = Path("/app/data")
_FALLBACK_DIR = Path(__file__).resolve().parent.parent / "data"

# Mínimo de liquidez diária em cotas para considerar o FII negociável
_MIN_LIQUIDEZ_COTAS = 100


def _find_fiis_csv() -> Path | None:
    """Localiza o arquivo fiis.csv no diretório de dados."""
    for directory in (_DATA_DIR, _FALLBACK_DIR):
        candidate = directory / "fiis.csv"
        if candidate.exists():
            return candidate
        # Aceita qualquer arquivo com "fii" no nome como fallback
        matches = sorted(directory.glob("*fii*.csv"), key=lambda p: p.stat().st_mtime, reverse=True)
        if matches:
            logger.info(f"fii_csv_service: usando arquivo alternativo {matches[0].name}")
            return matches[0]
    return None


def _parse_brl(value: str) -> float | None:
    """
    Converte string monetária brasileira para float.
    Ex: "R$ 1.121.743.483,26" → 1121743483.26
        "R$ 91,92"           → 91.92
    """
    if not value or value.strip() in ("N/A", "", "-"):
        return None
    clean = (
        value.replace("R$", "")
             .replace(" ", "")
             .replace(".", "")
             .replace(",", ".")
             .strip()
    )
    try:
        return float(clean)
    except (ValueError, TypeError):
        return None


def _parse_pct(value: str) -> float | None:
    """
    Converte string percentual brasileira para float decimal.
    Ex: "9,44%" → 0.0944
        "-5,04%" → -0.0504
    """
    if not value or value.strip() in ("N/A", "", "-"):
        return None
    clean = value.replace("%", "").replace(",", ".").strip()
    try:
        return float(clean) / 100
    except (ValueError, TypeError):
        return None


def _parse_float(value: str) -> float | None:
    """Converte string decimal brasileira para float. Ex: "0,78" → 0.78"""
    if not value or value.strip() in ("N/A", "", "-"):
        return None
    try:
        return float(value.replace(",", ".").strip())
    except (ValueError, TypeError):
        return None


def _normalize_row(row: list[str]) -> dict | None:
    """
    Normaliza uma linha do CSV para o dict de indicadores do FII.

    Retorna None se o ticker estiver vazio ou o FII não tiver dados mínimos.
    """
    if len(row) < 19:
        return None

    ticker = row[0].strip().upper()
    if not ticker or ticker == "LISTA DE FIIS":
        return None

    # Preço
    price = _parse_brl(row[2])

    # Liquidez diária em R$ (cotas × preço)
    liquidez_cotas = _parse_float(row[3])
    volume_rs: float | None = None
    if liquidez_cotas and price:
        volume_rs = liquidez_cotas * price

    # Dividendos e DY
    ultimo_dividendo = _parse_brl(row[4])
    dy_12m = _parse_pct(row[8])        # DY 12M — principal para scoring
    dy_med_12m = _parse_pct(row[11])   # DY médio mensal 12M
    dy_anual = _parse_pct(row[12])     # DY anualizado

    # Vacância real (disponível no CSV — melhor que estimativa)
    vacancia = _parse_pct(row[13])
    # Vacância negativa = erro de dados → tratar como 0
    if vacancia is not None and vacancia < 0:
        vacancia = 0.0

    # Patrimônio e valor patrimonial
    patrimonio_liq = _parse_brl(row[16])
    vp_cota = _parse_brl(row[17])

    # P/VP
    pvp = _parse_float(row[18])
    # Se não veio direto, calcula de preço / VP
    if pvp is None and price and vp_cota and vp_cota > 0:
        pvp = round(price / vp_cota, 4)

    # Segmento / tipo
    segmento = row[1].strip() if len(row) > 1 else ""

    return {
        "ticker":            ticker,
        "segmento":          segmento,
        "price":             price,
        "dy":                dy_12m,         # decimal (0.0944 = 9,44%)
        "dy_med_12m":        dy_med_12m,
        "dy_anual":          dy_anual,
        "p_vpa":             pvp,
        "volume":            volume_rs,      # R$/dia
        "ultimo_dividendo":  ultimo_dividendo,
        "vacancia":          vacancia,       # decimal (0.05 = 5%)
        "patrimonio_liq":    patrimonio_liq,
        "vp_cota":           vp_cota,
    }


def load_fiis() -> list[dict]:
    """
    Carrega e normaliza todos os FIIs do CSV.

    Filtra linhas com ticker vazio, DY e P/VP ambos ausentes,
    ou liquidez abaixo do mínimo de negociabilidade.

    Returns:
        Lista de dicts com indicadores normalizados. Nunca lança exceção.
    """
    csv_path = _find_fiis_csv()
    if csv_path is None:
        logger.error("fii_csv_service: arquivo fiis.csv não encontrado em /app/data/")
        return []

    logger.info(f"fii_csv_service: carregando {csv_path}")
    result: list[dict] = []
    skipped = 0

    try:
        with open(csv_path, encoding="utf-8", errors="replace") as f:
            reader = csv.reader(f)
            for row in reader:
                normalized = _normalize_row(row)
                if normalized is None:
                    skipped += 1
                    continue

                # Filtra FIIs sem DY e sem P/VP (sem dados úteis)
                if normalized["dy"] is None and normalized["p_vpa"] is None:
                    skipped += 1
                    continue

                # Filtra FIIs com liquidez muito baixa (ilíquidos)
                liq = normalized.get("volume")
                if liq is not None and liq < _MIN_LIQUIDEZ_COTAS * (normalized.get("price") or 1):
                    skipped += 1
                    continue

                result.append(normalized)

    except Exception as e:
        logger.error(f"fii_csv_service: erro ao ler CSV: {e}")
        return []

    logger.info(
        f"fii_csv_service: {len(result)} FIIs carregados, {skipped} ignorados"
    )
    return result
