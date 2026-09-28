export interface User { userId: string; name: string; email: string }
export interface Project { projectId: string; projectName: string; courseName: string; description: string | null; deadline: string | null; memberIds: string[] }
export type Status = 'Open' | 'In Progress' | 'Completed';
export type Priority = 'Low' | 'Medium' | 'High';
export interface TaskItem {
  id: string; projectId: string; title: string; description: string | null;
  assigneeId: string | null; deadline: string | null; status: Status; priority: Priority;
  needsHelp: boolean; assignmentRequests: string[]; version: number;
}
