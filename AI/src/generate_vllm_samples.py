"""
Generate coding and book writing samples using vLLM endpoint.

This script uses a vLLM server on port 8001 to generate two types of samples:
1. Coding samples - Various programming tasks with randomized topics
2. Book writing samples - Various writing tasks with randomized topics

Usage:
    # Generate 10 coding samples
    python generate_vllm_samples.py --type coding --count 10
    
    # Generate 20 book writing samples
    python generate_vllm_samples.py --type book --count 20
    
    # Generate both types (50 each)
    python generate_vllm_samples.py --type all --count 50
    
    # Custom endpoint and model
    python generate_vllm_samples.py --type all --count 30 \
        --endpoint http://localhost:8001 \
        --model meta-llama/Llama-3.1-70B-Instruct
"""

import argparse
import asyncio
import json
import os
import random
import sys
import time

import aiohttp

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "vllm-samples")

DEFAULT_ENDPOINT = "http://localhost:8001"
DEFAULT_MODEL = "meta-llama/Llama-3.1-70B-Instruct"
DEFAULT_CONCURRENCY = 8
DEFAULT_BATCH_SIZE = 5

# Topic pools for randomization
CODING_TOPICS = [
    "web development",
    "mobile app development",
    "backend API",
    "database optimization",
    "DevOps automation",
    "machine learning",
    "data processing",
    "microservices",
    "cloud infrastructure",
    "security implementation",
    "testing framework",
    "CI/CD pipeline",
    "real-time analytics",
    "blockchain",
    "IoT systems",
    "game development",
    "desktop application",
    "CLI tools",
    "browser extensions",
    "REST API",
    "GraphQL API",
    "websocket server",
    "authentication system",
    "payment processing",
    "file processing",
    "image manipulation",
    "video streaming",
    "chatbot",
    "search engine",
    "recommendation system",
]

CODING_LANGUAGES = [
    "Python",
    "JavaScript",
    "TypeScript",
    "Go",
    "Rust",
    "Java",
    "C#",
    "Ruby",
    "PHP",
    "Swift",
    "Kotlin",
    "C++",
]

BOOK_GENRES = [
    "science fiction",
    "fantasy",
    "mystery",
    "thriller",
    "romance",
    "historical fiction",
    "horror",
    "adventure",
    "dystopian",
    "literary fiction",
    "young adult",
    "contemporary",
    "crime",
    "biography",
    "memoir",
    "self-help",
    "business",
    "technology",
    "philosophy",
    "psychology",
]

BOOK_THEMES = [
    "artificial intelligence",
    "time travel",
    "parallel universes",
    "post-apocalyptic survival",
    "space exploration",
    "ancient civilizations",
    "supernatural powers",
    "political intrigue",
    "family dynamics",
    "personal transformation",
    "social justice",
    "environmental crisis",
    "technological singularity",
    "human-robot relationships",
    "magical realism",
    "coming of age",
    "identity and belonging",
    "love and loss",
    "redemption",
    "revolution",
]

BOOK_SETTINGS = [
    "futuristic megacity",
    "medieval kingdom",
    "Victorian London",
    "modern Silicon Valley",
    "isolated space station",
    "underwater colony",
    "magical academy",
    "post-war Europe",
    "rural countryside",
    "cyberpunk metropolis",
    "ancient Rome",
    "distant planet",
    "parallel dimension",
    "small coastal town",
    "mountain monastery",
]


def get_random_coding_prompt():
    """Generate a random coding task prompt."""
    topic = random.choice(CODING_TOPICS)
    language = random.choice(CODING_LANGUAGES)
    
    templates = [
        f"Implement a {topic} system in {language}",
        f"Create a {language} library for {topic}",
        f"Build a {topic} solution using {language}",
        f"Develop a {language} application for {topic}",
        f"Design and code a {topic} component in {language}",
    ]
    
    return random.choice(templates)


def get_random_book_prompt():
    """Generate a random book writing prompt."""
    genre = random.choice(BOOK_GENRES)
    theme = random.choice(BOOK_THEMES)
    setting = random.choice(BOOK_SETTINGS)
    
    templates = [
        f"Write a {genre} story about {theme} set in a {setting}",
        f"Create a {genre} narrative exploring {theme} in a {setting}",
        f"Develop a {genre} plot centered on {theme} within a {setting}",
        f"Compose a {genre} tale featuring {theme} against the backdrop of a {setting}",
        f"Craft a {genre} storyline dealing with {theme} in a {setting}",
    ]
    
    return random.choice(templates)


def build_generation_prompt(sample_type, batch_size, existing_inputs=None):
    """Build the prompt for generating samples."""
    
    # Existing inputs to avoid duplicates
    avoid_section = ""
    if existing_inputs:
        sample_list = existing_inputs[:20]
        avoid_section = (
            "\n\nDo NOT reuse these existing prompts (generate completely different ones):\n"
            + "\n".join(f"- {inp}" for inp in sample_list)
        )
    
    if sample_type == "coding":
        prompt = f"""Generate exactly {batch_size} diverse coding task samples in JSONL format.

Each sample must be a JSON object on its own line with these fields:
- "prompt": A specific coding task request (vary programming languages and domains)
- "response": A detailed implementation including code, explanations, and best practices

Requirements:
1. Each prompt must be UNIQUE with different programming domains and languages
2. Cover various areas: web, mobile, backend, data, DevOps, ML, security, etc.
3. Include multiple programming languages: Python, JavaScript, TypeScript, Go, Java, C#, etc.
4. Responses should contain realistic, working code with proper syntax
5. Add explanations of the approach and key implementation details
6. Include error handling and edge cases where appropriate{avoid_section}

Output ONLY the {batch_size} JSONL lines, one per line. No markdown, no explanation, no code fences."""

    else:  # book writing
        prompt = f"""Generate exactly {batch_size} diverse book writing samples in JSONL format.

Each sample must be a JSON object on its own line with these fields:
- "prompt": A creative writing prompt with genre, theme, and setting
- "response": A well-crafted story excerpt or chapter (500-1000 words)

Requirements:
1. Each prompt must be UNIQUE with different genres, themes, and settings
2. Cover various genres: sci-fi, fantasy, mystery, thriller, literary fiction, etc.
3. Include vivid descriptions, character development, and engaging dialogue
4. Vary writing styles and narrative perspectives
5. Create compelling plots with conflict and resolution
6. Show rather than tell through sensory details and actions{avoid_section}

Output ONLY the {batch_size} JSONL lines, one per line. No markdown, no explanation, no code fences."""

    return prompt


async def call_vllm(session, endpoint, model, prompt, temperature=0.8):
    """Send a request to the vLLM OpenAI-compatible endpoint."""
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


def parse_generated_lines(raw_text, sample_type):
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

        # Validate structure: must have prompt and response
        if not all(k in sample for k in ("prompt", "response")):
            invalid_count += 1
            continue

        if not isinstance(sample["prompt"], str) or not sample["prompt"].strip():
            invalid_count += 1
            continue

        if not isinstance(sample["response"], str) or not sample["response"].strip():
            invalid_count += 1
            continue

        # Basic content validation
        if len(sample["response"]) < 50:  # Too short to be useful
            invalid_count += 1
            continue

        valid_samples.append(sample)

    return valid_samples, invalid_count


async def generate_batch(session, endpoint, model, sample_type, batch_size, existing_inputs, temperature=0.8):
    """Generate one batch of samples via the vLLM endpoint."""
    prompt = build_generation_prompt(sample_type, batch_size, existing_inputs)
    raw, err = await call_vllm(session, endpoint, model, prompt, temperature)
    if err:
        return [], 0, err
    valid, invalid = parse_generated_lines(raw, sample_type)
    return valid, invalid, None


async def generate_all(args):
    """Orchestrate concurrent batch generation for the requested sample types."""
    if args.type == "all":
        sample_types = ["coding", "book"]
    else:
        sample_types = [args.type]

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    for sample_type in sample_types:
        print(f"\n{'='*60}")
        print(f"Generating {args.count} {sample_type} samples")
        print(f"Endpoint: {args.endpoint}")
        print(f"Model: {args.model}")
        print(f"Concurrency: {args.concurrency}, Batch size: {args.batch_size}")
        print(f"{'='*60}")

        output_path = os.path.join(OUTPUT_DIR, f"{sample_type}.jsonl")

        # Load existing samples to avoid duplicates
        existing_inputs = set()
        if os.path.isfile(output_path):
            with open(output_path, "r", encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if line:
                        try:
                            obj = json.loads(line)
                            existing_inputs.add(obj.get("prompt", ""))
                        except json.JSONDecodeError:
                            pass
            print(f"Existing samples: {len(existing_inputs)}")

        collected = []
        total_invalid = 0
        start_time = time.time()

        remaining = args.count
        semaphore = asyncio.Semaphore(args.concurrency)

        async with aiohttp.ClientSession() as session:
            while remaining > 0:
                # Determine how many concurrent batches to launch
                num_batches = min(
                    args.concurrency,
                    (remaining + args.batch_size - 1) // args.batch_size,
                )

                async def bounded_batch(batch_size_for_task):
                    async with semaphore:
                        return await generate_batch(
                            session, args.endpoint, args.model,
                            sample_type, batch_size_for_task,
                            list(existing_inputs),
                            args.temperature,
                        )

                # Fire concurrent requests
                batch_sizes = [
                    min(args.batch_size, remaining - i * args.batch_size)
                    for i in range(num_batches)
                ]
                batch_sizes = [s for s in batch_sizes if s > 0]
                tasks = [bounded_batch(s) for s in batch_sizes]
                results = await asyncio.gather(*tasks)

                prev_count = len(collected)
                for valid, invalid, err in results:
                    if err:
                        print(f"  Batch error: {err}")
                        continue
                    total_invalid += invalid
                    for sample in valid:
                        if sample["prompt"] not in existing_inputs:
                            collected.append(sample)
                            existing_inputs.add(sample["prompt"])

                remaining = args.count - len(collected)
                elapsed = time.time() - start_time
                print(
                    f"  Progress: {len(collected)}/{args.count} samples "
                    f"({total_invalid} invalid discarded) "
                    f"[{elapsed:.1f}s]"
                )

                if remaining <= 0:
                    break

                # Stop if no progress after several iterations
                if len(collected) == prev_count:
                    print("  No new samples generated. Stopping.")
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
            print(f"\nNo valid samples generated for {sample_type}.")

    # Final summary
    print(f"\n{'='*60}")
    print(f"Generation complete. Samples saved to {OUTPUT_DIR}/")
    print(f"{'='*60}")


def main():
    parser = argparse.ArgumentParser(
        description=(
            "Generate coding and book writing samples via a vLLM endpoint. "
            "Uses a large model to produce high-quality JSONL data with "
            "randomized topics."
        ),
    )
    parser.add_argument(
        "--type",
        choices=["coding", "book", "all"],
        required=True,
        help="Sample type to generate: 'coding', 'book', or 'all' for both types.",
    )
    parser.add_argument(
        "--count",
        type=int,
        required=True,
        help="Number of samples to generate per type.",
    )
    parser.add_argument(
        "--endpoint",
        default=DEFAULT_ENDPOINT,
        help=f"vLLM server URL (default: {DEFAULT_ENDPOINT}).",
    )
    parser.add_argument(
        "--model",
        default=DEFAULT_MODEL,
        help=(
            f"Model name served by vLLM (default: {DEFAULT_MODEL}). "
            "Recommended: 70B+ instruct models for high-quality generation."
        ),
    )
    parser.add_argument(
        "--concurrency",
        type=int,
        default=DEFAULT_CONCURRENCY,
        help=(
            f"Max concurrent requests to vLLM (default: {DEFAULT_CONCURRENCY})."
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
    args = parser.parse_args()

    if args.count < 1:
        print("Error: --count must be at least 1.")
        sys.exit(1)

    asyncio.run(generate_all(args))


if __name__ == "__main__":
    main()
