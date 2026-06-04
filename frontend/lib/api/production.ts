import { apiFetch } from "./client";

export type StageCountDto = {
  stage: string;
  inProcess: number;
  completed: number;
};

export type DashboardDto = {
  ordersActive: number;
  ordersCompleted: number;
  stages: StageCountDto[];
};

export const getDashboard = () => apiFetch<DashboardDto>("/api/v1/production/dashboard");
