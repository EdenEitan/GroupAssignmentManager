import type { Project, TaskItem, User } from './types';
export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}
export async function api<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  let response: Response;
  try {
    response = await fetch('/api' + path, {
      method, headers: { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body)
    });
  } catch { throw new ApiError(0, 'Cannot reach the backend. Start the API on port 5004.'); }
  if (!response.ok) {
    const text = await response.text();
    let message = text;
    try { const parsed = JSON.parse(text); message = parsed.detail || parsed.title || parsed.message || text; } catch { /* plain text */ }
    throw new ApiError(response.status, message || 'Request failed.');
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}
export const usersApi = {
  list: () => api<User[]>('/users'),
  create: (name: string, email: string) => api<User>('/users', 'POST', { name, email })
};
export const projectsApi = {
  list: (memberId: string) => api<Project[]>('/projects?memberId=' + memberId),
  create: (projectName: string, courseName: string, description: string, deadline: string, creatorId: string) =>
    api<Project>('/projects', 'POST', { projectName, courseName, description, deadline: deadline || null, creatorId }),
  addMember: (id: string, userId: string) => api<Project>(`/projects/${id}/members`, 'POST', { userId })
};
export const tasksApi = {
  list: (projectId: string) => api<TaskItem[]>('/tasks?projectId=' + projectId),
  create: (projectId: string, title: string, description: string, deadline: string, priority: string) =>
    api<TaskItem>('/tasks', 'POST', { projectId, title, description, deadline: deadline || null, priority }),
  details: (task: TaskItem, details: { title: string; description: string; deadline: string; priority: string }) =>
    api<TaskItem>(`/tasks/${task.id}`, 'PUT', { version: task.version, ...details, deadline: details.deadline || null }),
  request: (task: TaskItem, userId: string) =>
    api<TaskItem>(`/tasks/${task.id}/request-assignment`, 'POST', { version: task.version, userId }),
  assign: (task: TaskItem, userId: string) =>
    api<TaskItem>(`/tasks/${task.id}/assign`, 'PUT', { version: task.version, userId }),
  unassign: (task: TaskItem) => api<TaskItem>(`/tasks/${task.id}/unassign`, 'PUT', { version: task.version }),
  status: (task: TaskItem, status: string) =>
    api<TaskItem>(`/tasks/${task.id}/status`, 'PUT', { version: task.version, status }),
  help: (task: TaskItem, needsHelp: boolean) =>
    api<TaskItem>(`/tasks/${task.id}/help`, 'PUT', { version: task.version, needsHelp }),
  delete: (task: TaskItem) => api<void>(`/tasks/${task.id}?version=${task.version}`, 'DELETE')
};
