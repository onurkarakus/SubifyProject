# Subify - Entity Relationship Diagram (ERD)

Bu doküman, Subify uygulamasının veritabanı şemasını Mermaid formatında görselleştirir.

> **Referanslar:**
>
> - [Detaylı Veri Modeli](../DATA_MODEL.md)
> - [ADR Kararları](../ADR.md)

---

## 📊 Tam ERD Diyagramı

```mermaid
erDiagram
    %% Identity & Auth
    AspNetUsers {
        uniqueidentifier Id PK
        nvarchar UserName
        nvarchar Email
        bit EmailConfirmed
        nvarchar PasswordHash
        datetimeoffset LockoutEnd
    }

    profiles {
        uniqueidentifier id PK,FK
        nvarchar email
        nvarchar full_name
        varchar locale
        varchar plan
        datetimeoffset plan_renews_at
        varchar main_currency
        decimal monthly_budget
        nvarchar application_theme_color
        bit dark_theme
        datetimeoffset created_at
        datetimeoffset updated_at
    }

    refresh_tokens {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        nvarchar token
        datetimeoffset expires_at
        datetimeoffset created_at
        varchar created_by_ip
        datetimeoffset revoked_at
        varchar revoked_by_ip
        nvarchar replaced_by_token
        nvarchar reason_revoked
    }

    %% Core Business
    subscriptions {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        uniqueidentifier provider_id FK
        uniqueidentifier category_id FK
        uniqueidentifier user_category_id FK
        nvarchar name
        decimal price
        varchar currency
        varchar billing_cycle
        int shared_with_count
        date next_renewal_date
        date last_used_at
        nvarchar notes
        bit archived
        datetimeoffset created_at
        datetimeoffset updated_at
    }

    categories {
        uniqueidentifier id PK
        varchar slug UK
        nvarchar icon
        varchar color
        int sort_order
        bit is_default
        bit is_active
        datetimeoffset created_at
        datetimeoffset updated_at
    }

    user_categories {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        nvarchar name
        nvarchar icon
        varchar color
        datetimeoffset created_at
        datetimeoffset updated_at
    }

    providers {
        uniqueidentifier id PK
        nvarchar name
        varchar slug UK
        nvarchar logo_url
        varchar currency
        decimal price
        decimal price_before
        varchar billing_cycle
        varchar region
        nvarchar source_url
        datetimeoffset last_verified_at
        bit is_active
        datetimeoffset created_at
        datetimeoffset updated_at
    }

    %% Localization
    resources {
        uniqueidentifier id PK
        nvarchar page_name
        nvarchar name
        varchar language_code
        nvarchar value
        datetimeoffset created_at
        datetimeoffset updated_at
    }

    %% Billing
    billing_sessions {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        varchar provider
        nvarchar session_id
        varchar status
        datetimeoffset created_at
    }

    entitlements_cache {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        varchar entitlement
        varchar status
        datetimeoffset expires_at
        datetimeoffset updated_at
    }

    %% AI & Analytics
    ai_suggestions_logs {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        nvarchar request_payload
        nvarchar response_payload
        datetimeoffset created_at
    }

    activity_logs {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        varchar entity_type
        uniqueidentifier entity_id
        varchar action
        nvarchar description
        nvarchar old_values
        nvarchar new_values
        datetimeoffset created_at
    }

    %% Notifications
    notification_settings {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        bit email_enabled
        bit push_enabled
        int days_before_renewal
    }

    email_templates {
        uniqueidentifier id PK
        nvarchar name
        nvarchar language_code
        nvarchar subject
        nvarchar body
        datetimeoffset created_at
        datetimeoffset updated_at
    }

    %% System
    exchange_rate_snapshots {
        uniqueidentifier id PK
        varchar base_currency
        varchar target_currency
        decimal rate
        nvarchar source
        datetimeoffset fetched_at
        datetimeoffset created_at
    }

    %% Relationships
    AspNetUsers ||--|| profiles : "has"
    AspNetUsers ||--o{ refresh_tokens : "has"
    AspNetUsers ||--o{ subscriptions : "owns"
    AspNetUsers ||--o{ user_categories : "creates"
    AspNetUsers ||--|| notification_settings : "has"
    AspNetUsers ||--o{ ai_suggestions_logs : "generates"
    AspNetUsers ||--o{ activity_logs : "logs"
    AspNetUsers ||--o{ billing_sessions : "initiates"
    AspNetUsers ||--o{ entitlements_cache : "has"

    subscriptions }o--o| providers : "uses"
    subscriptions }o--o| categories : "belongs to"
    subscriptions }o--o| user_categories : "belongs to"
```

---

## 🔗 İlişki Açıklamaları

### 1:1 İlişkiler

| Kaynak        | Hedef                   | Açıklama                               |
| ------------- | ----------------------- | -------------------------------------- |
| `AspNetUsers` | `profiles`              | Her kullanıcının bir profili var       |
| `AspNetUsers` | `notification_settings` | Her kullanıcının bildirim ayarları var |

### 1:N İlişkiler

| Kaynak        | Hedef                 | Açıklama                                         |
| ------------- | --------------------- | ------------------------------------------------ |
| `AspNetUsers` | `refresh_tokens`      | Kullanıcının birden fazla token'ı olabilir       |
| `AspNetUsers` | `subscriptions`       | Kullanıcı birden fazla abonelik ekleyebilir      |
| `AspNetUsers` | `user_categories`     | Kullanıcı özel kategoriler oluşturabilir         |
| `AspNetUsers` | `ai_suggestions_logs` | Kullanıcı birden fazla AI önerisi alabilir       |
| `AspNetUsers` | `activity_logs`       | Kullanıcı aktiviteleri loglanır                  |
| `AspNetUsers` | `billing_sessions`    | Kullanıcı birden fazla ödeme girişimi yapabilir  |
| `AspNetUsers` | `entitlements_cache`  | Kullanıcının birden fazla entitlement'ı olabilir |

### N:1 İlişkiler (Optional)

| Kaynak          | Hedef             | Açıklama                                            |
| --------------- | ----------------- | --------------------------------------------------- |
| `subscriptions` | `providers`       | Abonelik bir sağlayıcıya bağlı olabilir (opsiyonel) |
| `subscriptions` | `categories`      | Abonelik sistem kategorisine bağlı olabilir         |
| `subscriptions` | `user_categories` | Abonelik kullanıcı kategorisine bağlı olabilir      |

---

## 📋 Modül Bazlı ERD

### Identity & Auth Modülü

```mermaid
erDiagram
    AspNetUsers ||--|| profiles : "1:1"
    AspNetUsers ||--o{ refresh_tokens : "1:N"

    AspNetUsers {
        uniqueidentifier Id PK
        nvarchar Email
        bit EmailConfirmed
    }

    profiles {
        uniqueidentifier id PK,FK
        varchar plan
        varchar main_currency
        bit dark_theme
    }

    refresh_tokens {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        nvarchar token
        datetimeoffset expires_at
    }
```

---

### Core Business Modülü

```mermaid
erDiagram
    AspNetUsers ||--o{ subscriptions : "owns"
    AspNetUsers ||--o{ user_categories : "creates"
    subscriptions }o--o| providers : "uses"
    subscriptions }o--o| categories : "system"
    subscriptions }o--o| user_categories : "custom"

    subscriptions {
        uniqueidentifier id PK
        nvarchar name
        decimal price
        int shared_with_count
    }

    categories {
        uniqueidentifier id PK
        varchar slug UK
        nvarchar icon
        varchar color
    }

    user_categories {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        nvarchar name
    }

    providers {
        uniqueidentifier id PK
        varchar slug UK
        nvarchar name
        decimal price
    }
```

---

### Localization Modülü

```mermaid
erDiagram
    resources {
        uniqueidentifier id PK
        nvarchar page_name
        nvarchar name
        varchar language_code
        nvarchar value
    }

    categories {
        uniqueidentifier id PK
        varchar slug UK
    }

    resources ||--o{ categories : "translates via slug"
```

> [!NOTE] > `categories.slug` değeri `resources` tablosunda `PageName='Category', Name=slug` şeklinde lookup yapılır.

---

### Billing & Entitlements Modülü

```mermaid
erDiagram
    AspNetUsers ||--o{ billing_sessions : "initiates"
    AspNetUsers ||--o{ entitlements_cache : "has"

    billing_sessions {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        varchar provider
        nvarchar session_id
        varchar status
    }

    entitlements_cache {
        uniqueidentifier id PK
        uniqueidentifier user_id FK
        varchar entitlement
        varchar status
        datetimeoffset expires_at
    }
```

---

## 🗃️ Index Stratejisi

| Tablo                     | Index                                               | Tip       | Açıklama              |
| ------------------------- | --------------------------------------------------- | --------- | --------------------- |
| `subscriptions`           | `(user_id, archived, next_renewal_date)`            | Composite | Dashboard sorguları   |
| `profiles`                | `(plan)`                                            | Single    | Plan bazlı filtreleme |
| `refresh_tokens`          | `(user_id, token)`                                  | Composite | Token validation      |
| `entitlements_cache`      | `(user_id, entitlement)`                            | Composite | Entitlement check     |
| `email_templates`         | `(name, language_code)`                             | Unique    | Template lookup       |
| `categories`              | `(slug)`                                            | Unique    | Slug bazlı lookup     |
| `providers`               | `(slug)`                                            | Unique    | Slug bazlı lookup     |
| `resources`               | `(page_name, name, language_code)`                  | Unique    | Localization lookup   |
| `exchange_rate_snapshots` | `(base_currency, target_currency, fetched_at DESC)` | Composite | Latest rate query     |
| `activity_logs`           | `(user_id, created_at DESC)`                        | Composite | Dashboard sorguları   |
