# vLLM Sample Generation

This directory contains training samples generated using the vLLM endpoint (port 8001).

## Sample Types

### Coding Samples (`coding.jsonl`)
Programming tasks covering various domains and languages with randomized topics:
- Web development, mobile apps, backend APIs, databases
- DevOps, ML, data processing, microservices
- Cloud infrastructure, security, testing, CI/CD
- Multiple languages: Python, JavaScript, TypeScript, Go, Java, C#, etc.

### Book Writing Samples (`book.jsonl`)
Creative writing samples with randomized genres, themes, and settings:
- Genres: sci-fi, fantasy, mystery, thriller, romance, horror, etc.
- Themes: AI, time travel, survival, space exploration, etc.
- Settings: futuristic cities, medieval kingdoms, space stations, etc.

## File Format

Each file is in JSONL format (one JSON object per line):
```json
{"prompt": "Task or creative prompt", "response": "Detailed response with code or story"}
```

## Generation

Samples are generated using `../generate_vllm_samples.py`:

```bash
# From AI/src directory
cd ..

# Generate 10 coding samples
python generate_vllm_samples.py --type coding --count 10

# Generate 20 book writing samples
python generate_vllm_samples.py --type book --count 20

# Generate both types (50 each)
python generate_vllm_samples.py --type all --count 50
```

The script automatically randomizes topics for diversity.
