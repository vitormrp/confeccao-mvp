import { PageShell } from "@/components/PageShell";
import { ProductionDashboard } from "./ProductionDashboard";

export default function ProductionPage() {
  return (
    <PageShell title="Produção" subtitle="Visão geral do gerente">
      <ProductionDashboard />
    </PageShell>
  );
}
