import { apiFetch } from "./client";

export type StageQueueItemDto = {
  pipelineItemId: string;
  orderId: string;
  orderNumber: number;
  modelName: string;
  size: string;
  colorName: string;
  colorHex: string;
  colorHasLining: boolean;
  fabricCode: string;
  quantityTotal: number;
  quantityDone: number;
  dispatchedAt: string | null;
};

export type CompleteItemResponse = {
  pipelineItemId: string;
  quantityCompleted: number;
  quantityDone: number;
  quantityTotal: number;
  stageDone: boolean;
  nextStage: string | null;
  orderCompleted: boolean;
};

export const getStageQueue = (stage: string) =>
  apiFetch<StageQueueItemDto[]>(`/api/v1/stages/${encodeURIComponent(stage)}/queue`);

export const completeStageItem = (
  stage: string,
  pipelineItemId: string,
  userId: string,
  quantity: number,
) =>
  apiFetch<CompleteItemResponse>(
    `/api/v1/stages/${encodeURIComponent(stage)}/items/${pipelineItemId}/complete`,
    {
      method: "POST",
      userId,
      body: JSON.stringify({ quantity }),
    },
  );
