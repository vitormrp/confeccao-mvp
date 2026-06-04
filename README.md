# Confecção MVP

Clothing production management — manager creates production orders that flow through stage-specific operator queues (cutting → interfacing? → sewing → washing → buttoning? → labeling → pressing). Each completion accrues operator credits which the manager pays out.

## Stack

- **Frontend** — Next.js 15 (App Router), TypeScript, Tailwind, npm.
- **Backend** — .NET 9 Minimal API, EF Core (Npgsql), xUnit.
- **Database** — Postgres 16.
- **Local dev** — Docker Compose for the full stack, or run backend/frontend natively against just the Dockerized Postgres for faster iteration.

## Getting started

```bash
cp .env.example .env
docker compose up
```

That brings up Postgres, the backend (`http://localhost:5080`), and the frontend (`http://localhost:3000`). Health checks: `GET http://localhost:5080/api/v1/health`.

### Faster local dev

Run only the database in Docker, and the apps natively:

```bash
docker compose up postgres -d
cd backend && dotnet watch --project src/Confeccao.Api
cd frontend && npm install && npm run dev
```

### Tests

Backend: `cd backend && dotnet test`. Frontend Playwright tests are future work.

## Layout

```
backend/
  Confeccao.sln
  src/Confeccao.Api/      # minimal API
  src/Confeccao.Domain/   # entities, enums, events
  tests/Confeccao.UnitTests/
frontend/
  app/                    # App Router pages
  lib/                    # api clients, hooks
  components/             # shared UI
docker-compose.yml
```

## Phase status

- [x] Phase 0 — repo skeleton, Docker compose, services booting
- [x] Phase 1 — reference data + seed
- [x] Phase 2 — order creation + cutter
- [x] Phase 3 — generic stage handlers + dispatch
- [x] Phase 4 — sewing (multi-user dispatch)
- [x] Phase 5 — laundry packages
- [x] Phase 6 — financial (pricing, credits, payments)
- [x] Phase 7 — polish (Cadastros CRUD, notifications strip, "A receber" widget, reports, sample seeder)

## Deploy

See **[DEPLOY.md](./DEPLOY.md)** for a step-by-step guide to getting this live on Render + Neon (free tier).
