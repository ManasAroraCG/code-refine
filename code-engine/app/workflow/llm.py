import os
from typing import Optional
from dotenv import load_dotenv

load_dotenv()


def get_llm():
    """Load the configured LangChain ChatModel (ChatGroq, NVIDIA NIM / OpenAI), or return None if unconfigured."""
    groq_api_key = os.getenv("GROQ_API_KEY")
    if groq_api_key:
        try:
            from langchain_groq import ChatGroq
            model_name = os.getenv("GROQ_MODEL", "openai/gpt-oss-20b")
            return ChatGroq(
                groq_api_key=groq_api_key,
                model_name=model_name,
                temperature=0.2,
                timeout=120,
            )
        except Exception as e:
            print(f"Warning: Failed to initialize ChatGroq: {e}")

    # Fallback to NVIDIA NIM / OpenAI compatible endpoint
    nvidia_api_key = os.getenv("NVIDIA_API_KEY")
    if nvidia_api_key:
        try:
            from langchain_openai import ChatOpenAI
            model_name = os.getenv("NVIDIA_MODEL", "meta/llama-3.3-70b-instruct")
            base_url = os.getenv("NVIDIA_BASE_URL", "https://integrate.api.nvidia.com/v1")
            return ChatOpenAI(
                api_key=nvidia_api_key,
                base_url=base_url,
                model_name=model_name,
                temperature=0.2,
                timeout=120,
            )
        except Exception as e:
            print(f"Warning: Failed to initialize ChatOpenAI (NVIDIA NIM): {e}")

    return None
