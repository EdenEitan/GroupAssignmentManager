"""End-to-end API check against a live MongoDB instance."""
import json
import urllib.error
import urllib.request
import uuid

BASE = "http://localhost:5004/api"


def call(method, path, body=None, expect=200):
    data = None if body is None else json.dumps(body).encode()
    request = urllib.request.Request(BASE + path, data=data, method=method,
                                     headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=12) as response:
            status, text = response.status, response.read().decode()
    except urllib.error.HTTPError as error:
        status, text = error.code, error.read().decode()
    assert status == expect, (method, path, status, expect, text)
    return json.loads(text) if text and text.startswith(("{", "[")) else text


suffix = uuid.uuid4().hex[:8]
eden = call("POST", "/users", {"name": "Eden", "email": f"eden-{suffix}@example.com"}, 201)
maya = call("POST", "/users", {"name": "Maya", "email": f"maya-{suffix}@example.com"}, 201)
project = call("POST", "/projects", {
    "projectName": "Group project", "courseName": "Software Engineering",
    "creatorId": eden["userId"]
}, 201)
pid = project["projectId"]
call("POST", f"/projects/{pid}/members", {"userId": maya["userId"]})
task = call("POST", "/tasks", {"projectId": pid, "title": "Build frontend", "priority": "High"}, 201)
tid = task["id"]
assert task["version"] == 1
requested = call("POST", f"/tasks/{tid}/request-assignment",
                 {"version": 1, "userId": maya["userId"]})
assert requested["version"] == 2 and maya["userId"] in requested["assignmentRequests"]
call("PUT", f"/tasks/{tid}/assign", {"version": 1, "userId": maya["userId"]}, 409)
assigned = call("PUT", f"/tasks/{tid}/assign",
                {"version": 2, "userId": maya["userId"]})
assert assigned["version"] == 3 and assigned["assigneeId"] == maya["userId"]
helped = call("PUT", f"/tasks/{tid}/help", {"version": 3, "needsHelp": True})
assert helped["version"] == 4 and helped["needsHelp"]
call("PUT", f"/tasks/{tid}/status", {"version": 3, "status": "Completed"}, 409)
done = call("PUT", f"/tasks/{tid}/status", {"version": 4, "status": "Completed"})
assert done["version"] == 5
assert call("GET", f"/tasks/{tid}")["status"] == "Completed"
assert any(x["id"] == tid for x in call("GET", f"/tasks?projectId={pid}"))
print("API and MongoDB smoke test passed, including stale version conflicts.")
