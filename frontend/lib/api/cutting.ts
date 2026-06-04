import { apiFetch } from "./client";

export type CutterOrderItemDto = {
  orderItemId: string;
  pipelineItemId: string;
  modelName: string;
  size: string;
  plannedQuantity: number;
};

export type CutterOrderDto = {
  orderId: string;
  number: number;
  fabricCode: string;
  colorName: string;
  colorHex: string;
  colorHasLining: boolean;
  instructions: string | null;
  items: CutterOrderItemDto[];
};

export const getCuttingQueue = (userId: string) =>
  apiFetch<CutterOrderDto[]>("/api/v1/cutting/queue", { userId });

export type RegisterCutRequest = {
  cuts: Array<{ orderItemId: string; quantityCut: number }>;
};

export type RegisterCutResponse = {
  orderId: string;
  totalCut: number;
  spawnedNextStageItems: number;
};

export const registerCut = (orderId: string, userId: string, request: RegisterCutRequest) =>
  apiFetch<RegisterCutResponse>(`/api/v1/cutting/orders/${orderId}/register`, {
    method: "POST",
    userId,
    body: JSON.stringify(request),
  });
