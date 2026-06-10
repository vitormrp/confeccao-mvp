/**
 * API base URL resolution:
 * - In the browser, always use NEXT_PUBLIC_API_URL — that's what reaches the backend
 *   from the user's machine through the published port.
 * - On the server (RSC, route handlers, server actions), prefer INTERNAL_API_URL
 *   when set so containers can talk over the docker network without going back
 *   out to the host.
 */
function resolveBaseUrl(): string {
  const isServer = typeof window === "undefined";
  if (isServer) {
    return (
      process.env.INTERNAL_API_URL ??
      process.env.NEXT_PUBLIC_API_URL ??
      "http://localhost:5080"
    );
  }
  return process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";
}

type FetchOptions = RequestInit & { userId?: string };

/**
 * HTTP statuses we treat as "transient, backend probably waking up" — Render's
 * proxy returns these (with its init-page HTML body) while a free-tier service
 * is cold-starting. Without retries, a sleeping backend torpedoes every SSR
 * render of /manager/production.
 */
const TRANSIENT_STATUSES = new Set([502, 503, 504]);

/**
 * Delays between retries (ms). Cumulative budget ≈ 27s — enough to cover a
 * typical Render + .NET cold start. Each delay also gives Neon time to wake.
 */
const RETRY_DELAYS_MS = [1000, 2000, 4000, 8000, 12000];

async function fetchWithRetry(url: string, init: RequestInit): Promise<Response> {
  let lastError: unknown = null;
  let lastStatus: number | null = null;

  for (let attempt = 0; attempt <= RETRY_DELAYS_MS.length; attempt++) {
    if (attempt > 0) {
      await new Promise((resolve) => setTimeout(resolve, RETRY_DELAYS_MS[attempt - 1]));
    }
    try {
      const res = await fetch(url, init);
      // Pass through any non-transient response (success or genuine error).
      if (!TRANSIENT_STATUSES.has(res.status)) return res;
      lastStatus = res.status;
      // Drain the body so the connection can be reused for the retry.
      await res.text().catch(() => "");
    } catch (err) {
      // Network errors throw TypeError in fetch — likely the proxy hasn't
      // routed to a live container yet. Worth retrying.
      lastError = err;
    }
  }

  if (lastError instanceof Error) throw lastError;
  throw new Error(
    `Backend did not respond after ${RETRY_DELAYS_MS.length + 1} attempts` +
      (lastStatus !== null ? ` (last status: ${lastStatus})` : ""),
  );
}

export async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  const { userId, headers, ...rest } = options;
  const finalHeaders = new Headers(headers);
  finalHeaders.set("Content-Type", "application/json");
  if (userId) finalHeaders.set("X-User-Id", userId);

  const res = await fetchWithRetry(`${resolveBaseUrl()}${path}`, {
    ...rest,
    headers: finalHeaders,
    cache: "no-store",
  });

  if (!res.ok) {
    const body = await res.text().catch(() => "");
    throw new Error(`API ${res.status} ${res.statusText}: ${body.slice(0, 200)}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}
