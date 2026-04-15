import type { TaskItem } from "../api/client";

interface Props {
  task: TaskItem;
  isDragging: boolean;
  onDragStart: (task: TaskItem) => void;
  onDragEnd: () => void;
  onDelete: (task: TaskItem) => Promise<void>;
}

export function TaskCard({ task, isDragging, onDragStart, onDragEnd, onDelete }: Props) {
  return (
    <div
      className={`task-card${isDragging ? " task-card--dragging" : ""}`}
      draggable
      onDragStart={(e) => {
        e.dataTransfer.effectAllowed = "move";
        e.dataTransfer.setData("text/plain", task.id);
        requestAnimationFrame(() => onDragStart(task));
      }}
      onDragEnd={onDragEnd}
    >
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
      <div className="task-drag-hint">⠿ drag to move</div>
    </div>
  );
}
