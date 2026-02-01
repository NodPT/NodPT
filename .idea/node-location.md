## do not learn or apply this file's content

Add a **Save Project** menu under **File** in the header. When clicked, collect all node positions on the canvas and save them in a new `NodeLayout` field on the Project data model. `NodeLayout` is a max-length string that stores a JSON array of `[x, y]` pairs. Send the full array in a single request to `ProjectController` to avoid per-node updates.

In `frontend/projectService` and `nodeService`, load `NodeLayout` and apply the stored positions to all nodes.


