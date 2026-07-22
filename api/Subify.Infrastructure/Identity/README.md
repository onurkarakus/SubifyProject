# Identity security (task 3.4)

Single source of truth: `Subify.Domain.Constants.IdentitySecurityDefaults`  
Applied via: `IdentityOptionsConfiguration.Apply` in DI.

## 3.4.1 Password policy

| Rule | Value |
| ---- | ----- |
| Min length | **8** |
| Uppercase | required |
| Lowercase | required |
| Digit | required |
| Non-alphanumeric | **not** required |

Mirrored in FluentValidation: `PasswordRuleBuilder.ApplySubifyPasswordRules()`  
(register, setup admin, change-password, admin reset-password).

Identity error mapping: weak password → `AUTH_006`.

## 3.4.2 Lockout

| Rule | Value |
| ---- | ----- |
| Max failed attempts | **5** |
| Lockout duration | **15 minutes** |
| Allowed for new users | **true** |

Login: `AccessFailedAsync` / `IsLockedOutAsync` → **AUTH_005** (HTTP 423).  
Successful login resets failure count.

## 3.4.3 Unique email

| Layer | Mechanism |
| ----- | --------- |
| Identity | `User.RequireUniqueEmail = true` |
| Application | `FindByEmailAsync` pre-check → `AUTH_008` |
| Identity create | `DuplicateEmail` / `DuplicateUserName` → `AUTH_008` (409) |
| EF / Identity schema | Unique index on normalized email (`EmailIndex` / AspNetUsers) |

## Confirm

`RequireConfirmedEmail` / `RequireConfirmedAccount` = **false** (OS product decision).
