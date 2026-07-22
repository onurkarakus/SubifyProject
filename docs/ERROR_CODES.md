# Subify - Hata Kodları Kataloğu

Bu doküman, Subify API'sinin döndürdüğü tüm hata kodlarını ve çözüm önerilerini içerir.

> ## 🔄 Subify OS uyumluluk notu
>
> **Geçerli ürün modeli:** [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md) · [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md).
>
> | Kod / grup | SaaS (bu dosya) | Subify OS |
> | ---------- | --------------- | --------- |
> | `SUB_001` Subscription Limit | Free max 3 | **Kullanılmaz** — limit yok |
> | `AI_001` Premium Required | Premium abonelik | **Kullanılmaz** — yerine örn. `AI_KEY_MISSING` (instance key yok) |
> | `REP_001` Premium Required | Premium rapor | **Kullanılmaz** — raporlar herkese açık (auth) |
> | `PRO_006` Push Requires Premium | Premium push | **Kullanılmaz** (veya push tamamen opsiyonel) |
> | `PAY_*` | Ödeme / webhook | **Kullanılmaz** — ödeme yok |
>
> Implementasyonda `DomainErrors` OS’a göre güncellenmelidir (task list `1.2.4`). Aşağıdaki tablolar hâlâ eski kataloğu gösterir; üst satırlar geçersiz sayılır.

> **Referanslar:**
>
> - [API_CONTRACTS.md](./API_CONTRACTS.md) *(OS notlarına dikkat)*
> - [Subify OS PRD](./SUBIFY_OS_PRD.md)
> - [Error Response Format (RFC 7807)](https://tools.ietf.org/html/rfc7807)

---

## 📋 Error Response Format

Tüm API hataları RFC 7807 ProblemDetails formatında döner:

```json
{
  "type": "https://api.subify.app/errors/{error_code}",
  "title": "Human Readable Title",
  "status": 400,
  "detail": "Detailed explanation of what went wrong.",
  "instance": "/api/endpoint",
  "traceId": "00-abc123-def456-00",
  "errors": {
    "field": ["Validation error message"]
  }
}
```

---

## 🔐 Authentication Errors (AUTH_xxx)

| Code       | HTTP | Title                     | Detail                                                                       | Çözüm                         |
| ---------- | ---- | ------------------------- | ---------------------------------------------------------------------------- | ----------------------------- |
| `AUTH_001` | 401  | Invalid Credentials       | Email or password is incorrect                                               | Giriş bilgilerini kontrol et  |
| `AUTH_002` | 401  | Email Not Verified        | Please verify your email before logging in                                   | Email doğrulama linkine tıkla |
| `AUTH_003` | 401  | Invalid Token             | The access token is invalid or expired                                       | Token'ı yenile                |
| `AUTH_004` | 401  | Invalid Refresh Token     | The refresh token is invalid, expired, or revoked                            | Yeniden giriş yap             |
| `AUTH_005` | 423  | Account Locked            | Too many failed attempts. Try again in {minutes} minutes                     | Bekle veya şifre sıfırla      |
| `AUTH_006` | 400  | Password Too Weak         | Password must be at least 8 characters with uppercase, lowercase, and number | Daha güçlü şifre kullan       |
| `AUTH_007` | 400  | Invalid Email Format      | The email address format is invalid                                          | Geçerli email gir             |
| `AUTH_008` | 409  | Email Already Registered  | This email is already registered                                             | Giriş yap veya şifre sıfırla  |
| `AUTH_009` | 400  | Invalid Reset Code        | The password reset code is invalid or expired                                | Yeni kod talep et             |
| `AUTH_010` | 400  | Invalid Verification Code | The email verification code is invalid or expired                            | Yeni doğrulama maili iste     |

### Örnek Response

```json
{
  "type": "https://api.subify.app/errors/AUTH_001",
  "title": "Invalid Credentials",
  "status": 401,
  "detail": "The email or password you entered is incorrect.",
  "instance": "/api/auth/login",
  "traceId": "00-abc123-def456-00"
}
```

---

## 📦 Subscription Errors (SUB_xxx)

| Code      | HTTP | Title                      | Detail                                                       | Çözüm                     |
| --------- | ---- | -------------------------- | ------------------------------------------------------------ | ------------------------- |
| `SUB_001` | 403  | Subscription Limit Reached | ~~Free plan max 3~~ · **OS: KALDIRILDI — kullanma**          | —                         |
| `SUB_002` | 404  | Subscription Not Found     | The subscription with ID {id} was not found                  | ID'yi kontrol et          |
| `SUB_003` | 403  | Subscription Access Denied | You don't have permission to access this subscription        | Kendi aboneliklerine eriş |
| `SUB_004` | 400  | Invalid Price              | Price must be greater than 0                                 | Pozitif fiyat gir         |
| `SUB_005` | 400  | Invalid Billing Cycle      | Billing cycle must be 'monthly' or 'yearly'                  | Geçerli döngü seç         |
| `SUB_006` | 400  | Invalid Renewal Date       | Renewal date must be in the future                           | Gelecek tarih seç         |
| `SUB_007` | 400  | Provider Not Active        | The selected provider is no longer active                    | Başka sağlayıcı seç       |
| `SUB_008` | 400  | Category Conflict          | Cannot set both category_id and user_category_id             | Sadece birini seç         |
| `SUB_009` | 404  | Category Not Found         | The category with ID {id} was not found                      | Geçerli kategori seç      |
| `SUB_010` | 400  | Invalid Shared Count       | Shared with count must be at least 1                         | Minimum 1 gir             |

### Örnek Response

```json
{
  "type": "https://api.subify.app/errors/SUB_001",
  "title": "Subscription Limit Reached",
  "status": 403,
  "detail": "Free plan allows maximum 3 subscriptions. Upgrade to premium for unlimited subscriptions.",
  "instance": "/api/subscriptions",
  "traceId": "00-xyz789-uvw012-00"
}
```

---

## 🤖 AI Errors (AI_xxx)

| Code     | HTTP | Title                        | Detail                                                    | Çözüm                  |
| -------- | ---- | ---------------------------- | --------------------------------------------------------- | ---------------------- |
| `AI_001` | 403  | Premium Required             | ~~Premium subscription~~ · **OS: KALDIRILDI** — key yoksa `AI_KEY_MISSING` kullan | Super Admin SystemSettings |
| `AI_002` | 429  | Rate Limit Exceeded (Minute) | You have exceeded the rate limit of 5 requests per minute | 1 dakika bekle         |
| `AI_003` | 429  | Rate Limit Exceeded (Daily)  | You have exceeded the daily limit of 20 AI requests       | Yarın tekrar dene      |
| `AI_004` | 503  | AI Service Unavailable       | The AI service is temporarily unavailable                 | Daha sonra tekrar dene |
| `AI_005` | 500  | AI Processing Error          | An error occurred while processing your request           | Tekrar dene            |
| `AI_006` | 400  | Insufficient Data            | You need at least 1 subscription for AI analysis          | Abonelik ekle          |

### Örnek Response

```json
{
  "type": "https://api.subify.app/errors/AI_002",
  "title": "Rate Limit Exceeded",
  "status": 429,
  "detail": "You have exceeded the rate limit of 5 requests per minute. Please wait 45 seconds.",
  "instance": "/api/ai/analyze",
  "traceId": "00-rate123-limit456-00",
  "retryAfter": 45
}
```

---

## 💳 Payment Errors (PAY_xxx)

> **⛔ Subify OS:** Tüm `PAY_*` kodları **geçersizdir** (ödeme altyapısı yok). Aşağısı legacy arşivdir.

| Code      | HTTP | Title                     | Detail                                          | Çözüm                        |
| --------- | ---- | ------------------------- | ----------------------------------------------- | ---------------------------- |
| `PAY_001` | 400  | Invalid Plan              | The selected plan is not valid                  | Geçerli plan seç             |
| `PAY_002` | 400  | Checkout Creation Failed  | Failed to create checkout session               | Tekrar dene                  |
| `PAY_003` | 400  | Already Premium           | You already have an active premium subscription | -                            |
| `PAY_004` | 400  | Payment Processing Failed | Payment could not be processed                  | Ödeme bilgilerini kontrol et |
| `PAY_005` | 400  | Invalid Webhook           | Invalid webhook signature                       | Sistem hatası, log incele    |
| `PAY_006` | 404  | Session Not Found         | Billing session not found                       | Yeni checkout başlat         |

---

## 👤 Profile Errors (PRO_xxx)

| Code      | HTTP | Title                 | Detail                                            | Çözüm                   |
| --------- | ---- | --------------------- | ------------------------------------------------- | ----------------------- |
| `PRO_001` | 404  | Profile Not Found     | User profile not found                            | Profil oluştur          |
| `PRO_002` | 400  | Invalid Locale        | Locale must be 'tr' or 'en'                       | Geçerli dil seç         |
| `PRO_003` | 400  | Invalid Currency      | Currency must be a valid ISO 4217 code            | Geçerli para birimi seç |
| `PRO_004` | 400  | Invalid Theme         | Theme color is not supported                      | Desteklenen tema seç    |
| `PRO_005` | 400  | Invalid Budget        | Monthly budget must be positive or null           | Pozitif değer gir       |
| `PRO_006` | 403  | Push Requires Premium | Push notifications require a premium subscription | Premium'a geç           |
| `PRO_007` | 400  | Invalid Device Token  | The device token format is invalid                | Geçerli FCM token gir   |

---

## 📊 Report Errors (REP_xxx)

| Code      | HTTP | Title              | Detail                                   | Çözüm                     |
| --------- | ---- | ------------------ | ---------------------------------------- | ------------------------- |
| `REP_001` | 403  | Premium Required   | Reports require a premium subscription   | Premium'a geç             |
| `REP_002` | 400  | Invalid Date Range | The date range is invalid                | Geçerli tarih aralığı seç |
| `REP_003` | 400  | Insufficient Data  | Not enough data for the requested report | Daha fazla abonelik ekle  |

---

## 🌍 Resource Errors (RES_xxx)

| Code      | HTTP | Title              | Detail                                              | Çözüm                        |
| --------- | ---- | ------------------ | --------------------------------------------------- | ---------------------------- |
| `RES_001` | 400  | Invalid Language   | Language code must be 'tr' or 'en'                  | Geçerli dil kodu gir         |
| `RES_002` | 400  | Invalid Since Date | The 'since' parameter must be a valid ISO 8601 date | Geçerli tarih formatı kullan |

---

## 🔧 System Errors (SYS_xxx)

| Code      | HTTP | Title                 | Detail                                 | Çözüm                                  |
| --------- | ---- | --------------------- | -------------------------------------- | -------------------------------------- |
| `SYS_001` | 500  | Internal Server Error | An unexpected error occurred           | Tekrar dene, sorun devam ederse destek |
| `SYS_002` | 503  | Service Unavailable   | The service is temporarily unavailable | Daha sonra tekrar dene                 |
| `SYS_003` | 504  | Gateway Timeout       | The request timed out                  | Tekrar dene                            |
| `SYS_004` | 429  | Too Many Requests     | General rate limit exceeded            | Bekle                                  |

---

## ✅ Validation Errors (VAL_xxx)

| Code      | HTTP | Title                  | Detail                                                | Çözüm                         |
| --------- | ---- | ---------------------- | ----------------------------------------------------- | ----------------------------- |
| `VAL_001` | 400  | Validation Failed      | One or more validation errors occurred                | `errors` field'ını kontrol et |
| `VAL_002` | 400  | Required Field Missing | The field '{field}' is required                       | Zorunlu alanı doldur          |
| `VAL_003` | 400  | Invalid Format         | The field '{field}' has an invalid format             | Formatı düzelt                |
| `VAL_004` | 400  | Max Length Exceeded    | The field '{field}' exceeds maximum length of {max}   | Daha kısa değer gir           |
| `VAL_005` | 400  | Min Length Required    | The field '{field}' must be at least {min} characters | Daha uzun değer gir           |

### Validation Error Örnek

```json
{
  "type": "https://api.subify.app/errors/VAL_001",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/subscriptions",
  "traceId": "00-val123-err456-00",
  "errors": {
    "name": ["Name is required", "Name must be at most 200 characters"],
    "price": ["Price must be greater than 0"],
    "billingCycle": ["Billing cycle must be 'monthly' or 'yearly'"]
  }
}
```

---

## 📱 Client Handling Guidelines

### Flutter

```dart
void handleApiError(DioException error) {
  final response = error.response;
  if (response == null) {
    showError('Network error. Please check your connection.');
    return;
  }

  final errorCode = response.data['type']?.split('/').last;

  switch (errorCode) {
    case 'AUTH_001':
    case 'AUTH_002':
      showError(response.data['detail']);
      break;
    case 'SUB_001':
      showPaywall();
      break;
    case 'AI_002':
    case 'AI_003':
      final retryAfter = response.data['retryAfter'] ?? 60;
      showError('Rate limited. Try again in $retryAfter seconds.');
      break;
    default:
      showError(response.data['detail'] ?? 'An error occurred');
  }
}
```

### Next.js

```typescript
function handleApiError(error: ApiError) {
  const errorCode = error.type?.split("/").pop();

  switch (errorCode) {
    case "AUTH_001":
    case "AUTH_002":
      toast.error(error.detail);
      break;
    case "SUB_001":
      openPaywallModal();
      break;
    case "AI_002":
    case "AI_003":
      toast.error(`Rate limited. Try again in ${error.retryAfter} seconds.`);
      break;
    default:
      toast.error(error.detail || "An error occurred");
  }
}
```

---

## 📊 Error Monitoring

| Log Level   | Error Codes                       | Action                         |
| ----------- | --------------------------------- | ------------------------------ |
| **Error**   | SYS_001, SYS_002, AI_004, AI_005  | Alert, investigate immediately |
| **Warning** | AUTH_005, AI_002, AI_003, PAY_004 | Monitor patterns               |
| **Info**    | AUTH_001, SUB_001, VAL_001        | Normal user errors, no action  |

---

## ✅ Error Codes Summary

| Prefix    | Domain         | Count  |
| --------- | -------------- | ------ |
| `AUTH_`   | Authentication | 10     |
| `SUB_`    | Subscriptions  | 10     |
| `AI_`     | AI Services    | 6      |
| `PAY_`    | Payments       | 6      |
| `PRO_`    | Profile        | 7      |
| `REP_`    | Reports        | 3      |
| `RES_`    | Resources      | 2      |
| `SYS_`    | System         | 4      |
| `VAL_`    | Validation     | 5      |
| **Total** |                | **53** |
