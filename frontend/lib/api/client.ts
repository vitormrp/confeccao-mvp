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

export async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  const { userId, headers, ...rest } = options;
  const finalHeaders = new Headers(headers);
  finalHeaders.set("Content-Type", "application/json");
  if (userId) finalHeaders.set("X-User-Id", userId);

  const res = await fetch(`${resolveBaseUrl()}${path}`, {
    ...rest,
    headers: finalHeaders,
    cache: "no-store",
  });

  if (!res.ok) {
    const body = await res.text().catch(() => "");
    throw new Error(`API ${res.status} ${res.statusText}: ${body}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}
