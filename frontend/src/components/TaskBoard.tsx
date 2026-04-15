import { useState, useRef, useMemo } from "react";
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
  onMoveTask: (task: TaskItem, newStatus: TaskItem["status"]) => Promise<void>;
  onDelete: (task: TaskItem) => Promise<void>;
}

export function TaskBoard({ projectName, tasks, onCreateTask, onMoveTask, onDelete }: Props) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [adding, setAdding] = useState(false);

  const [draggingTask, setDraggingTask] = useState<TaskItem | null>(null);
  const [dragOverCol, setDragOverCol] = useState<TaskItem["status"] | null>(null);
  const dragCounters = useRef<Record<string, number>>({});

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!title.trim()) return;
    setAdding(true);
    await onCreateTask(title.trim(), description.trim());
    setTitle("");
    setDescription("");
    setAdding(false);
  }

  function handleDragStart(task: TaskItem) {
    setDraggingTask(task);
  }

  function handleDragEnd() {
    setDraggingTask(null);
    setDragOverCol(null);
    dragCounters.current = {};
  }

  function handleDragEnter(colId: TaskItem["status"]) {
    dragCounters.current[colId] = (dragCounters.current[colId] ?? 0) + 1;
    setDragOverCol(colId);
  }

  function handleDragLeave(colId: TaskItem["status"]) {
    dragCounters.current[colId] = (dragCounters.current[colId] ?? 1) - 1;
    if (dragCounters.current[colId] <= 0) {
      dragCounters.current[colId] = 0;
      setDragOverCol((prev) => (prev === colId ? null : prev));
    }
  }

  async function handleDrop(e: React.DragEvent, colId: TaskItem["status"]) {
    e.preventDefault();
    dragCounters.current[colId] = 0;
    setDragOverCol(null);
    const taskId = e.dataTransfer.getData("text/plain");
    const task = tasks.find((t) => t.id === taskId);
    if (!task || task.status === colId) return;
    setDraggingTask(null);
    await onMoveTask(task, colId);
  }

  const tasksByStatus = useMemo(() => {
    const map: Record<string, TaskItem[]> = {};
    for (const col of COLUMNS) map[col.id] = [];
    for (const t of tasks) {
      if (map[t.status]) map[t.status].push(t);
    }
    return map;
  }, [tasks]);

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
          const colTasks = tasksByStatus[col.id];
          const isOver = dragOverCol === col.id;
          const isDragSource = draggingTask?.status === col.id;

          return (
            <div
              key={col.id}
              className={[
                "kanban-col",
                `col-${col.id}`,
                isOver && !isDragSource ? "col--drag-over" : "",
              ].filter(Boolean).join(" ")}
              onDragOver={(e) => e.preventDefault()}
              onDragEnter={() => handleDragEnter(col.id)}
              onDragLeave={() => handleDragLeave(col.id)}
              onDrop={(e) => handleDrop(e, col.id)}
            >
              <div className="col-header">
                <span className="col-title">{col.label}</span>
                <span className="col-count">{colTasks.length}</span>
              </div>
              <div className="col-tasks">
                {colTasks.map((task) => (
                  <TaskCard
                    key={task.id}
                    task={task}
                    isDragging={draggingTask?.id === task.id}
                    onDragStart={handleDragStart}
                    onDragEnd={handleDragEnd}
                    onDelete={onDelete}
                  />
                ))}
                {colTasks.length === 0 && (
                  <p className={`col-empty${isOver && !isDragSource ? " col-empty--over" : ""}`}>
                    {isOver && !isDragSource ? "Drop here" : "No tasks"}
                  </p>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
