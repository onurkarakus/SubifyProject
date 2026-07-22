# Subify - API Contracts

Bu doküman, Subify API'sinin tüm endpoint'lerini detaylı Request/Response örnekleri ile dokümante eder.

> ## 🔄 Subify OS uyumluluk notu
>
> **Geçerli ürün modeli:** [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md) · [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md).
>
> Bu dosyadaki örnekler büyük ölçüde **eski SaaS** sözleşmesinden gelir. OS uygularken:
>
> | Konu | OS kararı |
> | ---- | --------- |
> | Free / Premium plan, paywall, 3 abonelik limiti | **Yok** — 403 limit dönülmez |
> | `/api/payments/*`, `/api/billing/*`, `/api/webhooks/revenuecat` | **Yok** — implement edilmez |
> | Reports / AI “Premium only” notları | **Yok** — tüm auth kullanıcılar (AI için instance API key gerekir) |
> | `user.plan` / `isPremium` alanları | **Yok** |
> | Dev base URL | Örn. `http://localhost:5240/api` · Scalar: `/scalar/v1` |
> | Admin | Super Admin: users, invites, **SystemSettings** (SMTP + AI key) |
>
> Endpoint eklerken OS PRD §12 önceliklidir; bu dosya request/response şekli için yardımcı referanstır.

> **Referanslar:**
>
> - [Subify OS PRD](./SUBIFY_OS_PRD.md) *(geçerli)*
> - [Eski SaaS PRD — LEGACY](./Subify.Web.Uygulamasi.v2.PRD.md)
> - [Sequence Diagrams](./diagrams/SEQUENCE_DIAGRAMS.md)

---

## 📋 Genel Bilgiler

### Base URL

```
Production: https://api.subify.app/api
Development: http://localhost:5000/api
```

### Authentication

```http
Authorization: Bearer <access_token>
```

### Headers

```http
Content-Type: application/json
Accept: application/json
Accept-Language: tr  # veya 'en'
```

### Error Response Format (RFC 7807 ProblemDetails)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "Email is already registered.",
  "instance": "/api/auth/register",
  "traceId": "00-abc123-def456-00"
}
```

### Pagination

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8
  }
}
```

---

## 1. Auth Controller (`/api/auth`)

### POST /api/auth/register

Yeni kullanıcı kaydı.

**Request:**

```json
{
  "email": "user@example.com",
  "password": "SecureP@ss123",
  "fullName": "Ahmet Yılmaz",
  "locale": "tr-TR"
}
```

**Response (201 Created):**

```json
{
  "message": "Kayıt başarılı. Lütfen e-postanızı doğrulayın.",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Error Responses:**
| Status | Title | Detail |
|--------|-------|--------|
| 400 | Validation Error | Email format is invalid |
| 409 | Conflict | Email is already registered |

---

### GET /api/auth/confirm-email

E-posta doğrulama.

**Query Parameters:**

```
userId=3fa85f64-5717-4562-b3fc-2c963f66afa6
code=CfDJ8NrAkS...
```

**Response (200 OK):**

```json
{
  "message": "E-posta adresiniz başarıyla doğrulandı."
}
```

---

### POST /api/auth/login

Kullanıcı girişi.

**Request:**

```json
{
  "email": "user@example.com",
  "password": "SecureP@ss123"
}
```

**Response (200 OK):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "fullName": "Ahmet Yılmaz",
    "plan": "free",
    "locale": "tr-TR"
  }
}
```

**Error Responses:**
| Status | Title | Detail |
|--------|-------|--------|
| 401 | Unauthorized | Invalid email or password |
| 401 | Unauthorized | Email not confirmed. Please verify your email |
| 423 | Locked | Account is locked. Try again in 15 minutes |

---

### POST /api/auth/refresh-token

Token yenileme.

**Request:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Response (200 OK):**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

---

### POST /api/auth/logout

Çıkış (Refresh token revoke).

**Request:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Response (200 OK):**

```json
{
  "message": "Başarıyla çıkış yapıldı."
}
```

---

### POST /api/auth/forgot-password

Şifre sıfırlama isteği.

**Request:**

```json
{
  "email": "user@example.com"
}
```

**Response (200 OK):**

```json
{
  "message": "Şifre sıfırlama linki e-posta adresinize gönderildi."
}
```

---

### POST /api/auth/reset-password

Şifre sıfırlama.

**Request:**

```json
{
  "email": "user@example.com",
  "code": "CfDJ8NrAkS...",
  "newPassword": "NewSecureP@ss123"
}
```

**Response (200 OK):**

```json
{
  "message": "Şifreniz başarıyla güncellendi."
}
```

---

## 2. Subscriptions Controller (`/api/subscriptions`)

### GET /api/subscriptions

Kullanıcının aboneliklerini listele.

**Query Parameters:**

```
?includeArchived=false
&category=streaming
&page=1
&pageSize=20
```

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Netflix",
      "price": 149.99,
      "currency": "TRY",
      "billingCycle": "monthly",
      "sharedWithCount": 4,
      "userShare": 37.5,
      "nextRenewalDate": "2026-01-15",
      "lastUsedAt": "2026-01-01",
      "archived": false,
      "category": {
        "slug": "streaming",
        "name": "Video Akış",
        "icon": "play-circle",
        "color": "#E50914"
      },
      "provider": {
        "id": "abc123",
        "name": "Netflix",
        "logoUrl": "https://cdn.subify.app/logos/netflix.png"
      },
      "createdAt": "2025-06-15T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 5,
    "totalPages": 1
  },
  "summary": {
    "monthlyTotal": 450.5,
    "yearlyTotal": 5406.0,
    "currency": "TRY"
  }
}
```

---

### GET /api/subscriptions/{id}

Abonelik detayı.

**Response (200 OK):**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Netflix",
  "price": 149.99,
  "currency": "TRY",
  "billingCycle": "monthly",
  "sharedWithCount": 4,
  "userShare": 37.5,
  "nextRenewalDate": "2026-01-15",
  "lastUsedAt": "2026-01-01",
  "notes": "Aile planı, 4 kişi paylaşıyor",
  "archived": false,
  "category": {
    "slug": "streaming",
    "name": "Video Akış",
    "icon": "play-circle",
    "color": "#E50914"
  },
  "provider": {
    "id": "abc123",
    "name": "Netflix",
    "logoUrl": "https://cdn.subify.app/logos/netflix.png",
    "lastVerifiedAt": "2025-12-01T00:00:00Z"
  },
  "createdAt": "2025-06-15T10:30:00Z",
  "updatedAt": "2025-12-20T14:00:00Z"
}
```

---

### POST /api/subscriptions

Yeni abonelik ekle.

**Request:**

```json
{
  "name": "Spotify",
  "providerId": "def456",
  "categoryId": "cat789",
  "price": 59.99,
  "currency": "TRY",
  "billingCycle": "monthly",
  "sharedWithCount": 1,
  "nextRenewalDate": "2026-02-01",
  "notes": "Bireysel plan"
}
```

**Response (201 Created):**

```json
{
  "id": "new-subscription-id",
  "name": "Spotify",
  "price": 59.99,
  "currency": "TRY",
  "billingCycle": "monthly",
  "sharedWithCount": 1,
  "userShare": 59.99,
  "nextRenewalDate": "2026-02-01",
  "createdAt": "2026-01-01T22:30:00Z"
}
```

**Error Responses:**
| Status | Title | Detail |
|--------|-------|--------|
| 400 | Validation Error | Price must be greater than 0 |
| 400 | Bad Request | Provider is not active |
| 403 | Forbidden | Subscription limit reached. Upgrade to premium |

---

### PUT /api/subscriptions/{id}

Abonelik güncelle.

**Request:**

```json
{
  "name": "Spotify Premium",
  "price": 79.99,
  "sharedWithCount": 2,
  "nextRenewalDate": "2026-02-15",
  "notes": "Duo plan"
}
```

**Response (200 OK):**

```json
{
  "id": "subscription-id",
  "name": "Spotify Premium",
  "price": 79.99,
  "sharedWithCount": 2,
  "userShare": 39.995,
  "updatedAt": "2026-01-01T22:35:00Z"
}
```

---

### DELETE /api/subscriptions/{id}

Aboneliği arşivle (soft delete).

**Response (200 OK):**

```json
{
  "message": "Abonelik arşivlendi."
}
```

---

### GET /api/subscriptions/upcoming

Yaklaşan ödemeleri listele.

**Query Parameters:**

```
?days=7
```

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "sub-id-1",
      "name": "Netflix",
      "userShare": 37.5,
      "currency": "TRY",
      "nextRenewalDate": "2026-01-03",
      "daysUntilRenewal": 2
    },
    {
      "id": "sub-id-2",
      "name": "Spotify",
      "userShare": 59.99,
      "currency": "TRY",
      "nextRenewalDate": "2026-01-05",
      "daysUntilRenewal": 4
    }
  ],
  "total": 97.49,
  "currency": "TRY"
}
```

---

## 3. Categories Controller (`/api/categories`)

### GET /api/categories

Sistem kategorilerini listele.

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "cat-001",
      "slug": "streaming",
      "name": "Video Akış",
      "icon": "play-circle",
      "color": "#E50914",
      "sortOrder": 1
    },
    {
      "id": "cat-002",
      "slug": "music",
      "name": "Müzik",
      "icon": "music-note",
      "color": "#1DB954",
      "sortOrder": 2
    },
    {
      "id": "cat-003",
      "slug": "productivity",
      "name": "Üretkenlik",
      "icon": "briefcase",
      "color": "#0078D4",
      "sortOrder": 3
    }
  ]
}
```

> [!NOTE]
> Category name değerleri Accept-Language header'ına göre lokalize edilir.

---

### GET /api/categories/user

Kullanıcının özel kategorilerini listele.

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "ucat-001",
      "name": "Spor Salonu",
      "icon": "dumbbell",
      "color": "#FF6B6B"
    }
  ]
}
```

---

### POST /api/categories/user

Özel kategori oluştur.

**Request:**

```json
{
  "name": "VPN Servisleri",
  "icon": "shield",
  "color": "#6C5CE7"
}
```

**Response (201 Created):**

```json
{
  "id": "ucat-002",
  "name": "VPN Servisleri",
  "icon": "shield",
  "color": "#6C5CE7",
  "createdAt": "2026-01-01T22:40:00Z"
}
```

---

## 4. Reports Controller (`/api/reports`)

> [!IMPORTANT]
> ~~Bu endpoint'ler Premium kullanıcılara özeldir.~~ **Subify OS:** Premium kilidi **yok**; kimliği doğrulanmış tüm kullanıcılar erişir.

### GET /api/reports/monthly-spend

Aylık harcama grafiği.

**Query Parameters:**

```
?months=12
&currency=TRY
```

**Response (200 OK):**

```json
{
  "data": [
    { "month": "2025-02", "total": 380.5 },
    { "month": "2025-03", "total": 420.0 },
    { "month": "2025-04", "total": 450.5 }
  ],
  "currency": "TRY",
  "average": 417.0
}
```

---

### GET /api/reports/category-breakdown

Kategori bazlı dağılım.

**Response (200 OK):**

```json
{
  "data": [
    {
      "category": "streaming",
      "name": "Video Akış",
      "color": "#E50914",
      "total": 187.49,
      "percentage": 41.6,
      "count": 2
    },
    {
      "category": "music",
      "name": "Müzik",
      "color": "#1DB954",
      "total": 119.98,
      "percentage": 26.6,
      "count": 2
    }
  ],
  "grandTotal": 450.5,
  "currency": "TRY"
}
```

---

## 5. AI Controller (`/api/ai`)

> [!IMPORTANT]
> ~~Premium kullanıcılara özel.~~ **Subify OS:** Plan kilidi yok. Instance’ta Super Admin **LLM API key** tanımlı olmalı; aksi halde key-missing hatası. Rate limit (stabilite): örn. 5/dk, 20/gün.

### POST /api/ai/analyze

AI analizi ve öneri.

**Request:**

```json
{
  "lang": "tr"
}
```

**Response (200 OK):**

```json
{
  "summary": "Aylık toplam harcamanız 450.50 TL. 5 aktif aboneliğiniz var.",
  "tips": [
    {
      "type": "unused",
      "subscriptionId": "sub-123",
      "subscriptionName": "HBOMax",
      "message": "HBOMax'ı son 45 gündür kullanmadınız. Dondurmayı düşünebilirsiniz.",
      "potentialSaving": 79.99
    },
    {
      "type": "duplicate",
      "message": "Video Akış kategorisinde 3 aboneliğiniz var. Birini gözden geçirebilirsiniz.",
      "potentialSaving": 49.99
    },
    {
      "type": "general",
      "message": "Yıllık planlara geçerek toplam %15 tasarruf edebilirsiniz.",
      "potentialSaving": 81.07
    }
  ],
  "estimatedMonthlySaving": 211.05,
  "estimatedYearlySaving": 2532.6,
  "analyzedAt": "2026-01-01T22:45:00Z"
}
```

**Error Responses:**
| Status | Title | Detail |
|--------|-------|--------|
| 403 | Forbidden | Premium subscription required |
| 429 | Too Many Requests | Rate limit exceeded. Try again in 60 seconds |

---

### GET /api/ai/history

Geçmiş AI önerileri.

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "ai-log-001",
      "summary": "Aylık toplam...",
      "estimatedMonthlySaving": 211.05,
      "createdAt": "2026-01-01T22:45:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 3,
    "totalPages": 1
  }
}
```

---

## 6. Profile Controller (`/api/profile`)

### GET /api/profile

Profil bilgileri.

**Response (200 OK):**

```json
{
  "id": "user-id",
  "email": "user@example.com",
  "fullName": "Ahmet Yılmaz",
  "locale": "tr",
  "plan": "premium",
  "planRenewsAt": "2026-02-01T00:00:00Z",
  "mainCurrency": "TRY",
  "monthlyBudget": 500.0,
  "applicationThemeColor": "Royal Purple",
  "darkTheme": true,
  "createdAt": "2025-06-01T10:00:00Z"
}
```

---

### PUT /api/profile

Profil güncelle.

**Request:**

```json
{
  "fullName": "Ahmet Yılmaz",
  "locale": "en-US",
  "mainCurrency": "USD",
  "monthlyBudget": 50.0,
  "applicationThemeColor": "Ocean Blue",
  "darkTheme": false
}
```

**Response (200 OK):**

```json
{
  "message": "Profil güncellendi.",
  "profile": { ... }
}
```

---

### PUT /api/profile/notifications

Bildirim ayarları.

**Request:**

```json
{
  "emailEnabled": true,
  "pushEnabled": true,
  "daysBeforeRenewal": 5
}
```

**Response (200 OK):**

```json
{
  "emailEnabled": true,
  "pushEnabled": true,
  "daysBeforeRenewal": 5
}
```

---

### POST /api/profile/device-token

Push token kaydı.

**Request:**

```json
{
  "token": "fcm-token-here",
  "platform": "android"
}
```

**Response (200 OK):**

```json
{
  "message": "Device token registered."
}
```

**Error Responses:**
| Status | Title | Detail |
|--------|-------|--------|
| 403 | Forbidden | Push notifications require premium subscription |

---

## 7. Activity Controller (`/api/activity`)

Kullanıcı aktivite loglarını listeler. Dashboard'da "Son İşlemler" gösterimi için kullanılır.

### GET /api/activity

Kullanıcının son aktivitelerini listele.

**Query Parameters:**

```
?page=1
&pageSize=10
&entityType=subscription    # opsiyonel filtre
```

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "act-001",
      "entityType": "subscription",
      "entityId": "sub-123",
      "action": "created",
      "description": "Netflix aboneliği eklendi",
      "createdAt": "2026-01-01T22:30:00Z"
    },
    {
      "id": "act-002",
      "entityType": "subscription",
      "entityId": "sub-456",
      "action": "updated",
      "description": "Spotify fiyatı 59₺ → 79₺ güncellendi",
      "oldValues": {
        "price": 59.99
      },
      "newValues": {
        "price": 79.99
      },
      "createdAt": "2026-01-01T21:15:00Z"
    },
    {
      "id": "act-003",
      "entityType": "ai_suggestion",
      "entityId": "ai-log-001",
      "action": "created",
      "description": "AI analizi yapıldı",
      "createdAt": "2026-01-01T20:45:00Z"
    },
    {
      "id": "act-004",
      "entityType": "payment",
      "entityId": "billing-001",
      "action": "created",
      "description": "Premium satın alındı",
      "createdAt": "2026-01-01T15:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 4,
    "totalPages": 1
  }
}
```

> [!NOTE]
> Activity logları otomatik olarak oluşturulur. Kullanıcılar bu endpoint üzerinden CRUD işlemi yapamaz.

**Entity Types:**

| Entity Type     | Açıklama                |
| --------------- | ----------------------- |
| `subscription`  | Abonelik CRUD işlemleri |
| `profile`       | Profil güncellemeleri   |
| `ai_suggestion` | AI analiz istekleri     |
| `payment`       | Ödeme/Premium işlemleri |
| `auth`          | Login/Logout olayları   |

**Actions:**

| Action     | Açıklama              |
| ---------- | --------------------- |
| `created`  | Kayıt oluşturuldu     |
| `updated`  | Kayıt güncellendi     |
| `deleted`  | Kayıt silindi         |
| `archived` | Kayıt arşivlendi      |
| `login`    | Kullanıcı giriş yaptı |
| `logout`   | Kullanıcı çıkış yaptı |

---

## 8. Payments Controller (`/api/payments`, `/api/billing`)

> **⛔ Subify OS:** Bu bölüm **geçersizdir**. Ödeme, checkout, RevenueCat webhook ve premium status endpoint’leri yoktur. Aşağıdaki içerik yalnızca legacy arşivdir.

### GET /api/payments/status

Premium durum sorgusu.

**Response (200 OK):**

```json
{
  "isPremium": true,
  "plan": "premium_monthly_tr",
  "expiresAt": "2026-02-01T00:00:00Z",
  "willRenew": true,
  "managementUrl": "https://app.revenuecat.com/manage/..."
}
```

---

### POST /api/billing/checkout

Web ödeme oturumu başlat.

**Request:**

```json
{
  "plan": "premium_yearly_tr",
  "successUrl": "https://subify.app/payment/success",
  "cancelUrl": "https://subify.app/payment/cancel"
}
```

**Response (200 OK):**

```json
{
  "checkoutUrl": "https://pay.revenuecat.com/checkout/...",
  "sessionId": "cs_abc123"
}
```

---

### POST /api/webhooks/revenuecat

RevenueCat webhook handler.

**Headers:**

```http
X-RevenueCat-Signature: sha256=...
```

**Request (from RevenueCat):**

```json
{
  "event": {
    "type": "INITIAL_PURCHASE",
    "app_user_id": "user-id",
    "product_id": "premium_monthly_tr",
    "entitlement_identifier": "premium"
  }
}
```

**Response (200 OK):**

```json
{
  "received": true
}
```

---

## 8. Providers Controller (`/api/providers`)

### GET /api/providers

Aktif sağlayıcı listesi.

**Query Parameters:**

```
?search=netflix
&region=TR
```

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "prov-001",
      "name": "Netflix",
      "slug": "netflix",
      "logoUrl": "https://cdn.subify.app/logos/netflix.png",
      "currency": "TRY",
      "price": 149.99,
      "priceBefore": 99.99,
      "billingCycle": "monthly",
      "region": "TR",
      "lastVerifiedAt": "2025-12-28T10:00:00Z",
      "sourceUrl": "https://www.netflix.com/tr/signup"
    }
  ]
}
```

---

### GET /api/providers/{id}

Sağlayıcı detayı.

**Response (200 OK):**

```json
{
  "id": "prov-001",
  "name": "Netflix",
  "slug": "netflix",
  "logoUrl": "https://cdn.subify.app/logos/netflix.png",
  "currency": "TRY",
  "price": 149.99,
  "priceBefore": 99.99,
  "billingCycle": "monthly",
  "region": "TR",
  "lastVerifiedAt": "2025-12-28T10:00:00Z",
  "sourceUrl": "https://www.netflix.com/tr/signup",
  "plans": [
    { "name": "Basic", "price": 99.99 },
    { "name": "Standard", "price": 149.99 },
    { "name": "Premium", "price": 249.99 }
  ]
}
```

---

## 9. Resources Controller (`/api/resources`)

### GET /api/resources

Localization resources.

**Query Parameters:**

```
?lang=tr
&since=2025-12-01T00:00:00Z
```

**Response (200 OK):**

```json
{
  "data": [
    {
      "pageName": "Dashboard",
      "name": "title",
      "value": "Kontrol Paneli"
    },
    {
      "pageName": "Category",
      "name": "streaming",
      "value": "Video Akış"
    }
  ],
  "lastUpdated": "2025-12-28T15:00:00Z"
}
```

**Response (304 Not Modified):** If no updates since `since` parameter.

---

## 10. Exchange Rates Controller (`/api/exchange-rates`)

### GET /api/exchange-rates

Döviz kurları.

**Query Parameters:**

```
?base=TRY
```

**Response (200 OK):**

```json
{
  "base": "TRY",
  "rates": {
    "USD": 0.0308,
    "EUR": 0.0284,
    "GBP": 0.0244
  },
  "lastUpdated": "2026-01-01T22:00:00Z"
}
```

---

## 11. System Controller (`/api/system`)

### GET /api/system/health

Health check.

**Response (200 OK):**

```json
{
  "status": "Healthy",
  "checks": {
    "mssql": "Healthy",
    "redis": "Healthy",
    "revenuecat": "Healthy"
  },
  "version": "1.0.0",
  "uptime": "5d 3h 22m"
}
```

---

### GET /api/system/currencies

Desteklenen para birimleri.

**Response (200 OK):**

```json
{
  "data": [
    { "code": "TRY", "name": "Türk Lirası", "symbol": "₺" },
    { "code": "USD", "name": "US Dollar", "symbol": "$" },
    { "code": "EUR", "name": "Euro", "symbol": "€" }
  ]
}
```

---

## 12. Admin Controller (`/api/admin`)

> [!CAUTION]
> Bu endpoint'ler **Admin** rolüne sahip kullanıcılara özeldir.

### GET /api/admin/users

Kullanıcı listesi.

**Query Parameters:**

```
?search=ahmet
&plan=premium
&page=1
&pageSize=20
```

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "user-id",
      "email": "user@example.com",
      "fullName": "Ahmet Yılmaz",
      "plan": "premium",
      "subscriptionCount": 5,
      "createdAt": "2025-06-01T10:00:00Z",
      "lastLoginAt": "2026-01-01T20:00:00Z"
    }
  ],
  "pagination": { ... }
}
```

---

### GET /api/admin/stats

Dashboard metrikleri.

**Response (200 OK):**

```json
{
  "totalUsers": 1250,
  "premiumUsers": 125,
  "conversionRate": 10.0,
  "totalSubscriptions": 4500,
  "averageSubscriptionsPerUser": 3.6,
  "monthlyRecurringRevenue": 6125.0,
  "currency": "TRY",
  "newUsersToday": 15,
  "newUsersThisWeek": 85,
  "newUsersThisMonth": 320
}
```

---

## 13. Email Templates Controller (`/api/email-templates`)

> [!CAUTION]
> Admin rolü gereklidir.

### GET /api/email-templates

Şablon listesi.

**Response (200 OK):**

```json
{
  "data": [
    {
      "id": "tpl-001",
      "name": "VerifyEmail",
      "languageCode": "tr-TR",
      "subject": "E-posta Adresinizi Doğrulayın",
      "updatedAt": "2025-12-15T10:00:00Z"
    },
    {
      "id": "tpl-002",
      "name": "VerifyEmail",
      "languageCode": "en-US",
      "subject": "Verify Your Email Address",
      "updatedAt": "2025-12-15T10:00:00Z"
    }
  ]
}
```

---

### GET /api/email-templates/{id}

Şablon detayı.

**Response (200 OK):**

```json
{
  "id": "tpl-001",
  "name": "VerifyEmail",
  "languageCode": "tr-TR",
  "subject": "E-posta Adresinizi Doğrulayın",
  "body": "<!DOCTYPE html><html>...",
  "variables": ["{{FullName}}", "{{VerificationLink}}"],
  "createdAt": "2025-12-01T10:00:00Z",
  "updatedAt": "2025-12-15T10:00:00Z"
}
```

---

### PUT /api/email-templates/{id}

Şablon güncelle.

**Request:**

```json
{
  "subject": "E-postanızı Doğrulayın - Subify",
  "body": "<!DOCTYPE html><html>..."
}
```

**Response (200 OK):**

```json
{
  "message": "Şablon güncellendi.",
  "template": { ... }
}
```

---

### POST /api/email-templates/{id}/test

Test e-postası gönder.

**Request:**

```json
{
  "recipientEmail": "test@example.com"
}
```

**Response (200 OK):**

```json
{
  "message": "Test e-postası gönderildi."
}
```

---

## 📊 Rate Limiting

| Endpoint Group         | Limit | Window  |
| ---------------------- | ----- | ------- |
| Auth (login, register) | 10    | /minute |
| AI analyze             | 5     | /minute |
| AI analyze             | 20    | /day    |
| Subscriptions write    | 30    | /minute |
| General read           | 100   | /minute |

**Rate Limit Headers:**

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1704144000
```

**Rate Limit Exceeded Response (429):**

```json
{
  "type": "https://httpstatuses.io/429",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Try again in 45 seconds.",
  "retryAfter": 45
}
```
