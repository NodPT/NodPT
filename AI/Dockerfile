FROM python:3.11-slim

WORKDIR /app

# Copy source files from the new folder layout
COPY src/ ./

# Install dependencies when requirements.txt is provided
RUN if [ -f requirements.txt ]; then pip install --no-cache-dir -r requirements.txt; fi

EXPOSE 8000

CMD ["python", "-m", "http.server", "8000"]
