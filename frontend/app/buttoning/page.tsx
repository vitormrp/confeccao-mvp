import { PageShell } from "@/components/PageShell";
import { StageQueueClient } from "@/components/StageQueueClient";
import { UserPicker } from "@/components/UserPicker";

export default function ButtoningPage() {
  return (
    <PageShell title="Botão" subtitle="Interface de botão">
      <UserPicker role="buttoning" />
      <StageQueueClient stage="buttoning" />
    </PageShell>
  );
}
