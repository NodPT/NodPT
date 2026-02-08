"""
Generate fine-tuning training samples via a TensorRT-LLM endpoint.

Uses a large model (70B+) served by TensorRT-LLM to generate high-quality
JSONL training samples for each NodPT node type. Supports concurrent requests
to fully utilise TensorRT-LLM's continuous batching.

TensorRT-LLM exposes an OpenAI-compatible API at /v1/chat/completions,
which this script calls with structured prompts and JSON schema enforcement.

Usage:
    # Generate 50 director samples
    python scripts/generate_samples.py --node-type director --count 50

    # Generate 100 samples for every node type
    python scripts/generate_samples.py --node-type all --count 100

    # Custom endpoint, model, and concurrency
    python scripts/generate_samples.py --node-type agent --count 200 \
        --endpoint http://my-server:8000 \
        --model meta-llama/Llama-3.1-70B-Instruct \
        --concurrency 16 --batch-size 10

Recommended models (70B+ for high-quality sample generation):
    - meta-llama/Llama-3.1-70B-Instruct
    - meta-llama/Llama-3.3-70B-Instruct
    - Qwen/Qwen2.5-72B-Instruct
"""

import argparse
import asyncio
import json
import os
import sys
import time

import aiohttp

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
BASE_DIR = os.path.dirname(SCRIPT_DIR)
AI_SRC_DIR = os.path.dirname(BASE_DIR)
DATA_DIR = os.path.join(BASE_DIR, "data-samples")

NODE_TYPES = ["director", "manager", "supervisor", "agent"]

DEFAULT_ENDPOINT = "http://localhost:8000"
DEFAULT_MODEL = "meta-llama/Llama-3.1-70B-Instruct"
DEFAULT_CONCURRENCY = 8
DEFAULT_BATCH_SIZE = 5

# ── Node type configuration ──────────────────────────────────────────────────

NODE_CONFIG = {
    "director": {
        "instruction": (
            "You are a Director AI. Analyze the following project request "
            "and break it down into manager assignments. For each manager, "
            "provide a clear name and job description."
        ),
        "array_field": "managers",
        "item_fields": ["name", "job"],
    },
    "manager": {
        "instruction": (
            "You are a Manager AI. Analyze the following job assigned by "
            "the Director and break it down into supervisor assignments. "
            "For each supervisor, provide a clear name and job description."
        ),
        "array_field": "supervisors",
        "item_fields": ["name", "job"],
    },
    "supervisor": {
        "instruction": (
            "You are a Supervisor AI. Analyze the following job assigned by "
            "the Manager and break it down into agent assignments. For each "
            "agent, provide a clear name and job description."
        ),
        "array_field": "agents",
        "item_fields": ["name", "job"],
    },
    "agent": {
        "instruction": (
            "You are an Agent AI. Complete the following job and produce "
            "the file output. For each file, provide the filename and "
            "the full content."
        ),
        "array_field": "files",
        "item_fields": ["filename", "content"],
    },
}

# ── Seed topics for diverse input generation ─────────────────────────────────

SEED_PROJECT_IDEAS = [
    "Build a social media dashboard with analytics, scheduling, and multi-platform support.",
    "Create a hospital patient management system with appointments, records, and billing.",
    "Develop an online learning platform with courses, quizzes, progress tracking, and certificates.",
    "Build a restaurant ordering system with menu management, kitchen display, and delivery tracking.",
    "Create a personal finance app with budgeting, expense tracking, and investment portfolio.",
    "Develop a CI/CD pipeline management tool with build monitoring, deployment, and rollback.",
    "Build a real-time multiplayer game lobby with matchmaking, chat, and leaderboards.",
    "Create a document collaboration platform with version control, comments, and access control.",
    "Develop an IoT device management dashboard with telemetry, alerts, and firmware updates.",
    "Build a travel booking platform with flights, hotels, itineraries, and payment processing.",
    "Create a customer support ticketing system with SLA tracking, escalation, and knowledge base.",
    "Develop a warehouse inventory management system with barcode scanning and order fulfillment.",
    "Build a video conferencing app with screen sharing, recording, and virtual backgrounds.",
    "Create a recipe sharing platform with meal planning, grocery lists, and nutritional info.",
    "Develop an HR management system with recruitment, onboarding, payroll, and performance reviews.",
    "Build a music streaming service with playlists, recommendations, and offline playback.",
    "Create a fleet management system with GPS tracking, maintenance schedules, and driver logs.",
    "Develop a code review platform with inline comments, approvals, and CI integration.",
    "Build a weather monitoring dashboard with forecasts, alerts, and historical data visualization.",
    "Create an event management platform with ticketing, venue maps, and attendee check-in.",
    "Develop a cryptocurrency portfolio tracker with real-time prices, alerts, and tax reporting.",
    "Build a pet care app with appointment booking, health records, and medication reminders.",
    "Create a supply chain management platform with supplier portals, purchase orders, and logistics.",
    "Develop a smart home automation dashboard with device control, scenes, and energy monitoring.",
    "Build a freelancer marketplace with job posting, bidding, contracts, and invoicing.",
    "Create a fitness tracking app with workout plans, progress charts, and social challenges.",
    "Develop a news aggregation platform with personalized feeds, bookmarks, and offline reading.",
    "Build a property management system with tenant portals, maintenance requests, and rent collection.",
    "Create a language learning app with lessons, flashcards, speech recognition, and streaks.",
    "Develop a farm management system with crop planning, irrigation monitoring, and yield tracking.",
]


def load_format_schema(node_type):
    """Load the format.json schema for a node type from AI/src/<NodeType>/."""
    type_name = node_type.capitalize()
    path = os.path.join(AI_SRC_DIR, type_name, "format.json")
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def validate_sample_output(output_str, node_type):
    """Validate that a generated output string matches the node type schema."""
    config = NODE_CONFIG[node_type]
    try:
        obj = json.loads(output_str)
    except json.JSONDecodeError:
        return False, "output is not valid JSON"

    if not isinstance(obj, dict):
        return False, "output is not a JSON object"

    if "content" not in obj or not isinstance(obj["content"], str):
        return False, "missing or invalid 'content' field"

    if not obj["content"].strip():
        return False, "'content' field is empty"

    array_field = config["array_field"]
    if array_field not in obj or not isinstance(obj[array_field], list):
        return False, f"missing or invalid '{array_field}' field"

    if len(obj[array_field]) == 0:
        return False, f"'{array_field}' array is empty"

    for i, item in enumerate(obj[array_field]):
        if not isinstance(item, dict):
            return False, f"{array_field}[{i}] is not an object"
        for field in config["item_fields"]:
            if field not in item or not isinstance(item[field], str):
                return False, f"{array_field}[{i}] missing or invalid '{field}'"
            if not item[field].strip():
                return False, f"{array_field}[{i}].{field} is empty"

    return True, "valid"


def build_generation_prompt(node_type, batch_size, existing_inputs=None):
    """Build the prompt that asks the large model to generate training samples."""
    config = NODE_CONFIG[node_type]
    schema = load_format_schema(node_type)
    array_field = config["array_field"]
    item_fields = config["item_fields"]

    # Build the schema description for the output
    if node_type == "agent":
        item_desc = "each with 'filename' (string) and 'content' (string, the full file content)"
    else:
        item_desc = "each with 'name' (string) and 'job' (string)"

    # Existing inputs to avoid duplicates
    avoid_section = ""
    if existing_inputs:
        sample_list = existing_inputs[:20]  # Show up to 20 to avoid prompt bloat
        avoid_section = (
            "\n\nDo NOT reuse these existing inputs (generate completely different ones):\n"
            + "\n".join(f"- {inp}" for inp in sample_list)
        )

    node_role = {
        "director": "a Director AI that breaks down project requests into manager assignments",
        "manager": "a Manager AI that breaks down a job into supervisor assignments",
        "supervisor": "a Supervisor AI that breaks down a job into agent assignments",
        "agent": "an Agent AI that completes a job by producing source code files",
    }

    input_desc = {
        "director": "a realistic software project request (1-2 sentences describing what to build)",
        "manager": "a realistic job description assigned by a Director (specific technical domain to manage)",
        "supervisor": "a realistic job description assigned by a Manager (specific technical task area to supervise)",
        "agent": "a realistic job description assigned by a Supervisor (specific coding task to implement)",
    }

    prompt = f"""Generate exactly {batch_size} high-quality training samples for fine-tuning {node_role[node_type]}.

Each sample must be a JSON object on its own line (JSONL format) with these fields:
- "instruction": exactly "{config['instruction']}"
- "input": {input_desc[node_type]}
- "output": a JSON string containing a valid response with:
  - "content": a 1-3 sentence explanation of the plan/work
  - "{array_field}": an array of 2-5 items, {item_desc}

Rules:
1. Each input must be UNIQUE and cover different software domains (web, mobile, backend, data, DevOps, ML, etc.).
2. The "output" field must be a valid JSON STRING (escaped properly to be embedded in the JSONL line).
3. Each item in "{array_field}" must have meaningful, specific descriptions — not generic placeholders.
4. For Agent type: files must contain realistic code with proper syntax for the language used.
5. Vary the number of items in "{array_field}" between 2 and 5 across samples.
6. Inputs should be diverse: different industries, tech stacks, and complexity levels.
{avoid_section}

Output ONLY the {batch_size} JSONL lines, one per line. No markdown, no explanation, no code fences."""

    return prompt


async def call_tensorrt(session, endpoint, model, prompt, temperature=0.8):
    """Send a single request to the TensorRT-LLM OpenAI-compatible endpoint."""
    url = f"{endpoint}/v1/chat/completions"
    payload = {
        "model": model,
        "messages": [
            {
                "role": "system",
                "content": (
                    "You are a training data generator. You produce JSONL lines "
                    "for fine-tuning language models. Output ONLY valid JSONL — "
                    "one JSON object per line, no markdown, no extra text."
                ),
            },
            {"role": "user", "content": prompt},
        ],
        "temperature": temperature,
        "max_tokens": 4096,
        "stream": False,
    }

    try:
        async with session.post(
            url,
            json=payload,
            headers={"Content-Type": "application/json"},
            timeout=aiohttp.ClientTimeout(total=300),
        ) as resp:
            if resp.status != 200:
                text = await resp.text()
                return None, f"HTTP {resp.status}: {text[:200]}"
            data = await resp.json()
            content = data["choices"][0]["message"]["content"]
            return content, None
    except asyncio.TimeoutError:
        return None, "request timed out (300s)"
    except aiohttp.ClientError as e:
        return None, f"connection error: {e}"


def parse_generated_lines(raw_text, node_type):
    """Parse the raw model output into validated JSONL samples."""
    valid_samples = []
    invalid_count = 0

    for line in raw_text.strip().splitlines():
        line = line.strip()
        if not line:
            continue
        # Strip markdown code fences if present
        if line.startswith("```"):
            continue

        try:
            sample = json.loads(line)
        except json.JSONDecodeError:
            invalid_count += 1
            continue

        # Validate structure: must have instruction, input, output
        if not all(k in sample for k in ("instruction", "input", "output")):
            invalid_count += 1
            continue

        if not isinstance(sample["instruction"], str) or not sample["instruction"].strip():
            invalid_count += 1
            continue

        if not isinstance(sample["input"], str) or not sample["input"].strip():
            invalid_count += 1
            continue

        # Validate the output field is valid JSON matching the schema
        output_str = sample["output"]
        if not isinstance(output_str, str):
            # Sometimes the model returns the output as an object instead of string
            try:
                output_str = json.dumps(sample["output"], ensure_ascii=False)
                sample["output"] = output_str
            except (TypeError, ValueError):
                invalid_count += 1
                continue

        ok, reason = validate_sample_output(output_str, node_type)
        if not ok:
            invalid_count += 1
            continue

        # Normalise the instruction to the canonical one
        sample["instruction"] = NODE_CONFIG[node_type]["instruction"]

        valid_samples.append(sample)

    return valid_samples, invalid_count


async def generate_batch(session, endpoint, model, node_type, batch_size, existing_inputs):
    """Generate one batch of samples via the TensorRT-LLM endpoint."""
    prompt = build_generation_prompt(node_type, batch_size, existing_inputs)
    raw, err = await call_tensorrt(session, endpoint, model, prompt)
    if err:
        return [], 0, err
    valid, invalid = parse_generated_lines(raw, node_type)
    return valid, invalid, None


async def generate_all(args):
    """Orchestrate concurrent batch generation for the requested node types."""
    if args.node_type == "all":
        node_types = NODE_TYPES
    else:
        node_types = [args.node_type]

    os.makedirs(DATA_DIR, exist_ok=True)

    for node_type in node_types:
        print(f"\n{'='*60}")
        print(f"Generating {args.count} samples for: {node_type}")
        print(f"Endpoint: {args.endpoint}")
        print(f"Model: {args.model}")
        print(f"Concurrency: {args.concurrency}, Batch size: {args.batch_size}")
        print(f"{'='*60}")

        output_path = os.path.join(DATA_DIR, f"{node_type}.jsonl")

        # Load existing samples to avoid duplicates
        existing_inputs = set()
        if os.path.isfile(output_path):
            with open(output_path, "r", encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if line:
                        try:
                            obj = json.loads(line)
                            existing_inputs.add(obj.get("input", ""))
                        except json.JSONDecodeError:
                            pass
            print(f"Existing samples: {len(existing_inputs)}")

        collected = []
        total_invalid = 0
        start_time = time.time()

        # Calculate how many batches we need
        remaining = args.count
        semaphore = asyncio.Semaphore(args.concurrency)

        async with aiohttp.ClientSession() as session:
            while remaining > 0:
                # Determine how many concurrent batches to launch
                num_batches = min(
                    args.concurrency,
                    (remaining + args.batch_size - 1) // args.batch_size,
                )

                async def bounded_batch(batch_idx):
                    async with semaphore:
                        size = min(args.batch_size, remaining)
                        return await generate_batch(
                            session, args.endpoint, args.model,
                            node_type, size,
                            list(existing_inputs),
                        )

                # Fire concurrent requests
                tasks = [bounded_batch(i) for i in range(num_batches)]
                results = await asyncio.gather(*tasks)

                batch_new = 0
                for valid, invalid, err in results:
                    if err:
                        print(f"  Batch error: {err}")
                        continue
                    total_invalid += invalid
                    for sample in valid:
                        if sample["input"] not in existing_inputs:
                            collected.append(sample)
                            existing_inputs.add(sample["input"])
                            batch_new += 1

                remaining = args.count - len(collected)
                elapsed = time.time() - start_time
                print(
                    f"  Progress: {len(collected)}/{args.count} samples "
                    f"({total_invalid} invalid discarded) "
                    f"[{elapsed:.1f}s]"
                )

                if remaining <= 0:
                    break

                # Safety: stop if we've made too many attempts without progress
                if elapsed > 600 and len(collected) == 0:
                    print("  No samples generated after 10 minutes. Stopping.")
                    break

        # Append new samples to the JSONL file
        if collected:
            with open(output_path, "a", encoding="utf-8") as f:
                for sample in collected:
                    f.write(json.dumps(sample, ensure_ascii=False) + "\n")

            elapsed = time.time() - start_time
            print(f"\nDone: {len(collected)} new samples saved to {output_path}")
            print(f"Time: {elapsed:.1f}s | Invalid discarded: {total_invalid}")
        else:
            print(f"\nNo valid samples generated for {node_type}.")

    # Final summary
    print(f"\n{'='*60}")
    print("Generation complete. Samples saved to data-samples/")
    print(f"{'='*60}")


def main():
    parser = argparse.ArgumentParser(
        description=(
            "Generate fine-tuning training samples via a TensorRT-LLM endpoint. "
            "Uses a large model (70B+) to produce high-quality JSONL data for "
            "NodPT node types."
        ),
    )
    parser.add_argument(
        "--node-type",
        choices=NODE_TYPES + ["all"],
        required=True,
        help="Node type to generate samples for, or 'all' for every type.",
    )
    parser.add_argument(
        "--count",
        type=int,
        required=True,
        help="Number of samples to generate per node type.",
    )
    parser.add_argument(
        "--endpoint",
        default=DEFAULT_ENDPOINT,
        help=f"TensorRT-LLM server URL (default: {DEFAULT_ENDPOINT}).",
    )
    parser.add_argument(
        "--model",
        default=DEFAULT_MODEL,
        help=(
            f"Model name served by TensorRT-LLM (default: {DEFAULT_MODEL}). "
            "Recommended: 70B+ instruct models for high-quality generation."
        ),
    )
    parser.add_argument(
        "--concurrency",
        type=int,
        default=DEFAULT_CONCURRENCY,
        help=(
            f"Max concurrent requests to TensorRT-LLM (default: {DEFAULT_CONCURRENCY}). "
            "TensorRT-LLM uses continuous batching so concurrent requests are "
            "efficiently batched server-side."
        ),
    )
    parser.add_argument(
        "--batch-size",
        type=int,
        default=DEFAULT_BATCH_SIZE,
        help=f"Samples to request per API call (default: {DEFAULT_BATCH_SIZE}).",
    )
    parser.add_argument(
        "--temperature",
        type=float,
        default=0.8,
        help="Sampling temperature for generation (default: 0.8).",
    )
    parser.add_argument(
        "--append",
        action="store_true",
        default=True,
        help="Append to existing JSONL files (default: true).",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        default=False,
        help="Overwrite existing JSONL files instead of appending.",
    )
    args = parser.parse_args()

    if args.count < 1:
        print("Error: --count must be at least 1.")
        sys.exit(1)

    if args.overwrite:
        if args.node_type == "all":
            types_to_clear = NODE_TYPES
        else:
            types_to_clear = [args.node_type]
        for nt in types_to_clear:
            path = os.path.join(DATA_DIR, f"{nt}.jsonl")
            if os.path.isfile(path):
                os.remove(path)
                print(f"Cleared: {path}")

    asyncio.run(generate_all(args))


if __name__ == "__main__":
    main()
