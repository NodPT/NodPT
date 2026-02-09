"""
Export a fine-tuned model to GGUF format for use with Ollama.

This script merges the LoRA adapters back into the base model and exports
the result as a GGUF file that can be loaded directly by Ollama.

Usage:
    python scripts/export_gguf.py --model-dir output/all/final
    python scripts/export_gguf.py --model-dir output/director/final --quantization q4_k_m
"""

import argparse
import os
import sys

from unsloth import FastLanguageModel

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
BASE_DIR = os.path.dirname(SCRIPT_DIR)
EXPORT_DIR = os.path.join(BASE_DIR, "export")

MAX_SEQ_LENGTH = 2048


def export_gguf(args):
    model_dir = args.model_dir
    if not os.path.isdir(model_dir):
        print(f"Error: model directory not found: {model_dir}")
        sys.exit(1)

    print(f"Loading fine-tuned model from: {model_dir}")
    model, tokenizer = FastLanguageModel.from_pretrained(
        model_name=model_dir,
        max_seq_length=MAX_SEQ_LENGTH,
        load_in_4bit=True,
    )

    os.makedirs(EXPORT_DIR, exist_ok=True)

    quantization = args.quantization
    print(f"Exporting to GGUF with quantisation method: {quantization}")

    model.save_pretrained_gguf(
        EXPORT_DIR,
        tokenizer,
        quantization_method=quantization,
    )

    print(f"GGUF export complete. Files saved to: {EXPORT_DIR}")
    print("\nNext steps:")
    print(f"  1. Copy the Modelfile from {BASE_DIR}/Modelfile into {EXPORT_DIR}/")
    print(f"  2. Update the FROM line in Modelfile to point to the .gguf file")
    print("  3. Run:  ollama create nodpt -f Modelfile")
    print("  4. Run:  ollama run nodpt")


def main():
    parser = argparse.ArgumentParser(
        description="Export a fine-tuned NodPT model to GGUF for Ollama."
    )
    parser.add_argument(
        "--model-dir",
        required=True,
        help="Path to the fine-tuned model directory (e.g. output/all/final).",
    )
    parser.add_argument(
        "--quantization",
        default="q4_k_m",
        choices=["q4_k_m", "q5_k_m", "q8_0", "f16"],
        help="GGUF quantisation method (default: q4_k_m).",
    )
    args = parser.parse_args()
    export_gguf(args)


if __name__ == "__main__":
    main()
