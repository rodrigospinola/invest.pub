"""Variáveis de ambiente centralizadas para o batch."""
import os

try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass

DATABASE_URL: str = os.environ.get("DATABASE_URL", "")
VERTEX_AI_PROJECT: str = os.environ.get("VERTEX_AI_PROJECT", "")
VERTEX_AI_LOCATION: str = os.environ.get("VERTEX_AI_LOCATION", "us-central1")
VERTEX_AI_MODEL: str = os.environ.get("VERTEX_AI_MODEL", "gemini-2.5-flash")
LOG_LEVEL: str = os.environ.get("LOG_LEVEL", "info")
