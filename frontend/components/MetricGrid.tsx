type Tone = "process" | "done" | "neutral";

export function MetricGrid({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid gap-2 grid-cols-[repeat(auto-fit,minmax(110px,1fr))]">
      {children}
    </div>
  );
}

export function MetricCard({
  label,
  value,
  sub,
  tone = "neutral",
}: {
  label: string;
  value: string | number;
  sub?: string;
  tone?: Tone;
}) {
  const toneClass =
    tone === "process"
      ? "text-accent"
      : tone === "done"
        ? "text-green"
        : "text-text";
  return (
    <div className="bg-bg border border-border rounded-xl px-2.5 py-3 text-center">
      <div className="text-[10px] text-text3 mb-1">{label}</div>
      <div className={["text-2xl font-semibold font-mono tracking-tight", toneClass].join(" ")}>
        {value}
      </div>
      {sub && <div className="text-[10px] text-text3 mt-0.5">{sub}</div>}
    </div>
  );
}
