import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ProjectList } from "../../components/ProjectList";
import { makeProject } from "../testUtils";

const defaultProps = {
  projects: [],
  selectedId: null,
  onSelect: vi.fn(),
  onCreate: vi.fn().mockResolvedValue(undefined),
  onDelete: vi.fn().mockResolvedValue(undefined),
};

describe("ProjectList", () => {
  it("renders the TaskBoard logo", () => {
    render(<ProjectList {...defaultProps} />);
    expect(screen.getByText("TaskBoard")).toBeInTheDocument();
  });

  it("shows empty hint when there are no projects", () => {
    render(<ProjectList {...defaultProps} projects={[]} />);
    expect(screen.getByText(/no projects/i)).toBeInTheDocument();
  });

  it("renders each project name", () => {
    const projects = [
      makeProject({ name: "Alpha" }),
      makeProject({ name: "Beta" }),
    ];
    render(<ProjectList {...defaultProps} projects={projects} />);
    expect(screen.getByText("Alpha")).toBeInTheDocument();
    expect(screen.getByText("Beta")).toBeInTheDocument();
  });

  it("calls onSelect with the project id when clicked", () => {
    const onSelect = vi.fn();
    const project = makeProject({ id: "proj-abc", name: "Clickable" });
    render(<ProjectList {...defaultProps} projects={[project]} onSelect={onSelect} />);

    fireEvent.click(screen.getByText("Clickable"));

    expect(onSelect).toHaveBeenCalledWith("proj-abc");
  });

  it("applies active class to the selected project", () => {
    const project = makeProject({ id: "sel-1", name: "Selected" });
    render(
      <ProjectList
        {...defaultProps}
        projects={[project]}
        selectedId="sel-1"
      />
    );
    const item = screen.getByText("Selected").closest(".project-item");
    expect(item).toHaveClass("active");
  });

  it("calls onDelete when the × button is clicked", async () => {
    const onDelete = vi.fn().mockResolvedValue(undefined);
    const project = makeProject({ id: "del-1", name: "Delete me" });
    render(
      <ProjectList {...defaultProps} projects={[project]} onDelete={onDelete} />
    );
    fireEvent.click(screen.getByTitle("Delete project"));
    expect(onDelete).toHaveBeenCalledWith("del-1");
  });

  it("calls onCreate with name and description on form submit", async () => {
    const onCreate = vi.fn().mockResolvedValue(undefined);
    render(<ProjectList {...defaultProps} onCreate={onCreate} />);

    await userEvent.type(screen.getByPlaceholderText("Project name"), "New Project");
    fireEvent.click(screen.getByText("Create"));

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith("New Project", "")
    );
  });

  it("does not call onCreate when name is empty", () => {
    const onCreate = vi.fn().mockResolvedValue(undefined);
    render(<ProjectList {...defaultProps} onCreate={onCreate} />);
    fireEvent.click(screen.getByText("Create"));
    expect(onCreate).not.toHaveBeenCalled();
  });
});
