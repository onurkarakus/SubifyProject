# Subify OS — Architecture (English)

Short technical overview for reviewers, contributors, and self-hosters.  
Deep product memory (PRD, task lists, SaaS plans) lives in other files under [`docs/`](./README.md) and may be in Turkish — this page is the **public architecture snapshot**.

---

## Goals that shape the design

| Goal | Architectural consequence |
| ---- | ------------------------- |
| Self-hosted, no freemium | No Stripe/RevenueCat in the OS runtime; no plan gates on features |
| Own your data | Single PostgreSQL; auto-migrate on API start |
| BYOK AI & SMTP | Secrets on `SystemSettings` (admin UI); never required for core CRUD |
| Multi-user instance | ASP.NET Identity + roles (`SuperAdmin`, `Admin`, `User`); invites |
| Maintainable growth | Clean Architecture + CQRS (MediatR) |

---

## System context

```
                    Browser (Next.js)
                           │
                           │  HTTPS / JSON  +  JWT Bearer
                           ▼
              ┌────────────────────────────┐
              │  Subify.Api                │
              │  Minimal APIs · Middleware │
              │  Auth · Setup gate · CORS  │
              └────────────┬───────────────┘
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
  Application         Infrastructure      External
  (handlers)          EF · jobs · I/O     FX API
  MediatR CQRS        Identity            SMTP
  validators          AI HTTP client      LLM API
         │                 │
         └────────┬────────┘
                  ▼
            PostgreSQL
```

| Process | Role |
| ------- | ---- |
| **web** | UI only; tokens in `sessionStorage`; talks to `/api` |
| **api** | Business rules, auth, migrations, background jobs |
| **postgres** | Source of truth |

---

## Backend layers (`api/`)

```
Subify.Api                → HTTP, DI composition, middleware, OpenAPI (dev)
Subify.Application        → Use cases (commands/queries), DTOs, interfaces
Subify.Domain             → Entities, domain services (math, conversion), errors
Subify.Infrastructure     → EF Core, email, FX, AI client, hosted services
*.Tests                   → Domain + handler tests
```

### Dependency rule

`Api` → `Application` → `Domain`  
`Infrastructure` implements `Application` interfaces and is wired in `Api` / `DependencyInjection`.

### Request path (typical)

1. Minimal API endpoint maps route → MediatR request  
2. FluentValidation (where registered)  
3. Handler loads data via `ISubifyDbContext` / `UserManager`  
4. Domain entity methods enforce invariants (e.g. subscription price, category XOR)  
5. `Result<T>` / domain error codes → HTTP problem details  

### Auth

- JWT access token + refresh token flow  
- Roles on claims; Super Admin for instance settings, users, templates, jobs  
- **Setup gate:** until setup completes, only setup/auth/health-style routes are open  

### Background work

Hosted services (when `BackgroundJobs:Enabled`):

- **FX sync** — periodic fetch → `ExchangeRateSnapshot` rows  
- **Renewal reminders** — SMTP + user notification prefs + dedupe  

Super Admin can also trigger some jobs manually (ops).

---

## Domain concepts (core)

| Concept | Notes |
| ------- | ----- |
| **Subscription** | User-owned; archive = soft-delete; `SharedWithCount` → user share |
| **Billing cycle** | Monthly / yearly; monthly equivalent for totals |
| **Main currency** | Profile preference; reports convert via FX snapshots |
| **Provider catalog** | Optional catalog rows; free-text name still allowed |
| **SystemSettings** | Singleton instance config (locale/currency defaults, SMTP, AI) |
| **AiSuggestionLog** | Persisted analyze history (BYOK LLM) |
| **SubscriptionPriceHistory** | Audit when price/currency changes |

Financial helpers live under `Domain/Services` (subscription math, currency conversion) so Application stays thin.

---

## Frontend (`web/`)

| Area | Stack |
| ---- | ----- |
| Framework | Next.js App Router, TypeScript, Tailwind |
| Auth | Login/register/setup; session tokens; role-aware nav |
| App routes | Dashboard, subscriptions, reports, AI, profile, admin |
| UX extras | Dual money display, FX sidebar, i18n TR/EN strings |

The web app does **not** own business rules for money math; it presents API data and client-only helpers (ICS, CSV dry-run, what-if).

---

## Cross-cutting concerns

| Concern | Approach |
| ------- | -------- |
| **Errors** | Stable error codes → problem+json for clients |
| **Secrets** | Masked in admin GET; not written to activity logs in plain form |
| **CORS** | Explicit origins; production empty list = deny |
| **Health** | `/health` liveness, `/health/ready` DB readiness |
| **Migrations** | EF applied on API startup |

---

## Deployment sketch

```
docker compose
  ├── postgres
  ├── api   (migrate + seed + jobs)
  └── web   (NEXT_PUBLIC_API_URL → public API)
```

Optional reverse proxy: Caddy/nginx samples under `docker/`.  
Backup: host-side `pg_dump` scripts — see [OPS.md](./OPS.md).

---

## What this architecture deliberately excludes (OS)

- Multi-tenant `OrganizationId` / plan entitlements (Cloud path only)  
- Forced email confirmation (OS product decision)  
- Embedded payment SDKs  

Those are documented separately for a **future managed product**, not mixed into the self-host core.

---

## Where to go next

| Need | Doc |
| ---- | --- |
| Run / backup / troubleshoot | [OPS.md](./OPS.md), [docker/README.md](../docker/README.md) |
| Full docs index | [docs/README.md](./README.md) |
| API shapes | [API_CONTRACTS.md](./API_CONTRACTS.md) |
| Data notes | [DATA_MODEL.md](./DATA_MODEL.md) |
| Screenshots | [screenshots/](./screenshots/) |
