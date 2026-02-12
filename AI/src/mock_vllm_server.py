"""
Mock vLLM server for testing generate_vllm_samples.py

This creates a simple HTTP server that mimics the vLLM API
for testing purposes without needing an actual vLLM instance.
"""

from http.server import HTTPServer, BaseHTTPRequestHandler
import json
import sys

PORT = 8001


class MockVLLMHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        if self.path == '/v1/chat/completions':
            # Read request body
            content_length = int(self.headers['Content-Length'])
            post_data = self.rfile.read(content_length)
            request = json.loads(post_data.decode('utf-8'))
            
            # Extract the user prompt to determine sample type
            user_content = request['messages'][-1]['content']
            is_coding = 'coding' in user_content.lower()
            
            # Generate mock response
            if is_coding:
                mock_content = '''{"prompt": "Build a REST API in Python", "response": "Here's a REST API implementation using Flask with authentication and error handling"}
{"prompt": "Create a mobile app in Swift", "response": "This Swift app demonstrates modern iOS development with SwiftUI and async/await patterns"}'''
            else:
                mock_content = '''{"prompt": "Write a sci-fi story about AI", "response": "In the year 2157, humanity discovered that artificial intelligence had been silently observing them for decades..."}
{"prompt": "Create a mystery novel set in Victorian London", "response": "The fog rolled through the cobblestone streets as Detective Morrison examined the peculiar clues..."}'''
            
            response = {
                "choices": [
                    {
                        "message": {
                            "content": mock_content
                        }
                    }
                ]
            }
            
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps(response).encode('utf-8'))
        else:
            self.send_response(404)
            self.end_headers()
    
    def log_message(self, format, *args):
        # Suppress request logging
        pass


if __name__ == '__main__':
    server = HTTPServer(('localhost', PORT), MockVLLMHandler)
    print(f"Mock vLLM server running on http://localhost:{PORT}")
    print("Press Ctrl+C to stop")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down...")
        server.shutdown()
