export type Role = "Operator" | "Supervisor";

export type Session = {
  token: string;
  username: string;
  role: Role;
};

export type Telemetry = {
  id: number;
  machineId: number;
  timestampUtc: string;
  temperature: number;
  pressure: number;
  speed: number;
  status: string;
};

export type Machine = {
  id: number;
  name: string;
  line: string;
  status: string;
  maxTemperature: number;
  maxPressure: number;
  minSpeed: number;
  maxSpeed: number;
  latestTelemetry: Telemetry | null;
};

export type Alert = {
  id: number;
  machineId: number;
  machineName: string;
  metric: string;
  value: number;
  threshold: number;
  message: string;
  createdAtUtc: string;
  acknowledgedAtUtc: string | null;
  acknowledgedBy: string | null;
};

export type Maintenance = {
  id: number;
  machineId: number;
  machineName: string;
  description: string;
  performedAtUtc: string;
  performedBy: string;
};

export type Weather = {
  location: string;
  temperatureCelsius: number;
  weatherCode: number;
  condition: string;
};
