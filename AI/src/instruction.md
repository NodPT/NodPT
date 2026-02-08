# AI Testing Instructions

## Overview

This project provides a Python CLI tool (`run.py`) for sending structured requests to Ollama using NodPT node type formats, along with unit tests to validate the format schemas.

### Node Type Hierarchy

```
Director → managers (name/job)
  Manager → supervisors (name/job)
    Supervisor → agents (name/job)
      Agent → files (filename/content)
```

Each node type has:
- `format.json` — Ollama JSON Schema defining the structured output format
- `prompts/` — Folder containing `.txt` prompt files

## Prerequisites

- Python 3.8+
- [Ollama](https://ollama.ai/) running locally (for sending requests)
- A pulled model (e.g. `ollama pull llama3.1:8b`)

## Setup

```bash
cd AI/src

# Install dependencies
pip install -r requirements.txt
```

## Using `run.py`

The `run.py` script sends requests to the Ollama API with a selected node type format and prompt.

### List available prompts for a node type

```bash
python run.py director
python run.py manager
python run.py supervisor
python run.py agent
```

### Send a request using a prompt file

```bash
python run.py director --prompt sample.txt
python run.py manager --prompt sample.txt --model llama3.1:8b
python run.py agent --prompt sample.txt --model mistral:7b
```

### Send a request with custom prompt text

```bash
python run.py director --prompt-text "Build a chat application with real-time messaging"
python run.py agent --prompt-text "Write a user login endpoint in Python Flask"
```

### Options

| Option          | Description                                        | Default                    |
| --------------- | -------------------------------------------------- | -------------------------- |
| `node_type`     | Node type: `director`, `manager`, `supervisor`, `agent` | (required)            |
| `--prompt`      | Prompt filename from the node type's `prompts/` folder | Lists prompts if omitted |
| `--prompt-text` | Custom prompt string (overrides `--prompt`)         | —                          |
| `--model`       | Ollama model name                                  | `llama3.1:8b`              |
| `--ollama-url`  | Ollama server URL                                  | `http://localhost:11434`   |
| `--stream`      | Enable streaming mode                              | off                        |

### Equivalent curl command

The request sent by `run.py` is equivalent to:

```bash
curl -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama3.1:8b",
    "prompt": "Your prompt text here",
    "stream": false,
    "format": <contents of format.json>
  }'
```

## Adding Custom Prompts

Add `.txt` files to any node type's `prompts/` folder:

```
AI/src/Director/prompts/my-custom-prompt.txt
AI/src/Manager/prompts/backend-planning.txt
AI/src/Agent/prompts/write-auth-controller.txt
```

Then use them:

```bash
python run.py director --prompt my-custom-prompt.txt
python run.py manager --prompt backend-planning.txt
python run.py agent --prompt write-auth-controller.txt
```

## Running Tests

The tests validate the format schemas, prompt files, node hierarchy, and request payload structure. They do **not** require Ollama to be running.

```bash
cd AI/src

# Run all tests
python -m unittest tests.test_formats -v
```

### What the tests cover (34 tests)

- **Format validation** — Each node type's `format.json` is a valid JSON Schema with correct `type`, `properties`, and `required` fields
- **Node hierarchy** — Director only has `managers`, Manager only has `supervisors`, Supervisor only has `agents`, Agent only has `files`
- **Prompts** — Each node type has a `prompts/` folder with a non-empty `sample.txt`
- **Request payload** — The assembled Ollama request payload has all required fields (`model`, `prompt`, `stream`, `format`) and serializes correctly

## Project Structure

```
AI/src/
├── run.py                      # CLI tool for sending Ollama requests
├── requirements.txt            # Python dependencies
├── instruction.md              # This file
├── tests/
│   ├── __init__.py
│   └── test_formats.py         # Unit tests (34 tests)
├── Director/
│   ├── format.json             # Ollama JSON Schema
│   ├── format.md               # Format documentation
│   └── prompts/
│       └── sample.txt          # Sample prompt
├── Manager/
│   ├── format.json
│   ├── format.md
│   └── prompts/
│       └── sample.txt
├── Supervisor/
│   ├── format.json
│   ├── format.md
│   └── prompts/
│       └── sample.txt
└── Agent/
    ├── format.json
    ├── format.md
    └── prompts/
        └── sample.txt
```
