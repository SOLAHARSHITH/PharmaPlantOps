"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { loadSession } from "@/lib/session";
import type { Machine, Maintenance, Weather } from "@/lib/types";

export default function DashboardPage() {
  const router = useRouter();
  const [machines, setMachines] = useState<Machine[]>([]);
  const [maintenance, setMaintenance] = useState<Maintenance[]>([]);
  const [weather, setWeather] = useState<Weather | null>(null);
  const [weatherError, setWeatherError] = useState("");
  const [openAlerts, setOpenAlerts] = useState(0);
  const [role, setRole] = useState("");
  const [machineId, setMachineId] = useState(0);
  const [description, setDescription] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  async function load() {
    const [machineRows, maintenanceRows, alerts] = await Promise.all([
      api<Machine[]>("/api/machines"),
      api<Maintenance[]>("/api/maintenance"),
      api<{ acknowledgedAtUtc: string | null }[]>("/api/alerts"),
    ]);
    setMachines(machineRows);
    setMaintenance(maintenanceRows);
    setOpenAlerts(alerts.filter((alert) => !alert.acknowledgedAtUtc).length);
    if (machineRows.length > 0 && machineId === 0) {
      setMachineId(machineRows[0].id);
    }
    try {
      setWeather(await api<Weather>("/api/weather"));
      setWeatherError("");
    } catch (err) {
      setWeather(null);
      setWeatherError(err instanceof Error ? err.message : "Weather is unavailable.");
    }
  }

  useEffect(() => {
    const session = loadSession();
    if (!session) {
      router.replace("/login");
      return;
    }
    setRole(session.role);
    load()
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load the dashboard."))
      .finally(() => setLoading(false));
  }, [router]);

  async function createMaintenance(event: FormEvent) {
    event.preventDefault();
    setError("");
    try {
      await api("/api/maintenance", {
        method: "POST",
        body: JSON.stringify({ machineId, description }),
      });
      setDescription("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save maintenance.");
    }
  }

  if (loading) {
    return <p>Loading plant status...</p>;
  }

  return (
    <>
      <div className="stack">
        <div>
          <h1>Plant health</h1>
          <p className="lede">{openAlerts} open alert{openAlerts === 1 ? "" : "s"}. <Link href="/alerts">Review alerts</Link></p>
        </div>
        <div className="panel">
          <strong>Site weather</strong>
          {weather ? (
            <p className="meta">
              {weather.location}: {weather.temperatureCelsius.toFixed(1)}°C, {weather.condition}
            </p>
          ) : (
            <p className="error">{weatherError || "Weather is unavailable."}</p>
          )}
        </div>
      </div>
      {error ? <p className="error">{error}</p> : null}
      <section className="grid">
        {machines.map((machine) => (
          <article className="panel machine" key={machine.id}>
            <h2>{machine.name}</h2>
            <p className="meta">
              {machine.line} · <span className={`badge ${machine.status.toLowerCase()}`}>{machine.status}</span>
            </p>
            {machine.latestTelemetry ? (
              <ul className="readings">
                <li>
                  Temperature
                  <strong>{machine.latestTelemetry.temperature.toFixed(1)}°C</strong>
                </li>
                <li>
                  Pressure
                  <strong>{machine.latestTelemetry.pressure.toFixed(1)}</strong>
                </li>
                <li>
                  Speed
                  <strong>{machine.latestTelemetry.speed.toFixed(0)}</strong>
                </li>
              </ul>
            ) : (
              <p className="meta">No readings yet.</p>
            )}
          </article>
        ))}
      </section>
      <section className="panel">
        <h2>Maintenance</h2>
        <table>
          <thead>
            <tr>
              <th>When</th>
              <th>Machine</th>
              <th>Work</th>
              <th>By</th>
            </tr>
          </thead>
          <tbody>
            {maintenance.map((record) => (
              <tr key={record.id}>
                <td>{new Date(record.performedAtUtc).toLocaleString()}</td>
                <td>{record.machineName}</td>
                <td>{record.description}</td>
                <td>{record.performedBy}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {role === "Supervisor" ? (
          <form onSubmit={createMaintenance} className="form">
            <h3>Add a record</h3>
            <label>
              Machine
              <select value={machineId} onChange={(event) => setMachineId(Number(event.target.value))}>
                {machines.map((machine) => (
                  <option key={machine.id} value={machine.id}>
                    {machine.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Description
              <textarea value={description} onChange={(event) => setDescription(event.target.value)} required maxLength={500} />
            </label>
            <button className="primary" type="submit">Save record</button>
          </form>
        ) : null}
      </section>
    </>
  );
}
