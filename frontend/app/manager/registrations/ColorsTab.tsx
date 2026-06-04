"use client";

import { useEffect, useState } from "react";
import {
  createColor,
  getColors,
  updateColor,
  type ColorDto,
} from "@/lib/api/cadastros";

type Editing = {
  id: string | null;
  name: string;
  hexCode: string;
  hasLining: boolean;
  active: boolean;
};

const empty: Editing = {
  id: null,
  name: "",
  hexCode: "#888888",
  hasLining: false,
  active: true,
};

export function ColorsTab() {
  const [colors, setColors] = useState<ColorDto[] | null>(null);
  const [editing, setEditing] = useState<Editing | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const refresh = async () => {
    try {
      setColors(await getColors());
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
    setSubmitting(true);
    try {
      const payload = {
        name: editing.name.trim(),
        hexCode: editing.hexCode,
        hasLining: editing.hasLining,
        active: editing.active,
      };
      if (editing.id) await updateColor(editing.id, payload);
      else await createColor(payload);
      setEditing(null);
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSubmitting(false);
    }
  };

  const toggleActive = async (c: ColorDto) => {
    setError(null);
    try {
      await updateColor(c.id, {
        name: c.name,
        hexCode: c.hexCode,
        hasLining: c.hasLining,
        active: !c.active,
      });
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  return (
    <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
      <div className="flex items-center justify-between mb-3">
        <div className="text-sm font-semibold">Cores cadastradas</div>
        {editing === null && (
          <button
            type="button"
            onClick={() => setEditing({ ...empty })}
            className="px-3 py-1 rounded-lg text-xs font-medium bg-accent text-white border border-accent hover:bg-blue-700"
          >
            + Nova cor
          </button>
        )}
      </div>
      {error && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2 mb-3">
          {error}
        </div>
      )}
      {!colors ? (
        <p className="text-xs text-text3">Carregando…</p>
      ) : (
        <table className="w-full text-xs">
          <thead>
            <tr className="text-text3 text-[11px] font-medium border-b border-border">
              <th className="text-left py-2 pr-2">Cor</th>
              <th className="text-left py-2 pr-2">Hex</th>
              <th className="text-left py-2 pr-2">Forro</th>
              <th className="text-left py-2 pr-2">Status</th>
              <th className="text-right py-2 pr-2">Ações</th>
            </tr>
          </thead>
          <tbody>
            {colors.map((c) => (
              <tr key={c.id} className="border-b border-border last:border-0">
                <td className="py-2 pr-2">
                  <span
                    className="inline-block w-2.5 h-2.5 rounded-full mr-2 align-middle border border-black/10"
                    style={{ background: c.hexCode }}
                  />
                  {c.name}
                </td>
                <td className="py-2 pr-2 font-mono">{c.hexCode}</td>
                <td className="py-2 pr-2">
                  {c.hasLining ? (
                    <span className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-amber-light text-amber border border-amber-border">
                      com forro
                    </span>
                  ) : (
                    <span className="text-text3">—</span>
                  )}
                </td>
                <td className="py-2 pr-2">
                  {c.active ? "Ativa" : <span className="text-text3">Inativa</span>}
                </td>
                <td className="py-2 pr-2 text-right">
                  <button
                    type="button"
                    onClick={() =>
                      setEditing({
                        id: c.id,
                        name: c.name,
                        hexCode: c.hexCode,
                        hasLining: c.hasLining,
                        active: c.active,
                      })
                    }
                    className="text-[11px] text-accent hover:underline mr-2"
                  >
                    Editar
                  </button>
                  <button
                    type="button"
                    onClick={() => toggleActive(c)}
                    className="text-[11px] text-text3 hover:underline"
                  >
                    {c.active ? "Desativar" : "Ativar"}
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
            {editing.id ? "Editar cor" : "Adicionar nova cor"}
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
                placeholder="Ex: Rosa claro"
                className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg"
              />
            </div>
            <div>
              <label className="text-xs text-text2 font-medium block mb-1">Cor (hex)</label>
              <input
                type="color"
                value={editing.hexCode}
                onChange={(e) =>
                  setEditing((p) => (p ? { ...p, hexCode: e.target.value } : p))
                }
                className="w-full h-9 border border-border2 rounded-lg cursor-pointer"
              />
            </div>
            <div className="flex items-end gap-3 flex-wrap">
              <label className="flex items-center gap-2 text-xs">
                <input
                  type="checkbox"
                  checked={editing.hasLining}
                  onChange={(e) =>
                    setEditing((p) => (p ? { ...p, hasLining: e.target.checked } : p))
                  }
                  className="accent-accent"
                />
                Tem forro
              </label>
              <label className="flex items-center gap-2 text-xs">
                <input
                  type="checkbox"
                  checked={editing.active}
                  onChange={(e) =>
                    setEditing((p) => (p ? { ...p, active: e.target.checked } : p))
                  }
                  className="accent-accent"
                />
                Ativa
              </label>
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
