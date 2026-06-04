import { PageShell } from "@/components/PageShell";
import { UserPicker } from "@/components/UserPicker";
import { CuttingClient } from "./CuttingClient";

export default function CuttingPage() {
  return (
    <PageShell title="Cortador" subtitle="Interface do cortador">
      <UserPicker role="cutting" />
      <CuttingClient />
    </PageShell>
  );
}
