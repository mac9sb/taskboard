import type { TaskItem } from "../api/client";

const NEXT_STATUS: Record<TaskItem["status"], TaskItem["status"] | null> = {
  todo: "in-progress",
  "in-progress": "done",
  done: null,
};

const STATUS_LABEL: Record<TaskItem["status"], string> = {
  todo: "Start",
  "in-progress": "Complete",
  done: "",
};

interface Props {
  task: TaskItem;
  onAdvance: (task: TaskItem, next: TaskItem["status"]) => Promise<void>;
  onDelete: (task: TaskItem) => Promise<void>;
}

export function TaskCard({ task, onAdvance, onDelete }: Props) {
  const next = NEXT_STATUS[task.status];

  return (
    <div className="task-card">
      <div className="task-card-header">
        <span className="task-title">{task.title}</span>
        <button
          className="icon-btn delete-btn"
          onClick={() => onDelete(task)}
          title="Delete task"
        >
          ×
        </button>
      </div>
      {task.description && (
        <p className="task-description">{task.description}</p>
      )}
      {next && (
        <button
          className="btn btn-ghost btn-sm"
          onClick={() => onAdvance(task, next)}
        >
          {STATUS_LABEL[task.status]} →
        </button>
      )}
    </div>
  );
}
