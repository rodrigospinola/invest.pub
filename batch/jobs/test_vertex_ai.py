"""
Teste de conectividade com Gemini ou Claude.

Modos suportados (por prioridade):
  1. GEMINI_API_KEY definida  → Google AI Studio (mais simples, sem GCP)
  2. Modelo começa com gemini → Vertex AI com ADC
  3. Modelo começa com claude → Anthropic via Vertex AI com ADC

Uso:
    # Com API key (sem montar credenciais):
    docker compose run --rm batch python jobs/test_vertex_ai.py

    # Com ADC (Vertex AI):
    # PowerShell:
    docker compose run --rm -v "$env:APPDATA\gcloud:/root/.config/gcloud:ro" batch python jobs/test_vertex_ai.py
"""

import os
import sys

try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass

from utils.logger import get_logger

logger = get_logger("test_vertex_ai")

PERGUNTA = (
    "Responda em uma frase: você é o assistente de investimentos "
    "do Invest, uma plataforma que guia iniciantes do primeiro "
    "aporte até R$500k. O que você faz?"
)


def test_gemini_api_key(model: str, api_key: str) -> None:
    """Usa Google AI Studio com API key — sem necessidade de GCP/ADC."""
    try:
        from google import genai
    except ImportError:
        logger.error("Execute: pip install -r requirements.txt")
        sys.exit(1)

    logger.info(f"Usando Google AI Studio (API key) | modelo={model}")
    client = genai.Client(api_key=api_key)
    response = client.models.generate_content(model=model, contents=PERGUNTA)
    logger.info(f"Resposta: {response.text}")
    logger.info("Teste concluído com sucesso.")


def test_gemini_vertex(project: str, location: str, model: str) -> None:
    """Usa Vertex AI com ADC."""
    try:
        from google import genai
    except ImportError:
        logger.error("Execute: pip install -r requirements.txt")
        sys.exit(1)

    logger.info(f"Usando Vertex AI (ADC) | projeto={project} | modelo={model}")
    client = genai.Client(vertexai=True, project=project, location=location)
    response = client.models.generate_content(model=model, contents=PERGUNTA)
    logger.info(f"Resposta: {response.text}")
    logger.info("Teste concluído com sucesso.")


def test_claude(project: str, location: str, model: str) -> None:
    """Usa Claude via Anthropic Vertex AI."""
    try:
        from anthropic import AnthropicVertex
    except ImportError:
        logger.error("Execute: pip install -r requirements.txt")
        sys.exit(1)

    logger.info(f"Usando Claude via Vertex AI | projeto={project} | modelo={model}")
    client = AnthropicVertex(project_id=project, region=location)
    message = client.messages.create(
        model=model,
        max_tokens=256,
        messages=[{"role": "user", "content": PERGUNTA}],
    )
    logger.info(f"Resposta: {message.content[0].text}")
    logger.info("Teste concluído com sucesso.")


def main() -> None:
    project = os.environ.get("VERTEX_AI_PROJECT", "")
    location = os.environ.get("VERTEX_AI_LOCATION", "us-central1")
    model = os.environ.get("VERTEX_AI_MODEL", "gemini-1.5-flash")
    api_key = os.environ.get("GEMINI_API_KEY", "")

    logger.info(f"modelo={model} | região={location} | api_key={'sim' if api_key else 'não'}")

    try:
        if api_key:
            test_gemini_api_key(model, api_key)
        elif model.startswith("gemini"):
            if not project or project == "seu-projeto-gcp":
                logger.error("VERTEX_AI_PROJECT não configurado. Defina no .env ou use GEMINI_API_KEY.")
                sys.exit(1)
            test_gemini_vertex(project, location, model)
        else:
            if not project or project == "seu-projeto-gcp":
                logger.error("VERTEX_AI_PROJECT não configurado.")
                sys.exit(1)
            test_claude(project, location, model)

    except Exception as e:
        logger.error(f"Erro: {e}")
        logger.error(
            "Dicas:\n"
            "  - Gemini API key gratuita: https://aistudio.google.com/apikey\n"
            "  - Adicione no .env: GEMINI_API_KEY=sua-chave\n"
            "  - Ou configure ADC: gcloud auth application-default login"
        )
        sys.exit(1)


if __name__ == "__main__":
    main()
