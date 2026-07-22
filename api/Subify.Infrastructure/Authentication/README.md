# Authentication / JWT

## Token expiry (task 3.1.4)

Configured under `JwtOptions` in `appsettings.json` / `appsettings.Development.json` / env vars.

| Setting | Purpose | Default | Recommended | Hard range |
| ------- | ------- | ------- | ----------- | ---------- |
| `ExpirationInMinutes` | Access JWT lifetime | `60` | **15–60** | 5–1440 (24h) |
| `RefreshTokenExpirationDays` | Opaque refresh lifetime | `7` | **7** | 1–90 |
| `ClockSkewSeconds` | `nbf`/`exp` tolerance (3.1.5) | **30** | 0–60 | 0–300 |

### Example

```json
"JwtOptions": {
  "Issuer": "SubifyOS",
  "Audience": "SubifyOSClient",
  "SecretKey": "<at-least-32-chars>",
  "ExpirationInMinutes": 30,
  "RefreshTokenExpirationDays": 7,
  "ClockSkewSeconds": 30
}
```

### Environment override (self-host)

```bash
JwtOptions__ExpirationInMinutes=30
JwtOptions__RefreshTokenExpirationDays=14
JwtOptions__ClockSkewSeconds=30
JwtOptions__SecretKey=...
```

### Runtime resolution

`JwtOptions.ResolveAccessTokenLifetime()` / `ResolveRefreshTokenDays()` / `ResolveClockSkew()` clamp invalid values to defaults.  
`TokenService` uses resolved lifetimes when issuing tokens.  
`JwtTokenValidation.CreateParameters` sets `TokenValidationParameters.ClockSkew` for bearer auth.

### Clock skew notes (3.1.5)

- ASP.NET default is **5 minutes** — too loose for short-lived access tokens.
- Subify OS default is **30 seconds** to absorb small host clock drift without extending expired tokens much.
- Set `0` for strict expiry (no tolerance).

### Related

- Access claims: task 3.1.1 (`AccessTokenClaimsFactory`)
- Refresh hash: task 3.1.2 (`RefreshTokenHasher`)
- Rotation: task 3.1.3 (`RefreshHandler`)
- Expiry config: task 3.1.4
- Clock skew: task 3.1.5
