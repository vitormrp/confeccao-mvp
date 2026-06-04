import { PageShell } from "@/components/PageShell";
import { Tabs } from "@/components/Tabs";
import { ColorsTab } from "./ColorsTab";
import { ModelsTab } from "./ModelsTab";
import { PricesTab } from "./PricesTab";
import { UsersTab } from "./UsersTab";

export default function RegistrationsPage() {
  return (
    <PageShell
      title="Cadastros"
      subtitle="Cores, modelos, operadores e preços"
    >
      <Tabs
        tabs={[
          { id: "cores", label: "Cores", content: <ColorsTab /> },
          { id: "modelos", label: "Modelos", content: <ModelsTab /> },
          { id: "operadores", label: "Operadores", content: <UsersTab /> },
          { id: "precos", label: "Preços", content: <PricesTab /> },
        ]}
      />
    </PageShell>
  );
}
