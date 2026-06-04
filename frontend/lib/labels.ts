/**
 * UI display strings (Portuguese) for code values that come from the backend in
 * English. Keep this map in sync with the backend's enum string serialization.
 */

export const stageLabels: Record<string, string> = {
  cutting: "Corte",
  interfacing: "Entretela",
  sewing: "Costura",
  washing: "Lavanderia",
  buttoning: "Botão",
  labeling: "Etiqueta",
  pressing: "Passadeira",
};

export const roleLabels: Record<string, string> = {
  manager: "Gerente",
  cutting: "Cortador",
  interfacing: "Entretela",
  sewing: "Costureira",
  washing: "Lavanderia",
  buttoning: "Botão",
  labeling: "Etiqueta",
  pressing: "Passadeira",
};

export function stageLabel(code: string): string {
  return stageLabels[code] ?? code;
}

export function roleLabel(code: string): string {
  return roleLabels[code] ?? code;
}

export const orderStatusLabels: Record<string, string> = {
  "awaiting-cutting": "Aguardando corte",
  "in-production": "Em produção",
  "completed": "Concluída",
};

export const pipelineStatusLabels: Record<string, string> = {
  "awaiting-dispatch": "Pronto para envio",
  "in-progress": "Em andamento",
  "done": "Concluído",
};

export function orderStatusLabel(code: string): string {
  return orderStatusLabels[code] ?? code;
}

export function pipelineStatusLabel(code: string): string {
  return pipelineStatusLabels[code] ?? code;
}

export function formatBRL(value: number | null | undefined): string {
  if (value == null) return "—";
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}
