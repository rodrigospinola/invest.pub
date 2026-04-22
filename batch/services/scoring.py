"""Serviço de scoring quantitativo para ações e FIIs."""
from utils.logger import get_logger

logger = get_logger(__name__)


def _clamp(value: float, min_val: float = 0.0, max_val: float = 10.0) -> float:
    """Limita um valor ao intervalo [min_val, max_val]."""
    return max(min_val, min(max_val, value))


def _score_pl(pl: float | None) -> float:
    """
    Score do P/L (Price/Earnings). Ideal entre 5 e 15.
    - Negativo ou None: 0
    - < 5: bom mas pode indicar problema (7)
    - 5–15: ótimo (10 → 7)
    - 15–25: aceitável (7 → 4)
    - > 25: caro (4 → 0)
    """
    if pl is None or pl <= 0:
        return 0.0
    if pl < 5:
        return 7.0
    if pl <= 15:
        # Interpolação linear decrescente: 10 em pl=5, 7 em pl=15
        return round(10.0 - (pl - 5) * 0.3, 2)
    if pl <= 25:
        # Interpolação linear decrescente: 7 em pl=15, 4 em pl=25
        return round(7.0 - (pl - 15) * 0.3, 2)
    return _clamp(4.0 - (pl - 25) * 0.1)


def _score_roe(roe: float | None) -> float:
    """
    Score do ROE (Return on Equity). Ideal acima de 15%.
    roe esperado como decimal (ex: 0.15 = 15%) — formato yfinance.
    """
    if roe is None or roe <= 0:
        return 0.0
    pct = roe * 100
    if pct >= 30:
        return 10.0
    if pct >= 15:
        # Interpolação linear: 5 em 15%, 10 em 30%
        return round(5.0 + (pct - 15) * (5.0 / 15), 2)
    # 0–15%: 0–5
    return _clamp(pct * (5.0 / 15))


def _score_roe_pct(roe_pct: float | None) -> float:
    """
    Score do ROE em formato percentual (ex: 15.0 = 15%) — formato CSV.
    Ideal acima de 15%.
    """
    if roe_pct is None or roe_pct <= 0:
        return 0.0
    if roe_pct >= 30:
        return 10.0
    if roe_pct >= 15:
        return round(5.0 + (roe_pct - 15) * (5.0 / 15), 2)
    return _clamp(roe_pct * (5.0 / 15))


def _score_roic(roic_pct: float | None) -> float:
    """
    Score do ROIC (Return on Invested Capital) em % — formato CSV.
    Ideal acima de 15%. Excelente acima de 25%.
    """
    if roic_pct is None or roic_pct <= 0:
        return 0.0
    if roic_pct >= 25:
        return 10.0
    if roic_pct >= 15:
        # 7 em 15%, 10 em 25%
        return round(7.0 + (roic_pct - 15) * 0.3, 2)
    if roic_pct >= 8:
        # 4 em 8%, 7 em 15%
        return round(4.0 + (roic_pct - 8) * (3.0 / 7), 2)
    return _clamp(roic_pct * (4.0 / 8))


def _score_margem_ebitda(margem_pct: float | None) -> float:
    """
    Score da Margem EBITDA em % — formato CSV.
    Ideal acima de 20%. Excelente acima de 30%.
    """
    if margem_pct is None or margem_pct <= 0:
        return 0.0
    if margem_pct >= 30:
        return 10.0
    if margem_pct >= 20:
        # 7 em 20%, 10 em 30%
        return round(7.0 + (margem_pct - 20) * 0.3, 2)
    if margem_pct >= 10:
        # 4 em 10%, 7 em 20%
        return round(4.0 + (margem_pct - 10) * 0.3, 2)
    return _clamp(margem_pct * (4.0 / 10))


def _score_cagr(cagr_pct: float | None) -> float:
    """
    Score do CAGR (Crescimento Composto Anual) em % — formato CSV.
    Crescimento consistente, ideal >= 10%. Excelente >= 20%.
    Neutro (5.0) quando não disponível.
    """
    if cagr_pct is None:
        return 5.0  # neutro quando sem histórico
    if cagr_pct <= 0:
        return 0.0
    if cagr_pct >= 20:
        return 10.0
    if cagr_pct >= 10:
        # 7 em 10%, 10 em 20%
        return round(7.0 + (cagr_pct - 10) * 0.3, 2)
    # 0–10%: 0–7
    return round(cagr_pct * 0.7, 2)


def _score_div_ebitda(div_ebitda: float | None) -> float:
    """
    Score da Dívida Líquida / EBITDA — formato CSV.
    Menor é melhor. Negativo = empresa com caixa líquido (excelente).
    - <= 0: 10 (caixa líquido)
    - 0–1.5: 10 → 7
    - 1.5–3.0: 7 → 3
    - > 3.0: 3 → 0
    Neutro (5.0) quando não disponível.
    """
    if div_ebitda is None:
        return 5.0  # neutro quando não reportado
    if div_ebitda <= 0:
        return 10.0
    if div_ebitda <= 1.5:
        return round(10.0 - div_ebitda * (3.0 / 1.5), 2)
    if div_ebitda <= 3.0:
        return round(7.0 - (div_ebitda - 1.5) * (4.0 / 1.5), 2)
    return _clamp(3.0 - (div_ebitda - 3.0) * 1.0)


def _score_dy(dy: float | None) -> float:
    """
    Score do Dividend Yield. Maior melhor; DY como decimal (ex: 0.08 = 8%).
    Usado para FIIs via yfinance.
    """
    if dy is None or dy <= 0:
        return 0.0
    pct = dy * 100
    if pct >= 12:
        return 10.0
    return _clamp(pct * (10.0 / 12))


def _score_volume(volume: float | None, threshold_low: float = 1_000_000) -> float:
    """
    Score de liquidez por volume diário.
    Escala logarítmica: volume baixo → 0, volume alto → 10.
    Usado para FIIs via yfinance.
    """
    import math
    if volume is None or volume <= 0:
        return 0.0
    if volume >= 100_000_000:
        return 10.0
    log_vol = math.log10(max(volume, 1))
    log_low = math.log10(threshold_low)
    log_high = math.log10(100_000_000)
    return _clamp((log_vol - log_low) / (log_high - log_low) * 10)


def _score_beta(beta: float | None) -> float:
    """
    Score de beta (risco de mercado). Menor = mais estável = melhor.
    - beta <= 0.5: 10
    - beta 0.5–1.0: 10 → 7
    - beta 1.0–1.5: 7 → 4
    - beta > 1.5: 4 → 0
    """
    if beta is None:
        return 5.0  # neutro quando desconhecido
    if beta <= 0.5:
        return 10.0
    if beta <= 1.0:
        return round(10.0 - (beta - 0.5) * 6.0, 2)
    if beta <= 1.5:
        return round(7.0 - (beta - 1.0) * 6.0, 2)
    return _clamp(4.0 - (beta - 1.5) * 4.0)


def _score_pvpa(p_vpa: float | None) -> float:
    """
    Score do P/VP (Preço/Valor Patrimonial). Ideal abaixo de 1.1.
    - <= 0.8: 10
    - 0.8–1.1: 10 → 7
    - 1.1–1.5: 7 → 3
    - > 1.5: 3 → 0
    """
    if p_vpa is None or p_vpa <= 0:
        return 5.0  # neutro quando desconhecido
    if p_vpa <= 0.8:
        return 10.0
    if p_vpa <= 1.1:
        return round(10.0 - (p_vpa - 0.8) * 10.0, 2)
    if p_vpa <= 1.5:
        return round(7.0 - (p_vpa - 1.1) * 10.0, 2)
    return _clamp(3.0 - (p_vpa - 1.5) * 4.0)


def score_acao_csv(info: dict) -> float:
    """
    Score quantitativo para ações via CSV (0-10).

    Utiliza os indicadores fundamentalistas exportados do Fundamentus/Status Invest,
    que são mais ricos que os dados do yfinance (inclui ROIC, margens, CAGR, Dívida/EBITDA).

    Critérios com pesos:
    - P/L:           20% (valuation — menor melhor, ideal 5–15)
    - ROE:           15% (rentabilidade sobre patrimônio, em %)
    - ROIC:          15% (retorno sobre capital investido, em %)
    - Margem EBITDA: 15% (eficiência operacional, em %)
    - CAGR Receita:  15% (crescimento consistente, em %)
    - Dívida/EBITDA: 10% (alavancagem — menor melhor)
    - P/VP:          10% (desconto sobre valor patrimonial)

    Args:
        info: Dicionário com campos do CSV: pl, roe, roic, margem_ebitda,
              cagr_receita, div_ebitda, p_vpa.

    Returns:
        Score float de 0 a 10.
    """
    s_pl    = _score_pl(info.get("pl"))
    s_roe   = _score_roe_pct(info.get("roe"))
    s_roic  = _score_roic(info.get("roic"))
    s_margi = _score_margem_ebitda(info.get("margem_ebitda"))
    s_cagr  = _score_cagr(info.get("cagr_receita"))
    s_div   = _score_div_ebitda(info.get("div_ebitda"))
    s_pvpa  = _score_pvpa(info.get("p_vpa"))

    score = (
        s_pl    * 0.20
        + s_roe   * 0.15
        + s_roic  * 0.15
        + s_margi * 0.15
        + s_cagr  * 0.15
        + s_div   * 0.10
        + s_pvpa  * 0.10
    )

    logger.info(
        f"score_acao_csv | pl={s_pl} roe={s_roe} roic={s_roic} "
        f"margem={s_margi} cagr={s_cagr} div={s_div} pvpa={s_pvpa} → {score:.4f}"
    )
    return round(_clamp(score), 4)


def score_acao(info: dict) -> float:
    """
    Score quantitativo para ações via yfinance (0-10).

    Mantido por compatibilidade. Prefira score_acao_csv para dados do CSV.

    Critérios com pesos:
    - P/L:    25% (menor melhor, ideal 5-15)
    - ROE:    25% (maior melhor, ideal >15%)
    - DY:     20% (maior melhor)
    - Volume: 15% (liquidez)
    - Beta:   15% (risco — menor = mais estável)

    Args:
        info: Dicionário com pl, roe, dy, volume, beta (formato yfinance).

    Returns:
        Score float de 0 a 10.
    """
    s_pl  = _score_pl(info.get("pl"))
    s_roe = _score_roe(info.get("roe"))
    s_dy  = _score_dy(info.get("dy"))
    s_vol = _score_volume(info.get("volume"))
    s_bet = _score_beta(info.get("beta"))

    score = (
        s_pl  * 0.25
        + s_roe * 0.25
        + s_dy  * 0.20
        + s_vol * 0.15
        + s_bet * 0.15
    )

    logger.info(
        f"score_acao | pl={s_pl} roe={s_roe} dy={s_dy} vol={s_vol} beta={s_bet} → {score:.4f}"
    )
    return round(_clamp(score), 4)


def _score_vacancia(vacancia: float | None) -> float:
    """
    Score de vacância real do FII (obtida do CSV). Menor vacância = melhor.
    - None/desconhecida: neutro (5.0)
    - 0%:     10
    - 0–5%:   10 → 7
    - 5–15%:  7 → 3
    - 15–30%: 3 → 0
    - > 30%:  0
    """
    if vacancia is None:
        return 5.0
    if vacancia <= 0:
        return 10.0
    pct = vacancia * 100
    if pct <= 5:
        return round(10.0 - pct * (3.0 / 5), 2)
    if pct <= 15:
        return round(7.0 - (pct - 5) * (4.0 / 10), 2)
    if pct <= 30:
        return round(3.0 - (pct - 15) * (3.0 / 15), 2)
    return 0.0


def score_fii_csv(info: dict) -> float:
    """
    Score quantitativo para FIIs via CSV (0-10).

    Utiliza dados reais da planilha "Rendimentos FIIs - Clube do Valor",
    incluindo vacância real — mais preciso que a estimativa via yfinance.

    Critérios com pesos:
    - DY 12M:   35% (dividend yield anual — maior melhor)
    - P/VP:     30% (preço vs. valor patrimonial — menor melhor)
    - Vacância: 20% (vacância real — menor melhor)
    - Volume:   15% (liquidez diária em R$ — maior melhor)

    Args:
        info: Dicionário com campos do CSV: dy, p_vpa, vacancia, volume.

    Returns:
        Score float de 0 a 10.
    """
    s_dy       = _score_dy(info.get("dy"))
    s_pvpa     = _score_pvpa(info.get("p_vpa"))
    s_vacancia = _score_vacancia(info.get("vacancia"))
    s_vol      = _score_volume(info.get("volume"))

    score = (
        s_dy       * 0.35
        + s_pvpa   * 0.30
        + s_vacancia * 0.20
        + s_vol    * 0.15
    )

    logger.info(
        f"score_fii_csv | dy={s_dy} pvpa={s_pvpa} "
        f"vac={s_vacancia} vol={s_vol} → {score:.4f}"
    )
    return round(_clamp(score), 4)


def score_fii(info: dict) -> float:
    """
    Score quantitativo para FIIs (0-10).

    Critérios com pesos:
    - DY:      35% (dividend yield, maior melhor)
    - P/VP:    30% (menor melhor, ideal <1.1)
    - Volume:  20% (liquidez)
    - Vacância implícita: 15% (estimada inversamente pelo DY vs referência de 8%)

    Args:
        info: Dicionário com dy, p_vpa, volume (formato yfinance).

    Returns:
        Score float de 0 a 10.
    """
    s_dy   = _score_dy(info.get("dy"))
    s_pvpa = _score_pvpa(info.get("p_vpa"))
    s_vol  = _score_volume(info.get("volume"))

    # Vacância implícita: DY muito abaixo de 8% sugere alta vacância
    dy = info.get("dy") or 0.0
    dy_pct = dy * 100
    referencia = 8.0
    s_vacancia = _clamp(min(dy_pct / referencia, 1.0) * 10.0)

    score = (
        s_dy      * 0.35
        + s_pvpa  * 0.30
        + s_vol   * 0.20
        + s_vacancia * 0.15
    )

    logger.info(
        f"score_fii | dy={s_dy} pvpa={s_pvpa} vol={s_vol} vac={s_vacancia} → {score:.4f}"
    )
    return round(_clamp(score), 4)


def score_final(score_quant: float, score_qual: float) -> float:
    """
    Score final combinando score quantitativo e qualitativo.

    Fórmula: 70% quantitativo + 30% qualitativo (conforme CLAUDE.md).

    Args:
        score_quant: Score quantitativo (0-10).
        score_qual: Score qualitativo do Gemini (0-10).

    Returns:
        Score final float de 0 a 10.
    """
    return round(score_quant * 0.7 + score_qual * 0.3, 4)
