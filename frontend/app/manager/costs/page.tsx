import { PageShell } from "@/components/PageShell";
import { CostsClient } from "./CostsClient";

export default function CostsPage() {
  return (
    <PageShell title="Custos" subtitle="Custos avulsos da operação">
      <CostsClient />
    </PageShell>
  );
}
