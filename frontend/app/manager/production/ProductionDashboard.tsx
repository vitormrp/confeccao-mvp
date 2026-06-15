"use client";

import { useEffect, useState } from "react";
import { DispatchPanel } from "@/components/DispatchPanel";
import { MetricCard, MetricGrid } from "@/components/MetricGrid";
import { NewOrderForm } from "@/components/NewOrderForm";
import { NotificationsStrip } from "@/components/NotificationsStrip";
import {
  getColors,
  getModels,
  type ColorDto,
  type ModelDto,
} from "@/lib/api/cadastros";
import { listOrders, type OrderSummaryDto } from "@/lib/api/orders";
import { getDashboard, type DashboardDto } from "@/lib/api/production";
import { orderStatusLabel, stageLabel } from "@/lib/labels";

type State =
  | { kind: "loading" }
  | {
      kind: "ready";
      dashboard: DashboardDto;
      colors: ColorDto[];
      models: ModelDto[];
      orders: OrderSummaryDto[];
    }
  | { kind: "error"; message: string };

/**
 * Client-rendered so the page shell, sidebar, and the dispatch sections all
 * appear instantly — even when the backend is asleep. The data fetches happen
 * from the browser, which is what reliably triggers Render's wake-up logic
 * (the same mechanism a manual `curl /api/v1/health` uses). While the backend
 * boots, the dashboard shows a friendly "Acordando o servidor..." message
 * instead of a blank tab; once the apiFetch retry chain succeeds, the real
 * data slides in.
 */
export function ProductionDashboard() {
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    let cancelled = false;
    Promise.all([getDashboard(), getColors(), getModels(), listOrders()])
      .then(([dashboard, colors, models, orders]) => {
        if (!cancelled) setState({ kind: "ready", dashboard, colors, models, orders });
      })
      .catch((err: unknown) => {
        if (!cancelled)
          setState({
            kind: "error",
            message: err instanceof Error ? err.message : String(err),
          });
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (state.kind === "loading") {
    return (
      <div className="rounded-2xl border border-border bg-card p-8 shadow-card text-center">
        <div className="text-sm font-medium mb-1">Carregando dashboard…</div>
        <div className="text-xs text-text3">
          Se a primeira carga estiver demorando, o servidor pode estar acordando
          (até ~30s no plano gratuito). Não recarregue — a página atualiza sozinha.
        </div>
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div className="rounded-2xl border border-red-border bg-red-light p-5">
        <div className="text-xs text-red font-semibold mb-1">
          Não foi possível carregar o dashboard
        </div>
        <div className="text-xs text-red mb-3 break-all">{state.message}</div>
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-red text-white border border-red hover:opacity-90"
        >
          Tentar de novo
        </button>
      </div>
    );
  }

  const { dashboard, colors, models, orders } = state;

  return (
    <>
      <NotificationsStrip />

      <div className="rounded-2xl border border-border bg-card p-5 shadow-card mb-3">
        <div className="text-[10px] uppercase tracking-wider font-semibold text-text3 mb-2">
          Em processo
        </div>
        <MetricGrid>
          <MetricCard
            label="Ordens ativas"
            value={dashboard.ordersActive}
            tone="process"
          />
          {dashboard.stages.map((s) => (
            <MetricCard
              key={`${s.stage}-proc`}
              label={stageLabel(s.stage)}
              value={s.inProcess}
              sub="em processo"
              tone="process"
            />
          ))}
        </MetricGrid>

        <div className="text-[10px] uppercase tracking-wider font-semibold text-text3 mb-2 mt-4">
          Concluídas
        </div>
        <MetricGrid>
          <MetricCard
            label="Ordens fechadas"
            value={dashboard.ordersCompleted}
            tone="done"
          />
          {dashboard.stages.map((s) => (
            <MetricCard
              key={`${s.stage}-done`}
              label={stageLabel(s.stage)}
              value={s.completed}
              sub="concluídas"
              tone="done"
            />
          ))}
        </MetricGrid>
      </div>

      <div className="rounded-2xl border border-border bg-card p-5 shadow-card mb-3">
        <div className="flex items-center justify-between mb-2">
          <div className="text-sm font-semibold">Nova ordem de corte</div>
        </div>
        <NewOrderForm
          colors={colors.filter((c) => c.active)}
          models={models.filter((m) => m.active)}
        />
      </div>

      <div className="rounded-2xl border border-border bg-card p-5 shadow-card mb-3">
        <DispatchPanel />
      </div>

      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="text-sm font-semibold mb-3">Ordens recentes</div>
        {orders.length === 0 ? (
          <p className="text-xs text-text3">
            Nenhuma ordem ainda. Crie a primeira acima.
          </p>
        ) : (
          <table className="w-full text-xs">
            <thead>
              <tr className="text-text3 text-[11px] font-medium border-b border-border">
                <th className="text-left py-2 pr-2">#</th>
                <th className="text-left py-2 pr-2">Tecido</th>
                <th className="text-left py-2 pr-2">Cor</th>
                <th className="text-left py-2 pr-2">Status</th>
                <th className="text-left py-2 pr-2">Itens</th>
                <th className="text-left py-2 pr-2">Planejado</th>
                <th className="text-left py-2 pr-2">Criada em</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id} className="border-b border-border last:border-0">
                  <td className="py-2 pr-2 font-mono">#{o.number}</td>
                  <td className="py-2 pr-2 font-medium">{o.fabricCode}</td>
                  <td className="py-2 pr-2">
                    <span
                      className="inline-block w-2.5 h-2.5 rounded-full mr-2 align-middle border border-black/10"
                      style={{ background: o.colorHex }}
                    />
                    {o.colorName}
                  </td>
                  <td className="py-2 pr-2">
                    <span className="inline-block px-2 py-0.5 rounded-full text-[11px] bg-accent-light text-accent border border-accent-border">
                      {orderStatusLabel(o.status)}
                    </span>
                  </td>
                  <td className="py-2 pr-2 font-mono">{o.itemCount}</td>
                  <td className="py-2 pr-2 font-mono">{o.plannedTotal}</td>
                  <td className="py-2 pr-2 text-text3 text-[11px]">
                    {new Date(o.createdAt).toLocaleString("pt-BR")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  );
}
