import { apiFetch } from "./client";

export type ColorDto = {
  id: string;
  name: string;
  hexCode: string;
  hasLining: boolean;
  active: boolean;
};

export type ModelDto = {
  id: string;
  name: string;
  buttonCount: number;
  active: boolean;
  flow: string[];
};

export type UserDto = {
  id: string;
  name: string;
  role: string;
  active: boolean;
};

export type PriceTierDto = {
  id: string;
  upToQuantity: number;
  amount: number;
};

export type PriceDto = {
  id: string;
  userId: string;
  amount: number;
  liningExtra: number | null;
  interfacingExtra: number | null;
  coveredButtonPrice: number | null;
  readyButtonPrice: number | null;
  effectiveFrom: string;
  note: string | null;
  tiers: PriceTierDto[];
};

export const getColors = () => apiFetch<ColorDto[]>("/api/v1/cadastros/colors");
export const getModels = () => apiFetch<ModelDto[]>("/api/v1/cadastros/models");
export const getUsers = (role?: string) =>
  apiFetch<UserDto[]>(
    role ? `/api/v1/cadastros/users?role=${encodeURIComponent(role)}` : "/api/v1/cadastros/users",
  );
export const getPrices = (userId?: string) =>
  apiFetch<PriceDto[]>(
    userId ? `/api/v1/cadastros/prices?userId=${encodeURIComponent(userId)}` : "/api/v1/cadastros/prices",
  );

// ---- Mutations ----

export type ColorUpsert = {
  name: string;
  hexCode: string;
  hasLining: boolean;
  active?: boolean;
};
export const createColor = (req: ColorUpsert) =>
  apiFetch<ColorDto>("/api/v1/cadastros/colors", {
    method: "POST",
    body: JSON.stringify(req),
  });
export const updateColor = (id: string, req: ColorUpsert) =>
  apiFetch<ColorDto>(`/api/v1/cadastros/colors/${id}`, {
    method: "PUT",
    body: JSON.stringify(req),
  });

export type ModelUpsert = {
  name: string;
  buttonCount: number;
  flow: string[];
  active?: boolean;
};
export const createModel = (req: ModelUpsert) =>
  apiFetch<ModelDto>("/api/v1/cadastros/models", {
    method: "POST",
    body: JSON.stringify(req),
  });
export const updateModel = (id: string, req: ModelUpsert) =>
  apiFetch<ModelDto>(`/api/v1/cadastros/models/${id}`, {
    method: "PUT",
    body: JSON.stringify(req),
  });

export type UserUpsert = { name: string; role: string; active?: boolean };
export const createUser = (req: UserUpsert) =>
  apiFetch<UserDto>("/api/v1/cadastros/users", {
    method: "POST",
    body: JSON.stringify(req),
  });
export const updateUser = (id: string, req: UserUpsert) =>
  apiFetch<UserDto>(`/api/v1/cadastros/users/${id}`, {
    method: "PUT",
    body: JSON.stringify(req),
  });
