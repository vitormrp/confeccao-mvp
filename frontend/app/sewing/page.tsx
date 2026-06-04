import { PageShell } from "@/components/PageShell";
import { UserPicker } from "@/components/UserPicker";
import { SewingClient } from "./SewingClient";

export default function SewingPage() {
  return (
    <PageShell title="Costureira" subtitle="Interface da costureira">
      <UserPicker role="sewing" />
      <SewingClient />
    </PageShell>
  );
}
