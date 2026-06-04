import { PageShell } from "@/components/PageShell";
import { ReportsClient } from "./ReportsClient";

export default function ReportsPage() {
  return (
    <PageShell title="Relatório" subtitle="Produtividade e throughput">
      <ReportsClient />
    </PageShell>
  );
}
