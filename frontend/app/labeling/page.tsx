import { PageShell } from "@/components/PageShell";
import { StageQueueClient } from "@/components/StageQueueClient";
import { UserPicker } from "@/components/UserPicker";

export default function LabelingPage() {
  return (
    <PageShell title="Etiqueta" subtitle="Interface de etiqueta">
      <UserPicker role="labeling" />
      <StageQueueClient stage="labeling" />
    </PageShell>
  );
}
