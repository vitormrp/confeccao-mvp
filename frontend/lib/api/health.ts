import { apiFetch } from "./client";

export type HealthResponse = {
  status: string;
  database: string;
  timestamp: string;
};

export function getHealth() {
  return apiFetch<HealthResponse>("/api/v1/health");
}
