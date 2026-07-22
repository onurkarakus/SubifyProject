# EF Core migrations — baseline review (task 2.3.12)

**Status (2026-07-22):** Model is **in sync** with the last migration.  
`dotnet ef migrations has-pending-model-changes` → *No changes have been made to the model since the last migration.*

**Applied on dev Postgres:** all **11** migrations present in `__EFMigrationsHistory`.

## Chain (linear)

| # | Migration | Purpose |
| - | --------- | ------- |
| 1 | `20260716202334_InitialCreate` | Identity + core tables |
| 2 | `20260721163806_UpdateRefreshTokenEntity` | Refresh token field renames |
| 3 | `20260722094204_RenameLocateToLocale` | `Locate` → `Locale` |
| 4 | `20260722094434_AlignApplicationUserProfileFields` | Profile lengths/defaults |
| 5 | `20260722095508_StrengthenSubscriptionDomain` | Subscription constraints/indexes |
| 6 | `20260722095705_RenameProviderLogoutToLogoUrl` | `Logout` → `LogoUrl` |
| 7 | `20260722095845_ExpandSystemSettingsInstanceModel` | Setup + SMTP + AI fields |
| 8 | `20260722100059_AlignRefreshTokenRotationFields` | Rotation/hash fields + indexes |
| 9 | `20260722100525_AddUserInviteEntity` | `UserInvites` |
| 10 | `20260722100729_AddUserDeviceTokenEntity` | `UserDeviceTokens` |
| 11 | `20260722101332_CompleteEntityTypeConfigurations` | Max lengths, unique indexes, composite indexes |

**Tip of baseline:** `CompleteEntityTypeConfigurations`  
**Snapshot:** `SubifyDbContextModelSnapshot.cs`

## Code-only (no migration needed)

| Feature | Why no schema migration |
| ------- | ------------------------ |
| Soft-delete query filters | Filter on existing `DeletedAt` |
| UUID v7 client Ids | App-side generation; `ValueGeneratedNever` / SaveChanges |
| Seed data (2.3.3+) | Runtime `IDataSeeder`, not EF migrations |

## Squash policy

**Do not squash now** while:

- Local/dev databases already hold the 11-row history
- Active development continues on Faz 2–3

**Consider squash** before first public release / Docker “fresh install only” image:

1. Ensure no pending model changes.
2. Backup any shared DBs.
3. Remove all migrations + snapshot.
4. `dotnet ef migrations add InitialCreateOsBaseline`
5. Document: existing installs must either stay on old chain **or** wipe DB and re-seed (no automatic upgrade from pre-squash).

For self-host OS, prefer **linear history until v1.0**, then optional single baseline for clean installs only.

## Commands

```bash
cd api

# List migrations (applied vs pending)
dotnet ef migrations list \
  --project Subify.Infrastructure \
  --startup-project Subify.Api

# Detect drift vs model
dotnet ef migrations has-pending-model-changes \
  --project Subify.Infrastructure \
  --startup-project Subify.Api

# Add migration after domain change
dotnet ef migrations add <Name> \
  --project Subify.Infrastructure \
  --startup-project Subify.Api

# Manual apply (optional — API auto-migrates on start)
dotnet ef database update \
  --project Subify.Infrastructure \
  --startup-project Subify.Api
```

Startup path: `DatabaseInitializer` → `DatabaseMigrator.MigrateAsync` → seeders.
