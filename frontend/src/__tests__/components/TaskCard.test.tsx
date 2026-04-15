import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { TaskCard } from "../../components/TaskCard";
import type { TaskItem } from "../../api/client";

const makeTask = (overrides: Partial<TaskItem> = {}): TaskItem => ({
  id: "task-1",
  projectId: "proj-1",
  title: "Test task",
  description: "A description",
  status: "todo",
  createdAt: new Date().toISOString(),
  ...overrides,
});

describe("TaskCard", () => {
  it("renders the task title", () => {
    render(
      <TaskCard
        task={makeTask({ title: "My task" })}
        isDragging={false}
        onDragStart={vi.fn()}
        onDragEnd={vi.fn()}
        onDelete={vi.fn()}
      />
    );
    expect(screen.getByText("My task")).toBeInTheDocument();
  });

  it("renders the description when present", () => {
    render(
      <TaskCard
        task={makeTask({ description: "Some details" })}
        isDragging={false}
        onDragStart={vi.fn()}
        onDragEnd={vi.fn()}
        onDelete={vi.fn()}
      />
    );
    expect(screen.getByText("Some details")).toBeInTheDocument();
  });

  it("omits description element when description is empty", () => {
    render(
      <TaskCard
        task={makeTask({ description: "" })}
        isDragging={false}
        onDragStart={vi.fn()}
        onDragEnd={vi.fn()}
        onDelete={vi.fn()}
      />
    );
    expect(screen.queryByRole("paragraph")).not.toBeInTheDocument();
  });

  it("calls onDelete when the × button is clicked", async () => {
    const onDelete = vi.fn().mockResolvedValue(undefined);
    const task = makeTask();
    render(
      <TaskCard
        task={task}
        isDragging={false}
        onDragStart={vi.fn()}
        onDragEnd={vi.fn()}
        onDelete={onDelete}
      />
    );
    fireEvent.click(screen.getByTitle("Delete task"));
    expect(onDelete).toHaveBeenCalledWith(task);
  });

  it("applies dragging class when isDragging is true", () => {
    const { container } = render(
      <TaskCard
        task={makeTask()}
        isDragging={true}
        onDragStart={vi.fn()}
        onDragEnd={vi.fn()}
        onDelete={vi.fn()}
      />
    );
    expect(container.firstChild).toHaveClass("task-card--dragging");
  });

  it("does not apply dragging class when isDragging is false", () => {
    const { container } = render(
      <TaskCard
        task={makeTask()}
        isDragging={false}
        onDragStart={vi.fn()}
        onDragEnd={vi.fn()}
        onDelete={vi.fn()}
      />
    );
    expect(container.firstChild).not.toHaveClass("task-card--dragging");
  });

  it("card element is draggable", () => {
    const { container } = render(
      <TaskCard
        task={makeTask()}
        isDragging={false}
        onDragStart={vi.fn()}
        onDragEnd={vi.fn()}
        onDelete={vi.fn()}
      />
    );
    expect(container.firstChild).toHaveAttribute("draggable", "true");
  });
});
