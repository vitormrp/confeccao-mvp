"use client";

import { useState, type ReactNode } from "react";

export type Tab = { id: string; label: string; content: ReactNode };

export function Tabs({ tabs, initialId }: { tabs: Tab[]; initialId?: string }) {
  const [activeId, setActiveId] = useState(initialId ?? tabs[0]?.id);
  const active = tabs.find((t) => t.id === activeId) ?? tabs[0];

  return (
    <div>
      <div className="inline-flex gap-0.5 mb-4 bg-bg2 p-0.5 rounded-xl">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => setActiveId(tab.id)}
            className={[
              "px-3.5 py-1.5 rounded-lg text-xs font-medium transition-colors",
              tab.id === active?.id
                ? "bg-card text-text shadow-card"
                : "text-text2 hover:text-text",
            ].join(" ")}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <div>{active?.content}</div>
    </div>
  );
}
