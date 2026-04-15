import { useEffect, useState } from "react";
import { api } from "./api/client";
import type { Project, TaskItem } from "./api/client";
import { ProjectList } from "./components/ProjectList";
import { TaskBoard } from "./components/TaskBoard";

export default function App() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.projects
      .list()
      .then(setProjects)
      .catch(() => setError("Could not connect to API. Is the backend running?"))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedProjectId) return;
    api.tasks.list(selectedProjectId).then(setTasks);
  }, [selectedProjectId]);

  const selectedProject = projects.find((p) => p.id === selectedProjectId) ?? null;

  async function handleCreateProject(name: string, description: string) {
    const project = await api.projects.create({ name, description });
    setProjects((prev) => [project, ...prev]);
    setSelectedProjectId(project.id);
    setTasks([]);
  }

  async function handleDeleteProject(id: string) {
    await api.projects.delete(id);
    setProjects((prev) => prev.filter((p) => p.id !== id));
    if (selectedProjectId === id) {
      setSelectedProjectId(null);
      setTasks([]);
    }
  }

  async function handleCreateTask(title: string, description: string) {
    if (!selectedProjectId) return;
    const task = await api.tasks.create(selectedProjectId, { title, description });
    setTasks((prev) => [...prev, task]);
  }

  async function handleMoveTask(task: TaskItem, newStatus: TaskItem["status"]) {
    setTasks((prev) => prev.map((t) => t.id === task.id ? { ...t, status: newStatus } : t));
    try {
      const updated = await api.tasks.updateStatus(task.projectId, task.id, newStatus);
      setTasks((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
    } catch {
      setTasks((prev) => prev.map((t) => t.id === task.id ? task : t));
    }
  }

  async function handleDeleteTask(task: TaskItem) {
    await api.tasks.delete(task.projectId, task.id);
    setTasks((prev) => prev.filter((t) => t.id !== task.id));
  }

  return (
    <div className="app">
      <ProjectList
        projects={projects}
        selectedId={selectedProjectId}
        onSelect={(id) => {
          setSelectedProjectId(id);
          setTasks([]);
        }}
        onCreate={handleCreateProject}
        onDelete={handleDeleteProject}
      />

      <main className="main">
        {loading && <div className="state-message">Connecting…</div>}
        {error && <div className="state-message error">{error}</div>}
        {!loading && !error && !selectedProject && (
          <div className="state-message">
            <h2>Select or create a project</h2>
            <p>Use the sidebar to get started.</p>
          </div>
        )}
        {selectedProject && (
          <TaskBoard
            projectName={selectedProject.name}
            tasks={tasks}
            onCreateTask={handleCreateTask}
            onMoveTask={handleMoveTask}
            onDelete={handleDeleteTask}
          />
        )}
      </main>
    </div>
  );
}
