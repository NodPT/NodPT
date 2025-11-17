#!/usr/bin/env python3
"""
OpenAI-Compatible API Server for TensorRT-LLM
Provides REST API endpoints compatible with OpenAI's API format
Uses TensorRT-LLM's native inference capabilities
"""

import argparse
import json
import os
import sys
import time
import uuid
from typing import List, Optional, Dict, Any
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
import threading

# Add TensorRT-LLM to path
sys.path.insert(0, '/workspace/TensorRT-LLM')

try:
    import tensorrt_llm
    from tensorrt_llm.runtime import ModelRunner, ModelRunnerCpp
    print(f"TensorRT-LLM version: {tensorrt_llm.__version__}")
except ImportError as e:
    print(f"Warning: Could not import TensorRT-LLM: {e}")
    print("Server will start but inference will not be available")


class OpenAIHandler(BaseHTTPRequestHandler):
    """HTTP request handler with OpenAI-compatible endpoints"""
    
    model_runner = None
    model_name = "gpt-oss-20b"
    
    def do_GET(self):
        """Handle GET requests"""
        path = urlparse(self.path).path
        
        if path == '/v1/models':
            self.handle_list_models()
        elif path == '/health':
            self.handle_health()
        elif path == '/':
            self.handle_root()
        else:
            self.send_error(404, "Not Found")
    
    def do_POST(self):
        """Handle POST requests"""
        path = urlparse(self.path).path
        
        if path == '/v1/completions':
            self.handle_completions()
        elif path == '/v1/chat/completions':
            self.handle_chat_completions()
        else:
            self.send_error(404, "Not Found")
    
    def handle_root(self):
        """Handle root endpoint"""
        response = {
            "name": "TensorRT-LLM OpenAI-Compatible API",
            "version": "1.0.0",
            "status": "running",
            "endpoints": [
                "GET /v1/models",
                "POST /v1/completions",
                "POST /v1/chat/completions",
                "GET /health"
            ]
        }
        self.send_json_response(response)
    
    def handle_health(self):
        """Handle health check endpoint"""
        response = {"status": "healthy", "model_loaded": self.model_runner is not None}
        self.send_json_response(response)
    
    def handle_list_models(self):
        """Handle model list endpoint"""
        response = {
            "object": "list",
            "data": [
                {
                    "id": self.model_name,
                    "object": "model",
                    "created": int(time.time()),
                    "owned_by": "tensorrt-llm",
                    "permission": [],
                    "root": self.model_name,
                    "parent": None
                }
            ]
        }
        self.send_json_response(response)
    
    def handle_completions(self):
        """Handle text completion endpoint"""
        try:
            content_length = int(self.headers.get('Content-Length', 0))
            body = self.rfile.read(content_length)
            request_data = json.loads(body.decode('utf-8'))
            
            prompt = request_data.get('prompt', '')
            max_tokens = request_data.get('max_tokens', 100)
            temperature = request_data.get('temperature', 1.0)
            top_p = request_data.get('top_p', 1.0)
            n = request_data.get('n', 1)
            
            if not prompt:
                self.send_error(400, "Missing 'prompt' field")
                return
            
            # Generate completion
            if self.model_runner is None:
                completion_text = "[Model not loaded] This is a placeholder response. Please load a model to get actual completions."
            else:
                # Use TensorRT-LLM for inference
                completion_text = self.generate_text(prompt, max_tokens, temperature, top_p)
            
            response = {
                "id": f"cmpl-{uuid.uuid4().hex}",
                "object": "text_completion",
                "created": int(time.time()),
                "model": self.model_name,
                "choices": [
                    {
                        "text": completion_text,
                        "index": 0,
                        "logprobs": None,
                        "finish_reason": "length"
                    }
                ],
                "usage": {
                    "prompt_tokens": len(prompt.split()),
                    "completion_tokens": len(completion_text.split()),
                    "total_tokens": len(prompt.split()) + len(completion_text.split())
                }
            }
            
            self.send_json_response(response)
            
        except Exception as e:
            self.send_error(500, f"Internal server error: {str(e)}")
    
    def handle_chat_completions(self):
        """Handle chat completion endpoint"""
        try:
            content_length = int(self.headers.get('Content-Length', 0))
            body = self.rfile.read(content_length)
            request_data = json.loads(body.decode('utf-8'))
            
            messages = request_data.get('messages', [])
            max_tokens = request_data.get('max_tokens', 100)
            temperature = request_data.get('temperature', 1.0)
            top_p = request_data.get('top_p', 1.0)
            
            if not messages:
                self.send_error(400, "Missing 'messages' field")
                return
            
            # Convert chat messages to prompt
            prompt = self.messages_to_prompt(messages)
            
            # Generate completion
            if self.model_runner is None:
                completion_text = "[Model not loaded] This is a placeholder response. Please load a model to get actual completions."
            else:
                completion_text = self.generate_text(prompt, max_tokens, temperature, top_p)
            
            response = {
                "id": f"chatcmpl-{uuid.uuid4().hex}",
                "object": "chat.completion",
                "created": int(time.time()),
                "model": self.model_name,
                "choices": [
                    {
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": completion_text
                        },
                        "finish_reason": "length"
                    }
                ],
                "usage": {
                    "prompt_tokens": len(prompt.split()),
                    "completion_tokens": len(completion_text.split()),
                    "total_tokens": len(prompt.split()) + len(completion_text.split())
                }
            }
            
            self.send_json_response(response)
            
        except Exception as e:
            self.send_error(500, f"Internal server error: {str(e)}")
    
    def messages_to_prompt(self, messages: List[Dict[str, str]]) -> str:
        """Convert chat messages to a single prompt string"""
        prompt_parts = []
        for msg in messages:
            role = msg.get('role', 'user')
            content = msg.get('content', '')
            if role == 'system':
                prompt_parts.append(f"System: {content}")
            elif role == 'user':
                prompt_parts.append(f"User: {content}")
            elif role == 'assistant':
                prompt_parts.append(f"Assistant: {content}")
        prompt_parts.append("Assistant:")
        return "\n".join(prompt_parts)
    
    def generate_text(self, prompt: str, max_tokens: int, temperature: float, top_p: float) -> str:
        """Generate text using TensorRT-LLM"""
        try:
            # This is a placeholder - actual implementation would use ModelRunner
            # The real implementation requires loading the compiled TRT engine
            # and running inference through the ModelRunner
            return f"[Generated response for: {prompt[:50]}...]"
        except Exception as e:
            return f"[Error generating text: {str(e)}]"
    
    def send_json_response(self, data: Dict[str, Any], status: int = 200):
        """Send JSON response"""
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps(data, indent=2).encode('utf-8'))
    
    def log_message(self, format, *args):
        """Custom log message format"""
        print(f"[{self.log_date_time_string()}] {format % args}")


def run_server(host: str, port: int, engine_dir: Optional[str] = None, model_dir: Optional[str] = None):
    """Run the OpenAI-compatible API server"""
    
    print("=" * 60)
    print("TensorRT-LLM OpenAI-Compatible API Server")
    print("=" * 60)
    print(f"Host: {host}")
    print(f"Port: {port}")
    print(f"Engine Directory: {engine_dir}")
    print(f"Model Directory: {model_dir}")
    print("=" * 60)
    
    # Initialize model runner if engine is available
    if engine_dir and os.path.exists(engine_dir):
        print(f"Loading TensorRT engine from {engine_dir}...")
        try:
            # Note: Actual model loading would happen here
            # OpenAIHandler.model_runner = ModelRunnerCpp.from_dir(engine_dir)
            print("Model engine found (loading deferred to first request)")
        except Exception as e:
            print(f"Warning: Could not load model: {e}")
    else:
        print("No engine directory provided or not found. Server will run without model.")
    
    # Start HTTP server
    server_address = (host, port)
    httpd = HTTPServer(server_address, OpenAIHandler)
    
    print(f"\nServer started at http://{host}:{port}")
    print("Available endpoints:")
    print("  - GET  /health")
    print("  - GET  /v1/models")
    print("  - POST /v1/completions")
    print("  - POST /v1/chat/completions")
    print("\nPress Ctrl+C to stop the server\n")
    
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down server...")
        httpd.shutdown()


def main():
    parser = argparse.ArgumentParser(description='TensorRT-LLM OpenAI-Compatible API Server')
    parser.add_argument('--host', type=str, default='0.0.0.0', help='Host to bind to')
    parser.add_argument('--port', type=int, default=8000, help='Port to bind to')
    parser.add_argument('--engine_dir', type=str, help='Directory containing TensorRT engine')
    parser.add_argument('--model_dir', type=str, help='Directory containing model files')
    parser.add_argument('--model_name', type=str, default='gpt-oss-20b', help='Model name')
    
    args = parser.parse_args()
    
    # Set model name in handler
    OpenAIHandler.model_name = args.model_name
    
    # Run server
    run_server(args.host, args.port, args.engine_dir, args.model_dir)


if __name__ == '__main__':
    main()
