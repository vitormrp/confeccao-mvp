"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import {
  createPayment,
  getCreditsForUser,
  getFinancialSummary,
  getOperatorBalances,
  type CreditDto,
  type OperatorBalanceDto,
  type SummaryDto,
} from "@/lib/api/financial";
import { formatBRL, roleLabel, stageLabel } from "@/lib/labels";

type State =
  | { kind: "loading" }
  | {
      kind: "ready";
      operators: OperatorBalanceDto[];
      summary: SummaryDto;
    }
  | { kind: "error"; message: string };

export function FinancialClient() {
  const router = useRouter();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [expanded, setExpanded] = useState<string | null>(null);
  const [credits, setCredits] = useState<Record<string, CreditDto[]>>({});
  const [selected, setSelected] = useState<Record<string, Set<string>>>({});
  const [note, setNote] = useState("");
  const [paying, setPaying] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const refresh = async () => {
    try {
      const [operators, summary] = await Promise.all([
        getOperatorBalances(),
        getFinancialSummary(),
      ]);
      setState({ kind: "ready", operators, summary });
    } catch (err) {
      setState({
        kind: "error",
        message: err instanceof Error ? err.message : String(err),
      });
    }
  };

  useEffect(() => {
    refresh();
  }, []);

  const toggleExpand = async (userId: string) => {
    if (expanded === userId) {
      setExpanded(null);
      return;
    }
    setExpanded(userId);
    if (!credits[userId]) {
      try {
        const data = await getCreditsForUser(userId, "unpaid");
        setCredits((prev) => ({ ...prev, [userId]: data }));
      } catch (err) {
        setActionError(err instanceof Error ? err.message : String(err));
      }
    }
  };

  const toggleCredit = (userId: string, creditId: string) => {
    setSelected((prev) => {
      const set = new Set(prev[userId] ?? []);
      if (set.has(creditId)) set.delete(creditId);
      else set.add(creditId);
      return { ...prev, [userId]: set };
    });
  };

  const selectAll = (userId: string) => {
    const list = credits[userId] ?? [];
    setSelected((prev) => ({
      ...prev,
      [userId]: new Set(list.map((c) => c.id)),
    }));
  };

  const clearSelection = (userId: string) => {
    setSelected((prev) => ({ ...prev, [userId]: new Set() }));
  };

  const pay = async (userId: string) => {
    const ids = Array.from(selected[userId] ?? []);
    if (ids.length === 0) {
      setActionError("Selecione ao menos um crédito.");
      return;
    }
    setActionError(null);
    setPaying(true);
    try {
      await createPayment({ userId, creditIds: ids, note: note.trim() || undefined });
      setCredits((prev) => ({ ...prev, [userId]: [] }));
      setSelected((prev) => ({ ...prev, [userId]: new Set() }));
      setNote("");
      await refresh();
      // Reload unpaid credits for this user.
      const data = await getCreditsForUser(userId, "unpaid");
      setCredits((prev) => ({ ...prev, [userId]: data }));
      router.refresh();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : String(err));
    } finally {
      setPaying(false);
    }
  };

  if (state.kind === "loading") {
    return (
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <p className="text-xs text-text3">Carregando…</p>
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div className="rounded-2xl border border-red-border bg-red-light p-5">
        <p className="text-xs text-red font-semibold">Erro</p>
        <p className="text-xs text-red mt-1 break-all">{state.message}</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <SummaryCell label="Total a pagar" value={formatBRL(state.summary.totalUnpaid)} tone="red" />
          <SummaryCell label="Total já pago" value={formatBRL(state.summary.totalPaid)} tone="green" />
          <SummaryCell label="Custos avulsos" value={formatBRL(state.summary.totalMiscCosts)} />
          <SummaryCell
            label="Operadores com saldo"
            value={String(state.summary.operatorsWithBalance)}
          />
        </div>
      </div>

      {actionError && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2">
          {actionError}
        </div>
      )}

      {state.operators.length === 0 ? (
        <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
          <p className="text-xs text-text3">
            Nenhum operador com créditos ainda. Eles serão criados conforme as etapas avançam.
          </p>
        </div>
      ) : (
        state.operators.map((op) => {
          const isOpen = expanded === op.userId;
          const userCredits = credits[op.userId] ?? [];
          const userSelected = selected[op.userId] ?? new Set<string>();
          const selectedAmount = userCredits
            .filter((c) => userSelected.has(c.id))
            .reduce((acc, c) => acc + c.amount, 0);

          return (
            <div
              key={op.userId}
              className="rounded-2xl border border-border bg-card p-5 shadow-card"
            >
              <div className="flex items-center justify-between gap-3 flex-wrap">
                <div>
                  <div className="text-sm font-semibold">{op.name}</div>
                  <div className="text-[11px] text-text3 mt-0.5">
                    <span className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-accent-light text-accent border border-accent-border mr-2">
                      {roleLabel(op.role)}
                    </span>
                    {op.unpaidCount} crédito{op.unpaidCount === 1 ? "" : "s"} a pagar
                    {op.lastCreditAt &&
                      ` · último: ${new Date(op.lastCreditAt).toLocaleDateString("pt-BR")}`}
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <div>
                    <div className="text-[10px] text-text3 text-right">A receber</div>
                    <div className="text-xl font-mono font-semibold text-accent">
                      {formatBRL(op.unpaidBalance)}
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => toggleExpand(op.userId)}
                    className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-bg2 text-text border border-border hover:bg-bg3 transition-colors"
                  >
                    {isOpen ? "Fechar" : "Ver créditos"}
                  </button>
                </div>
              </div>

              {isOpen && (
                <div className="mt-4 border-t border-border pt-3">
                  {userCredits.length === 0 ? (
                    <p className="text-xs text-text3">Nenhum crédito em aberto.</p>
                  ) : (
                    <>
                      <div className="flex gap-2 mb-2 text-[11px]">
                        <button
                          type="button"
                          onClick={() => selectAll(op.userId)}
                          className="text-accent hover:underline"
                        >
                          Selecionar tudo
                        </button>
                        <button
                          type="button"
                          onClick={() => clearSelection(op.userId)}
                          className="text-text3 hover:underline"
                        >
                          Limpar
                        </button>
                      </div>
                      <table className="w-full text-xs">
                        <thead>
                          <tr className="text-text3 text-[11px] border-b border-border">
                            <th className="py-1.5 pr-2 text-left w-6"></th>
                            <th className="py-1.5 pr-2 text-left">Etapa</th>
                            <th className="py-1.5 pr-2 text-left">Modelo</th>
                            <th className="py-1.5 pr-2 text-left">Tam</th>
                            <th className="py-1.5 pr-2 text-right">Qtd</th>
                            <th className="py-1.5 pr-2 text-right">Valor</th>
                            <th className="py-1.5 pr-2 text-left">Quando</th>
                          </tr>
                        </thead>
                        <tbody>
                          {userCredits.map((credit) => (
                            <tr key={credit.id} className="border-b border-border last:border-0">
                              <td className="py-1.5 pr-2">
                                <input
                                  type="checkbox"
                                  checked={userSelected.has(credit.id)}
                                  onChange={() => toggleCredit(op.userId, credit.id)}
                                  className="accent-accent"
                                />
                              </td>
                              <td className="py-1.5 pr-2">{stageLabel(credit.stage)}</td>
                              <td className="py-1.5 pr-2">
                                {credit.modelName}{" "}
                                <span className="text-text3">#{credit.orderNumber}</span>
                              </td>
                              <td className="py-1.5 pr-2">{credit.size}</td>
                              <td className="py-1.5 pr-2 text-right font-mono">
                                {credit.quantity}
                              </td>
                              <td className="py-1.5 pr-2 text-right font-mono">
                                {formatBRL(credit.amount)}
                              </td>
                              <td className="py-1.5 pr-2 text-text3 text-[11px]">
                                {new Date(credit.occurredAt).toLocaleString("pt-BR")}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>

                      <div className="flex items-center gap-2 mt-3 flex-wrap">
                        <input
                          type="text"
                          value={note}
                          onChange={(e) => setNote(e.target.value)}
                          placeholder="Nota (opcional)"
                          className="text-xs px-2.5 py-1.5 border border-border2 rounded-lg flex-1 min-w-[200px]"
                        />
                        <span className="text-[11px] text-text2 font-mono">
                          Selecionado: {formatBRL(selectedAmount)}
                        </span>
                        <button
                          type="button"
                          disabled={paying || userSelected.size === 0}
                          onClick={() => pay(op.userId)}
                          className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-green text-white border border-green hover:opacity-90 transition-opacity disabled:opacity-60"
                        >
                          {paying ? "Registrando..." : "Registrar pagamento"}
                        </button>
                      </div>
                    </>
                  )}
                </div>
              )}
            </div>
          );
        })
      )}
    </div>
  );
}

function SummaryCell({
  label,
  value,
  tone = "neutral",
}: {
  label: string;
  value: string;
  tone?: "neutral" | "red" | "green";
}) {
  const toneClass =
    tone === "red" ? "text-red" : tone === "green" ? "text-green" : "text-text";
  return (
    <div>
      <div className="text-[10px] text-text3 uppercase tracking-wider">{label}</div>
      <div className={`text-xl font-mono font-semibold ${toneClass}`}>{value}</div>
    </div>
  );
}
