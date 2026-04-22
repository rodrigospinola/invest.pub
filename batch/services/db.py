"""Serviço de conexão e execução de queries PostgreSQL."""
import contextlib
from typing import Generator
import psycopg2
import psycopg2.extras
from psycopg2.extensions import connection as PgConnection, cursor as PgCursor
from config import DATABASE_URL
from utils.logger import get_logger

logger = get_logger(__name__)

psycopg2.extras.register_uuid()


def get_connection() -> PgConnection:
    """Retorna uma conexão psycopg2 usando DATABASE_URL do config."""
    return psycopg2.connect(DATABASE_URL)


@contextlib.contextmanager
def get_cursor() -> Generator[PgCursor, None, None]:
    """Context manager que abre uma conexão, expõe um cursor e fecha ao final."""
    conn = get_connection()
    try:
        with conn:
            with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
                yield cur
    finally:
        conn.close()


def execute_query(sql: str, params: tuple = ()) -> list[dict]:
    """
    Executa um SELECT e retorna lista de dicts.

    Args:
        sql: Query SQL com placeholders %s.
        params: Tupla de parâmetros para o placeholder.

    Returns:
        Lista de dicionários com as linhas retornadas.
    """
    with get_cursor() as cur:
        cur.execute(sql, params)
        rows = cur.fetchall()
        return [dict(row) for row in rows]


def execute_command(sql: str, params: tuple = ()) -> None:
    """
    Executa um INSERT, UPDATE ou DELETE.

    Args:
        sql: Comando SQL com placeholders %s.
        params: Tupla de parâmetros para o placeholder.
    """
    with get_cursor() as cur:
        cur.execute(sql, params)
