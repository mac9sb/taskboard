import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { TaskCard } from "../../components/TaskCard";
import { makeTask } from "../testUtils";

describe("TaskCard", () => {
  const baseProps = {
    isDragging: false,
    onDragStart: vi.fn(),
    onDragEnd: vi.fn(),
    onDelete: vi.fn(),
  };

  it("renders the task title", () => {
    render(<TaskCard {...baseProps} task={makeTask({ title: "My task" })} />);
    expect(screen.getByText("My task")).toBeInTheDocument();
  });

  it("renders the description when present", () => {
    render(<TaskCard {...baseProps} task={makeTask({ description: "Some details" })} />);
    expect(screen.getByText("Some details")).toBeInTheDocument();
  });

  it("omits description element when description is empty", () => {
    render(<TaskCard {...baseProps} task={makeTask({ description: "" })} />);
    expect(screen.queryByRole("paragraph")).not.toBeInTheDocument();
  });

  it("calls onDelete when × is clicked", async () => {
    const onDelete = vi.fn().mockResolvedValue(undefined);
    const task = makeTask();
    render(<TaskCard {...baseProps} task={task} onDelete={onDelete} />);
    fireEvent.click(screen.getByTitle("Delete task"));
    expect(onDelete).toHaveBeenCalledWith(task);
  });

  it("applies dragging class when isDragging is true", () => {
    const { container } = render(<TaskCard {...baseProps} task={makeTask()} isDragging={true} />);
    expect(container.firstChild).toHaveClass("task-card--dragging");
  });

  it("does not apply dragging class when isDragging is false", () => {
    const { container } = render(<TaskCard {...baseProps} task={makeTask()} isDragging={false} />);
    expect(container.firstChild).not.toHaveClass("task-card--dragging");
  });

  it("card element is draggable", () => {
    const { container } = render(<TaskCard {...baseProps} task={makeTask()} />);
    expect(container.firstChild).toHaveAttribute("draggable", "true");
  });
});
