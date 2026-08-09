# Subify OS Web

Next.js 16 (App Router) + TypeScript + Tailwind CSS 4.

## Setup

```bash
cd web
cp .env.example .env.local
# edit NEXT_PUBLIC_API_URL if needed (default http://localhost:5240/api)
npm install
npm run dev
```

Open http://localhost:3000

## Auth storage (10.1.6)

Access + refresh tokens live in **sessionStorage** (cleared when the tab closes).

- Practical for self-host without a BFF/httpOnly cookie layer
- XSS still matters: do not render untrusted HTML; use CSP in production
- Alternative later: memory-only access token + httpOnly refresh via BFF

## Scripts

| Command | Description |
| ------- | ----------- |
| `npm run dev` | Dev server |
| `npm run build` | Production build |
| `npm run start` | Serve production build |
| `npm run lint` | ESLint |

## App routes

| Path | Notes |
| ---- | ----- |
| `/` | Landing |
| `/login`, `/register`, `/accept-invite` | Auth |
| `/dashboard` | Summary, upcoming, activity |
| `/subscriptions` | List / create / edit / archive |
| `/reports` | Monthly + category charts (CSS bars) |
| `/ai` | Analyze + history |
| `/profile` | Profile + change password |
| `/admin/users` | Users, create, invite, reset password |
| `/admin/settings` | Instance + AI + SMTP (SuperAdmin) |
