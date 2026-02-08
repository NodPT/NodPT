# Agent Format

The Agent is the last node type that does the actual work. It receives a job from the Supervisor and produces file output.

## Ollama JSON Schema

The `format.json` file is a JSON Schema used in the Ollama API `format` field to enforce structured output.

| Property | Type   | Description                                                   |
| -------- | ------ | ------------------------------------------------------------- |
| content  | string | Conversation response describing the work done by the agent.  |
| files    | array  | List of files produced by the agent with filename and content. |

### File Item

| Property | Type   | Description                           |
| -------- | ------ | ------------------------------------- |
| filename | string | Name of the file including extension. |
| content  | string | The full content of the file.         |

## Usage

```bash
python run.py agent --prompt sample.txt --model llama3.1:8b
```

## Prompts

Sample prompts are in the `prompts/` folder. Add custom `.txt` files to test different agent tasks.
