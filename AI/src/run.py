import argparse
import json
import os
import sys

import requests

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
NODE_TYPES = ["Director", "Manager", "Supervisor", "Agent"]
DEFAULT_OLLAMA_URL = "http://localhost:11434"
DEFAULT_MODEL = "llama3.1:8b"


def load_format(node_type):
    path = os.path.join(BASE_DIR, node_type, "format.json")
    with open(path, "r") as f:
        return json.load(f)


def list_prompts(node_type):
    prompts_dir = os.path.join(BASE_DIR, node_type, "prompts")
    if not os.path.isdir(prompts_dir):
        return []
    return [f for f in os.listdir(prompts_dir) if f.endswith(".txt")]


def load_prompt(node_type, prompt_name):
    path = os.path.join(BASE_DIR, node_type, "prompts", prompt_name)
    with open(path, "r") as f:
        return f.read().strip()


def send_request(ollama_url, model, prompt, fmt, stream=False):
    url = f"{ollama_url}/api/generate"
    payload = {
        "model": model,
        "prompt": prompt,
        "stream": stream,
        "format": fmt,
    }
    response = requests.post(
        url,
        headers={"Content-Type": "application/json"},
        json=payload,
    )
    response.raise_for_status()
    return response.json()


def main():
    parser = argparse.ArgumentParser(
        description="Send structured requests to Ollama using NodPT node type formats."
    )
    parser.add_argument(
        "node_type",
        choices=[t.lower() for t in NODE_TYPES],
        help="Node type to use (director, manager, supervisor, agent).",
    )
    parser.add_argument(
        "--prompt",
        default=None,
        help="Prompt filename from the node type's prompts/ folder. "
        "Lists available prompts if omitted.",
    )
    parser.add_argument(
        "--prompt-text",
        default=None,
        help="Use a custom prompt string instead of a file.",
    )
    parser.add_argument(
        "--model",
        default=DEFAULT_MODEL,
        help=f"Ollama model to use (default: {DEFAULT_MODEL}).",
    )
    parser.add_argument(
        "--ollama-url",
        default=DEFAULT_OLLAMA_URL,
        help=f"Ollama server URL (default: {DEFAULT_OLLAMA_URL}).",
    )
    parser.add_argument(
        "--stream",
        action="store_true",
        help="Enable streaming mode.",
    )
    args = parser.parse_args()

    node_type = args.node_type.capitalize()

    # Load the format schema for the selected node type
    fmt = load_format(node_type)
    print(f"Loaded format for: {node_type}")

    # Determine prompt
    if args.prompt_text:
        prompt = args.prompt_text
    elif args.prompt:
        prompt = load_prompt(node_type, args.prompt)
    else:
        # List available prompts and exit
        prompts = list_prompts(node_type)
        if not prompts:
            print(f"No prompts found in {node_type}/prompts/")
            sys.exit(1)
        print(f"Available prompts for {node_type}:")
        for p in prompts:
            print(f"  - {p}")
        print(f"\nUsage: python run.py {args.node_type} --prompt <filename>")
        sys.exit(0)

    print(f"Model: {args.model}")
    print(f"Prompt: {prompt[:100]}{'...' if len(prompt) > 100 else ''}")
    print(f"Sending request to {args.ollama_url}...")

    try:
        result = send_request(args.ollama_url, args.model, prompt, fmt, args.stream)
        response_text = result.get("response", "")
        try:
            parsed = json.loads(response_text)
            print("\nResponse (parsed JSON):")
            print(json.dumps(parsed, indent=2))
        except json.JSONDecodeError:
            print("\nResponse (raw):")
            print(response_text)
    except requests.exceptions.ConnectionError:
        print(f"\nError: Could not connect to Ollama at {args.ollama_url}")
        print("Make sure Ollama is running: docker compose up -d")
        sys.exit(1)
    except requests.exceptions.HTTPError as e:
        print(f"\nHTTP Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
