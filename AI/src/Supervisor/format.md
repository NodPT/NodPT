# Supervisor Format

The Supervisor receives a job from the Manager and analyzes it to determine the agents needed to create the file content to complete the assigned job.

## Ollama JSON Schema

The `format.json` file is a JSON Schema used in the Ollama API `format` field to enforce structured output.

| Property | Type   | Description                                                                  |
| -------- | ------ | ---------------------------------------------------------------------------- |
| content  | string | Conversation response describing how the supervisor plans to handle the job. |
| agents   | array  | List of agent nodes the supervisor assigns to create file content.           |

### Agent Item

| Property | Type   | Description                                           |
| -------- | ------ | ----------------------------------------------------- |
| name     | string | Name of the agent node.                               |
| job      | string | Description of the job this agent is responsible for.  |

## Usage

```bash
python run.py supervisor --prompt sample.txt --model llama3.1:8b
```

## Prompts

Sample prompts are in the `prompts/` folder. Add custom `.txt` files to test different supervisor jobs.
