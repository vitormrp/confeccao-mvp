"use client";

import { useEffect, useState } from "react";
import {
  getOperatorReport,
  getOrderThroughput,
  type OperatorReportResponse,
  type OrderThroughputDto,
} from "@/lib/api/reports";
import { formatBRL, roleLabel, stageLabel } from "@/lib/labels";

const PRESETS = [7, 30, 90];

export function ReportsClient() {
  const [days, setDays] = useState(30);
  const [operators, setOperators] = useState<OperatorReportResponse | null>(null);
  const [throughput, setThroughput] = useState<OrderThroughputDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setError(null);
    Promise.all([getOperatorReport(days), getOrderThroughput(days)])
      .then(([ops, tp]) => {
        setOperators(ops);
        setThroughput(tp);
      })
      .catch((err) => setError(err instanceof Error ? err.message : String(err)));
  }, [days]);

  return (
    <div className="space-y-3">
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="text-sm font-semibold">Período</div>
          <div className="inline-flex gap-1 bg-bg2 p-0.5 rounded-xl">
            {PRESETS.map((p) => (
              <button
                key={p}
                type="button"
                onClick={() => setDays(p)}
                className={`px-3.5 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                  days === p ? "bg-card text-text shadow-card" : "text-text2 hover:text-text"
                }`}
              >
                {p} dias
              </button>
            ))}
          </div>
        </div>
      </div>

      {error && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2">
          {error}
        </div>
      )}

      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="text-sm font-semibold mb-3">Throughput de ordens</div>
        {!throughput ? (
          <p className="text-xs text-text3">Carregando…</p>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <Cell label="Ordens criadas" value={String(throughput.ordersCreated)} />
            <Cell label="Ordens concluídas" value={String(throughput.ordersCompleted)} />
            <Cell
              label="Peças iniciadas"
              value={String(throughput.piecesStarted)}
            />
            <Cell
              label="Lead time médio"
              value={
                throughput.averageLeadTimeDays != null
                  ? `${throughput.averageLeadTimeDays.toString().replace(".", ",")} dias`
                  : "—"
              }
            />
          </div>
        )}
      </div>

      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="text-sm font-semibold mb-3">Produtividade por operador</div>
        {!operators ? (
          <p className="text-xs text-text3">Carregando…</p>
        ) : operators.operators.length === 0 ? (
          <p className="text-xs text-text3">
            Nenhuma atividade registrada no período.
          </p>
        ) : (
          <table className="w-full text-xs">
            <thead>
              <tr className="text-text3 text-[11px] font-medium border-b border-border">
                <th className="text-left py-2 pr-2">Operador</th>
                <th className="text-left py-2 pr-2">Função</th>
                <th className="text-right py-2 pr-2">Peças</th>
                <th className="text-right py-2 pr-2">Valor gerado</th>
                <th className="text-left py-2 pr-2">Por etapa</th>
              </tr>
            </thead>
            <tbody>
              {operators.operators.map((op) => (
                <tr key={op.userId} className="border-b border-border last:border-0 align-top">
                  <td className="py-2 pr-2 font-medium">{op.name}</td>
                  <td className="py-2 pr-2">
                    <span className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-accent-light text-accent border border-accent-border">
                      {roleLabel(op.role)}
                    </span>
                  </td>
                  <td className="py-2 pr-2 text-right font-mono">{op.totalPieces}</td>
                  <td className="py-2 pr-2 text-right font-mono">
                    {formatBRL(op.totalAmount)}
                  </td>
                  <td className="py-2 pr-2">
                    <div className="flex flex-wrap gap-1">
                      {op.byStage.map((s) => (
                        <span
                          key={s.stage}
                          className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-bg2 border border-border text-text2"
                        >
                          {stageLabel(s.stage)}: {s.pieces}
                        </span>
                      ))}
                    </div>
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

function Cell({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-[10px] text-text3 uppercase tracking-wider">{label}</div>
      <div className="text-xl font-mono font-semibold">{value}</div>
    </div>
  );
}
