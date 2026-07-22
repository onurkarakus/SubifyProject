# Startup seed pipeline (task 2.3.3)

Runs **after** EF migrations (`DatabaseMigrator`) and **before** HTTP traffic.

```
Program → DatabaseInitializer.InitializeAsync
            ├─ DatabaseMigrator.MigrateAsync   (2.3.2)
            └─ DatabaseSeeder.SeedAsync        (2.3.3)
                   └─ foreach IDataSeeder by Order
```

## Rules

| Rule | Detail |
| ---- | ------ |
| Idempotent (2.3.10) | Safe on every boot — **no duplicates** on second start |
| Never overwrite | Existing admin/custom rows are **not** updated by seeders |
| Scoped | Seeders resolve from a DI scope (`SubifyDbContext`, `RoleManager`, …) |
| Auto-register | Public non-abstract `IDataSeeder` types in Infrastructure are registered |
| Fail-fast | Unhandled seeder exception aborts startup |

## Idempotency strategies (task 2.3.10)

| Seeder | Strategy | “Empty?” check |
| ------ | -------- | -------------- |
| Roles | Per **role name** (`RoleExistsAsync`) | Missing names only |
| Categories | Per **slug** (`IgnoreQueryFilters` — unique index includes soft-deleted) | Missing slugs only |
| Providers | Per **slug** (same as categories) | Missing slugs only |
| Resources | Per **(PageName, Name, LanguageCode)** | Missing keys only |
| SystemSettings | **Table empty** → insert 1 singleton | `Count == 0` |
| EmailTemplates | Per **(Name, LanguageCode)** | Missing keys only |

**Contract:** second `DatabaseSeeder.SeedAsync` must leave row counts unchanged. Covered by `SeedIdempotencyTests`.

## Order bands

| Order | Seeder | Task | Status |
| ----- | ------ | ---- | ------ |
| 10 | `RolesDataSeeder` — SuperAdmin, Admin, User | 2.3.4 | Done |
| 20 | `CategoriesDataSeeder` — 10 system categories | 2.3.5 | Done |
| 30 | `ProvidersDataSeeder` — TR/global catalog (~27) | 2.3.6 | Done |
| 40 | `ResourcesDataSeeder` — Common/Category/Dashboard/Subscription/Error TR+EN | 2.3.7 | Done |
| 50 | `SystemSettingsDataSeeder` — singleton row (`IsSetupComplete=false`) | 2.3.9 | Done |
| 60 | `EmailTemplatesDataSeeder` — ResetPassword, RenewalReminder, Invite (no VerifyEmail) | 2.3.8 | Done (send = Faz 15) |

## Adding a seeder

1. Create `Persistence/Seeding/{Name}DataSeeder.cs` implementing `IDataSeeder`.
2. Set `Order` / `Name`.
3. Check existence before insert (slug, role name, etc.).
4. No extra DI registration needed.
