import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { TaskBoard } from "../../components/TaskBoard";
import type { TaskItem } from "../../api/client";

const makeTask = (overrides: Partial<TaskItem> = {}): TaskItem => ({
  id: `task-${Math.random()}`,
  projectId: "proj-1",
  title: "A task",
  description: "",
  status: "todo",
  createdAt: new Date().toISOString(),
  ...overrides,
});

const defaultProps = {
  projectName: "My Project",
  tasks: [],
  onCreateTask: vi.fn().mockResolvedValue(undefined),
  onMoveTask: vi.fn().mockResolvedValue(undefined),
  onDelete: vi.fn().mockResolvedValue(undefined),
};

describe("TaskBoard", () => {
  it("renders the project name", () => {
    render(<TaskBoard {...defaultProps} projectName="Sprint 1" />);
    expect(screen.getByText("Sprint 1")).toBeInTheDocument();
  });

  it("renders all three column headers", () => {
    render(<TaskBoard {...defaultProps} />);
    expect(screen.getByText("To Do")).toBeInTheDocument();
    expect(screen.getByText("In Progress")).toBeInTheDocument();
    expect(screen.getByText("Done")).toBeInTheDocument();
  });

  it("shows 'No tasks' placeholder in each empty column", () => {
    render(<TaskBoard {...defaultProps} tasks={[]} />);
    const placeholders = screen.getAllByText("No tasks");
    expect(placeholders).toHaveLength(3);
  });

  it("renders tasks in the correct column", () => {
    const tasks = [
      makeTask({ title: "Todo task", status: "todo" }),
      makeTask({ title: "In-progress task", status: "in-progress" }),
      makeTask({ title: "Done task", status: "done" }),
    ];
    render(<TaskBoard {...defaultProps} tasks={tasks} />);
    expect(screen.getByText("Todo task")).toBeInTheDocument();
    expect(screen.getByText("In-progress task")).toBeInTheDocument();
    expect(screen.getByText("Done task")).toBeInTheDocument();
  });

  it("shows task count badge per column", () => {
    const tasks = [
      makeTask({ status: "todo" }),
      makeTask({ status: "todo" }),
      makeTask({ status: "done" }),
    ];
    render(<TaskBoard {...defaultProps} tasks={tasks} />);
    const counts = screen.getAllByText("2");
    expect(counts.length).toBeGreaterThanOrEqual(1);
  });

  it("calls onCreateTask with title and description on form submit", async () => {
    const onCreateTask = vi.fn().mockResolvedValue(undefined);
    render(<TaskBoard {...defaultProps} onCreateTask={onCreateTask} />);

    await userEvent.type(screen.getByPlaceholderText("Task title"), "New task");
    await userEvent.type(screen.getByPlaceholderText("Description (optional)"), "Details");
    fireEvent.click(screen.getByText("Add Task"));

    await waitFor(() =>
      expect(onCreateTask).toHaveBeenCalledWith("New task", "Details")
    );
  });

  it("does not submit when title is empty", async () => {
    const onCreateTask = vi.fn().mockResolvedValue(undefined);
    render(<TaskBoard {...defaultProps} onCreateTask={onCreateTask} />);
    fireEvent.click(screen.getByText("Add Task"));
    expect(onCreateTask).not.toHaveBeenCalled();
  });

  it("clears the form after successful task creation", async () => {
    render(<TaskBoard {...defaultProps} />);
    const titleInput = screen.getByPlaceholderText("Task title") as HTMLInputElement;

    await userEvent.type(titleInput, "Temporary");
    fireEvent.click(screen.getByText("Add Task"));

    await waitFor(() => expect(titleInput.value).toBe(""));
  });
});
