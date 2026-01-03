# Subify - Veri Modeli Dokümantasyonu

Bu doküman, Subify uygulamasının tüm veritabanı tablolarını, ilişkilerini ve kısıtlamalarını detaylı şekilde açıklar.

> **Referanslar:**
>
> - [Ana PRD](./Subify.Web.Uygulamasi.v2.PRD.md)
> - [ADR Kararları](./ADR.md)
> - [ERD Diyagramı](./diagrams/ERD.md)

---

## 📊 Genel Bakış

| Kategori            | Tablolar                                                      |
| ------------------- | ------------------------------------------------------------- |
| **Identity & Auth** | `AspNetUsers`, `AspNetRoles`, `profiles`, `refresh_tokens`    |
| **Core Business**   | `subscriptions`, `categories`, `user_categories`, `providers` |
| **Localization**    | `resources`                                                   |
| **Billing**         | `billing_sessions`, `entitlements_cache`                      |
| **AI & Analytics**  | `ai_suggestions_logs`, `activity_logs`                        |
| **Notifications**   | `notification_settings`, `email_templates`                    |
| **System**          | `exchange_rate_snapshots`                                     |

---

## 🔐 Identity & Auth

### `AspNetUsers` (ASP.NET Core Identity)

ASP.NET Core Identity tarafından otomatik oluşturulur.

| Alan                 | Tip                  | Açıklama                        |
| -------------------- | -------------------- | ------------------------------- |
| Id                   | uniqueidentifier, PK | Kullanıcı ID                    |
| UserName             | nvarchar(256)        | Kullanıcı adı (email)           |
| NormalizedUserName   | nvarchar(256)        | Normalize edilmiş kullanıcı adı |
| Email                | nvarchar(256)        | E-posta adresi                  |
| NormalizedEmail      | nvarchar(256)        | Normalize edilmiş e-posta       |
| EmailConfirmed       | bit                  | E-posta doğrulandı mı           |
| PasswordHash         | nvarchar(max)        | Hashlenmiş şifre                |
| SecurityStamp        | nvarchar(max)        | Güvenlik damgası                |
| ConcurrencyStamp     | nvarchar(max)        | Eşzamanlılık damgası            |
| PhoneNumber          | nvarchar(max)        | Telefon numarası (opsiyonel)    |
| PhoneNumberConfirmed | bit                  | Telefon doğrulandı mı           |
| TwoFactorEnabled     | bit                  | 2FA aktif mi                    |
| LockoutEnd           | datetimeoffset       | Kilitlenme bitiş zamanı         |
| LockoutEnabled       | bit                  | Kilitlenme aktif mi             |
| AccessFailedCount    | int                  | Başarısız giriş sayısı          |

---

### `profiles`

Kullanıcı profil bilgileri ve tercihler.

| Alan                    | Tip                      | Default             | Açıklama                                           |
| ----------------------- | ------------------------ | ------------------- | -------------------------------------------------- |
| id                      | uniqueidentifier, PK, FK | -                   | AspNetUsers.Id ile 1:1 ilişki                      |
| email                   | nvarchar(320)            | -                   | Kullanıcı e-postası                                |
| full_name               | nvarchar(200)            | -                   | Tam ad                                             |
| locale                  | varchar(5)               | 'tr'                | Dil tercihi ('tr', 'en')                           |
| plan                    | varchar(20)              | 'free'              | Plan tipi ('free', 'premium')                      |
| plan_renews_at          | datetimeoffset           | null                | Premium yenileme tarihi                            |
| main_currency           | varchar(10)              | 'TRY'               | **[ADR-009]** Ana para birimi                      |
| monthly_budget          | decimal(10,2)            | null                | **[ADR-009]** Aylık bütçe limiti (null = disabled) |
| application_theme_color | nvarchar(50)             | 'Royal Purple'      | **[ADR-009]** Tema rengi                           |
| dark_theme              | bit                      | 0                   | **[ADR-009]** Karanlık tema aktif mi               |
| created_at              | datetimeoffset           | sysdatetimeoffset() | Oluşturulma zamanı                                 |
| updated_at              | datetimeoffset           | sysdatetimeoffset() | Güncellenme zamanı                                 |

**Tema Rengi Seçenekleri:**

- Royal Purple, Ocean Blue, Forest Green, Sunset Orange, Cherry Red, Golden Yellow

**Budget Warning Logic:**

```csharp
if (user.MonthlyBudget > 0 && monthlyTotal > user.MonthlyBudget)
{
    // Trigger budget exceeded warning
}
```

---

### `refresh_tokens`

JWT refresh token yönetimi.

| Alan              | Tip                  | Default             | Açıklama                      |
| ----------------- | -------------------- | ------------------- | ----------------------------- |
| id                | uniqueidentifier, PK | -                   | Token ID                      |
| user_id           | uniqueidentifier, FK | -                   | AspNetUsers.Id                |
| token             | nvarchar(max)        | -                   | Hashlenmiş token              |
| expires_at        | datetimeoffset       | -                   | Geçerlilik bitiş zamanı       |
| created_at        | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı            |
| created_by_ip     | varchar(45)          | -                   | Oluşturan IP                  |
| revoked_at        | datetimeoffset       | null                | İptal zamanı                  |
| revoked_by_ip     | varchar(45)          | null                | İptal eden IP                 |
| replaced_by_token | nvarchar(max)        | null                | Yerine geçen token (rotation) |
| reason_revoked    | nvarchar(200)        | null                | İptal nedeni                  |

**Revoke Reasons:** `'logout'`, `'replaced'`, `'theft_detected'`

**Indexes:**

- `(user_id, token)`

---

## 📦 Core Business

### `subscriptions`

Kullanıcı abonelikleri.

| Alan              | Tip                  | Default             | Açıklama                                         |
| ----------------- | -------------------- | ------------------- | ------------------------------------------------ |
| id                | uniqueidentifier, PK | NEWSEQUENTIALID()   | Abonelik ID                                      |
| user_id           | uniqueidentifier, FK | -                   | AspNetUsers.Id                                   |
| provider_id       | uniqueidentifier, FK | null                | **[PRD]** providers.id (opsiyonel)               |
| category_id       | uniqueidentifier, FK | null                | **[ADR-006]** categories.id                      |
| user_category_id  | uniqueidentifier, FK | null                | **[ADR-006]** user_categories.id                 |
| name              | nvarchar(200)        | -                   | Abonelik adı                                     |
| price             | decimal(10,2)        | -                   | Fiyat                                            |
| currency          | varchar(10)          | 'TRY'               | Para birimi                                      |
| billing_cycle     | varchar(10)          | -                   | Döngü ('monthly', 'yearly')                      |
| shared_with_count | int                  | 1                   | **[ADR-007]** Paylaşım sayısı (1 = paylaşım yok) |
| next_renewal_date | date                 | -                   | Sonraki yenileme tarihi                          |
| last_used_at      | date                 | null                | Son kullanım tarihi                              |
| notes             | nvarchar(max)        | null                | Notlar                                           |
| archived          | bit                  | 0                   | Arşivlendi mi (soft delete)                      |
| created_at        | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı                               |
| updated_at        | datetimeoffset       | sysdatetimeoffset() | Güncellenme zamanı                               |

> [!IMPORTANT] > **Kategori Kuralı:** `category_id` ve `user_category_id` mutually exclusive'dir. Biri dolu ise diğeri null olmalıdır.

**Computed Property (DB'de saklanmaz):**

```csharp
public decimal UserShare => SharedWithCount > 0 ? Price / SharedWithCount : Price;
```

**Indexes:**

- `(user_id, archived, next_renewal_date)`

---

### `categories`

Sistem kategorileri. **[ADR-004]** Name bu tabloda tutulmaz, Resource tablosundan lookup yapılır.

| Alan       | Tip                  | Default             | Açıklama                           |
| ---------- | -------------------- | ------------------- | ---------------------------------- |
| id         | uniqueidentifier, PK | NEWSEQUENTIALID()   | Kategori ID                        |
| slug       | varchar(50)          | -                   | Unique slug ('streaming', 'music') |
| icon       | nvarchar(50)         | -                   | İkon adı ('play-circle')           |
| color      | varchar(10)          | -                   | Renk kodu ('#E50914')              |
| sort_order | int                  | 0                   | Sıralama                           |
| is_default | bit                  | 1                   | Sistem kategorisi mi               |
| is_active  | bit                  | 1                   | Aktif mi                           |
| created_at | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı                 |
| updated_at | datetimeoffset       | sysdatetimeoffset() | Güncellenme zamanı                 |

**Localization Lookup:**

```
Resource: { PageName: 'Category', Name: slug, LanguageCode: 'TR' }
Örnek: PageName='Category', Name='streaming' → Value='Video Akış'
```

**Indexes:**

- `UNIQUE (slug)`
- `(is_active)`

---

### `user_categories`

**[ADR-006]** Kullanıcı tanımlı özel kategoriler.

| Alan       | Tip                  | Default             | Açıklama              |
| ---------- | -------------------- | ------------------- | --------------------- |
| id         | uniqueidentifier, PK | NEWSEQUENTIALID()   | Kategori ID           |
| user_id    | uniqueidentifier, FK | -                   | AspNetUsers.Id        |
| name       | nvarchar(100)        | -                   | Kategori adı          |
| icon       | nvarchar(50)         | null                | İkon adı (opsiyonel)  |
| color      | varchar(10)          | null                | Renk kodu (opsiyonel) |
| created_at | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı    |
| updated_at | datetimeoffset       | sysdatetimeoffset() | Güncellenme zamanı    |

**Indexes:**

- `(user_id)`

---

### `providers`

Abonelik sağlayıcıları (Netflix, Spotify, vb.)

| Alan             | Tip                  | Default             | Açıklama               |
| ---------------- | -------------------- | ------------------- | ---------------------- |
| id               | uniqueidentifier, PK | NEWSEQUENTIALID()   | Sağlayıcı ID           |
| name             | nvarchar(200)        | -                   | Sağlayıcı adı          |
| slug             | varchar(100)         | -                   | Unique slug            |
| logo_url         | nvarchar(500)        | null                | Logo URL               |
| currency         | varchar(10)          | 'TRY'               | Varsayılan para birimi |
| price            | decimal(10,2)        | null                | Önerilen fiyat         |
| price_before     | decimal(10,2)        | null                | Önceki fiyat           |
| billing_cycle    | varchar(10)          | 'monthly'           | Varsayılan döngü       |
| region           | varchar(10)          | 'TR'                | Bölge                  |
| source_url       | nvarchar(500)        | null                | Fiyat kaynağı URL      |
| last_verified_at | datetimeoffset       | null                | Son doğrulama zamanı   |
| is_active        | bit                  | 1                   | Aktif mi               |
| created_at       | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı     |
| updated_at       | datetimeoffset       | sysdatetimeoffset() | Güncellenme zamanı     |

**Indexes:**

- `UNIQUE (slug)`
- `(is_active)`

---

## 🌍 Localization

### `resources`

**[ADR-001]** DB-driven localization tablosu.

| Alan          | Tip                  | Default             | Açıklama                                            |
| ------------- | -------------------- | ------------------- | --------------------------------------------------- |
| id            | uniqueidentifier, PK | NEWSEQUENTIALID()   | Resource ID                                         |
| page_name     | nvarchar(100)        | -                   | Sayfa/Modül adı ('Dashboard', 'Category', 'Common') |
| name          | nvarchar(100)        | -                   | Resource key ('title', 'streaming', 'save')         |
| language_code | varchar(5)           | -                   | Dil kodu ('tr', 'en')                               |
| value         | nvarchar(max)        | -                   | Çeviri metni                                        |
| created_at    | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı                                  |
| updated_at    | datetimeoffset       | sysdatetimeoffset() | Güncellenme zamanı                                  |

**Indexes:**

- `UNIQUE (page_name, name, language_code)`

**API Endpoint:**

```
GET /api/resources?lang=TR&since={lastSyncedAt}
```

**Client Sync Flow:**

1. App açılışında delta sync çağrısı
2. Client LocalStorage'da cache
3. Backend Redis cache (TTL: 1 saat)

---

## 💳 Billing

### `billing_sessions`

Ödeme oturumları.

| Alan       | Tip                  | Default             | Açıklama                            |
| ---------- | -------------------- | ------------------- | ----------------------------------- |
| id         | uniqueidentifier, PK | NEWSEQUENTIALID()   | Session ID                          |
| user_id    | uniqueidentifier, FK | -                   | AspNetUsers.Id                      |
| provider   | varchar(30)          | 'revenuecat'        | Ödeme sağlayıcısı                   |
| session_id | nvarchar(200)        | -                   | Checkout session ID                 |
| status     | varchar(20)          | 'pending'           | Durum ('pending', 'paid', 'failed') |
| created_at | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı                  |

---

### `entitlements_cache`

**[ADR-002]** RevenueCat entitlement cache.

| Alan        | Tip                  | Default             | Açıklama                    |
| ----------- | -------------------- | ------------------- | --------------------------- |
| id          | uniqueidentifier, PK | NEWSEQUENTIALID()   | Cache ID                    |
| user_id     | uniqueidentifier, FK | -                   | AspNetUsers.Id              |
| entitlement | varchar(100)         | -                   | Entitlement adı ('premium') |
| status      | varchar(20)          | -                   | Durum ('active', 'expired') |
| expires_at  | datetimeoffset       | null                | Bitiş zamanı                |
| updated_at  | datetimeoffset       | sysdatetimeoffset() | Güncellenme zamanı          |

**Indexes:**

- `(user_id, entitlement)`

**Redis Cache:** TTL 5-15 dakika, Webhook → DEL key

---

## 🤖 AI & Analytics

### `ai_suggestions_logs`

AI öneri logları.

| Alan             | Tip                  | Default             | Açıklama           |
| ---------------- | -------------------- | ------------------- | ------------------ |
| id               | uniqueidentifier, PK | NEWSEQUENTIALID()   | Log ID             |
| user_id          | uniqueidentifier, FK | -                   | AspNetUsers.Id     |
| request_payload  | nvarchar(max)        | -                   | İstek JSON         |
| response_payload | nvarchar(max)        | -                   | Yanıt JSON         |
| created_at       | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı |

---

### `activity_logs`

Kullanıcı aktivite logları. Dashboard'da "Son İşlemler" listesi için kullanılır.

| Alan        | Tip                  | Default             | Açıklama                                                 |
| ----------- | -------------------- | ------------------- | -------------------------------------------------------- |
| id          | uniqueidentifier, PK | NEWSEQUENTIALID()   | Log ID                                                   |
| user_id     | uniqueidentifier, FK | -                   | AspNetUsers.Id                                           |
| entity_type | varchar(50)          | -                   | Entity tipi ('subscription', 'profile', 'ai_suggestion') |
| entity_id   | uniqueidentifier     | null                | İlgili kaydın ID'si (opsiyonel)                          |
| action      | varchar(30)          | -                   | Aksiyon ('created', 'updated', 'deleted', 'archived')    |
| description | nvarchar(500)        | -                   | Okunabilir açıklama ("Netflix aboneliği eklendi")        |
| old_values  | nvarchar(max)        | null                | JSON - güncelleme öncesi değerler                        |
| new_values  | nvarchar(max)        | null                | JSON - güncelleme sonrası değerler                       |
| ip_address  | varchar(45)          | null                | İstek IP adresi                                          |
| user_agent  | nvarchar(500)        | null                | Browser/App user agent                                   |
| created_at  | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı                                       |

**Entity Types:**

- `subscription` - Abonelik işlemleri
- `profile` - Profil güncellemeleri
- `ai_suggestion` - AI analiz istekleri
- `payment` - Ödeme işlemleri
- `auth` - Kimlik doğrulama (login/logout)

**Actions:**

- `created` - Yeni kayıt oluşturuldu
- `updated` - Kayıt güncellendi
- `deleted` - Kayıt silindi
- `archived` - Kayıt arşivlendi
- `login` - Kullanıcı giriş yaptı
- `logout` - Kullanıcı çıkış yaptı

**Indexes:**

- `(user_id, created_at DESC)` - Dashboard sorguları için

**Örnek Kayıtlar:**

| entity_type   | action   | description                          |
| ------------- | -------- | ------------------------------------ |
| subscription  | created  | Netflix aboneliği eklendi            |
| subscription  | updated  | Spotify fiyatı 59₺ → 79₺ güncellendi |
| subscription  | archived | HBOMax arşivlendi                    |
| ai_suggestion | created  | AI analizi yapıldı                   |
| profile       | updated  | Tema rengi değiştirildi              |
| payment       | created  | Premium satın alındı                 |

**API Endpoint:**

```
GET /api/activity?page=1&pageSize=10
```

---

## 🔔 Notifications

### `notification_settings`

Kullanıcı bildirim tercihleri.

| Alan                | Tip                  | Default           | Açıklama                   |
| ------------------- | -------------------- | ----------------- | -------------------------- |
| id                  | uniqueidentifier, PK | NEWSEQUENTIALID() | Setting ID                 |
| user_id             | uniqueidentifier, FK | -                 | AspNetUsers.Id             |
| email_enabled       | bit                  | 1                 | E-posta bildirimi aktif mi |
| push_enabled        | bit                  | 0                 | Push bildirimi aktif mi    |
| days_before_renewal | int                  | 3                 | Kaç gün önce uyar          |

---

### `email_templates`

Admin tarafından yönetilen e-posta şablonları.

| Alan          | Tip                  | Default             | Açıklama                                      |
| ------------- | -------------------- | ------------------- | --------------------------------------------- |
| id            | uniqueidentifier, PK | NEWSEQUENTIALID()   | Template ID                                   |
| name          | nvarchar(100)        | -                   | Şablon adı ('VerifyEmail', 'RenewalReminder') |
| language_code | nvarchar(5)          | -                   | Dil kodu ('tr', 'en')                         |
| subject       | nvarchar(255)        | -                   | E-posta konusu                                |
| body          | nvarchar(max)        | -                   | HTML gövdesi                                  |
| created_at    | datetimeoffset       | sysdatetimeoffset() | Oluşturulma zamanı                            |
| updated_at    | datetimeoffset       | sysdatetimeoffset() | Güncellenme zamanı                            |

**Indexes:**

- `UNIQUE (name, language_code)`

---

## 💱 System

### `exchange_rate_snapshots`

**[ADR-008]** Döviz kuru snapshot'ları.

| Alan            | Tip                  | Default             | Açıklama                              |
| --------------- | -------------------- | ------------------- | ------------------------------------- |
| id              | uniqueidentifier, PK | NEWSEQUENTIALID()   | Snapshot ID                           |
| base_currency   | varchar(10)          | -                   | Kaynak para birimi ('TRY')            |
| target_currency | varchar(10)          | -                   | Hedef para birimi ('USD', 'EUR')      |
| rate            | decimal(18,6)        | -                   | Kur değeri                            |
| source          | nvarchar(100)        | -                   | Veri kaynağı ('exchangerate-api.com') |
| fetched_at      | datetimeoffset       | -                   | API'den çekilme zamanı                |
| created_at      | datetimeoffset       | sysdatetimeoffset() | Kayıt zamanı                          |

**Indexes:**

- `(base_currency, target_currency, fetched_at DESC)`

**Background Job:** Saatlik sync, Redis cache (TTL: 1 saat)

**API Endpoint:**

```
GET /api/exchange-rates?base=TRY
```

---

## 📊 Cache Stratejisi

**[ADR-002]** Redis Cache-Aside (Lazy Loading) Pattern

| Entity             | Redis Cache | TTL         | Invalidation             |
| ------------------ | ----------- | ----------- | ------------------------ |
| `Resource`         | ✅          | 1 saat      | Admin CRUD → DEL key     |
| `EntitlementCache` | ✅          | 5-15 dakika | Webhook → DEL key        |
| `Category`         | ✅          | 1 saat      | Admin CRUD → DEL key     |
| `Provider`         | ✅          | 1 saat      | Admin CRUD → DEL key     |
| `ExchangeRate`     | ✅          | 1 saat      | Background job → refresh |

**Cache Key Patterns:**

```
resources:{languageCode}           → JSON array
entitlement:{userId}               → JSON object
categories:all                     → JSON array
providers:active                   → JSON array
exchange-rates:{baseCurrency}      → JSON object
```

---

## 🔗 İlişki Özeti

```
AspNetUsers (1) ──── (1) profiles
     │
     │ (1) ──── (N) refresh_tokens
     │ (1) ──── (N) subscriptions
     │ (1) ──── (N) user_categories
     │ (1) ──── (1) notification_settings
     │ (1) ──── (N) ai_suggestions_logs
     │ (1) ──── (N) activity_logs
     │ (1) ──── (N) billing_sessions
     │ (1) ──── (N) entitlements_cache

subscriptions (N) ──── (1) providers (optional)
subscriptions (N) ──── (1) categories (optional)
subscriptions (N) ──── (1) user_categories (optional)
```

---

## ✅ GUID Generation Strategy

**[ADR-010]** EF Core ile GUID oluşturma:

```csharp
// BaseEntity - No default assignment
public Guid Id { get; set; }

// EF Core Configuration
builder.Property(e => e.Id)
    .HasDefaultValueSql("NEWSEQUENTIALID()");
```

**Gerekçe:**

- `NEWSEQUENTIALID()`: Clustered index fragmentation minimize
- Insert performance iyileşir
- Unit test'lerde manuel ID atanması gerekir
