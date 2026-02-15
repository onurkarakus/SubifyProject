# Subify Project Overview (Gemini CLI Agent Reference)

This document synthesizes key information about the Subify project, derived from its comprehensive documentation. It serves as a quick reference for Gemini CLI operations.

## 1. Project Summary

Subify is a web (Next.js) and mobile (Flutter) application for managing subscriptions and getting AI-powered spending analysis. The backend is an ASP.NET Core 8 Web API with MSSQL, utilizing RevenueCat for payments and OpenAI for AI features. It operates on a Freemium model with TR/EN language support, deployed via Docker Compose on a VPS.

**Core Technologies:**
- **Frontend:** Next.js (Web), Flutter (Mobile)
- **Backend:** ASP.NET Core 8 Web API (C#)
- **Database:** MSSQL
- **Cache:** Redis
- **Payments:** RevenueCat (Stripe for Web, App Store/Play Store IAP for Mobile)
- **AI:** OpenAI API
- **Notifications:** SMTP/Resend (Email), Firebase Cloud Messaging (FCM) (Push)
- **Background Jobs:** Hangfire

## 2. Key Architectural Decisions (ADRs)

- **Localization Strategy (ADR-001, ADR-004):** DB-driven using a `resources` table (PageName, Name, LanguageCode, Value). Category names are localized via this table using slugs. Updates do not require app store updates.
- **Cache Strategy (ADR-002):** Redis Cache-Aside pattern is used for `Resource`, `EntitlementCache`, `Category`, `Provider`, and `ExchangeRate` entities with specific TTLs and invalidation methods.
- **Entity Base Classes (ADR-003):** `BaseEntity` (Id, CreatedAt, UpdatedAt) for domain entities. `ApplicationUser` extends `IdentityUser<Guid>` directly to prevent diamond inheritance. `ISoftDeletable` for soft delete.
- **Mobile Framework (ADR-005):** Flutter chosen over alternatives due to IAP stability, type safety, performance, and UI consistency.
- **GUID Generation (ADR-010):** `NEWSEQUENTIALID()` via EF Core configuration for `Id` to minimize clustered index fragmentation and avoid runtime allocations.

## 3. API Structure & Authentication

- **Base URLs:** `https://api.subify.app/api` (Production), `http://localhost:5000/api` (Development).
- **Authentication:** Bearer JWT for `access_token`. Refresh token rotation is implemented.
- **Headers:** `Content-Type: application/json`, `Accept: application/json`, `Accept-Language: tr` or `en`.
- **Error Handling:** All API errors return RFC 7807 ProblemDetails format. Specific error codes (AUTH_xxx, SUB_xxx, etc.) are cataloged in `ERROR_CODES.md`.
- **Pagination:** Standard `data`, `pagination` (page, pageSize, totalItems, totalPages) structure.

**Main Controller Groups:**
- `AuthController`: User registration, login, token refresh, password reset, email verification.
- `SubscriptionsController`: CRUD for user subscriptions, upcoming payments.
- `CategoriesController`: System and user-defined categories.
- `ReportsController`: Premium-gated financial reports (monthly spend, category breakdown).
- `AiController`: Premium-gated AI analysis and suggestions.
- `ProfileController`: User profile management, notification settings, device token registration.
- `ActivityController`: User activity logs (read-only).
- `PaymentsController`: Premium status, checkout initiation, RevenueCat webhooks.
- `ProvidersController`: List of subscription providers.
- `ResourcesController`: Localization resource synchronization.
- `ExchangeRatesController`: Cached currency exchange rates.
- `SystemController`: Health checks, supported currencies.
- `AdminController`: Admin-only endpoints (user management, stats, logs, email templates).

## 4. Data Model Highlights

- **`AspNetUsers` / `profiles`:** Standard ASP.NET Core Identity with a 1:1 `profiles` table for user-specific data (locale, plan, theme, budget).
- **`refresh_tokens`:** Stores JWT refresh tokens with rotation and revocation logic.
- **`subscriptions`:** Core entity. Includes `shared_with_count` (ADR-007), `category_id`, `user_category_id` (mutually exclusive), `archived` for soft delete.
- **`resources`:** DB-driven localization table (ADR-001).
- **`entitlements_cache`:** RevenueCat entitlement cache (ADR-002).
- **`activity_logs`:** Tracks user actions for dashboard display.
- **`exchange_rate_snapshots`:** Stores historical exchange rates for currency conversion (ADR-008).

## 5. Payment & Premium Gating

- **RevenueCat Integration:** Handles web (Stripe) and mobile (IAP) payments.
- **Premium Features:** AI analysis, detailed reports, push notifications, unlimited subscriptions.
- **Freemium Limit:** Max 3 active subscriptions for free users.
- **Gating:** Premium features are blurred/paywalled for free users, returning 403 Forbidden.
- **Webhooks:** RevenueCat webhooks are crucial for syncing entitlement status with the backend.

## 6. Localization Strategy

- **DB-driven:** All UI texts are stored in the `resources` table.
- **Delta Sync:** Clients fetch updated resources via `GET /api/resources?lang={lang}&since={timestamp}` on startup.
- **Cache:** Backend uses Redis for resource caching (1 hour TTL).
- **Language Codes:** `tr`, `en`.

## 7. Testing Strategy

- **Test Pyramid:** Unit -> Integration -> E2E.
- **Backend (C#/xUnit):** Unit tests for business logic, services, validators. Integration tests for API controllers using `WebApplicationFactory` and `Testcontainers`.
- **Frontend (Flutter/Next.js):** Unit tests for components/utilities. Widget/Integration tests for Flutter. Playwright for Web E2E.
- **Coverage Targets:** Domain/Business Logic (80%+), Services (70%+), Controllers (60%+), Infrastructure (50%+).

## 8. Deployment & Environment

- **Hosting:** VPS (Linux)
- **Orchestration:** Docker Compose
- **Services:** `reverse-proxy` (Caddy/Nginx), `frontend` (Next.js), `api` (ASP.NET Core), `worker` (Hangfire), `db` (MSSQL), `redis`, `otel-collector` (optional).
- **Volumes:** `db-data`, `redis-data`, `certs`.
- **CI/CD:** GitHub Actions for build, test, and deploy via SSH to VPS.
- **Secrets:** Managed via `.env` files on VPS, not committed to VCS.

## 9. Observability

- **Logging:** Serilog with Console, File, and Seq sinks. Structured JSON logging with PII redaction.
- **Tracing/Metrics:** OpenTelemetry integration for traces and metrics (`AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, `AddSqlClientInstrumentation`, `AddRedisInstrumentation`).
- **Health Checks:** `/health`, `/health/ready`, `/health/live` endpoints for DB, Redis, external APIs (OpenAI, RevenueCat), and Hangfire.
- **Alerting:** Rules based on error rates, response times, resource usage.

## 10. Key Workflows

- User Registration & Email Verification
- JWT Authentication & Token Refresh
- Subscription CRUD (with freemium limit check)
- RevenueCat Payment Flows (Web & Mobile)
- AI Suggestion Request
- Push Notification Registration
- Renewal Reminder (background job)
- Resource Localization Sync
- Exchange Rate Sync

## 11. UI/UX Notes

- **Mobile First:** Development priority.
- **Design System:** Defined color palette (Primary Purple: `#6B46C1`), typography (Inter font). User-selectable accent colors.
- **Premium Gating:** Blur effects and CTA buttons for premium features in free tier.
- **State Variations:** Specific visual states for subscription cards (upcoming, overdue, shared, archived) and button states (normal, hover, pressed, disabled, loading).
- **Mockups:** Available in `docs/mockups/`

## 12. Development Best Practices & Conventions

- **ProblemDetails:** All API errors conform to RFC 7807 ProblemDetails.
- **FluentValidation:** Used for input validation.
- **PII Redaction:** Sensitive information is masked in logs.
- **Repository Pattern:** Implied by unit test examples (e.g., `ISubscriptionRepository`).
- **Dependency Injection:** ASP.NET Core's built-in DI is used.
- **Code Generation:** Riverpod for Flutter state management, assumed to use code generation.
- **Soft Delete:** `archived` flag on `subscriptions`.
- **Rate Limiting:** Implemented at API level for various endpoint groups.
