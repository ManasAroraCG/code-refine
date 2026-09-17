import os
import re
import time
from dotenv import load_dotenv

load_dotenv()


def get_llm():
    """Load the configured LangChain ChatGroq model, or return None if unconfigured."""
    groq_api_key = os.getenv("GROQ_API_KEY")
    if groq_api_key:
        try:
            from langchain_groq import ChatGroq
            model_name = os.getenv("GROQ_MODEL", "openai/gpt-oss-120b")
            return ChatGroq(
                groq_api_key=groq_api_key,
                model_name=model_name,
                temperature=0.2,
                timeout=120,
                max_tokens=int(os.getenv("GROQ_MAX_TOKENS", "1024")),
                model_kwargs={"reasoning_effort": os.getenv("GROQ_REASONING_EFFORT", "low")},
            )
        except Exception as e:
            print(f"Warning: Failed to initialize ChatGroq: {e}")

    return None


def invoke_llm(prompt: str, purpose: str):
    """Invoke Groq with small retry handling for transient empty or rate-limited responses."""
    llm = get_llm()
    if not llm:
        return None

    max_attempts = int(os.getenv("GROQ_MAX_ATTEMPTS", "2"))
    last_error = None

    for attempt in range(1, max_attempts + 1):
        try:
            response = llm.invoke(prompt)
            content = response.content if hasattr(response, "content") else str(response)
            if str(content).strip():
                return response
            last_error = ValueError(f"Groq returned empty content for {purpose}")
        except Exception as exc:
            last_error = exc
            message = str(exc)
            wait_match = re.search(r"try again in ([0-9.]+)s", message, re.IGNORECASE)
            if wait_match and attempt < max_attempts:
                time.sleep(min(float(wait_match.group(1)) + 1, 20))
                continue

        if attempt < max_attempts:
            time.sleep(1)

    if last_error:
        raise last_error
    return None
