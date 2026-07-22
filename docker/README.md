# Docker (development)

## PostgreSQL only (current)

```bash
cd docker
docker compose up -d
```

### Credential alignment (task 2.3.11)

| Item | Value |
| ---- | ----- |
| Host (from host machine) | `localhost` |
| Port | `5432` |
| Database | `subify_db` |
| User | `subify_admin` |
| Password | `SecretPassword123!` |
| Container | `subify_postgres` |

**Same values** are used in:

| File | Role |
| ---- | ---- |
| `docker-compose.yaml` | `POSTGRES_*` (defaults + `.env`) |
| `docker/.env.example` | Documented sample |
| `api/Subify.Api/appsettings.json` | Connection string fallback |
| `api/Subify.Api/appsettings.Development.json` | Development override (explicit) |

Npgsql:

```
Host=localhost;Port=5432;Database=subify_db;Username=subify_admin;Password=SecretPassword123!
```

### Override locally

```bash
cp .env.example .env
# edit POSTGRES_PASSWORD etc.
docker compose up -d
```

If you change password/db/user, update **both** `.env` and `appsettings.Development.json`.

### Useful commands

```bash
docker compose ps
docker compose logs -f postgres
docker exec -it subify_postgres psql -U subify_admin -d subify_db
docker compose down          # stop (keep volume)
docker compose down -v       # stop + wipe data
```

Full stack compose (api + web + postgres) is planned later (Faz 11).
