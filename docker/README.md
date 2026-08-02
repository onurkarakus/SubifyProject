# Subify OS — Docker

## One command (full stack)

```bash
cd docker
cp .env.example .env
# edit JWT_SECRET_KEY and POSTGRES_PASSWORD
docker compose up -d --build
```

| Service | URL |
| ------- | --- |
| Web | http://localhost:3000 |
| API | http://localhost:5240 |
| Scalar (dev only*) | — use Development profile for Scalar |
| Health | http://localhost:5240/health |
| Ready | http://localhost:5240/health/ready |
| Postgres | localhost:5432 |

\* Scalar OpenAPI UI is enabled when `ASPNETCORE_ENVIRONMENT=Development`.

First API start **auto-migrates** EF Core schema and runs seeders (roles, categories, resources, empty SystemSettings). Cold start empty volume is supported (11.1.7).

### First-run setup

1. Open web → register/login may be blocked until setup completes  
2. Create SuperAdmin via API setup: see [ops docs](../docs/OPS.md)  
3. Or call `POST /api/setup/admin` then finish setup steps  

---

## Postgres only (local API/web on host)

```bash
cd docker
docker compose -f docker-compose.db.yaml up -d
```

Then:

```bash
cd ../api/Subify.Api && dotnet run --launch-profile http
cd ../web && npm run dev
```

Connection string must match compose credentials (defaults in `.env.example`).

---

## Environment

| Variable | Purpose |
| -------- | ------- |
| `POSTGRES_*` | Database |
| `JWT_SECRET_KEY` | **Change in production** (≥ 32 chars) |
| `WEB_ORIGIN` | CORS allowed origin |
| `NEXT_PUBLIC_API_URL` | Browser → API base (`…/api`) |
| `EXCHANGE_RATE_API_KEY` | Optional FX provider key |

See [`.env.example`](./.env.example).

---

## Healthchecks (11.1.6)

| Check | Endpoint | Meaning |
| ----- | -------- | ------- |
| API liveness | `GET /health` | Process up |
| API readiness | `GET /health/ready` | Postgres reachable |
| Compose | service `healthcheck` | api waits for postgres healthy; web waits for api healthy |

---

## Reverse proxy (11.1.5)

Examples:

- [`Caddyfile`](./Caddyfile)
- [`nginx.conf.example`](./nginx.conf.example)

Set:

```env
WEB_ORIGIN=https://subify.example.com
NEXT_PUBLIC_API_URL=https://subify.example.com/api
```

Rebuild web after changing `NEXT_PUBLIC_API_URL` (build-time bake).

---

## Useful commands

```bash
docker compose ps
docker compose logs -f api
docker compose logs -f web
docker exec -it subify_postgres psql -U subify_admin -d subify_db
docker compose down          # keep volume
docker compose down -v       # wipe DB volume
```

### Backup / restore

```bash
# From repo root
./docker/scripts/backup-postgres.sh
# → ./backups/subify-YYYYMMDD-HHMMSS.dump

# Restore (interactive "yes"; stop api/web first)
# docker compose stop api web
./docker/scripts/restore-postgres.sh ./backups/subify-YYYYMMDD-HHMMSS.dump
```

Provider catalog sample for SuperAdmin import:  
[`data/provider-catalog.sample.json`](../data/provider-catalog.sample.json)

Backup / upgrade / troubleshooting: [`docs/OPS.md`](../docs/OPS.md)
