import { describe, it, expect, vi, beforeEach } from "vitest";
import { api } from "../../api/client";

const mockFetch = vi.fn();
vi.stubGlobal("fetch", mockFetch);

function mockResponse<T>(body: T, status = 200) {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    status,
    json: async () => body,
  });
}

function mockEmpty(status = 204) {
  mockFetch.mockResolvedValueOnce({ ok: true, status, json: async () => undefined });
}

function mockError(status = 500) {
  mockFetch.mockResolvedValueOnce({
    ok: false,
    status,
    statusText: "Internal Server Error",
  });
}

beforeEach(() => mockFetch.mockReset());

describe("api.projects", () => {
  it("list() calls GET /api/projects", async () => {
    mockResponse([]);
    await api.projects.list();
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/projects",
      expect.objectContaining({ headers: expect.any(Object) })
    );
  });

  it("list() returns the parsed array", async () => {
    const projects = [{ id: "1", name: "Alpha", description: "", createdAt: "" }];
    mockResponse(projects);
    const result = await api.projects.list();
    expect(result).toEqual(projects);
  });

  it("create() calls POST /api/projects with body", async () => {
    mockResponse({ id: "1", name: "Alpha", description: "", createdAt: "" });
    await api.projects.create({ name: "Alpha", description: "" });
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/projects",
      expect.objectContaining({ method: "POST", body: JSON.stringify({ name: "Alpha", description: "" }) })
    );
  });

  it("delete() calls DELETE /api/projects/:id", async () => {
    mockEmpty();
    await api.projects.delete("abc");
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/projects/abc",
      expect.objectContaining({ method: "DELETE" })
    );
  });

  it("throws on non-ok response", async () => {
    mockError();
    await expect(api.projects.list()).rejects.toThrow("500");
  });
});

describe("api.tasks", () => {
  it("list() calls GET /api/projects/:id/tasks", async () => {
    mockResponse([]);
    await api.tasks.list("proj-1");
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/projects/proj-1/tasks",
      expect.any(Object)
    );
  });

  it("create() sends task payload", async () => {
    const task = { id: "t1", projectId: "proj-1", title: "T", description: "", status: "todo" as const, createdAt: "" };
    mockResponse(task, 201);
    await api.tasks.create("proj-1", { title: "T", description: "" });
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/projects/proj-1/tasks",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("updateStatus() sends PATCH with status body", async () => {
    const task = { id: "t1", projectId: "p1", title: "T", description: "", status: "done" as const, createdAt: "" };
    mockResponse(task);
    await api.tasks.updateStatus("p1", "t1", "done");
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/projects/p1/tasks/t1",
      expect.objectContaining({
        method: "PATCH",
        body: JSON.stringify({ status: "done" }),
      })
    );
  });

  it("delete() calls DELETE /api/projects/:pid/tasks/:tid", async () => {
    mockEmpty();
    await api.tasks.delete("p1", "t1");
    expect(mockFetch).toHaveBeenCalledWith(
      "/api/projects/p1/tasks/t1",
      expect.objectContaining({ method: "DELETE" })
    );
  });
});
