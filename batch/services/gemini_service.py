"""Serviço de avaliação qualitativa de ativos via Gemini (Google AI Studio ou Vertex AI)."""
import json
from config import VERTEX_AI_PROJECT, VERTEX_AI_LOCATION, VERTEX_AI_MODEL
from utils.logger import get_logger

logger = get_logger(__name__)

_FALLBACK_RESPONSE = {"score": 5.0, "justificativa": "Avaliação indisponível."}

_PROMPT_TEMPLATE = """\
Você é um analista de investimentos brasileiro especializado em renda variável.
Avalie o ativo abaixo com base nos indicadores fornecidos.

Ativo: {ticker} — {nome}
Indicadores quantitativos:
{indicadores_fmt}

Avalie os seguintes aspectos qualitativos:
- Governança corporativa e histórico de gestão
- Perspectivas do setor e posicionamento competitivo
- Riscos relevantes (regulatório, setorial, financeiro)
- Momentum e tendências recentes

Responda EXCLUSIVAMENTE em JSON válido, sem explicações externas, no formato:
{{"score": <número de 0 a 10>, "justificativa": "<duas frases em português>"}}
"""


def _format_indicadores(indicadores: dict) -> str:
    """Formata o dicionário de indicadores como texto legível."""
    linhas = []
    for chave, valor in indicadores.items():
        if valor is not None:
            linhas.append(f"  {chave}: {valor}")
    return "\n".join(linhas) if linhas else "  (sem indicadores disponíveis)"


def _parse_response(text: str) -> dict:
    """
    Extrai JSON da resposta do modelo.

    Returns:
        Dict com 'score' (float) e 'justificativa' (str).
        Retorna _FALLBACK_RESPONSE se o parse falhar.
    """
    try:
        # Tenta parsear direto
        data = json.loads(text.strip())
        score = float(data.get("score", 5.0))
        score = max(0.0, min(10.0, score))
        justificativa = str(data.get("justificativa", "Avaliação indisponível."))
        return {"score": score, "justificativa": justificativa}
    except (json.JSONDecodeError, ValueError, TypeError):
        # Tenta extrair bloco JSON de dentro do texto
        try:
            start = text.index("{")
            end = text.rindex("}") + 1
            data = json.loads(text[start:end])
            score = float(data.get("score", 5.0))
            score = max(0.0, min(10.0, score))
            justificativa = str(data.get("justificativa", "Avaliação indisponível."))
            return {"score": score, "justificativa": justificativa}
        except Exception:
            logger.warning(f"Não foi possível parsear resposta do Gemini: {text[:200]}")
            return dict(_FALLBACK_RESPONSE)


def get_qualitative_score(ticker: str, nome: str, indicadores: dict) -> dict:
    """
    Usa Gemini para avaliar qualitativamente um ativo.

    Autenticação via Vertex AI com Application Default Credentials (ADC).
    Requer VERTEX_AI_PROJECT configurado.

    Args:
        ticker: Código do ativo (ex: PETR4.SA, HGLG11.SA).
        nome: Nome da empresa ou fundo.
        indicadores: Dicionário com indicadores quantitativos já coletados.

    Returns:
        Dict com 'score' (float 0-10) e 'justificativa' (str em português).
        Retorna {"score": 5.0, "justificativa": "Avaliação indisponível."} em caso de erro.
    """
    prompt = _PROMPT_TEMPLATE.format(
        ticker=ticker,
        nome=nome,
        indicadores_fmt=_format_indicadores(indicadores),
    )

    try:
        from google import genai  # type: ignore

        if not VERTEX_AI_PROJECT:
            logger.error("VERTEX_AI_PROJECT não configurado. Defina no .env.")
            return dict(_FALLBACK_RESPONSE)

        logger.info(f"Avaliação qualitativa de {ticker} via Vertex AI")
        client = genai.Client(
            vertexai=True,
            project=VERTEX_AI_PROJECT,
            location=VERTEX_AI_LOCATION,
        )

        response = client.models.generate_content(
            model=VERTEX_AI_MODEL,
            contents=prompt,
        )
        return _parse_response(response.text)

    except ImportError:
        logger.error("google-genai não instalado. Execute: pip install -r requirements.txt")
        return dict(_FALLBACK_RESPONSE)
    except Exception as e:
        logger.error(f"Erro na avaliação qualitativa de {ticker}: {e}")
        return dict(_FALLBACK_RESPONSE)
