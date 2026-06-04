"use client";

import { useEffect, useState } from "react";
import {
  createMiscCost,
  getMiscCosts,
  type MiscCostDto,
} from "@/lib/api/financial";
import { formatBRL } from "@/lib/labels";

export function CostsClient() {
  const [costs, setCosts] = useState<MiscCostDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [description, setDescription] = useState("");
  const [amount, setAmount] = useState("");
  const [category, setCategory] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const refresh = async () => {
    try {
      const data = await getMiscCosts();
      setCosts(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  useEffect(() => {
    refresh();
  }, []);

  const submit = async () => {
    setError(null);
    const amt = parseFloat(amount.replace(",", "."));
    if (!description.trim()) return setError("Informe a descrição.");
    if (!Number.isFinite(amt) || amt <= 0) return setError("Valor inválido.");
    setSubmitting(true);
    try {
      await createMiscCost({
        description: description.trim(),
        amount: amt,
        category: category.trim() || undefined,
      });
      setDescription("");
      setAmount("");
      setCategory("");
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-3">
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="text-sm font-semibold mb-3">Novo custo avulso</div>
        {error && (
          <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2 mb-3">
            {error}
          </div>
        )}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-3">
          <div>
            <label className="text-xs text-text2 font-medium block mb-1">Descrição</label>
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Ex: Compra de tecido"
              className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg"
            />
          </div>
          <div>
            <label className="text-xs text-text2 font-medium block mb-1">Valor (R$)</label>
            <input
              type="text"
              inputMode="decimal"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="0,00"
              className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg font-mono"
            />
          </div>
          <div>
            <label className="text-xs text-text2 font-medium block mb-1">Categoria (opcional)</label>
            <input
              type="text"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              placeholder="Ex: Insumos"
              className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg"
            />
          </div>
        </div>
        <button
          type="button"
          disabled={submitting}
          onClick={submit}
          className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-accent text-white border border-accent hover:bg-blue-700 transition-colors disabled:opacity-60"
        >
          {submitting ? "Salvando..." : "Registrar custo"}
        </button>
      </div>

      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="text-sm font-semibold mb-3">Custos registrados</div>
        {!costs ? (
          <p className="text-xs text-text3">Carregando…</p>
        ) : costs.length === 0 ? (
          <p className="text-xs text-text3">Nenhum custo avulso registrado.</p>
        ) : (
          <table className="w-full text-xs">
            <thead>
              <tr className="text-text3 text-[11px] border-b border-border">
                <th className="py-2 pr-2 text-left">Descrição</th>
                <th className="py-2 pr-2 text-left">Categoria</th>
                <th className="py-2 pr-2 text-right">Valor</th>
                <th className="py-2 pr-2 text-left">Data</th>
              </tr>
            </thead>
            <tbody>
              {costs.map((c) => (
                <tr key={c.id} className="border-b border-border last:border-0">
                  <td className="py-2 pr-2 font-medium">{c.description}</td>
                  <td className="py-2 pr-2 text-text2">{c.category ?? "—"}</td>
                  <td className="py-2 pr-2 text-right font-mono">{formatBRL(c.amount)}</td>
                  <td className="py-2 pr-2 text-text3 text-[11px]">
                    {new Date(c.date).toLocaleDateString("pt-BR")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
