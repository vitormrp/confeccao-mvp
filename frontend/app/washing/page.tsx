import { PageShell } from "@/components/PageShell";
import { UserPicker } from "@/components/UserPicker";
import { WashingClient } from "./WashingClient";

export default function WashingPage() {
  return (
    <PageShell title="Lavanderia" subtitle="Interface da lavanderia">
      <UserPicker role="washing" />
      <WashingClient />
    </PageShell>
  );
}
