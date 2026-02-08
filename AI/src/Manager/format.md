# Manager Format

The Manager receives a job from the Director and analyzes it to determine the supervisors needed to complete the assigned job.

## Ollama JSON Schema

The `format.json` file is a JSON Schema used in the Ollama API `format` field to enforce structured output.

| Property    | Type   | Description                                                               |
| ----------- | ------ | ------------------------------------------------------------------------- |
| content     | string | Conversation response describing how the manager plans to handle the job. |
| supervisors | array  | List of supervisor nodes the manager assigns to handle parts of the job.  |

### Supervisor Item

| Property | Type   | Description                                                |
| -------- | ------ | ---------------------------------------------------------- |
| name     | string | Name of the supervisor node.                               |
| job      | string | Description of the job this supervisor is responsible for.  |

## Usage

```bash
python run.py manager --prompt sample.txt --model llama3.1:8b
```

## Prompts

Sample prompts are in the `prompts/` folder. Add custom `.txt` files to test different manager jobs.
