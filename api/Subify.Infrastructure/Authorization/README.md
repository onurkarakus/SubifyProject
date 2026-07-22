# Authorization (task 3.3)

## Policies

| Policy constant | Name | Who |
| --------------- | ---- | --- |
| `AuthPolicies.SuperAdmin` | `RequireSuperAdmin` | SuperAdmin only |
| `AuthPolicies.AdminOrAbove` | `RequireAdminOrAbove` | SuperAdmin, Admin |
| `AuthPolicies.Authenticated` | `RequireAuthenticatedUser` | Any signed-in user |

## Fallback (3.3.4)

`FallbackPolicy = RequireAuthenticatedUser`.  
Every endpoint must either:

- `.AllowAnonymous()` — health, auth public, setup status/admin create, OpenAPI/Scalar, or  
- `.RequireAuthorization(...)` — explicit policy

## SuperAdmin bootstrap (3.3.1 / 3.3.6)

| Path | Role |
| ---- | ---- |
| `POST /api/setup/admin` | Creates **first** SuperAdmin (race-safe) while setup incomplete |
| `POST /api/auth/register` | **Blocked** until setup complete; then **User** only if `AllowPublicRegistration` |

## SuperAdmin transfer (3.3.5)

**Out of v1.** Document only: promote another user to SuperAdmin and demote self is not implemented.  
Self-host recovery: admin password reset + DB role edit if needed.
