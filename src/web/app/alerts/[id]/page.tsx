"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { loadSession } from "@/lib/session";
import type { Alert } from "@/lib/types";

export default function AlertDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const [alert, setAlert] = useState<Alert | null>(null);
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  useEffect(() => {
    if (!loadSession()) {
      router.replace("/login");
      return;
    }
    api<Alert>(`/api/alerts/${params.id}`)
      .then(setAlert)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load the alert."));
  }, [params.id, router]);

  async function acknowledge() {
    setPending(true);
    setError("");
    try {
      setAlert(await api<Alert>(`/api/alerts/${params.id}/acknowledge`, { method: "POST" }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not acknowledge the alert.");
    } finally {
      setPending(false);
    }
  }

  if (!alert && !error) {
    return <p>Loading alert...</p>;
  }

  return (
    <>
      <p><Link href="/alerts">Back to alerts</Link></p>
      <h1>Alert {params.id}</h1>
      {error ? <p className="error">{error}</p> : null}
      {alert ? (
        <section className="panel">
          <p>{alert.message}</p>
          <p className="meta">Machine: {alert.machineName}</p>
          <p className="meta">Metric: {alert.metric}</p>
          <p className="meta">Value: {alert.value} · Limit: {alert.threshold}</p>
          <p className="meta">Raised: {new Date(alert.createdAtUtc).toLocaleString()}</p>
          {alert.acknowledgedAtUtc ? (
            <p className="meta">
              Acknowledged by {alert.acknowledgedBy} at {new Date(alert.acknowledgedAtUtc).toLocaleString()}
            </p>
          ) : (
            <button className="primary" type="button" onClick={acknowledge} disabled={pending}>
              {pending ? "Saving..." : "Acknowledge"}
            </button>
          )}
        </section>
      ) : null}
    </>
  );
}
