"""
NodPT Fine-Tuning Script using Unsloth with FP4 Precision.

Loads training data from data-samples/, fine-tunes a base model on NodPT node-type
tasks, saves checkpoints, and writes final weights to the output directory.

Usage:
    python scripts/finetune.py --node-type director
    python scripts/finetune.py --node-type all
    python scripts/finetune.py --node-type agent --base-model unsloth/Llama-3.1-8B-Instruct-bnb-4bit --epochs 5
"""

import argparse
import json
import os
import sys

from datasets import Dataset
from trl import SFTTrainer
from transformers import TrainingArguments
from unsloth import FastLanguageModel

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
BASE_DIR = os.path.dirname(SCRIPT_DIR)
DATA_DIR = os.path.join(BASE_DIR, "data-samples")
OUTPUT_BASE = os.path.join(BASE_DIR, "output")

NODE_TYPES = ["director", "manager", "supervisor", "agent"]

DEFAULT_MODEL = "unsloth/Llama-3.1-8B-Instruct-bnb-4bit"
MAX_SEQ_LENGTH = 2048
LOAD_IN_4BIT = True  # FP4 precision


def load_jsonl(filepath):
    """Load a JSONL file and return a list of dicts."""
    records = []
    with open(filepath, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if line:
                records.append(json.loads(line))
    return records


def format_prompt(sample):
    """Convert a data sample to an Alpaca-style prompt string for SFT."""
    instruction = sample.get("instruction", "")
    inp = sample.get("input", "")
    output = sample.get("output", "")
    return (
        "### Instruction:\n" + instruction + "\n\n"
        "### Input:\n" + inp + "\n\n"
        "### Response:\n" + output
    )


def prepare_dataset(node_types):
    """Load and merge JSONL data for the requested node types."""
    all_records = []
    for nt in node_types:
        path = os.path.join(DATA_DIR, f"{nt}.jsonl")
        if not os.path.isfile(path):
            print(f"Warning: data file not found for '{nt}': {path}")
            continue
        records = load_jsonl(path)
        print(f"Loaded {len(records)} samples from {nt}.jsonl")
        all_records.extend(records)

    if not all_records:
        print("Error: no training data loaded.")
        sys.exit(1)

    texts = [format_prompt(r) for r in all_records]
    return Dataset.from_dict({"text": texts})


def run_finetune(args):
    """Execute the fine-tuning loop."""
    # Determine which node types to train on
    if args.node_type == "all":
        node_types = NODE_TYPES
    else:
        node_types = [args.node_type]

    print(f"Node types: {node_types}")
    print(f"Base model: {args.base_model}")
    print(f"Epochs: {args.epochs}")
    print(f"FP4 (4-bit quantisation): {LOAD_IN_4BIT}")

    # ── 1. Load base model with FP4 quantisation ──
    model, tokenizer = FastLanguageModel.from_pretrained(
        model_name=args.base_model,
        max_seq_length=MAX_SEQ_LENGTH,
        load_in_4bit=LOAD_IN_4BIT,
    )

    # ── 2. Apply LoRA adapters ──
    model = FastLanguageModel.get_peft_model(
        model,
        r=16,
        target_modules=[
            "q_proj", "k_proj", "v_proj",
            "o_proj", "gate_proj", "up_proj", "down_proj",
        ],
        lora_alpha=16,
        lora_dropout=0,
        bias="none",
        use_gradient_checkpointing="unsloth",
    )

    # ── 3. Prepare dataset ──
    dataset = prepare_dataset(node_types)
    print(f"Total training samples: {len(dataset)}")

    # ── 4. Training arguments ──
    output_dir = os.path.join(OUTPUT_BASE, "-".join(node_types))
    os.makedirs(output_dir, exist_ok=True)

    training_args = TrainingArguments(
        output_dir=output_dir,
        per_device_train_batch_size=2,
        gradient_accumulation_steps=4,
        warmup_steps=5,
        num_train_epochs=args.epochs,
        learning_rate=2e-4,
        fp16=False,
        bf16=True,
        logging_steps=1,
        save_strategy="epoch",
        optim="adamw_8bit",
        seed=42,
    )

    # ── 5. Trainer ──
    trainer = SFTTrainer(
        model=model,
        tokenizer=tokenizer,
        train_dataset=dataset,
        args=training_args,
        dataset_text_field="text",
        max_seq_length=MAX_SEQ_LENGTH,
        packing=False,
    )

    # ── 6. Train ──
    print("Starting fine-tuning …")
    trainer.train()

    # ── 7. Save final weights ──
    final_dir = os.path.join(output_dir, "final")
    model.save_pretrained(final_dir)
    tokenizer.save_pretrained(final_dir)
    print(f"Fine-tuning complete. Weights saved to {final_dir}")


def main():
    parser = argparse.ArgumentParser(
        description="Fine-tune a model on NodPT node-type data using Unsloth (FP4)."
    )
    parser.add_argument(
        "--node-type",
        choices=NODE_TYPES + ["all"],
        default="all",
        help="Node type to train on, or 'all' for combined training (default: all).",
    )
    parser.add_argument(
        "--base-model",
        default=DEFAULT_MODEL,
        help=f"Unsloth-compatible base model (default: {DEFAULT_MODEL}).",
    )
    parser.add_argument(
        "--epochs",
        type=int,
        default=3,
        help="Number of training epochs (default: 3).",
    )
    args = parser.parse_args()
    run_finetune(args)


if __name__ == "__main__":
    main()
