"""Wrapper yfinance com retry e fallback para coleta de dados de ativos."""
import time
import yfinance as yf
from utils.retry import with_retry
from utils.logger import get_logger

logger = get_logger(__name__)

# Delay entre chamadas para evitar 429 do Yahoo Finance
_RATE_LIMIT_DELAY = 10  # segundos


@with_retry(max_attempts=3, delay_seconds=5.0)
def get_stock_info(ticker: str) -> dict | None:
    """
    Busca dados fundamentalistas de uma ação via yfinance.

    Args:
        ticker: Ticker da ação. Para ações brasileiras deve ter sufixo .SA (ex: PETR4.SA).

    Returns:
        Dicionário com: price, pl, roe, dy, market_cap, volume, beta.
        Retorna None em caso de falha.
    """
    time.sleep(_RATE_LIMIT_DELAY)
    t = yf.Ticker(ticker)
    info = t.info
    if not info or info.get("regularMarketPrice") is None:
        logger.warning(f"Dados insuficientes para {ticker}")
        return None

    return {
        "ticker": ticker,
        "price": info.get("regularMarketPrice"),
        "pl": info.get("trailingPE"),
        "roe": info.get("returnOnEquity"),
        "dy": info.get("dividendYield"),
        "market_cap": info.get("marketCap"),
        "volume": info.get("regularMarketVolume"),
        "beta": info.get("beta"),
    }


@with_retry(max_attempts=3, delay_seconds=5.0)
def get_price_history(ticker: str, period: str = "1y") -> list[dict]:
    """
    Retorna histórico de preços de um ativo.

    Args:
        ticker: Ticker do ativo.
        period: Período (ex: "1y", "6mo", "3mo"). Padrão: "1y".

    Returns:
        Lista de dicts com: date, open, high, low, close, volume.
        Retorna lista vazia em caso de falha (tratado pelo with_retry como None — caller deve verificar).
    """
    t = yf.Ticker(ticker)
    hist = t.history(period=period)
    if hist.empty:
        logger.warning(f"Histórico vazio para {ticker} (period={period})")
        return []

    records = []
    for dt, row in hist.iterrows():
        records.append({
            "date": dt.strftime("%Y-%m-%d"),
            "open": row.get("Open"),
            "high": row.get("High"),
            "low": row.get("Low"),
            "close": row.get("Close"),
            "volume": row.get("Volume"),
        })
    return records


@with_retry(max_attempts=3, delay_seconds=5.0)
def get_fii_info(ticker: str) -> dict | None:
    """
    Busca dados de FII via yfinance.

    Args:
        ticker: Ticker do FII com sufixo .SA (ex: HGLG11.SA).

    Returns:
        Dicionário com: price, dy, p_vpa, volume.
        Retorna None em caso de falha.
    """
    time.sleep(_RATE_LIMIT_DELAY)
    t = yf.Ticker(ticker)
    info = t.info
    if not info or info.get("regularMarketPrice") is None:
        logger.warning(f"Dados insuficientes para FII {ticker}")
        return None

    return {
        "ticker": ticker,
        "price": info.get("regularMarketPrice"),
        "dy": info.get("dividendYield"),
        "p_vpa": info.get("priceToBook"),
        "volume": info.get("regularMarketVolume"),
    }


@with_retry(max_attempts=3, delay_seconds=2.0)
def get_benchmark_prices(tickers: list[str]) -> dict[str, float]:
    """
    Busca preços/valores atuais de benchmarks.

    Args:
        tickers: Lista de tickers de benchmark (ex: ["^BVSP", "BRL=X", "^IFIX"]).

    Returns:
        Dicionário {ticker: price}. Tickers com falha são omitidos.
    """
    result: dict[str, float] = {}
    for ticker in tickers:
        try:
            t = yf.Ticker(ticker)
            info = t.info
            price = info.get("regularMarketPrice") or info.get("previousClose")
            if price is not None:
                result[ticker] = float(price)
            else:
                logger.warning(f"Preço indisponível para benchmark {ticker}")
        except Exception as e:
            logger.warning(f"Falha ao buscar benchmark {ticker}: {e}")
    return result
