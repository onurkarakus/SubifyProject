# Authentication / JWT

## Token expiry (task 3.1.4)

Configured under `JwtOptions` in `appsettings.json` / `appsettings.Development.json` / env vars.

| Setting | Purpose | Default | Recommended | Hard range |
| ------- | ------- | ------- | ----------- | ---------- |
| `ExpirationInMinutes` | Access JWT lifetime | `60` | **15–60** | 5–1440 (24h) |
| `RefreshTokenExpirationDays` | Opaque refresh lifetime | `7` | **7** | 1–90 |

### Example

```json
"JwtOptions": {
  "Issuer": "SubifyOS",
  "Audience": "SubifyOSClient",
  "SecretKey": "<at-least-32-chars>",
  "ExpirationInMinutes": 30,
  "RefreshTokenExpirationDays": 7
}
```

### Environment override (self-host)

```bash
JwtOptions__ExpirationInMinutes=30
JwtOptions__RefreshTokenExpirationDays=14
JwtOptions__SecretKey=...
```

### Runtime resolution

`JwtOptions.ResolveAccessTokenLifetime()` / `ResolveRefreshTokenDays()` clamp invalid values to defaults.  
`TokenService` always uses these resolved lifetimes when issuing tokens.

### Related

- Access claims: task 3.1.1 (`AccessTokenClaimsFactory`)
- Refresh hash: task 3.1.2 (`RefreshTokenHasher`)
- Rotation: task 3.1.3 (`RefreshHandler`)
- Clock skew: task 3.1.5
