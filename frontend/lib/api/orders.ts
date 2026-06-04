import { apiFetch } from "./client";

export type Size = "P" | "M" | "G" | "GG";

export type OrderSummaryDto = {
  id: string;
  number: number;
  fabricCode: string;
  colorName: string;
  colorHex: string;
  status: string;
  createdAt: string;
  itemCount: number;
  plannedTotal: number;
};

export type OrderItemDto = {
  id: string;
  modelId: string;
  modelName: string;
  size: string;
  plannedQuantity: number;
};

export type OrderDetailDto = {
  id: string;
  number: number;
  fabricCode: string;
  colorName: string;
  colorHex: string;
  colorHasLining: boolean;
  instructions: string | null;
  status: string;
  createdAt: string;
  completedAt: string | null;
  items: OrderItemDto[];
};

export type CreateOrderRequest = {
  fabricCode: string;
  colorId: string;
  instructions?: string;
  items: Array<{ modelId: string; size: Size; plannedQuantity: number }>;
};

export const listOrders = (status?: string) =>
  apiFetch<OrderSummaryDto[]>(
    status ? `/api/v1/orders?status=${encodeURIComponent(status)}` : "/api/v1/orders",
  );

export const getOrder = (id: string) => apiFetch<OrderDetailDto>(`/api/v1/orders/${id}`);

export const createOrder = (request: CreateOrderRequest) =>
  apiFetch<OrderDetailDto>("/api/v1/orders", {
    method: "POST",
    body: JSON.stringify(request),
  });
