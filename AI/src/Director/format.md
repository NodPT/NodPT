# Director Format

The Director is the top-level node that analyzes a user request and creates a project plan by determining the managers needed to complete the project.

## JSON Structure

| Field    | Type   | Description                                                              |
| -------- | ------ | ------------------------------------------------------------------------ |
| content  | string | Conversation response describing the project analysis and plan.          |
| managers | array  | List of manager nodes the director assigns to handle parts of the project. |

### Manager Object

| Field | Type   | Description                                           |
| ----- | ------ | ----------------------------------------------------- |
| name  | string | Name of the manager node.                             |
| job   | string | Description of the job this manager is responsible for. |

## Example

See [format.json](format.json) for the JSON sample.
