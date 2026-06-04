"use client";

import { useEffect, useState } from "react";
import { getUsers, type UserDto } from "@/lib/api/cadastros";
import { getCreditsForUser } from "@/lib/api/financial";
import { useCurrentUser } from "@/lib/currentUser";
import { formatBRL, roleLabel } from "@/lib/labels";

/**
 * Shown at the top of every operator page. Lists users with the matching role
 * and persists the selection to localStorage via useCurrentUser. Auto-selects
 * the first user if nothing is stored yet (so a fresh visitor isn't blocked).
 *
 * When auth lands, this component is replaced by a session readout — the
 * useCurrentUser hook stays the same.
 */
export function UserPicker({ role }: { role: string }) {
  const { userId, setUserId } = useCurrentUser();
  const [users, setUsers] = useState<UserDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [unpaidBalance, setUnpaidBalance] = useState<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    getUsers(role)
      .then((list) => {
        if (cancelled) return;
        const active = list.filter((u) => u.active);
        setUsers(active);
        // If nothing chosen, or the chosen user isn't in this role's list, pick the first.
        if (active.length > 0 && (!userId || !active.some((u) => u.id === userId))) {
          setUserId(active[0].id);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : String(err));
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [role]);

  useEffect(() => {
    if (!userId) {
      setUnpaidBalance(null);
      return;
    }
    let cancelled = false;
    getCreditsForUser(userId, "unpaid")
      .then((credits) => {
        if (cancelled) return;
        setUnpaidBalance(credits.reduce((acc, c) => acc + c.amount, 0));
      })
      .catch(() => {
        if (!cancelled) setUnpaidBalance(null);
      });
    return () => {
      cancelled = true;
    };
  }, [userId]);

  return (
    <div className="rounded-2xl border border-border bg-card p-4 shadow-card mb-4 flex items-center gap-3 flex-wrap">
      <span className="text-xs font-semibold">{roleLabel(role)}:</span>
      {error && (
        <span className="text-xs text-red">Falha ao carregar usuários: {error}</span>
      )}
      {!error && users === null && <span className="text-xs text-text3">Carregando…</span>}
      {!error && users !== null && users.length === 0 && (
        <span className="text-xs text-text3">
          Nenhum operador cadastrado para esta função.
        </span>
      )}
      {!error && users !== null && users.length > 0 && (
        <select
          value={userId ?? ""}
          onChange={(e) => setUserId(e.target.value || null)}
          className="text-xs px-2.5 py-1.5 border border-border2 rounded-lg bg-card"
        >
          {users.map((u) => (
            <option key={u.id} value={u.id}>
              {u.name}
            </option>
          ))}
        </select>
      )}
      {unpaidBalance !== null && (
        <div className="ml-auto text-right">
          <div className="text-[10px] text-text3 uppercase tracking-wider">A receber</div>
          <div className="text-sm font-mono font-semibold text-green">
            {formatBRL(unpaidBalance)}
          </div>
        </div>
      )}
    </div>
  );
}
