"use client";

import { useEffect, useState } from "react";
import { getPrices, getUsers, type PriceDto, type UserDto } from "@/lib/api/cadastros";
import { formatBRL, roleLabel } from "@/lib/labels";

export function PricesTab() {
  const [prices, setPrices] = useState<PriceDto[] | null>(null);
  const [users, setUsers] = useState<UserDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([getPrices(), getUsers()])
      .then(([p, u]) => {
        setPrices(p);
        setUsers(u);
      })
      .catch((err) => setError(err instanceof Error ? err.message : String(err)));
  }, []);

  const usersById = new Map((users ?? []).map((u) => [u.id, u]));

  return (
    <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
      <div className="text-sm font-semibold mb-2">Preços vigentes</div>
      <p className="text-[11px] text-text3 mb-3">
        Visualização somente leitura — edição de preços com vigência chega numa
        próxima iteração.
      </p>
      {error && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2 mb-3">
          {error}
        </div>
      )}
      {!prices ? (
        <p className="text-xs text-text3">Carregando…</p>
      ) : prices.length === 0 ? (
        <p className="text-xs text-text3">Nenhum preço cadastrado.</p>
      ) : (
        <table className="w-full text-xs">
          <thead>
            <tr className="text-text3 text-[11px] font-medium border-b border-border">
              <th className="text-left py-2 pr-2">Operador</th>
              <th className="text-left py-2 pr-2">Função</th>
              <th className="text-left py-2 pr-2">Base</th>
              <th className="text-left py-2 pr-2">Adicional forro</th>
              <th className="text-left py-2 pr-2">Adicional entretela</th>
              <th className="text-left py-2 pr-2">Botão (encapado / pronto)</th>
              <th className="text-left py-2 pr-2">Faixas (corte)</th>
              <th className="text-left py-2 pr-2">Vigência</th>
            </tr>
          </thead>
          <tbody>
            {prices.map((p) => {
              const u = usersById.get(p.userId);
              return (
                <tr key={p.id} className="border-b border-border last:border-0 align-top">
                  <td className="py-2 pr-2 font-medium">{u?.name ?? p.userId}</td>
                  <td className="py-2 pr-2 text-text2">{u ? roleLabel(u.role) : "—"}</td>
                  <td className="py-2 pr-2 font-mono">{formatBRL(p.amount)}</td>
                  <td className="py-2 pr-2 font-mono">{formatBRL(p.liningExtra)}</td>
                  <td className="py-2 pr-2 font-mono">{formatBRL(p.interfacingExtra)}</td>
                  <td className="py-2 pr-2 font-mono">
                    {p.coveredButtonPrice == null && p.readyButtonPrice == null
                      ? "—"
                      : `${formatBRL(p.coveredButtonPrice)} / ${formatBRL(p.readyButtonPrice)}`}
                  </td>
                  <td className="py-2 pr-2">
                    {p.tiers.length === 0
                      ? "—"
                      : (
                        <div className="space-y-0.5">
                          {p.tiers.map((t) => (
                            <div key={t.id} className="font-mono text-[11px]">
                              ≤{" "}
                              {t.upToQuantity > 1_000_000_000 ? "∞" : t.upToQuantity}:{" "}
                              {formatBRL(t.amount)}
                            </div>
                          ))}
                        </div>
                      )}
                  </td>
                  <td className="py-2 pr-2 text-text3 text-[11px]">
                    {new Date(p.effectiveFrom).toLocaleString("pt-BR")}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
