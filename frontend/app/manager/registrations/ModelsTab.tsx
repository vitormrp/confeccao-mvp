"use client";

import { useEffect, useState } from "react";
import {
  createModel,
  getModels,
  updateModel,
  type ModelDto,
} from "@/lib/api/cadastros";
import { stageLabel } from "@/lib/labels";

// Stages presented in canonical order; cutting is always required and first.
const CANONICAL_STAGES = [
  "cutting",
  "interfacing",
  "sewing",
  "washing",
  "buttoning",
  "labeling",
  "pressing",
];

type Editing = {
  id: string | null;
  name: string;
  buttonCount: string;
  active: boolean;
  flow: Set<string>;
};

const empty: Editing = {
  id: null,
  name: "",
  buttonCount: "0",
  active: true,
  flow: new Set(["cutting", "sewing", "labeling", "pressing"]),
};

export function ModelsTab() {
  const [models, setModels] = useState<ModelDto[] | null>(null);
  const [editing, setEditing] = useState<Editing | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const refresh = async () => {
    try {
      setModels(await getModels());
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };
  useEffect(() => {
    refresh();
  }, []);

  const submit = async () => {
    if (!editing) return;
    setError(null);
    const buttonCount = parseInt(editing.buttonCount, 10);
    if (!Number.isFinite(buttonCount) || buttonCount < 0)
      return setError("Quantidade de botões inválida.");
    if (editing.flow.size === 0) return setError("Selecione ao menos uma etapa.");
    if (!editing.flow.has("cutting"))
      return setError("O fluxo precisa começar com Corte.");

    const flow = CANONICAL_STAGES.filter((s) => editing.flow.has(s));

    setSubmitting(true);
    try {
      const payload = {
        name: editing.name.trim(),
        buttonCount,
        flow,
        active: editing.active,
      };
      if (editing.id) await updateModel(editing.id, payload);
      else await createModel(payload);
      setEditing(null);
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSubmitting(false);
    }
  };

  const toggleActive = async (m: ModelDto) => {
    setError(null);
    try {
      await updateModel(m.id, {
        name: m.name,
        buttonCount: m.buttonCount,
        flow: m.flow,
        active: !m.active,
      });
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  return (
    <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
      <div className="flex items-center justify-between mb-3">
        <div className="text-sm font-semibold">Modelos cadastrados</div>
        {editing === null && (
          <button
            type="button"
            onClick={() => setEditing({ ...empty, flow: new Set(empty.flow) })}
            className="px-3 py-1 rounded-lg text-xs font-medium bg-accent text-white border border-accent hover:bg-blue-700"
          >
            + Novo modelo
          </button>
        )}
      </div>
      {error && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2 mb-3">
          {error}
        </div>
      )}
      {!models ? (
        <p className="text-xs text-text3">Carregando…</p>
      ) : (
        <table className="w-full text-xs">
          <thead>
            <tr className="text-text3 text-[11px] font-medium border-b border-border">
              <th className="text-left py-2 pr-2">Modelo</th>
              <th className="text-left py-2 pr-2">Botões</th>
              <th className="text-left py-2 pr-2">Fluxo</th>
              <th className="text-left py-2 pr-2">Status</th>
              <th className="text-right py-2 pr-2">Ações</th>
            </tr>
          </thead>
          <tbody>
            {models.map((m) => (
              <tr key={m.id} className="border-b border-border last:border-0 align-top">
                <td className="py-2 pr-2 font-medium">{m.name}</td>
                <td className="py-2 pr-2 font-mono">{m.buttonCount}</td>
                <td className="py-2 pr-2">
                  <div className="flex flex-wrap gap-1">
                    {m.flow.map((s, i) => (
                      <span
                        key={i}
                        className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-bg2 border border-border text-text2"
                      >
                        {i + 1}. {stageLabel(s)}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="py-2 pr-2">
                  {m.active ? "Ativo" : <span className="text-text3">Inativo</span>}
                </td>
                <td className="py-2 pr-2 text-right">
                  <button
                    type="button"
                    onClick={() =>
                      setEditing({
                        id: m.id,
                        name: m.name,
                        buttonCount: String(m.buttonCount),
                        active: m.active,
                        flow: new Set(m.flow),
                      })
                    }
                    className="text-[11px] text-accent hover:underline mr-2"
                  >
                    Editar
                  </button>
                  <button
                    type="button"
                    onClick={() => toggleActive(m)}
                    className="text-[11px] text-text3 hover:underline"
                  >
                    {m.active ? "Desativar" : "Ativar"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {editing !== null && (
        <div className="mt-4 border-t border-border pt-4">
          <div className="text-sm font-semibold mb-3">
            {editing.id ? "Editar modelo" : "Adicionar modelo"}
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-3">
            <div>
              <label className="text-xs text-text2 font-medium block mb-1">Nome</label>
              <input
                type="text"
                value={editing.name}
                onChange={(e) =>
                  setEditing((p) => (p ? { ...p, name: e.target.value } : p))
                }
                placeholder="Ex: Camisa social"
                className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg"
              />
            </div>
            <div>
              <label className="text-xs text-text2 font-medium block mb-1">
                Quantidade de botões
              </label>
              <input
                type="number"
                min={0}
                value={editing.buttonCount}
                onChange={(e) =>
                  setEditing((p) => (p ? { ...p, buttonCount: e.target.value } : p))
                }
                className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg font-mono"
              />
            </div>
            <div className="flex items-end">
              <label className="flex items-center gap-2 text-xs">
                <input
                  type="checkbox"
                  checked={editing.active}
                  onChange={(e) =>
                    setEditing((p) => (p ? { ...p, active: e.target.checked } : p))
                  }
                  className="accent-accent"
                />
                Ativo
              </label>
            </div>
          </div>
          <div className="mb-3">
            <label className="text-xs text-text2 font-medium block mb-1">
              Fluxo (etapas) — sempre na ordem canônica abaixo
            </label>
            <div className="flex flex-wrap gap-2">
              {CANONICAL_STAGES.map((s) => {
                const checked = editing.flow.has(s);
                const required = s === "cutting";
                return (
                  <label
                    key={s}
                    className={`flex items-center gap-2 px-2.5 py-1.5 rounded-lg text-xs cursor-pointer border ${
                      checked ? "bg-accent-light border-accent-border text-accent" : "bg-bg2 border-border text-text2"
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      disabled={required}
                      onChange={(e) =>
                        setEditing((p) => {
                          if (!p) return p;
                          const next = new Set(p.flow);
                          if (e.target.checked) next.add(s);
                          else next.delete(s);
                          return { ...p, flow: next };
                        })
                      }
                      className="accent-accent"
                    />
                    {stageLabel(s)}
                    {required && <span className="text-[10px] text-text3">(obrigatório)</span>}
                  </label>
                );
              })}
            </div>
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={submitting}
              onClick={submit}
              className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-accent text-white border border-accent hover:bg-blue-700 disabled:opacity-60"
            >
              {submitting ? "Salvando..." : editing.id ? "Atualizar" : "Adicionar"}
            </button>
            <button
              type="button"
              onClick={() => setEditing(null)}
              className="px-3.5 py-1.5 rounded-lg text-xs text-text2 hover:bg-bg2"
            >
              Cancelar
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
