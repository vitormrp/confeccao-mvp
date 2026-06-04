# Deploy — Render + Neon (free)

This walks through getting the MVP live on the free tiers of [Render](https://render.com) (backend + frontend) and [Neon](https://neon.tech) (Postgres). Expected end state: two public URLs, ~$0/month, MVP-ready.

> **No auth yet.** Operator pages are anonymous — anyone with the URL can register cuts as anyone. Share the URL only with people you trust until you wire login.

## What you'll get

| Piece | Where it runs | Free-tier caveat |
|---|---|---|
| `confeccao-frontend.onrender.com` | Render web service | Cold-starts after 15 min idle (~30–60s first hit) |
| `confeccao-backend.onrender.com` | Render web service | Same cold-start behavior |
| Postgres | Neon | 0.5 GB storage, autosuspends after ~5 min, ~1s wake-up |

## Prerequisites

- GitHub account with this repo pushed up (Render pulls from GitHub).
- A [Neon account](https://console.neon.tech) (free).
- A [Render account](https://dashboard.render.com) (free, asks for a card but doesn't charge on free tier).

## Step 1 — Create a Neon Postgres

1. Log into Neon → **New Project**.
2. Project name: `confeccao` (anything). Region: pick whatever's closest to your Render region.
3. Database name: `confeccao` (any).
4. After creation, on the project's dashboard, copy the **Connection string** that looks like:
   ```
   postgresql://owner_user:somerandompassword@ep-cool-name-12345.us-east-2.aws.neon.tech/confeccao?sslmode=require
   ```
   Stash this — you'll paste it into Render in step 3.

That's it for Neon. Schema + seed data run automatically on the backend's first boot.

## Step 2 — Create the Render services from the blueprint

1. Push this repo to GitHub if you haven't already.
2. Render dashboard → **New** → **Blueprint**.
3. Connect your GitHub repo. Render auto-detects `render.yaml` at the root.
4. Review the two services it's about to create (`confeccao-backend`, `confeccao-frontend`) and click **Apply**.
5. The first build will pause on the backend with a "missing environment variable" prompt — that's expected. Continue to step 3.

## Step 3 — Set DATABASE_URL on the backend

1. Render dashboard → `confeccao-backend` service → **Environment**.
2. Find the `DATABASE_URL` row, click **Edit**, paste your Neon connection string.
3. Save. Render auto-redeploys.

Watch the backend's deploy log: you should see EF Core apply migrations, then `Application started. Now listening on: http://+:8080`.

Hit `https://confeccao-backend.onrender.com/api/v1/health` — expect `{"status":"ok","database":"reachable",...}`.

## Step 4 — Wait for the frontend deploy

The frontend service builds in parallel. Once it's green:

- `https://confeccao-frontend.onrender.com/` redirects to `/manager/production`.
- The HealthBadge component on the production page should turn green; if it stays red, see *Troubleshooting* below.

## Step 5 — (Optional) Seed sample data

If the dashboard looks too empty to evaluate:

1. `confeccao-backend` service → **Environment** → set `SEED_SAMPLE_DATA` to `true`.
2. Redeploy. The startup seeder inserts 2 demo orders sitting at "Aguardando corte".
3. Set the env var back to `false` so subsequent boots don't keep trying (the seeder is idempotent anyway, but it's tidier).

---

## Troubleshooting

**Backend says `Postgres connection string not configured`.**
You skipped step 3, or the env var name is misspelled. It must be exactly `DATABASE_URL`.

**Backend connects but errors on `column "MigrationId" does not exist`.**
Same issue we hit locally: the `__EFMigrationsHistory` table was created without the snake-case naming convention. In Neon → SQL editor: `DROP TABLE "__EFMigrationsHistory";` then redeploy. Won't happen on a fresh DB.

**Frontend renders but every API call fails with CORS error.**
The `CORS_ALLOWED_ORIGINS` value on the backend doesn't match the actual frontend URL. Render will have used `confeccao-frontend.onrender.com` if the service name is exactly that; otherwise edit the env var to match what's in your address bar.

**Frontend hits `localhost:5080` in the browser even after deploy.**
`NEXT_PUBLIC_API_URL` wasn't set when the frontend Docker image was *built*. Render does inject service env vars into the build, but if the env var was missing on first build it baked in the fallback. Fix: confirm `NEXT_PUBLIC_API_URL` is set on the frontend service, then **Manual Deploy → Clear build cache & deploy**.

**First request after a long pause hangs for ~30s.**
Render's free tier sleeps services after 15 minutes of no traffic. First hit wakes both services + Neon (~30–60s total). Not fixable without upgrading; tolerable for MVP validation.

**You're getting paid plans pushed at you.**
The free path is: Render Starter (free) for both web services, Neon Free Tier for Postgres. If Render asks you to upgrade, you don't need to — the blueprint sets `plan: free` explicitly.

---

## Updating after the first deploy

`autoDeploy: true` is set in the blueprint, so every push to your default branch triggers a new build for both services. Migrations are applied automatically on backend startup.

## When you outgrow the free tier

- Backend not cold-starting: upgrade Render's backend service to Starter ($7/mo).
- Database > 0.5 GB or want backups: Neon Launch tier ($19/mo) or any other Postgres provider.
- Real auth: see the original plan's auth foundation — `ICurrentUserContext` is the swap point on the backend, `useCurrentUser` on the frontend.
