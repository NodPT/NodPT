# Director Format

The Director is the top-level node that analyzes a user request and creates a project plan by determining the managers needed to complete the project.

## Ollama JSON Schema

The `format.json` file is a JSON Schema used in the Ollama API `format` field to enforce structured output.

| Property | Type   | Description                                                                |
| -------- | ------ | -------------------------------------------------------------------------- |
| content  | string | Conversation response describing the project analysis and plan.            |
| managers | array  | List of manager nodes the director assigns to handle parts of the project. |

### Manager Item

| Property | Type   | Description                                             |
| -------- | ------ | ------------------------------------------------------- |
| name     | string | Name of the manager node.                               |
| job      | string | Description of the job this manager is responsible for.  |

## Usage

```bash
python run.py director --prompt sample.txt --model llama3.1:8b
```

## Prompts

Sample prompts are in the `prompts/` folder. Add custom `.txt` files to test different project requests.
