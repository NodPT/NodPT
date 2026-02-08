# Agent Format

The Agent is the last node type that does the actual work. It receives a job from the Supervisor and produces file output.

## JSON Structure

| Field   | Type   | Description                                                     |
| ------- | ------ | --------------------------------------------------------------- |
| content | string | Conversation response describing the work done by the agent.    |
| files   | array  | List of files produced by the agent with filename and content.   |

### File Object

| Field    | Type   | Description                              |
| -------- | ------ | ---------------------------------------- |
| filename | string | Name of the file including extension.    |
| content  | string | The full content of the file.            |

## Example

See [format.json](format.json) for the JSON sample.
