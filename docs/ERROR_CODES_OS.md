# Subify OS — Error codes (canonical)

> **Source of truth:** `api/Subify.Domain/Errors/DomainErrors.cs`  
> **HTTP mapping:** `ProblemDetailsStatusMapper` + RFC 7807 via `ResultExtensions`  
> **Legacy SaaS tables:** [ERROR_CODES.md](./ERROR_CODES.md) (historical; freemium/premium rows are obsolete)

Format:

```json
{
  "type": "https://api.subify.app/errors/{CODE}",
  "title": "...",
  "status": 400,
  "detail": "...",
  "instance": "/api/...",
  "errorCode": "{CODE}"
}
```

Validation failures may include `errors: { "field": ["msg"] }` and use `VAL_*` / FluentValidation codes.

---

## AUTH_*

| Code | HTTP | Title | Notes |
| ---- | ---- | ----- | ----- |
| `AUTH_001` | 401 | Invalid Credentials | Login |
| `AUTH_002` | 401 | Email Not Verified | OS: confirm flow disabled; rarely used |
| `AUTH_003` | 401 | Invalid Token | Access JWT |
| `AUTH_004` | 401 | Invalid Refresh Token | |
| `AUTH_005` | 423 | Account Locked | Lockout |
| `AUTH_006` | 400 | Password Too Weak | |
| `AUTH_007` | 400 | Invalid Email Format | |
| `AUTH_008` | 409 | Email Already Registered | |
| `AUTH_009` | 400 | Invalid Reset Code | Mail password reset |
| `AUTH_010` | 400 | Invalid Verification Code | Confirm — not used in OS |
| `AUTH_011` | 401 | Session Expired | |
| `AUTH_012` | 400 | Email Already Confirmed | |
| `AUTH_013` | 400 | Email Not Confirmed | |
| `AUTH_014` | 403 | Registration Disabled | `AllowPublicRegistration=false` |
| `AUTH_015` | 400 | Invalid Invite Token | |
| `AUTH_016` | 401 | Refresh Token Reuse Detected | Rotation theft signal |
| `AUTH_017` | 403 | Setup Required | Setup gate |
| `AUTH_018` | 409 | Super Admin Already Exists | |
| `AUTH_019` | 409 | Super Admin Bootstrap Race | |

## SETUP_*

| Code | HTTP | Title |
| ---- | ---- | ----- |
| `SETUP_001` | 409 | Setup Already Complete |
| `SETUP_002` | 400 | Super Admin Required |
| `SETUP_003` | 404 | Settings Not Initialized |

## SUB_* (no freemium limit)

| Code | HTTP | Title | Notes |
| ---- | ---- | ----- | ----- |
| `SUB_001` | 404 | Subscription Not Found | **Not** “limit reached” |
| `SUB_002` | 403 | Subscription Access Denied | |
| `SUB_003` | 400 | Invalid Price | |
| `SUB_004` | 400 | Invalid Billing Cycle | |
| `SUB_005` | 400 | Invalid Renewal Date | |
| `SUB_006` | 400 | Provider Not Active | |
| `SUB_007` | 400 | Category Conflict | XOR system vs user category |
| `SUB_008` | 404 | Category Not Found | |
| `SUB_009` | 400 | Invalid Shared Count | |

## AI_*

| Code | HTTP | Title | Notes |
| ---- | ---- | ----- | ----- |
| `AI_KEY_MISSING` | 503 | AI API Key Missing | BYOK — not premium |
| `AI_002` | 429 | Rate Limit (Minute) | |
| `AI_003` | 429 | Rate Limit (Daily) | |
| `AI_004` | 503 | AI Service Unavailable | |
| `AI_005` | 400/500 | AI Processing Error | |
| `AI_006` | 400 | Insufficient Data | Need ≥1 subscription |

## PRO_* (profile)

| Code | HTTP | Title |
| ---- | ---- | ----- |
| `PRO_001` | 404 | Profile Not Found |
| `PRO_002` | 400 | Invalid Locale |
| `PRO_003` | 400 | Invalid Currency |
| `PRO_004` | 400 | Invalid Theme |
| `PRO_005` | 400 | Invalid Budget |
| `PRO_006` | 400 | Invalid Device Token |

## REP_*

| Code | HTTP | Title | Notes |
| ---- | ---- | ----- | ----- |
| `REP_001` | 400 | Invalid Date Range | **Not** premium |
| `REP_002` | 400 | Insufficient Data | |

## FX_*

| Code | HTTP | Title |
| ---- | ---- | ----- |
| `FX_001` | 400 | Invalid Base Currency |
| `FX_002` | 503 | Exchange Rate Provider Unavailable |
| `FX_003` | 404 | Exchange Rates Not Found |

## RES_*

| Code | HTTP | Title |
| ---- | ---- | ----- |
| `RES_001` | 404 | Resource Not Found |
| `RES_002` | 403 | Resource Access Denied |
| `RES_003` | 400 | Invalid Language |
| `RES_004` | 409 | Resource Conflict |
| `RES_005` | 400 | Invalid Since Date |

## SET_*

| Code | HTTP | Title | Notes |
| ---- | ---- | ----- | ----- |
| `SET_001` | 404 | Settings Not Found | |
| `SET_002` | 403 | Settings Access Denied | SuperAdmin only |
| `SET_003` | 400 | SMTP Not Configured | Any outbound mail path when SMTP off/incomplete |
| `SET_004` | 400 | SMTP Test Failed | Test SMTP / delivery failure |

## SYS_* / VAL_*

| Code | HTTP | Title |
| ---- | ---- | ----- |
| `SYS_001` | 500 | Internal Server Error |
| `SYS_002` | 503 | Service Unavailable |
| `SYS_003` | 504 | Gateway Timeout |
| `SYS_004` | 429 | Too Many Requests |
| `VAL_001` | 400 | Validation Failed |
| `VAL_002` | 400 | Required Field Missing |
| `VAL_003` | 400 | Invalid Format |
| `VAL_004` | 400 | Max Length Exceeded |
| `VAL_005` | 400 | Min Length Required |

## USER_*

| Code | HTTP | Title |
| ---- | ---- | ----- |
| `USER_001` | 404 | User Not Found |
| `USER_002` | 403 | User Access Denied |
| `USER_003` | 401 | User Not Authorized |
| `USER_004` | 403 | Cannot Modify Super Admin |
| `USER_005` | 400 | Use Change Password |
| `USER_006` | 403 | Account Disabled |
| `USER_007` | 400 | Cannot Disable Self |
| `USER_008` | 400 | Invalid Role |
| `USER_009` | 400 | Cannot Change Own Role |

## CAT_* / UCAT_* / PROV_*

| Code | HTTP | Title |
| ---- | ---- | ----- |
| `CAT_001` | 404 | Category Not Found |
| `CAT_002` | 403 | Cannot Delete System Category |
| `CAT_003` | 409 | Has Active Subscriptions |
| `CAT_004` | 409 | Duplicate slug |
| `UCAT_001` | 404 | User Category Not Found |
| `UCAT_002` | 403 | User Category Access Denied |
| `UCAT_003` | 409 | Has Active Subscriptions |
| `UCAT_004` | 409 | Duplicate Name |
| `PROV_001` | 404 | Provider Not Found |
| `PROV_002` | 409 | Duplicate Name |
| `PROV_003` | 409 | Duplicate Slug |
| `PROV_004` | 400 | Inactive Provider |
| `PROV_005` | 409 | Has Active Subscriptions |

---

## Removed vs legacy SaaS (do not reintroduce)

| Legacy | OS |
| ------ | -- |
| Subscription free limit / `SUB_001` limit | Removed — unlimited |
| Premium AI / reports gates | Removed — `AI_KEY_MISSING`, open reports |
| `PAY_*` / RevenueCat | Removed |
| Email confirm required | Cancelled permanently |
