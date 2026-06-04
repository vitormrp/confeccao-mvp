"use client";

import { useEffect, useState } from "react";
import {
  createUser,
  getUsers,
  updateUser,
  type UserDto,
} from "@/lib/api/cadastros";
import { roleLabel } from "@/lib/labels";

const ROLES = [
  "manager",
  "cutting",
  "interfacing",
  "sewing",
  "washing",
  "buttoning",
  "labeling",
  "pressing",
];

type Editing = { id: string | null; name: string; role: string; active: boolean };
const empty: Editing = { id: null, name: "", role: "sewing", active: true };

export function UsersTab() {
  const [users, setUsers] = useState<UserDto[] | null>(null);
  const [editing, setEditing] = useState<Editing | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const refresh = async () => {
    try {
      setUsers(await getUsers());
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
        role: editing.role,
        active: editing.active,
      };
      if (editing.id) await updateUser(editing.id, payload);
      else await createUser(payload);
      setEditing(null);
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setSubmitting(false);
    }
  };

  const toggleActive = async (u: UserDto) => {
    setError(null);
    try {
      await updateUser(u.id, { name: u.name, role: u.role, active: !u.active });
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    }
  };

  return (
    <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
      <div className="flex items-center justify-between mb-3">
        <div className="text-sm font-semibold">Operadores cadastrados</div>
        {editing === null && (
          <button
            type="button"
            onClick={() => setEditing({ ...empty })}
            className="px-3 py-1 rounded-lg text-xs font-medium bg-accent text-white border border-accent hover:bg-blue-700"
          >
            + Novo operador
          </button>
        )}
      </div>
      {error && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2 mb-3">
          {error}
        </div>
      )}
      {!users ? (
        <p className="text-xs text-text3">Carregando…</p>
      ) : (
        <table className="w-full text-xs">
          <thead>
            <tr className="text-text3 text-[11px] font-medium border-b border-border">
              <th className="text-left py-2 pr-2">Nome</th>
              <th className="text-left py-2 pr-2">Função</th>
              <th className="text-left py-2 pr-2">Status</th>
              <th className="text-right py-2 pr-2">Ações</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="border-b border-border last:border-0">
                <td className="py-2 pr-2 font-medium">{u.name}</td>
                <td className="py-2 pr-2">
                  <span className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-accent-light text-accent border border-accent-border">
                    {roleLabel(u.role)}
                  </span>
                </td>
                <td className="py-2 pr-2">
                  {u.active ? "Ativo" : <span className="text-text3">Inativo</span>}
                </td>
                <td className="py-2 pr-2 text-right">
                  <button
                    type="button"
                    onClick={() =>
                      setEditing({
                        id: u.id,
                        name: u.name,
                        role: u.role,
                        active: u.active,
                      })
                    }
                    className="text-[11px] text-accent hover:underline mr-2"
                  >
                    Editar
                  </button>
                  <button
                    type="button"
                    onClick={() => toggleActive(u)}
                    className="text-[11px] text-text3 hover:underline"
                  >
                    {u.active ? "Desativar" : "Ativar"}
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
            {editing.id ? "Editar operador" : "Adicionar operador"}
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
                placeholder="Ex: Costureira D"
                className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg"
              />
            </div>
            <div>
              <label className="text-xs text-text2 font-medium block mb-1">Função</label>
              <select
                value={editing.role}
                onChange={(e) =>
                  setEditing((p) => (p ? { ...p, role: e.target.value } : p))
                }
                className="w-full text-sm px-2.5 py-1.5 border border-border2 rounded-lg bg-card"
              >
                {ROLES.map((r) => (
                  <option key={r} value={r}>
                    {roleLabel(r)}
                  </option>
                ))}
              </select>
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
