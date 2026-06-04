import type { ReactNode } from "react";

export function PageShell({
  title,
  subtitle,
  actions,
  children,
}: {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="flex-1 overflow-y-auto flex flex-col">
      <div className="px-6 py-3.5 border-b border-border bg-card flex items-center justify-between flex-shrink-0">
        <div>
          <div className="text-base font-semibold tracking-tight">{title}</div>
          {subtitle && (
            <div className="text-xs text-text3 mt-0.5">{subtitle}</div>
          )}
        </div>
        {actions && <div>{actions}</div>}
      </div>
      <div className="px-6 py-5 flex-1">{children}</div>
    </div>
  );
}
