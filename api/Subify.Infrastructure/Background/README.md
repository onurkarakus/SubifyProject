# Background jobs (Faz 8)

## Decision (8.4.1)

| Option | v1 Subify OS |
| ------ | ------------ |
| **`BackgroundService`** | **Chosen** — zero extra deps, works in Docker single process |
| Hangfire / Quartz | Deferred — overkill until multi-node or many job types |

## Jobs

| Job | Class | Config | Task |
| --- | ----- | ------ | ---- |
| FX snapshot sync | `ExchangeRateSyncBackgroundService` | `ExchangeRates:*` | 6.2.4 / 8.4 |
| **Renewal reminder emails** | `RenewalReminderBackgroundService` → `IRenewalReminderService` | `EmailJobs:*` | **8.1 / 15.3.1** |

### 8.1 Renewal reminder email job

Runs periodically when:

1. `BackgroundJobs:Enabled` = true  
2. `EmailJobs:RenewalRemindersEnabled` = true  
3. SMTP configured (`SystemSettings.HasSmtpConfigured`)  
4. User has `NotificationSettings.EmailEnabled`  
5. Active subscription `nextRenewal` ∈ `[today, today + daysBeforeRenewal]`  

Dedupe key (8.2 / 15.3.2): `renewal:{subscriptionId}:{yyyy-MM-dd}` via `EmailSendLogs`.

**Manual run (ops):**

```http
POST /api/admin/jobs/renewal-reminders/run
Authorization: Bearer <superadmin>
```

Response: `{ "processedCount": N }`

### 8.3 Mail job reliability

| Behaviour | Implementation |
| --------- | -------------- |
| SMTP off / incomplete | Job iteration **no-op** + debug log (not a crash) |
| One send / iteration fails | Logged; loop continues (`IsolatedPeriodicBackgroundService` + per-item handling) |
| Master switch | `BackgroundJobs:Enabled=false` disables **all** periodic jobs including mail |

## Schedule config (8.4.2)

Human intervals via `IntervalParser`:

| Value | Meaning |
| ----- | ------- |
| `1h` / `30m` / `90s` / `2d` | Hours / minutes / seconds / days |
| `1` (plain number) | Hours |

```bash
# FX
ExchangeRates__SyncInterval=1h
ExchangeRates__Enabled=true
BackgroundJobs__Enabled=true

# Renewal emails (8.1)
EmailJobs__RenewalRemindersEnabled=true
EmailJobs__RenewalReminderInterval=6h
EmailJobs__StartupDelaySeconds=45
```

## Error isolation (8.4.3 / 8.3)

`IsolatedPeriodicBackgroundService`:

1. Catches per-iteration exceptions → log + continue  
2. Per-base FX failures isolated inside `ExchangeRateSyncService.SyncAllAsync`  
3. Mail job: SMTP missing is soft skip; send failures logged per subscription via delivery layer  

Never rethrows into the host process for routine job errors.
