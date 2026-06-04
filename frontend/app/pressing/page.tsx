import { PageShell } from "@/components/PageShell";
import { StageQueueClient } from "@/components/StageQueueClient";
import { UserPicker } from "@/components/UserPicker";

export default function PressingPage() {
  return (
    <PageShell title="Passadeira" subtitle="Interface da passadeira">
      <UserPicker role="pressing" />
      <StageQueueClient stage="pressing" />
    </PageShell>
  );
}
