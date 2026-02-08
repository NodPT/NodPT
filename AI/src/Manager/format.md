# Manager Format

The Manager receives a job from the Director and analyzes it to determine the supervisors needed to complete the assigned job.

## JSON Structure

| Field       | Type   | Description                                                               |
| ----------- | ------ | ------------------------------------------------------------------------- |
| content     | string | Conversation response describing how the manager plans to handle the job. |
| supervisors | array  | List of supervisor nodes the manager assigns to handle parts of the job.  |

### Supervisor Object

| Field | Type   | Description                                               |
| ----- | ------ | --------------------------------------------------------- |
| name  | string | Name of the supervisor node.                              |
| job   | string | Description of the job this supervisor is responsible for. |

## Example

See [format.json](format.json) for the JSON sample.
