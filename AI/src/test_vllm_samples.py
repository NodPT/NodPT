#!/usr/bin/env python
"""
Test suite for generate_vllm_samples.py

Validates the sample generation logic without requiring a vLLM server.
"""

import json
import sys
import os

# Add parent directory to path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from generate_vllm_samples import (
    get_random_coding_prompt,
    get_random_book_prompt,
    build_generation_prompt,
    parse_generated_lines,
)


def test_random_prompts():
    """Test that random prompt generation produces varied outputs."""
    print("Testing random prompt generation...")
    
    # Generate multiple prompts and check for variety
    coding_prompts = [get_random_coding_prompt() for _ in range(10)]
    book_prompts = [get_random_book_prompt() for _ in range(10)]
    
    # Check that we get some variety (not all identical)
    assert len(set(coding_prompts)) > 1, "Coding prompts should vary"
    assert len(set(book_prompts)) > 1, "Book prompts should vary"
    
    # Check that prompts contain expected keywords
    for prompt in coding_prompts[:3]:
        print(f"  Coding: {prompt}")
        assert any(word in prompt.lower() for word in ["implement", "create", "build", "develop", "design"])
    
    for prompt in book_prompts[:3]:
        print(f"  Book: {prompt}")
        assert any(word in prompt.lower() for word in ["write", "create", "develop", "compose", "craft"])
    
    print("  ✓ Random prompts are varied and well-formed")


def test_prompt_building():
    """Test that generation prompts are properly constructed."""
    print("\nTesting prompt building...")
    
    # Test coding prompt
    coding_prompt = build_generation_prompt("coding", 5, None)
    assert "coding" in coding_prompt.lower()
    assert "JSONL" in coding_prompt
    assert "prompt" in coding_prompt
    assert "response" in coding_prompt
    print(f"  Coding prompt: {len(coding_prompt)} chars")
    
    # Test book prompt
    book_prompt = build_generation_prompt("book", 5, None)
    assert "book" in book_prompt.lower() or "writing" in book_prompt.lower()
    assert "JSONL" in book_prompt
    assert "prompt" in book_prompt
    assert "response" in book_prompt
    print(f"  Book prompt: {len(book_prompt)} chars")
    
    # Test with existing inputs
    existing = ["test1", "test2"]
    prompt_with_avoid = build_generation_prompt("coding", 3, existing)
    assert "NOT reuse" in prompt_with_avoid
    assert "test1" in prompt_with_avoid
    print(f"  Prompt with avoidance: {len(prompt_with_avoid)} chars")
    
    print("  ✓ Prompts are well-structured")


def test_sample_parsing():
    """Test parsing of generated JSONL samples."""
    print("\nTesting sample parsing...")
    
    # Valid samples
    valid_jsonl = '''{"prompt": "Task 1", "response": "This is a valid response with enough content to pass"}
{"prompt": "Task 2", "response": "Another valid response with sufficient length for validation"}'''
    
    valid, invalid = parse_generated_lines(valid_jsonl, "coding")
    assert len(valid) == 2, f"Expected 2 valid samples, got {len(valid)}"
    assert invalid == 0, f"Expected 0 invalid samples, got {invalid}"
    print(f"  Valid JSONL: {len(valid)} samples parsed")
    
    # Invalid samples (too short, missing fields)
    invalid_jsonl = '''{"prompt": "Task", "response": "Short"}
{"prompt": "Task 2"}
{"response": "Missing prompt"}
not even json
{"prompt": "", "response": "Empty prompt should be invalid"}'''
    
    valid, invalid = parse_generated_lines(invalid_jsonl, "coding")
    assert len(valid) == 0, f"Expected 0 valid samples, got {len(valid)}"
    assert invalid == 5, f"Expected 5 invalid samples, got {invalid}"
    print(f"  Invalid JSONL: {invalid} samples rejected")
    
    # Mixed valid and invalid
    mixed_jsonl = '''{"prompt": "Good task 1", "response": "This is a complete and valid response with enough content"}
{"prompt": "Bad", "response": "Short"}
{"prompt": "Good task 2", "response": "Another complete and valid response with sufficient content"}'''
    
    valid, invalid = parse_generated_lines(mixed_jsonl, "book")
    assert len(valid) == 2, f"Expected 2 valid samples, got {len(valid)}"
    assert invalid == 1, f"Expected 1 invalid sample, got {invalid}"
    print(f"  Mixed JSONL: {len(valid)} valid, {invalid} invalid")
    
    print("  ✓ Parsing correctly validates samples")


def test_markdown_stripping():
    """Test that markdown code fences are properly stripped."""
    print("\nTesting markdown fence handling...")
    
    # JSONL with markdown fences
    markdown_jsonl = '''```json
{"prompt": "Task", "response": "Valid response with enough content to pass validation"}
```'''
    
    valid, invalid = parse_generated_lines(markdown_jsonl, "coding")
    assert len(valid) == 1, f"Expected 1 valid sample (fences stripped), got {len(valid)}"
    print(f"  Markdown fences stripped: {len(valid)} sample parsed")
    
    print("  ✓ Markdown handling works correctly")


def run_tests():
    """Run all tests."""
    print("=" * 60)
    print("Running generate_vllm_samples.py tests")
    print("=" * 60)
    
    try:
        test_random_prompts()
        test_prompt_building()
        test_sample_parsing()
        test_markdown_stripping()
        
        print("\n" + "=" * 60)
        print("✓ All tests passed!")
        print("=" * 60)
        return 0
    except AssertionError as e:
        print(f"\n✗ Test failed: {e}")
        return 1
    except Exception as e:
        print(f"\n✗ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(run_tests())
