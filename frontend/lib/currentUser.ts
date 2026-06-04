"use client";

import { useSyncExternalStore } from "react";

const STORAGE_KEY = "confeccao.currentUserId";
const SAME_TAB_EVENT = "confeccao:currentUserId-changed";

/**
 * Subscribes to BOTH the native cross-tab `storage` event AND a custom
 * same-tab event we dispatch from `setUserId` below. Without the custom
 * event, components in the same tab that read this hook independently
 * end up with diverging local state — the bug where actions kept firing
 * with the previously-picked user's id until a refresh.
 */
function subscribe(callback: () => void) {
  const handle = (event: StorageEvent | Event) => {
    if (event instanceof StorageEvent && event.key !== STORAGE_KEY && event.key !== null) return;
    callback();
  };
  window.addEventListener("storage", handle);
  window.addEventListener(SAME_TAB_EVENT, handle);
  return () => {
    window.removeEventListener("storage", handle);
    window.removeEventListener(SAME_TAB_EVENT, handle);
  };
}

function getSnapshot(): string | null {
  return window.localStorage.getItem(STORAGE_KEY);
}

function getServerSnapshot(): string | null {
  // SSR has no localStorage; emit null so the client matches on first paint.
  return null;
}

/**
 * MVP impl of the "current user" — backed by localStorage. Future: replace
 * the body with a session-context lookup once auth lands. Same hook signature,
 * no call-site changes.
 *
 * All components that call this share a single source of truth; updates from
 * any one propagate to every consumer synchronously via the change event.
 */
export function useCurrentUser() {
  const userId = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);

  const setUserId = (id: string | null) => {
    if (typeof window === "undefined") return;
    const current = window.localStorage.getItem(STORAGE_KEY);
    if (current === id) return; // no-op; avoid notifying subscribers for nothing
    if (id) window.localStorage.setItem(STORAGE_KEY, id);
    else window.localStorage.removeItem(STORAGE_KEY);
    window.dispatchEvent(new Event(SAME_TAB_EVENT));
  };

  return { userId, setUserId };
}
