"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { type ColorDto, type ModelDto } from "@/lib/api/cadastros";
import { createOrder, type Size } from "@/lib/api/orders";

const SIZES: Size[] = ["P", "M", "G", "GG"];

export function NewOrderForm({
  colors,
  models,
}: {
  colors: ColorDto[];
  models: ModelDto[];
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [fabricCode, setFabricCode] = useState("");
  const [colorId, setColorId] = useState(colors[0]?.id ?? "");
  const [instructions, setInstructions] = useState("");
  const [selectedModels, setSelectedModels] = useState<Set<string>>(new Set());
  // Map of (modelId,size) -> quantity entered.
  const [qtys, setQtys] = useState<Record<string, number>>({});
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const toggleModel = (id: string) => {
    setSelectedModels((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const setQty = (modelId: string, size: Size, value: string) => {
    const n = parseInt(value, 10);
    setQtys((prev) => ({
      ...prev,
      [`${modelId}|${size}`]: Number.isFinite(n) && n > 0 ? n : 0,
    }));
  };

  const reset = () => {
    setFabricCode("");
    setInstructions("");
    setSelectedModels(new Set());
    setQtys({});
    setError(null);
  };

  const submit = async () => {
    setError(null);
    if (!fabricCode.trim()) return setError("Informe o código do tecido.");
    if (!colorId) return setError("Selecione uma cor.");
    if (selectedModels.size === 0) return setError("Selecione ao menos um modelo.");

    const items = Array.from(selectedModels).flatMap((modelId) =>
      SIZES.flatMap((size) => {
        const q = qtys[`${modelId}|${size}`] ?? 0;
        return q > 0 ? [{ modelId, size, plannedQuantity: q }] : [];
      }),
    );
    if (items.length === 0) return setError("Informe ao menos uma quantidade > 0.");

    setSubmitting(true);
    try {
      await createOrder({
        fabricCode: fabricCode.trim(),
        colorId,
        instructions: instructions.trim() || undefined,
        items,
      });
      reset();
      setOpen(false);
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSubmitting(false);
    }
  };

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-accent text-white border border-accent hover:bg-blue-700 transition-colors"
      >
        + Nova ordem
      </button>
    );
  }

  return (
    <div className="border border-border bg-card rounded-2xl p-5 shadow-card mt-3">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mb-3">
        <div>
          <label className="text-xs text-text2 font-medium block mb-1">
            Código do tecido
          </label>
          <input
            type="text"
            value={fabricCode}
            onChange={(e) => setFabricCode(e.target.value)}
            placeholder="Ex: TEC-001"
            className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg"
          />
        </div>
        <div>
          <label className="text-xs text-text2 font-medium block mb-1">Cor</label>
          <select
            value={colorId}
            onChange={(e) => setColorId(e.target.value)}
            className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg bg-card"
          >
            {colors.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
                {c.hasLining ? " (c/ forro)" : ""}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="mb-3">
        <label className="text-xs text-text2 font-medium block mb-1">
          Instruções para o cortador (opcional)
        </label>
        <textarea
          value={instructions}
          onChange={(e) => setInstructions(e.target.value)}
          rows={2}
          className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg"
        />
      </div>

      <div className="text-[10px] uppercase tracking-wider font-semibold text-text3 mb-2">
        Modelos
      </div>
      <div className="flex flex-wrap gap-2 mb-3">
        {models.map((m) => (
          <label
            key={m.id}
            className="flex items-center gap-2 px-2.5 py-1.5 rounded-lg bg-bg2 border border-border text-xs cursor-pointer"
          >
            <input
              type="checkbox"
              checked={selectedModels.has(m.id)}
              onChange={() => toggleModel(m.id)}
              className="accent-accent"
            />
            {m.name}
          </label>
        ))}
      </div>

      {selectedModels.size > 0 && (
        <div className="overflow-x-auto mb-4">
          <table className="text-xs w-full">
            <thead>
              <tr>
                <th className="text-left bg-bg2 border border-border px-2 py-1.5 text-[11px]">
                  Modelo
                </th>
                {SIZES.map((s) => (
                  <th
                    key={s}
                    className="bg-bg2 border border-border px-2 py-1.5 text-[11px]"
                  >
                    {s}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {models
                .filter((m) => selectedModels.has(m.id))
                .map((m) => (
                  <tr key={m.id}>
                    <td className="border border-border px-2 py-1.5 font-medium">
                      {m.name}
                    </td>
                    {SIZES.map((s) => (
                      <td key={s} className="border border-border px-1 py-1 text-center">
                        <input
                          type="number"
                          min={0}
                          value={qtys[`${m.id}|${s}`] || ""}
                          onChange={(e) => setQty(m.id, s, e.target.value)}
                          placeholder=""
                          className="w-14 text-center font-mono text-xs px-1 py-0.5 border border-border2 rounded"
                        />
                      </td>
                    ))}
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}

      {error && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2 mb-3">
          {error}
        </div>
      )}

      <div className="flex gap-2">
        <button
          type="button"
          disabled={submitting}
          onClick={submit}
          className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-accent text-white border border-accent hover:bg-blue-700 transition-colors disabled:opacity-60"
        >
          {submitting ? "Enviando..." : "Enviar para cortador"}
        </button>
        <button
          type="button"
          disabled={submitting}
          onClick={() => {
            reset();
            setOpen(false);
          }}
          className="px-3.5 py-1.5 rounded-lg text-xs text-text2 hover:bg-bg2 transition-colors"
        >
          Cancelar
        </button>
      </div>
    </div>
  );
}
