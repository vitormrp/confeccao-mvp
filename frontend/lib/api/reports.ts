import { apiFetch } from "./client";

export type OperatorReportStageDto = {
  stage: string;
  pieces: number;
  amount: number;
};

export type OperatorReportDto = {
  userId: string;
  name: string;
  role: string;
  totalPieces: number;
  totalAmount: number;
  creditCount: number;
  byStage: OperatorReportStageDto[];
};

export type OperatorReportResponse = {
  windowDays: number;
  since: string;
  operators: OperatorReportDto[];
};

export type OrderThroughputDto = {
  windowDays: number;
  since: string;
  ordersCreated: number;
  ordersCompleted: number;
  averageLeadTimeDays: number | null;
  piecesStarted: number;
};

export const getOperatorReport = (days: number) =>
  apiFetch<OperatorReportResponse>(`/api/v1/reports/operators?days=${days}`);

export const getOrderThroughput = (days: number) =>
  apiFetch<OrderThroughputDto>(`/api/v1/reports/orders?days=${days}`);
