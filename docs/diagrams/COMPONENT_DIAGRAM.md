# Subify - Component Diagram

Bu doküman, Subify uygulamasının sistem bileşenlerini ve aralarındaki bağımlılıkları görselleştirir.

> **Referanslar:**
>
> - [Ana PRD](../Subify.Web.Uygulamasi.v2.PRD.md)
> - [Deployment Diagram](./DEPLOYMENT_DIAGRAM.md)

---

## 🏗️ Sistem Mimarisi Genel Bakış

```mermaid
graph TB
    subgraph Clients["📱 Clients"]
        Web["🌐 Web App<br/>(Next.js)"]
        Mobile["📱 Mobile App<br/>(Flutter)"]
    end

    subgraph Backend["⚙️ Backend Services"]
        API["🔧 ASP.NET Core API"]
        Worker["⏰ Hangfire Worker"]
    end

    subgraph DataLayer["💾 Data Layer"]
        DB[(🗄️ MSSQL)]
        Cache[(🔴 Redis)]
    end

    subgraph External["☁️ External Services"]
        RevenueCat["💳 RevenueCat"]
        OpenAI["🤖 OpenAI"]
        FCM["📲 Firebase CM"]
        SMTP["📧 SMTP/Resend"]
        ExchangeAPI["💱 Exchange Rate API"]
    end

    Web -->|HTTPS| API
    Mobile -->|HTTPS| API

    API --> DB
    API --> Cache
    Worker --> DB
    Worker --> Cache

    API --> RevenueCat
    API --> OpenAI
    API --> FCM
    Worker --> SMTP
    Worker --> ExchangeAPI

    RevenueCat -->|Webhook| API
```

---

## 📦 Bileşen Detayları

### 1. Web Frontend (Next.js)

```mermaid
graph TB
    subgraph NextJS["🌐 Next.js App"]
        subgraph Public["Public Pages"]
            Landing["Landing Page"]
            Pricing["Pricing Page"]
        end

        subgraph App["User App (Protected)"]
            Dashboard["Dashboard"]
            Subscriptions["Subscriptions"]
            Reports["Reports (Premium)"]
            Settings["Settings"]
            AIPage["AI Suggestions (Premium)"]
        end

        subgraph Admin["Admin Panel (Role: Admin)"]
            UserMgmt["User Management"]
            Stats["System Stats"]
            EmailTpl["Email Templates"]
            Logs["System Logs"]
        end

        subgraph Core["Core Features"]
            AuthProvider["Auth Provider"]
            I18n["next-i18next"]
            ReactQuery["React Query"]
            Paywall["Paywall Modal"]
        end
    end

    AuthProvider --> Dashboard
    AuthProvider --> Admin
    I18n --> Public
    I18n --> App
    ReactQuery --> App
```

**Tech Stack:**
| Bileşen | Teknoloji |
|---------|-----------|
| Framework | Next.js (App Router) |
| Language | TypeScript |
| State | React Query |
| i18n | next-i18next |
| Styling | Tailwind / Chakra UI |
| Auth | JWT (httpOnly cookie) |

---

### 2. Mobile App (Flutter)

```mermaid
graph TB
    subgraph Flutter["📱 Flutter App"]
        subgraph Screens["Screens"]
            AuthScreens["Auth<br/>(Login/Register/Forgot)"]
            DashboardScreen["Dashboard"]
            SubsScreen["Subscriptions"]
            ReportsScreen["Reports (Premium)"]
            SettingsScreen["Settings"]
            AIScreen["AI Suggestions"]
            PaywallScreen["Paywall"]
        end

        subgraph StateManagement["State Management"]
            Riverpod["Riverpod"]
            Providers["Providers"]
            Notifiers["StateNotifiers"]
        end

        subgraph Networking["Networking"]
            Dio["Dio Client"]
            AuthInterceptor["Auth Interceptor"]
            ErrorInterceptor["Error Interceptor"]
        end

        subgraph LocalStorage["Local Storage"]
            SecureStorage["flutter_secure_storage<br/>(Tokens)"]
            Prefs["SharedPreferences<br/>(Settings)"]
            ResourceCache["Resource Cache"]
        end

        subgraph SDKs["External SDKs"]
            RevenueCatSDK["RevenueCat SDK"]
            FirebaseSDK["Firebase Messaging"]
        end
    end

    Riverpod --> Screens
    Dio --> AuthInterceptor
    AuthInterceptor --> SecureStorage
    RevenueCatSDK --> PaywallScreen
    FirebaseSDK --> SettingsScreen
```

**Tech Stack:**
| Bileşen | Teknoloji |
|---------|-----------|
| Framework | Flutter |
| Language | Dart |
| State | Riverpod (Code generation) |
| Navigation | GoRouter |
| HTTP | Dio |
| i18n | Flutter Intl (.arb) + DB resources |
| Secure Storage | flutter_secure_storage |
| IAP | RevenueCat (purchases_flutter) |
| Push | firebase_messaging |

---

### 3. Backend API (ASP.NET Core)

```mermaid
graph TB
    subgraph API["⚙️ ASP.NET Core 8 Web API"]
        subgraph Controllers["Controllers"]
            AuthCtrl["AuthController"]
            SubsCtrl["SubscriptionsController"]
            CatCtrl["CategoriesController"]
            ReportsCtrl["ReportsController"]
            AICtrl["AiController"]
            ProfileCtrl["ProfileController"]
            PaymentsCtrl["PaymentsController"]
            WebhookCtrl["WebhookController"]
            AdminCtrl["AdminController"]
            EmailTplCtrl["EmailTemplatesController"]
            ProvidersCtrl["ProvidersController"]
            ResourcesCtrl["ResourcesController"]
            ExchangeCtrl["ExchangeRatesController"]
            SystemCtrl["SystemController"]
        end

        subgraph Middleware["Middleware"]
            AuthMiddleware["JWT Authentication"]
            RateLimit["Rate Limiting"]
            CORS["CORS"]
            ErrorHandler["Global Error Handler<br/>(ProblemDetails)"]
        end

        subgraph Services["Services"]
            AuthService["AuthService"]
            SubscriptionService["SubscriptionService"]
            AIService["AIService"]
            PaymentService["PaymentService"]
            NotificationService["NotificationService"]
            CacheService["CacheService"]
            ResourceService["ResourceService"]
        end

        subgraph Infrastructure["Infrastructure"]
            EFCore["EF Core"]
            Identity["ASP.NET Identity"]
            Redis["IDistributedCache"]
            HttpClients["HttpClient Factory"]
        end
    end

    Controllers --> Services
    Services --> Infrastructure
    Middleware --> Controllers
```

**Controller Responsibility Matrix:**

| Controller               | Endpoints                       | Auth Required  | Roles                |
| ------------------------ | ------------------------------- | -------------- | -------------------- |
| AuthController           | /api/auth/\*                    | ❌ (public)    | -                    |
| SubscriptionsController  | /api/subscriptions/\*           | ✅             | User                 |
| CategoriesController     | /api/categories/\*              | ✅             | User                 |
| ReportsController        | /api/reports/\*                 | ✅             | User (Premium gated) |
| AiController             | /api/ai/\*                      | ✅             | User (Premium gated) |
| ProfileController        | /api/profile/\*                 | ✅             | User                 |
| PaymentsController       | /api/payments/_, /api/billing/_ | ✅             | User                 |
| WebhookController        | /api/webhooks/\*                | ❌ (signature) | -                    |
| AdminController          | /api/admin/\*                   | ✅             | Admin                |
| EmailTemplatesController | /api/email-templates/\*         | ✅             | Admin                |
| ProvidersController      | /api/providers/\*               | ✅             | User                 |
| ResourcesController      | /api/resources/\*               | ❌ (public)    | -                    |
| ExchangeRatesController  | /api/exchange-rates/\*          | ❌ (public)    | -                    |
| SystemController         | /api/system/\*                  | ❌ (public)    | -                    |

---

### 4. Background Worker (Hangfire)

```mermaid
graph TB
    subgraph Worker["⏰ Hangfire Worker"]
        subgraph Jobs["Scheduled Jobs"]
            RenewalJob["Renewal Reminder<br/>Daily 09:00 UTC"]
            ExchangeJob["Exchange Rate Sync<br/>Hourly"]
            EntitlementJob["Entitlement Reconciliation<br/>Every 6 hours"]
            CleanupJob["AI Log Cleanup<br/>Weekly"]
            MetricsJob["Metrics Housekeeping<br/>Daily"]
        end

        subgraph Dashboard["Hangfire Dashboard"]
            JobMonitor["Job Monitor"]
            RetryQueue["Retry Queue"]
            FailedJobs["Failed Jobs"]
        end
    end

    RenewalJob --> SMTP["📧 SMTP"]
    RenewalJob --> FCM["📲 FCM"]
    ExchangeJob --> ExchangeAPI["💱 API"]
    EntitlementJob --> RevenueCat["💳 RevenueCat"]
```

**Job Schedule:**

| Job                          | Schedule                  | Description                              |
| ---------------------------- | ------------------------- | ---------------------------------------- |
| RenewalReminderJob           | `0 9 * * *` (Daily 09:00) | Check upcoming renewals, send email/push |
| ExchangeRateSyncJob          | `0 * * * *` (Hourly)      | Fetch rates from exchangerate-api.com    |
| EntitlementReconciliationJob | `0 */6 * * *` (Every 6h)  | Sync with RevenueCat for consistency     |
| AILogCleanupJob              | `0 3 * * 0` (Weekly)      | Archive old AI suggestion logs           |
| MetricsHousekeepingJob       | `0 4 * * *` (Daily 04:00) | Aggregate and archive metrics            |

---

### 5. Data Layer

```mermaid
graph TB
    subgraph DataLayer["💾 Data Layer"]
        subgraph MSSQL["🗄️ MSSQL Server"]
            Identity["Identity Tables<br/>(AspNetUsers, AspNetRoles)"]
            Business["Business Tables<br/>(subscriptions, categories, providers)"]
            Billing["Billing Tables<br/>(billing_sessions, entitlements_cache)"]
            System["System Tables<br/>(resources, exchange_rate_snapshots)"]
        end

        subgraph Redis["🔴 Redis Cache"]
            SessionCache["Session Cache"]
            ResourceCache["Resource Cache<br/>TTL: 1h"]
            EntitlementCache["Entitlement Cache<br/>TTL: 5-15min"]
            RateLimitCache["Rate Limit Counters"]
            ExchangeCache["Exchange Rate Cache<br/>TTL: 1h"]
        end
    end

    API --> MSSQL
    API --> Redis
    Worker --> MSSQL
    Worker --> Redis
```

---

### 6. External Services Integration

```mermaid
graph LR
    subgraph API["ASP.NET Core API"]
        PaymentService["PaymentService"]
        AIService["AIService"]
        NotifService["NotificationService"]
        ExchangeService["ExchangeRateService"]
    end

    subgraph RevenueCat["💳 RevenueCat"]
        RC_API["REST API"]
        RC_Webhook["Webhooks"]
        RC_SDK["Mobile SDK"]
        Stripe["Stripe (Web)"]
    end

    subgraph OpenAI["🤖 OpenAI"]
        ChatAPI["Chat Completions API"]
    end

    subgraph Firebase["📲 Firebase"]
        FCM["Cloud Messaging"]
    end

    subgraph Email["📧 Email"]
        SMTP["SMTP / Resend"]
    end

    subgraph Exchange["💱 Exchange"]
        ExchangeAPI["exchangerate-api.com"]
    end

    PaymentService --> RC_API
    RC_Webhook --> API
    AIService --> ChatAPI
    NotifService --> FCM
    NotifService --> SMTP
    ExchangeService --> ExchangeAPI
```

**Integration Details:**

| Service              | Purpose                 | Auth Method     | Rate Limit     |
| -------------------- | ----------------------- | --------------- | -------------- |
| RevenueCat           | Subscription management | API Key         | Per plan       |
| OpenAI               | AI suggestions          | API Key         | Token-based    |
| Firebase CM          | Push notifications      | Service Account | 500k/day free  |
| SMTP/Resend          | Email notifications     | API Key         | Per plan       |
| exchangerate-api.com | Currency rates          | API Key         | 250/month free |

---

## 🔐 Security Layers

```mermaid
graph TB
    subgraph Security["🔐 Security Architecture"]
        subgraph EdgeSecurity["Edge Security"]
            TLS["TLS 1.3<br/>(Let's Encrypt)"]
            RateLimit["Rate Limiting"]
            CORS["CORS Policy"]
        end

        subgraph AuthSecurity["Authentication"]
            JWT["JWT Tokens<br/>(Access + Refresh)"]
            Identity["ASP.NET Identity"]
            RoleBased["Role-Based Auth"]
        end

        subgraph DataSecurity["Data Security"]
            InputVal["Input Validation<br/>(FluentValidation)"]
            Parameterized["Parameterized Queries<br/>(EF Core)"]
            Hashing["Password Hashing<br/>(Argon2/BCrypt)"]
        end

        subgraph WebhookSecurity["Webhook Security"]
            SignatureVal["Signature Validation"]
            IPWhitelist["IP Whitelist (optional)"]
        end
    end
```

---

## 📊 Dependency Matrix

| Component      | Depends On                            | Depended By         |
| -------------- | ------------------------------------- | ------------------- |
| **Web App**    | API, RevenueCat (checkout URL)        | -                   |
| **Mobile App** | API, RevenueCat SDK, FCM              | -                   |
| **API**        | MSSQL, Redis, OpenAI, RevenueCat, FCM | Web, Mobile, Worker |
| **Worker**     | MSSQL, Redis, SMTP, Exchange API      | -                   |
| **MSSQL**      | -                                     | API, Worker         |
| **Redis**      | -                                     | API, Worker         |

---

## 🔄 Data Flow Summary

```mermaid
flowchart LR
    subgraph Client["Clients"]
        Web["Web"]
        Mobile["Mobile"]
    end

    subgraph Core["Core"]
        API["API"]
        Worker["Worker"]
    end

    subgraph Data["Data"]
        DB["MSSQL"]
        Cache["Redis"]
    end

    subgraph External["External"]
        RC["RevenueCat"]
        AI["OpenAI"]
        FCM["FCM"]
        SMTP["SMTP"]
    end

    Web <-->|REST| API
    Mobile <-->|REST| API
    API <-->|EF Core| DB
    API <-->|Cache| Cache
    API <-->|AI prompts| AI
    API <-->|Payments| RC
    RC -->|Webhooks| API
    Worker -->|Cron| DB
    Worker -->|Email| SMTP
    Worker -->|Push| FCM
```
