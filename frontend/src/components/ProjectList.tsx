import { useState } from "react";
import type { Project } from "../api/client";

interface Props {
  projects: Project[];
  selectedId: string | null;
  onSelect: (id: string) => void;
  onCreate: (name: string, description: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
}

export function ProjectList({ projects, selectedId, onSelect, onCreate, onDelete }: Props) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [creating, setCreating] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    setCreating(true);
    await onCreate(name.trim(), description.trim());
    setName("");
    setDescription("");
    setCreating(false);
  }

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <span className="logo">TaskBoard</span>
      </div>

      <nav className="project-nav">
        <p className="nav-label">Projects</p>
        {projects.length === 0 && (
          <p className="empty-hint">No projects yet</p>
        )}
        {projects.map((p) => (
          <div
            key={p.id}
            className={`project-item ${p.id === selectedId ? "active" : ""}`}
            onClick={() => onSelect(p.id)}
          >
            <span className="project-name">{p.name}</span>
            <button
              className="icon-btn delete-btn"
              onClick={(e) => {
                e.stopPropagation();
                onDelete(p.id);
              }}
              title="Delete project"
            >
              ×
            </button>
          </div>
        ))}
      </nav>

      <form className="new-project-form" onSubmit={handleSubmit}>
        <p className="nav-label">New Project</p>
        <input
          className="input"
          placeholder="Project name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
        <input
          className="input"
          placeholder="Description (optional)"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <button className="btn btn-primary" type="submit" disabled={creating}>
          {creating ? "Creating…" : "Create"}
        </button>
      </form>
    </aside>
  );
}
