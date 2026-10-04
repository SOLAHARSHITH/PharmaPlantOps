"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError } from "@/lib/api";
import { saveSession } from "@/lib/session";
import type { Role } from "@/lib/types";

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export default function LoginPage() {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError("");
    setPending(true);
    try {
      const response = await fetch(`${apiUrl}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });
      if (!response.ok) {
        throw new ApiError(response.status, "Invalid username or password.");
      }
      const body = (await response.json()) as { token: string; username: string; role: Role };
      saveSession({ token: body.token, username: body.username, role: body.role });
      router.push("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Sign in failed.");
    } finally {
      setPending(false);
    }
  }

  return (
    <>
      <h1>Sign in</h1>
      <p className="lede">Operators and supervisors use the accounts provided for this plant.</p>
      <form className="panel form" onSubmit={onSubmit}>
        <label>
          Username
          <input value={username} onChange={(event) => setUsername(event.target.value)} autoComplete="username" required />
        </label>
        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
            required
          />
        </label>
        {error ? <p className="error">{error}</p> : null}
        <button className="primary" type="submit" disabled={pending}>
          {pending ? "Signing in..." : "Sign in"}
        </button>
      </form>
    </>
  );
}
