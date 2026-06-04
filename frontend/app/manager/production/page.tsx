import { PageShell } from "@/components/PageShell";
import { DispatchPanel } from "@/components/DispatchPanel";
import { MetricCard, MetricGrid } from "@/components/MetricGrid";
import { NewOrderForm } from "@/components/NewOrderForm";
import { NotificationsStrip } from "@/components/NotificationsStrip";
import { getColors, getModels } from "@/lib/api/cadastros";
import { listOrders } from "@/lib/api/orders";
import { getDashboard } from "@/lib/api/production";
import { orderStatusLabel, stageLabel } from "@/lib/labels";

export const dynamic = "force-dynamic";

export default async function ProductionPage() {
  const [dashboard, colors, models, orders] = await Promise.all([
    getDashboard(),
    getColors(),
    getModels(),
    listOrders(),
  ]);

  return (
    <PageShell title="Produção" subtitle="Visão geral do gerente">
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
    </PageShell>
  );
}
