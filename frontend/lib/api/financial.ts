import { apiFetch } from "./client";

export type OperatorBalanceDto = {
  userId: string;
  name: string;
  role: string;
  unpaidBalance: number;
  lifetimePaid: number;
  unpaidCount: number;
  lastCreditAt: string | null;
};

export type CreditDto = {
  id: string;
  orderId: string;
  orderNumber: number;
  stage: string;
  modelName: string;
  size: string;
  quantity: number;
  amount: number;
  occurredAt: string;
  paymentId: string | null;
};

export type PaymentDto = {
  id: string;
  number: number;
  userId: string;
  amount: number;
  paidAt: string;
  note: string | null;
  creditCount: number;
};

export type MiscCostDto = {
  id: string;
  description: string;
  amount: number;
  date: string;
  category: string | null;
};

export type SummaryDto = {
  totalUnpaid: number;
  totalPaid: number;
  totalMiscCosts: number;
  operatorsWithBalance: number;
};

export const getOperatorBalances = () =>
  apiFetch<OperatorBalanceDto[]>("/api/v1/financial/operators");

export const getCreditsForUser = (userId: string, status?: "paid" | "unpaid") =>
  apiFetch<CreditDto[]>(
    status
      ? `/api/v1/financial/operators/${userId}/credits?status=${status}`
      : `/api/v1/financial/operators/${userId}/credits`,
  );

export const createPayment = (params: {
  userId: string;
  creditIds: string[];
  note?: string;
}) =>
  apiFetch<PaymentDto>("/api/v1/financial/payments", {
    method: "POST",
    body: JSON.stringify(params),
  });

export const getPayments = (userId?: string) =>
  apiFetch<PaymentDto[]>(
    userId ? `/api/v1/financial/payments?userId=${userId}` : "/api/v1/financial/payments",
  );

export const getMiscCosts = () =>
  apiFetch<MiscCostDto[]>("/api/v1/financial/misc-costs");

export const createMiscCost = (params: {
  description: string;
  amount: number;
  date?: string;
  category?: string;
}) =>
  apiFetch<MiscCostDto>("/api/v1/financial/misc-costs", {
    method: "POST",
    body: JSON.stringify(params),
  });

export const getFinancialSummary = () =>
  apiFetch<SummaryDto>("/api/v1/financial/summary");
