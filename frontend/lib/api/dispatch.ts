import { apiFetch } from "./client";

export type AwaitingItemDto = {
  pipelineItemId: string;
  orderId: string;
  orderNumber: number;
  stage: string;
  dispatchKind: "generic" | "per-user" | "package" | "none";
  quantity: number;
  modelName: string;
  size: string;
  colorName: string;
  colorHex: string;
  fabricCode: string;
  colorHasLining: boolean;
  awaitingSince: string;
};

export const getAwaiting = () =>
  apiFetch<AwaitingItemDto[]>("/api/v1/dispatch/awaiting");

export const dispatchGeneric = (pipelineItemId: string) =>
  apiFetch<{ pipelineItemId: string; stage: string }>("/api/v1/dispatch/generic", {
    method: "POST",
    body: JSON.stringify({ pipelineItemId }),
  });

export type DispatchSewingResponse = {
  dispatchedItemId: string;
  quantity: number;
  wasSplit: boolean;
  remainingItemId: string | null;
  remainingQuantity: number;
};

export const dispatchSewing = (params: {
  pipelineItemId: string;
  userId: string;
  quantity: number;
}) =>
  apiFetch<DispatchSewingResponse>("/api/v1/dispatch/sewing", {
    method: "POST",
    body: JSON.stringify(params),
  });
