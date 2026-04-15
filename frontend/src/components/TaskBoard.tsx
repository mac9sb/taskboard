import { useState } from "react";
import type { TaskItem } from "../api/client";
import { TaskCard } from "./TaskCard";

const COLUMNS: { id: TaskItem["status"]; label: string }[] = [
  { id: "todo", label: "To Do" },
  { id: "in-progress", label: "In Progress" },
  { id: "done", label: "Done" },
];

interface Props {
  projectName: string;
  tasks: TaskItem[];
  onCreateTask: (title: string, description: string) => Promise<void>;
  onAdvance: (task: TaskItem, next: TaskItem["status"]) => Promise<void>;
  onDelete: (task: TaskItem) => Promise<void>;
}

export function TaskBoard({ projectName, tasks, onCreateTask, onAdvance, onDelete }: Props) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [adding, setAdding] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!title.trim()) return;
    setAdding(true);
    await onCreateTask(title.trim(), description.trim());
    setTitle("");
    setDescription("");
    setAdding(false);
  }

  return (
    <div className="board-container">
      <div className="board-header">
        <h1 className="board-title">{projectName}</h1>
        <form className="add-task-form" onSubmit={handleSubmit}>
          <input
            className="input"
            placeholder="Task title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
          />
          <input
            className="input"
            placeholder="Description (optional)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <button className="btn btn-primary" type="submit" disabled={adding}>
            {adding ? "Adding…" : "Add Task"}
          </button>
        </form>
      </div>

      <div className="kanban">
        {COLUMNS.map((col) => {
          const colTasks = tasks.filter((t) => t.status === col.id);
          return (
            <div key={col.id} className={`kanban-col col-${col.id}`}>
              <div className="col-header">
                <span className="col-title">{col.label}</span>
                <span className="col-count">{colTasks.length}</span>
              </div>
              <div className="col-tasks">
                {colTasks.map((task) => (
                  <TaskCard
                    key={task.id}
                    task={task}
                    onAdvance={onAdvance}
                    onDelete={onDelete}
                  />
                ))}
                {colTasks.length === 0 && (
                  <p className="col-empty">No tasks</p>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
