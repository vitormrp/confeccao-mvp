"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { getUsers, type UserDto } from "@/lib/api/cadastros";
import {
  dispatchGeneric,
  dispatchSewing,
  getAwaiting,
  type AwaitingItemDto,
} from "@/lib/api/dispatch";
import { bundleLaundryPackage } from "@/lib/api/laundry";
import { stageLabel } from "@/lib/labels";

type State =
  | { kind: "loading" }
  | { kind: "ready"; items: AwaitingItemDto[] }
  | { kind: "error"; message: string };

export function DispatchPanel() {
  const router = useRouter();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [seamstresses, setSeamstresses] = useState<UserDto[]>([]);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [selectedForLaundry, setSelectedForLaundry] = useState<Set<string>>(new Set());
  const [bundling, setBundling] = useState(false);

  const refresh = async () => {
    setState({ kind: "loading" });
    try {
      const items = await getAwaiting();
      setState({ kind: "ready", items });
    } catch (err) {
      setState({
        kind: "error",
        message: err instanceof Error ? err.message : String(err),
      });
    }
  };

  useEffect(() => {
    refresh();
    getUsers("sewing")
      .then((list) => setSeamstresses(list.filter((u) => u.active)))
      .catch(() => {});
  }, []);

  const sendGeneric = async (item: AwaitingItemDto) => {
    setActionError(null);
    setPendingId(item.pipelineItemId);
    try {
      await dispatchGeneric(item.pipelineItemId);
      await refresh();
      router.refresh();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : String(err));
    } finally {
      setPendingId(null);
    }
  };

  const bundlePackage = async () => {
    const ids = Array.from(selectedForLaundry);
    if (ids.length === 0) return;
    setActionError(null);
    setBundling(true);
    try {
      await bundleLaundryPackage(ids);
      setSelectedForLaundry(new Set());
      await refresh();
      router.refresh();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : String(err));
    } finally {
      setBundling(false);
    }
  };

  const sendSewing = async (
    item: AwaitingItemDto,
    userId: string,
    quantity: number,
  ) => {
    setActionError(null);
    setPendingId(item.pipelineItemId);
    try {
      await dispatchSewing({
        pipelineItemId: item.pipelineItemId,
        userId,
        quantity,
      });
      await refresh();
      router.refresh();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : String(err));
    } finally {
      setPendingId(null);
    }
  };

  if (state.kind === "loading") {
    return <p className="text-xs text-text3">Carregando…</p>;
  }

  if (state.kind === "error") {
    return (
      <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2">
        {state.message}
      </div>
    );
  }

  const generic = state.items.filter((i) => i.dispatchKind === "generic");
  const perUser = state.items.filter((i) => i.dispatchKind === "per-user");
  const pkg = state.items.filter((i) => i.dispatchKind === "package");

  return (
    <div className="space-y-4">
      {actionError && (
        <div className="text-xs text-red bg-red-light border border-red-border rounded-lg px-3 py-2">
          {actionError}
        </div>
      )}

      <Section
        title="Distribuir lotes prontos → Próxima etapa"
        emptyMessage="Nenhum lote pronto para etapas genéricas."
      >
        {generic.length > 0 && (
          <ul className="space-y-2">
            {generic.map((item) => (
              <li
                key={item.pipelineItemId}
                className="border border-border rounded-xl px-3 py-2 bg-bg flex items-center justify-between gap-3"
              >
                <div>
                  <div className="text-xs font-medium">
                    {item.modelName}{" "}
                    <span className="text-text3 font-normal">tam. {item.size}</span>{" "}
                    — <span className="font-mono">{item.quantity}</span> peças → próxima:{" "}
                    <strong>{stageLabel(item.stage)}</strong>
                  </div>
                  <div className="text-[11px] text-text3 mt-0.5">
                    Ordem #{item.orderNumber} ·{" "}
                    <span
                      className="inline-block w-2 h-2 rounded-full mr-1 align-middle border border-black/10"
                      style={{ background: item.colorHex }}
                    />
                    {item.colorName}
                    {item.colorHasLining && (
                      <span className="ml-2 text-amber">com forro</span>
                    )}
                  </div>
                </div>
                <button
                  type="button"
                  disabled={pendingId !== null}
                  onClick={() => sendGeneric(item)}
                  className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-accent-light text-accent border border-accent-border hover:opacity-90 transition-opacity disabled:opacity-60"
                >
                  {pendingId === item.pipelineItemId
                    ? "Enviando..."
                    : `Enviar para ${stageLabel(item.stage)}`}
                </button>
              </li>
            ))}
          </ul>
        )}
      </Section>

      <Section
        title="Distribuir peças cortadas → Costureira"
        emptyMessage="Nenhum lote aguardando costura."
      >
        {perUser.length > 0 &&
          (seamstresses.length === 0 ? (
            <p className="text-[11px] text-text3">
              Nenhuma costureira cadastrada — adicione um operador com função “Costureira”.
            </p>
          ) : (
            <ul className="space-y-2">
              {perUser.map((item) => (
                <SewingDispatchRow
                  key={item.pipelineItemId}
                  item={item}
                  seamstresses={seamstresses}
                  pending={pendingId === item.pipelineItemId}
                  disabled={pendingId !== null}
                  onDispatch={(userId, quantity) => sendSewing(item, userId, quantity)}
                />
              ))}
            </ul>
          ))}
      </Section>

      <Section
        title="Montar pacote → Lavanderia"
        emptyMessage="Nenhum lote aguardando lavanderia."
      >
        {pkg.length > 0 && (
          <div>
            <p className="text-[11px] text-text3 mb-2">
              Selecione os lotes a enviar juntos como pacote.
            </p>
            <ul className="space-y-1 mb-3">
              {pkg.map((item) => {
                const checked = selectedForLaundry.has(item.pipelineItemId);
                return (
                  <li key={item.pipelineItemId}>
                    <label className="flex items-center gap-2 px-3 py-2 border border-border rounded-lg bg-bg cursor-pointer hover:border-accent-border">
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={(e) =>
                          setSelectedForLaundry((prev) => {
                            const next = new Set(prev);
                            if (e.target.checked) next.add(item.pipelineItemId);
                            else next.delete(item.pipelineItemId);
                            return next;
                          })
                        }
                        className="accent-accent"
                      />
                      <div className="flex-1">
                        <div className="text-xs font-medium">
                          {item.modelName} tam. {item.size} —{" "}
                          <strong className="text-accent font-mono">
                            {item.quantity}
                          </strong>{" "}
                          peças
                        </div>
                        <div className="text-[11px] text-text3">
                          Ordem #{item.orderNumber} ·{" "}
                          <span
                            className="inline-block w-2 h-2 rounded-full mr-1 align-middle border border-black/10"
                            style={{ background: item.colorHex }}
                          />
                          {item.colorName}
                        </div>
                      </div>
                    </label>
                  </li>
                );
              })}
            </ul>
            <button
              type="button"
              disabled={bundling || selectedForLaundry.size === 0}
              onClick={bundlePackage}
              className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-green text-white border border-green hover:opacity-90 transition-opacity disabled:opacity-60"
            >
              {bundling
                ? "Enviando..."
                : `Enviar pacote (${selectedForLaundry.size} ${selectedForLaundry.size === 1 ? "lote" : "lotes"})`}
            </button>
          </div>
        )}
      </Section>
    </div>
  );
}

function SewingDispatchRow({
  item,
  seamstresses,
  pending,
  disabled,
  onDispatch,
}: {
  item: AwaitingItemDto;
  seamstresses: UserDto[];
  pending: boolean;
  disabled: boolean;
  onDispatch: (userId: string, quantity: number) => void;
}) {
  const [userId, setUserId] = useState(seamstresses[0]?.id ?? "");
  const [qty, setQty] = useState<string>(String(item.quantity));

  return (
    <li className="border border-border rounded-xl px-3 py-2 bg-bg">
      <div className="flex items-start justify-between gap-3 mb-2">
        <div>
          <div className="text-xs font-medium">
            {item.modelName}{" "}
            <span className="text-text3 font-normal">tam. {item.size}</span>{" "}
            — <span className="font-mono">{item.quantity}</span> peças disponíveis
          </div>
          <div className="text-[11px] text-text3 mt-0.5">
            Ordem #{item.orderNumber} ·{" "}
            <span
              className="inline-block w-2 h-2 rounded-full mr-1 align-middle border border-black/10"
              style={{ background: item.colorHex }}
            />
            {item.colorName}
            {item.colorHasLining && <span className="ml-2 text-amber">com forro</span>}
          </div>
        </div>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <label className="text-[11px] text-text2">Costureira:</label>
        <select
          value={userId}
          onChange={(e) => setUserId(e.target.value)}
          className="text-xs px-2 py-1.5 border border-border2 rounded-lg bg-card"
        >
          {seamstresses.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
        <label className="text-[11px] text-text2">Qtd:</label>
        <input
          type="number"
          min={1}
          max={item.quantity}
          value={qty}
          onChange={(e) => setQty(e.target.value)}
          className="w-20 text-xs px-2 py-1.5 border border-border2 rounded-lg font-mono"
        />
        <button
          type="button"
          disabled={disabled}
          onClick={() => {
            const n = parseInt(qty, 10);
            if (!Number.isFinite(n) || n <= 0 || n > item.quantity) return;
            onDispatch(userId, n);
          }}
          className="px-3.5 py-1.5 rounded-lg text-xs font-medium bg-accent-light text-accent border border-accent-border hover:opacity-90 transition-opacity disabled:opacity-60"
        >
          {pending ? "Enviando..." : "Distribuir"}
        </button>
        {Number.parseInt(qty, 10) < item.quantity && (
          <span className="text-[11px] text-text3">
            ({item.quantity - (Number.parseInt(qty, 10) || 0)} ficarão aguardando)
          </span>
        )}
      </div>
    </li>
  );
}

function Section({
  title,
  emptyMessage,
  children,
}: {
  title: string;
  emptyMessage: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <div className="text-sm font-semibold mb-2">{title}</div>
      {Array.isArray(children) ? children : children}
      {!hasChildren(children) && (
        <p className="text-xs text-text3">{emptyMessage}</p>
      )}
    </div>
  );
}

function hasChildren(node: React.ReactNode): boolean {
  if (node === null || node === undefined || node === false) return false;
  if (Array.isArray(node)) return node.some(hasChildren);
  return true;
}
