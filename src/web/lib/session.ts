import type { Session } from "./types";

const storageKey = "plantops.session";

export function loadSession(): Session | null {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = localStorage.getItem(storageKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as Session;
  } catch {
    return null;
  }
}

export function saveSession(session: Session) {
  localStorage.setItem(storageKey, JSON.stringify(session));
}

export function clearSession() {
  localStorage.removeItem(storageKey);
}
