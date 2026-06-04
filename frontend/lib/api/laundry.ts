import { apiFetch } from "./client";

export type LaundryPackageItemDto = {
  pipelineItemId: string;
  orderId: string;
  orderNumber: number;
  modelName: string;
  size: string;
  colorName: string;
  colorHex: string;
  quantity: number;
};

export type LaundryPackageDto = {
  packageId: string;
  number: number;
  sentAt: string;
  totalQuantity: number;
  items: LaundryPackageItemDto[];
};

export const getLaundryQueue = () =>
  apiFetch<LaundryPackageDto[]>("/api/v1/laundry/queue");

export type BundlePackageResponse = {
  packageId: string;
  itemCount: number;
  totalQuantity: number;
};

export const bundleLaundryPackage = (pipelineItemIds: string[]) =>
  apiFetch<BundlePackageResponse>("/api/v1/dispatch/laundry-package", {
    method: "POST",
    body: JSON.stringify({ pipelineItemIds }),
  });

export type CompletePackageResponse = {
  packageId: string;
  itemCount: number;
  totalQuantity: number;
  completedOrderIds: string[];
};

export const completeLaundryPackage = (packageId: string, userId: string) =>
  apiFetch<CompletePackageResponse>(
    `/api/v1/laundry/packages/${packageId}/complete`,
    { method: "POST", userId },
  );
