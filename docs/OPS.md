# Subify OS — Operations

Self-host ops for Docker and bare-metal. Complements [`docker/README.md`](../docker/README.md).

---

## 11.2.1 Install (one command)

```bash
git clone <your-fork> SubifyProject
cd SubifyProject/docker
cp .env.example .env
# Set POSTGRES_PASSWORD and JWT_SECRET_KEY to strong values
docker compose up -d --build
```

Open **http://localhost:3000**

API: **http://localhost:5240** · Health: **/health** · Ready: **/health/ready**

### First SuperAdmin (setup)

While setup is incomplete, app APIs are gated. Create SuperAdmin:

```bash
curl -s -X POST http://localhost:5240/api/setup/admin \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@example.com","password":"Password1","fullName":"Admin"}'
```

Then complete instance steps (or use setup endpoints documented in Scalar when `ASPNETCORE_ENVIRONMENT=Development`).

Finish setup:

```bash
# After login as SuperAdmin, complete remaining PUT /api/setup/* steps
# then POST /api/setup/complete
```

---

## 11.2.2 Backup / restore (Postgres)

Self-host **does not** ship an in-app backup/restore UI (risk of remote wipe).  
SuperAdmin sees copy-paste commands under **System settings → Health / Ops**.

### Quick script (recommended)

```bash
# From repo root — custom-format dump into ./backups/
chmod +x docker/scripts/backup-postgres.sh docker/scripts/restore-postgres.sh
./docker/scripts/backup-postgres.sh
# → backups/subify-YYYYMMDD-HHMMSS.dump
```

Optional env: `CONTAINER`, `POSTGRES_USER`, `POSTGRES_DB`, `OUT_DIR`.

### Manual backup

```bash
# Custom format (best for pg_restore)
docker exec -t subify_postgres pg_dump -U subify_admin -d subify_db \
  --format=custom -f /tmp/subify.dump

mkdir -p ./backups
docker cp subify_postgres:/tmp/subify.dump ./backups/subify-$(date +%Y%m%d).dump
```

SQL plain text:

```bash
docker exec -t subify_postgres pg_dump -U subify_admin -d subify_db \
  > ./backups/subify-$(date +%Y%m%d).sql
```

### Restore

```bash
# Stop API to avoid writes (recommended)
cd docker && docker compose stop api web

# Script (asks for "yes")
./docker/scripts/restore-postgres.sh ./backups/subify-YYYYMMDD-HHMMSS.dump

# or manual:
# docker cp ./backups/subify.dump subify_postgres:/tmp/subify.dump
# docker exec -i subify_postgres pg_restore -U subify_admin -d subify_db \
#   --clean --if-exists /tmp/subify.dump

docker compose start api web
```

### Scheduled backup (cron example)

Daily 03:15 host local time, keep last 14 dumps:

```cron
15 3 * * * cd /path/to/SubifyProject && ./docker/scripts/backup-postgres.sh >> /var/log/subify-backup.log 2>&1
# optional prune:
# 20 3 * * * find /path/to/SubifyProject/backups -name 'subify-*.dump' -mtime +14 -delete
```

Off-host copies: rsync/S3 the `backups/` folder — never only keep dumps on the same disk as the volume.

**Volumes:** Postgres data is in Docker volume `subify_postgres_data` (project-prefixed).  
Wipe: `docker compose down -v` — **destroys data**.

---

## 11.2.3 Upgrade / migrations

1. Pull new code  
2. Rebuild and recreate:

```bash
cd docker
docker compose pull   # if using published images
docker compose up -d --build
```

3. On API start, **EF Core migrations apply automatically** (`DatabaseMigrator` + seed).  
   No manual `dotnet ef database update` required for compose.

4. If a migration fails, check logs:

```bash
docker compose logs api | tail -100
```

5. Web: `NEXT_PUBLIC_API_URL` is compile-time. Changing the public API URL requires **rebuild** of the web image.

### Host-run API upgrade

```bash
cd api
dotnet build
cd Subify.Api && dotnet run
# migrate-on-start still applies
```

---

## 11.2.4 Troubleshooting

| Symptom | Checks |
| ------- | ------ |
| API unhealthy / restart loop | `docker compose logs api` — Postgres password/host; wait for healthy postgres |
| `Connection refused` to DB | Use host `postgres` inside compose; `localhost` only from host machine |
| CORS errors in browser | Set `Cors__AllowedOrigins` / `WEB_ORIGIN` to the exact web origin (scheme+host+port) |
| 401 after login | Clock skew; JWT secret changed (all tokens invalid); refresh token revoked |
| Setup required (403 AUTH_017) | Finish setup; `GET /api/setup/status` |
| Web cannot reach API | `NEXT_PUBLIC_API_URL` must be browser-reachable (not `http://api:8080`) |
| AI_KEY_MISSING | SuperAdmin → Admin → System settings → set LLM API key |
| FX rates empty | Check `ExchangeRates__Enabled`; optional `EXCHANGE_RATE_API_KEY`; fallback uses last snapshot |
| SMTP / email | Enable SMTP in Admin settings; test via **Test SMTP**. Forgot-password + invite + renewal reminders when `SmtpEnabled` + host/port/from set |
| Renewal emails not sending (8.1) | (1) SMTP configured (2) `BackgroundJobs__Enabled=true` (3) `EmailJobs__RenewalRemindersEnabled=true` (4) user **profile → email notifications** on (5) sub in `daysBeforeRenewal` window. Manual: `POST /api/admin/jobs/renewal-reminders/run` |
| Port already in use | Change `API_PORT` / `WEB_PORT` / `POSTGRES_PORT` in `.env` |
| Permission / non-root | Images run as non-root users `subify` / `nextjs` |

### Useful probes

```bash
curl -s http://localhost:5240/health | jq .
curl -s http://localhost:5240/health/ready | jq .
curl -s http://localhost:5240/api/setup/status | jq .
```

---

## Security checklist (self-host)

- [ ] Strong `POSTGRES_PASSWORD` and `JWT_SECRET_KEY`  
- [ ] Do not expose Postgres port publicly (remove port mapping or firewall)  
- [ ] TLS via Caddy/Nginx in production  
- [ ] Restrict CORS to your real web origin (`Cors__AllowedOrigins__0` / `WEB_ORIGIN`)  
- [ ] Keep SuperAdmin credentials private  
- [ ] Regular `pg_dump` backups  
- [ ] Edge security headers (see below)  

### CORS (14.1.3)

| Environment | Behaviour |
| ----------- | --------- |
| Development / Testing | Defaults to `http://localhost:3000` if `Cors:AllowedOrigins` empty |
| Production | **Empty = deny all browser origins** — set explicit web origin(s) |

```bash
# docker/.env or compose environment
# Cors__AllowedOrigins__0=https://subify.example.com
```

Never use wildcard `*` with credentialed cookies/JWT-from-browser SPA patterns.

### Security headers (14.1.4)

**API process** (`SecurityHeadersMiddleware`): `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, minimal CSP for `/api`, `Cache-Control: no-store` on API paths.

**Reverse proxy (recommended for HSTS + HTML CSP):**

| Header | Suggested value |
| ------ | --------------- |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (HTTPS only) |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |

See `docker/Caddyfile` and `docker/nginx.conf.example`.

### Secrets in responses / logs (14.1.1–14.1.2)

- `GET/PUT /api/admin/settings` returns `apiKeyMasked` / `passwordMasked` (`••••••••`) — never plain AI key or SMTP password  
- Invite **list** never returns plain tokens; create returns token **once** for admin copy  
- HTTP request logging: method/path/status only — no Authorization header, no body  
- Serilog destructuring masks properties named `password`, `*token*`, `*secret*`, `*apiKey*`, etc.  

### Dependency scan (14.1.5)

```bash
dotnet list api/Subify.Api/Subify.Api.csproj package --vulnerable --include-transitive
```

Microsoft.OpenApi must be **≥ 2.7.5** (GHSA-v5pm-xwqc-g5wc).

---

## Architecture (compose)

```
Browser → :3000 web (Next)
       → :5240 api (ASP.NET) → postgres:5432
```

API runs migrations before accepting traffic (startup). Background FX job runs inside API process (`BackgroundService`).
