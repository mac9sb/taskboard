export interface Project {
  id: string;
  name: string;
  description: string;
  createdAt: string;
}

export interface TaskItem {
  id: string;
  projectId: string;
  title: string;
  description: string;
  status: "todo" | "in-progress" | "done";
  createdAt: string;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const api = {
  projects: {
    list: () => request<Project[]>("/api/projects"),
    create: (data: Pick<Project, "name" | "description">) =>
      request<Project>("/api/projects", { method: "POST", body: JSON.stringify(data) }),
    delete: (id: string) =>
      request<void>(`/api/projects/${id}`, { method: "DELETE" }),
  },
  tasks: {
    list: (projectId: string) =>
      request<TaskItem[]>(`/api/projects/${projectId}/tasks`),
    create: (projectId: string, data: Pick<TaskItem, "title" | "description">) =>
      request<TaskItem>(`/api/projects/${projectId}/tasks`, {
        method: "POST",
        body: JSON.stringify(data),
      }),
    updateStatus: (projectId: string, taskId: string, status: TaskItem["status"]) =>
      request<TaskItem>(`/api/projects/${projectId}/tasks/${taskId}`, {
        method: "PATCH",
        body: JSON.stringify({ status }),
      }),
    delete: (projectId: string, taskId: string) =>
      request<void>(`/api/projects/${projectId}/tasks/${taskId}`, { method: "DELETE" }),
  },
};
