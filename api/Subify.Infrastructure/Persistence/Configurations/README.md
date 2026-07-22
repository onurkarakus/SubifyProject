# EF Core entity configurations

All `IEntityTypeConfiguration<T>` implementations live in this folder.

## Convention

| Item | Rule |
| ---- | ---- |
| Location | `Subify.Infrastructure/Persistence/Configurations/` |
| Class naming | `{EntityName}Configuration.cs` |
| Registration | Auto-scanned via `builder.ApplyConfigurationsFromAssembly(typeof(SubifyDbContext).Assembly)` in `SubifyDbContext.OnModelCreating` |
| Cross-cutting | Soft-delete filters + GUID v7 Id: applied in `SubifyDbContext` (not per-entity) |
| **DB naming** | **PascalCase** tables and columns (EF defaults). **No snake_case.** |

### Database naming (task 2.2.9 / ADR-011)

Subify OS keeps **PascalCase** for Postgres identifiers:

| Kind | Examples |
| ---- | -------- |
| Identity tables | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, … |
| Domain tables | `Subscriptions`, `Categories`, `SystemSettings`, … |
| Columns | `UserId`, `NextRenewalDate`, `LogoUrl`, … |

**Why not snake_case?**

- Matches ASP.NET Core Identity defaults out of the box
- Avoids a full rewrite of existing migrations / history
- Docs and tools already reference `AspNetUsers`
- `EFCore.NamingConventions` / `UseSnakeCaseNamingConvention()` is **not** used

If snake_case is ever required later, it is a greenfield migration decision (new major schema), not a silent rename.

## Files

| Configuration | Entity | Notes |
| ------------- | ------ | ----- |
| `ApplicationUserConfiguration` | `ApplicationUser` | → `AspNetUsers`; profile fields |
| `Identity*Configuration` | Identity role/claim/login/token | → standard `AspNet*` tables |
| `SubscriptionConfiguration` | `Subscription` | Core indexes, FKs, UserShare ignored |
| `CategoryConfiguration` | `Category` | Unique slug, is_active |
| `UserCategoryConfiguration` | `UserCategory` | Per-user index |
| `ProviderConfiguration` | `Provider` | Unique slug, LogoUrl, is_active |
| `ResourceConfiguration` | `Resource` | Unique (PageName, Name, LanguageCode) |
| `RefreshTokenConfiguration` | `RefreshToken` | Token hash index, rotation fields |
| `ActivityLogConfiguration` | `ActivityLog` | Dashboard query index |
| `AiSuggestionLogConfiguration` | `AiSuggestionLog` | User history index |
| `NotificationSettingConfiguration` | `NotificationSetting` | One row per user |
| `EmailTemplatesConfiguration` | `EmailTemplates` | Unique (Name, LanguageCode) — Faz 15 |
| `ExchangeRateSnapshotConfiguration` | `ExchangeRateSnapshot` | Rate lookup index |
| `SystemSettingsConfiguration` | `SystemSettings` | Instance singleton fields |
| `UserInviteConfiguration` | `UserInvite` | Unique token hash |
| `UserDeviceTokenConfiguration` | `UserDeviceToken` | Unique push token |

## Adding a new entity

1. Create domain entity under `Subify.Domain/Entities`.
2. Add `DbSet<>` on `SubifyDbContext`.
3. Add `{Name}Configuration.cs` here.
4. `dotnet ef migrations add ...` and let auto-migrate apply.
