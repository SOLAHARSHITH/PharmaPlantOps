"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { loadSession } from "@/lib/session";
import type { Alert } from "@/lib/types";

export default function AlertsPage() {
  const router = useRouter();
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!loadSession()) {
      router.replace("/login");
      return;
    }
    api<Alert[]>("/api/alerts")
      .then(setAlerts)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load alerts."));
  }, [router]);

  return (
    <>
      <h1>Alerts</h1>
      <p className="lede">Threshold breaches from machine telemetry.</p>
      {error ? <p className="error">{error}</p> : null}
      <div className="panel">
        <table>
          <thead>
            <tr>
              <th>When</th>
              <th>Machine</th>
              <th>Metric</th>
              <th>Value</th>
              <th>Limit</th>
              <th>State</th>
            </tr>
          </thead>
          <tbody>
            {alerts.map((alert) => (
              <tr key={alert.id}>
                <td>{new Date(alert.createdAtUtc).toLocaleString()}</td>
                <td><Link href={`/alerts/${alert.id}`}>{alert.machineName}</Link></td>
                <td>{alert.metric}</td>
                <td>{alert.value}</td>
                <td>{alert.threshold}</td>
                <td>
                  <span className={`badge ${alert.acknowledgedAtUtc ? "ok" : "open"}`}>
                    {alert.acknowledgedAtUtc ? "Acknowledged" : "Open"}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
