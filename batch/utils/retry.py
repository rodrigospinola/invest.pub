"""Utilitários de retry com backoff exponencial."""
import time
import functools
from typing import Callable, TypeVar, Any
from utils.logger import get_logger

logger = get_logger(__name__)
T = TypeVar("T")


def retry(max_attempts: int = 3, delay_seconds: float = 5.0, exceptions: tuple = (Exception,)):
    """
    Decorator de retry que re-lança a exceção após esgotar as tentativas.

    Diferença de with_retry: lança exceção no final em vez de retornar None.
    Útil para jobs onde a falha deve propagar.
    """
    def decorator(func: Callable[..., T]) -> Callable[..., T]:
        @functools.wraps(func)
        def wrapper(*args: Any, **kwargs: Any) -> T:
            last_exception: Exception | None = None
            for attempt in range(1, max_attempts + 1):
                try:
                    return func(*args, **kwargs)
                except exceptions as e:
                    last_exception = e
                    if attempt < max_attempts:
                        logger.warning(
                            f"{func.__name__} falhou (tentativa {attempt}/{max_attempts}): {e}. "
                            f"Retentando em {delay_seconds}s..."
                        )
                        time.sleep(delay_seconds)
                    else:
                        logger.error(f"{func.__name__} falhou após {max_attempts} tentativas: {e}")
            raise last_exception  # type: ignore
        return wrapper
    return decorator


def with_retry(max_attempts: int = 3, delay_seconds: float = 2.0):
    """
    Decorator de retry com backoff exponencial que retorna None após esgotar tentativas.

    Diferença de retry: engole a exceção final e retorna None.
    Útil para serviços de coleta de dados onde falhas individuais são aceitáveis.
    """
    def decorator(func: Callable[..., T]) -> Callable[..., T | None]:
        @functools.wraps(func)
        def wrapper(*args: Any, **kwargs: Any) -> T | None:
            for attempt in range(1, max_attempts + 1):
                try:
                    return func(*args, **kwargs)
                except Exception as e:
                    if attempt == max_attempts:
                        logger.error(
                            f"{func.__name__} falhou após {max_attempts} tentativas: {e}"
                        )
                        return None
                    wait = delay_seconds * (2 ** (attempt - 1))
                    logger.warning(
                        f"{func.__name__} tentativa {attempt} falhou: {e}. "
                        f"Aguardando {wait}s..."
                    )
                    time.sleep(wait)
            return None
        return wrapper
    return decorator
