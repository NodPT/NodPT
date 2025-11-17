#!/usr/bin/env python3
"""
Example usage of the NodPT AI Service with OpenAI Python client
This demonstrates how to interact with the TensorRT-LLM service

Installation:
    pip install openai

Usage:
    python usage_example.py
"""

from openai import OpenAI

# Configure client to use local TensorRT-LLM service
client = OpenAI(
    base_url="http://localhost:8000/v1",
    api_key="not-needed"  # API key not required for local service
)

def example_chat_completion():
    """Example: Chat completion with conversation context"""
    print("=" * 60)
    print("Example 1: Chat Completion")
    print("=" * 60)
    
    response = client.chat.completions.create(
        model="gpt-oss-20b",
        messages=[
            {"role": "system", "content": "You are a helpful AI assistant."},
            {"role": "user", "content": "What is TensorRT-LLM and why is it useful?"}
        ],
        max_tokens=200,
        temperature=0.7
    )
    
    print(f"Response: {response.choices[0].message.content}")
    print(f"Tokens used: {response.usage.total_tokens}")
    print()

def example_text_completion():
    """Example: Simple text completion"""
    print("=" * 60)
    print("Example 2: Text Completion")
    print("=" * 60)
    
    response = client.completions.create(
        model="gpt-oss-20b",
        prompt="The benefits of using TensorRT for AI inference include",
        max_tokens=100,
        temperature=0.7
    )
    
    print(f"Prompt: The benefits of using TensorRT for AI inference include")
    print(f"Completion: {response.choices[0].text}")
    print()

def example_streaming():
    """Example: Multi-turn conversation"""
    print("=" * 60)
    print("Example 3: Multi-turn Conversation")
    print("=" * 60)
    
    messages = [
        {"role": "system", "content": "You are a knowledgeable AI expert."},
        {"role": "user", "content": "Explain GPU acceleration in simple terms."}
    ]
    
    response = client.chat.completions.create(
        model="gpt-oss-20b",
        messages=messages,
        max_tokens=150,
        temperature=0.7
    )
    
    assistant_message = response.choices[0].message.content
    print(f"User: {messages[1]['content']}")
    print(f"Assistant: {assistant_message}")
    
    # Continue conversation
    messages.append({"role": "assistant", "content": assistant_message})
    messages.append({"role": "user", "content": "Can you give a specific example?"})
    
    response = client.chat.completions.create(
        model="gpt-oss-20b",
        messages=messages,
        max_tokens=150,
        temperature=0.7
    )
    
    print(f"User: {messages[3]['content']}")
    print(f"Assistant: {response.choices[0].message.content}")
    print()

def list_available_models():
    """Example: List available models"""
    print("=" * 60)
    print("Available Models")
    print("=" * 60)
    
    models = client.models.list()
    for model in models.data:
        print(f"- {model.id}")
    print()

if __name__ == "__main__":
    print("\n🚀 NodPT AI Service - Usage Examples\n")
    
    try:
        # List models
        list_available_models()
        
        # Run examples
        example_chat_completion()
        example_text_completion()
        example_streaming()
        
        print("✅ All examples completed successfully!")
        
    except Exception as e:
        print(f"❌ Error: {e}")
        print("\nMake sure the AI service is running:")
        print("  cd AI && docker compose up -d")
        print("\nCheck service health:")
        print("  curl http://localhost:8000/health")
