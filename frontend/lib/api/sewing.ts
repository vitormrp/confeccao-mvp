import { apiFetch } from "./client";
import type { StageQueueItemDto } from "./stages";

/**
 * Sewing's queue is shaped identically to the generic stage queue, but it's
 * filtered server-side by the X-User-Id header to one seamstress's items.
 */
export const getSewingQueue = (userId: string) =>
  apiFetch<StageQueueItemDto[]>("/api/v1/sewing/queue", { userId });
