import type { Project, TaskItem } from "../api/client";

let _seq = 0;
const uid = () => `test-${++_seq}`;

export const makeTask = (overrides: Partial<TaskItem> = {}): TaskItem => ({
  id: uid(),
  projectId: "proj-1",
  title: "A task",
  description: "",
  status: "todo",
  createdAt: new Date().toISOString(),
  ...overrides,
});

export const makeProject = (overrides: Partial<Project> = {}): Project => ({
  id: uid(),
  name: "Test Project",
  description: "",
  createdAt: new Date().toISOString(),
  ...overrides,
});
