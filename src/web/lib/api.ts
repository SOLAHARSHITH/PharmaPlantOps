import { clearSession, loadSession } from "./session";

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const session = loadSession();
  const headers = new Headers(options.headers);
  if (options.body) {
    headers.set("Content-Type", "application/json");
  }
  if (session?.token) {
    headers.set("Authorization", `Bearer ${session.token}`);
  }

  const response = await fetch(`${apiUrl}${path}`, { ...options, headers });
  if (response.status === 401) {
    clearSession();
    if (typeof window !== "undefined" && window.location.pathname !== "/login") {
      window.location.href = "/login";
    }
    throw new ApiError(401, "Sign in again.");
  }

  if (!response.ok) {
    let message = response.statusText;
    try {
      const problem = (await response.json()) as { error?: string };
      if (problem.error) {
        message = problem.error;
      }
    } catch {
      message = await response.text();
    }
    throw new ApiError(response.status, message || "Request failed.");
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
