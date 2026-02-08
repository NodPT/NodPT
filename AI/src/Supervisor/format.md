# Supervisor Format

The Supervisor receives a job from the Manager and analyzes it to determine the agents needed to create the file content to complete the assigned job.

## JSON Structure

| Field   | Type   | Description                                                                  |
| ------- | ------ | ---------------------------------------------------------------------------- |
| content | string | Conversation response describing how the supervisor plans to handle the job. |
| agents  | array  | List of agent nodes the supervisor assigns to create file content.           |

### Agent Object

| Field | Type   | Description                                          |
| ----- | ------ | ---------------------------------------------------- |
| name  | string | Name of the agent node.                              |
| job   | string | Description of the job this agent is responsible for. |

## Example

See [format.json](format.json) for the JSON sample.
