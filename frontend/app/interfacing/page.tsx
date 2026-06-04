import { PageShell } from "@/components/PageShell";
import { StageQueueClient } from "@/components/StageQueueClient";
import { UserPicker } from "@/components/UserPicker";

export default function InterfacingPage() {
  return (
    <PageShell title="Entretela" subtitle="Interface de entretela">
      <UserPicker role="interfacing" />
      <StageQueueClient stage="interfacing" />
    </PageShell>
  );
}
