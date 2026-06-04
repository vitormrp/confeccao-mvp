import { PageShell } from "./PageShell";

export function StubPage({
  title,
  subtitle,
  phase,
}: {
  title: string;
  subtitle: string;
  phase: string;
}) {
  return (
    <PageShell title={title} subtitle={subtitle}>
      <div className="rounded-2xl border border-border bg-card p-5 shadow-card">
        <div className="text-xs uppercase tracking-wider text-text3 mb-2">
          {phase}
        </div>
        <p className="text-sm text-text2">
          Página em construção. A funcionalidade chega na fase indicada.
        </p>
      </div>
    </PageShell>
  );
}
