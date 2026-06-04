import { PageShell } from "@/components/PageShell";
import { FinancialClient } from "./FinancialClient";

export default function FinancialPage() {
  return (
    <PageShell
      title="Financeiro"
      subtitle="Créditos, pagamentos e saldos por operador"
    >
      <FinancialClient />
    </PageShell>
  );
}
