"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

type NavItem = { href: string; label: string; icon: string };

const managerNav: NavItem[] = [
  { href: "/manager/production", label: "Produção", icon: "⚙" },
  { href: "/manager/registrations", label: "Cadastros", icon: "📋" },
  { href: "/manager/costs", label: "Custos", icon: "💰" },
  { href: "/manager/financial", label: "Financeiro", icon: "💳" },
  { href: "/manager/reports", label: "Relatório", icon: "📊" },
];

const operatorNav: NavItem[] = [
  { href: "/cutting", label: "Cortador", icon: "✂" },
  { href: "/interfacing", label: "Entretela", icon: "🧵" },
  { href: "/sewing", label: "Costureira", icon: "🪡" },
  { href: "/washing", label: "Lavanderia", icon: "💧" },
  { href: "/buttoning", label: "Botão", icon: "🔘" },
  { href: "/labeling", label: "Etiqueta", icon: "🏷" },
  { href: "/pressing", label: "Passadeira", icon: "👔" },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="w-[220px] flex-shrink-0 bg-card border-r border-border flex flex-col">
      <div className="px-4 pt-5 pb-4 border-b border-border">
        <h1 className="text-[13px] font-semibold tracking-tight">Confecção</h1>
        <p className="text-[11px] text-text3 mt-0.5">Gestão de produção</p>
      </div>
      <nav className="p-2 flex-1 overflow-y-auto">
        <NavSection label="Gerente" items={managerNav} pathname={pathname} />
        <NavSection label="Operadores" items={operatorNav} pathname={pathname} />
      </nav>
    </aside>
  );
}

function NavSection({
  label,
  items,
  pathname,
}: {
  label: string;
  items: NavItem[];
  pathname: string;
}) {
  return (
    <div className="mb-4">
      <div className="text-[10px] font-semibold text-text3 uppercase tracking-wider px-2 py-1">
        {label}
      </div>
      {items.map((item) => {
        const active = pathname.startsWith(item.href);
        return (
          <Link
            key={item.href}
            href={item.href}
            className={[
              "flex items-center gap-2 px-2.5 py-1.5 rounded-lg text-[13px] transition-colors w-full text-left",
              active
                ? "bg-accent-light text-accent font-medium"
                : "text-text2 hover:bg-bg2 hover:text-text",
            ].join(" ")}
          >
            <span className="w-4 text-center text-[14px] flex-shrink-0">
              {item.icon}
            </span>
            {item.label}
          </Link>
        );
      })}
    </div>
  );
}
