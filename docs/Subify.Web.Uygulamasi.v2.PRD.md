> ## ⚠️ LEGACY — UYGULAMA KAYNAĞI DEĞİL
>
> Bu dosya **eski freemium / SaaS** ürün PRD’sidir (RevenueCat, premium limit, MSSQL, mobile-first vb.).
>
> **Uygulama ve yeni kararlar için kullanmayın.** Geçerli kaynaklar (öncelik sırasıyla):
>
> 1. [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md)
> 2. [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md)
> 3. [SUBIFY_OS_TASK_LIST.md](./SUBIFY_OS_TASK_LIST.md)
>
> Bu belge yalnızca tarihsel referans / arşiv amaçlıdır.

---

## 📝 Product Requirements Document: Subify (Web + Mobile, v1 - MVP, Revamped Tech Stack)

> **Son Güncelleme:** 2026-01-01
>
> **Geliştirme Önceliği:** Mobile First → Web → Admin *(legacy; OS modelinde Web-first)*

---

### 📚 İlgili Dokümanlar

| Doküman                                                   | Açıklama                                            |
| --------------------------------------------------------- | --------------------------------------------------- |
| [DATA_MODEL.md](./DATA_MODEL.md)                          | Detaylı veritabanı şeması ve tablo açıklamaları     |
| [API_CONTRACTS.md](./API_CONTRACTS.md)                    | Tüm API endpoint'leri ve Request/Response örnekleri |
| [UI_MOCKUPS.md](./UI_MOCKUPS.md)                          | Mobil ve web arayüz tasarımları                     |
| [ADR.md](./ADR.md)                                        | Mimari karar kayıtları                              |
| **Diyagramlar**                                           |                                                     |
| [ERD.md](./diagrams/ERD.md)                               | Entity Relationship Diagram (Mermaid)               |
| [SEQUENCE_DIAGRAMS.md](./diagrams/SEQUENCE_DIAGRAMS.md)   | Kritik akış diyagramları                            |
| [COMPONENT_DIAGRAM.md](./diagrams/COMPONENT_DIAGRAM.md)   | Sistem bileşenleri                                  |
| [DEPLOYMENT_DIAGRAM.md](./diagrams/DEPLOYMENT_DIAGRAM.md) | Docker Compose deployment                           |
| **Ek Dokümanlar**                                         |                                                     |
| [SEED_DATA.md](./SEED_DATA.md)                            | Başlangıç verileri (kategoriler, sağlayıcılar)      |
| [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md)            | RevenueCat ödeme entegrasyonu                       |
| [ERROR_CODES.md](./ERROR_CODES.md)                        | API hata kodları kataloğu                           |
| [TESTING_STRATEGY.md](./TESTING_STRATEGY.md)              | Test stratejisi ve coverage hedefleri               |
| [LOGGING_MONITORING.md](./LOGGING_MONITORING.md)          | Loglama ve izleme stratejisi                        |

---

### ✅ TL;DR

Subify, kullanıcıların tüm aboneliklerini (Netflix, Spotify, HBOMax vb.) tek bir yerden yönetmesini sağlar; ödemeleri görür, AI destekli analiz alır. MVP:

- **Web**: Next.js (App Router)
- **Backend**: ASP.NET Core 8 Web API (REST) + MSSQL
- **Mobile**: Flutter (iOS/Android)
- **Ödeme**: RevenueCat (web & mobil abonelik yönetimi)
- **AI**: OpenAI API
- **Host**: VPS (Docker + reverse proxy + HTTPS)
- **Model**: Freemium → Premium (TR ve EN)

---

### 🎯 Goals

#### Business Goals

- Abonelik yönetimi ihtiyacını karşılayarak ücretli kullanıcıya dönüşüm sağlamak.
- Minimum altyapı maliyetiyle (VPS) gelir üretmeye başlamak (RevenueCat üzerinden).
- Kullanıcı davranışlarını öğrenip mobil ve global genişleme için zemin hazırlamak.
- E-posta şablonlarının SaaS admin paneli üzerinden yönetilmesini sağlamak.

#### User Goals

- Tüm abonelikleri tek ekranda görmek.
- Aylık/yıllık toplam ödeme tutarlarını izlemek.
- Ödeme zamanı geldiğinde uyarı almak (e-posta, mobil push).
- AI önerileriyle gereksiz harcamaları belirlemek.
- Premium özellikleri (AI analiz, kategori bazlı rapor, push notification) kullanmak.
- Admin olarak e-posta şablonlarını düzenlemek ve farklı diller için özelleştirmek.

#### Non-Goals

- Kurumsal team accounts (şimdilik yok).
- Otomatik kredi kartı çekimi entegrasyonu (card vaulting) (MVP’de yok; RevenueCat kullanıyoruz).
- Gelişmiş finansal open-banking entegrasyonları (MVP’de yok).

---

### 👥 User Stories

- Kullanıcı olarak yeni abonelik eklemek istiyorum.
- Kullanıcı olarak aylık/yıllık toplam giderimi görmek istiyorum.
- Kullanıcı olarak ödeme günü yaklaştığında e-posta/push almak istiyorum.
- Premium kullanıcı olarak kategori bazlı analiz görmek istiyorum.
- Premium kullanıcı olarak AI önerileri almak istiyorum.
- Free kullanıcı olarak premium özelliklere tıklayınca CTA/paywall görmek istiyorum.
- Admin olarak e-posta şablonlarını düzenlemek ve farklı diller için özelleştirmek istiyorum.

---

### 🧭 User Experience (Web & Mobile)

1. **Onboarding & Account Setup**
   - E-posta ile kayıt / giriş (JWT tabanlı; sosyal giriş sonraya).
   - **E-posta Doğrulama**: Kayıt sonrası kullanıcıya doğrulama linki gönderilir. Linke tıklandıktan sonra giriş yapılabilir.
   - İlk 3 abonelik ücretsiz.
   - Dashboard yönlendirme.
2. **Dashboard**
   - Aktif abonelik listesi.
   - Aylık/Yıllık toplam harcama.
   - “+ Yeni Abonelik” butonu.
   - Kart: logo, fiyat, döngü, tarih.
3. **Kategori ve Raporlama (Premium)**
   - Raporlar sekmesi (kategori bazlı).
   - Free’de blur + CTA “Premium’a geç”.
4. **AI Önerileri (Premium)**
   - “AI’dan analiz al” butonu (Free’de paywall).
   - Premium’da: özet + öneriler + tahmini tasarruf.
5. **Bildirimler**
   - E-posta (Freemium).
   - Mobil push (Premium) – FCM/APNS, RevenueCat entegre plan doğrulama.
6. **Paywall**
   - Premium özelliklere tıklanınca modal + fiyat + CTA.
7. **Mobil**
   - Flutter app: Dashboard, abonelik listesi, raporlar (premium), AI öneri tetikleme, push, profil yönetimi.
8. **E-posta Şablon Yönetimi (Admin)**
   - Admin panelinde e-posta şablonlarını listeleme, düzenleme ve silme.
   - Şablonlara dil bazlı özelleştirme ekleme (ör. TR/EN).
   - Şablonların önizlemesini görme ve test e-postası gönderme.

---

### 📊 Success Metrics

- Kayıtlı kullanıcı sayısı.
- Premium’a dönüşüm oranı.
- Kullanıcı başına ortalama abonelik sayısı.
- AI önerisi sonrası iptal/dondurma oranı.
- Bildirim tıklanma oranı (email + push).
- Admin panelinde e-posta şablonlarının düzenlenme oranı.

---

### 🧱 Technical Stack

- **Web Frontend**: Next.js (App Router), TypeScript, next-i18next, Tailwind/Chakra (tercih).
- **Mobile**: Flutter (iOS/Android), Riverpod (State Mgmt), GoRouter (Navigation), Dio (HTTP), Flutter Intl (i18n).
- **Backend**: ASP.NET Core 8 Web API (minimal APIs veya controllers), C#, DI (built-in), ProblemDetails, FluentValidation.
- **Database**: MSSQL (on VPS). Migrations: EF Core.
- **Auth**: ASP.NET Core Identity + JWT (access+refresh). Password flow (MVP), sosyal giriş sonraya.
- **Payments**: RevenueCat
  - Web: RevenueCat + Stripe (RevenueCat Hosted Paywalls/Stripe entegre) — checkout URL’leri.
  - Mobile: RevenueCat SDK (App Store / Play Store IAP), entitlements ile premium kontrolü.
- **AI**: OpenAI API (chat/completions). Prompt server-side; rate limit & logging.
- **Notifications**:
  - Email: SMTP/Resend (sunucu tarafı job).
  - Push: Firebase Cloud Messaging (FCM) + RevenueCat entitlement webhook ile plan sync.
- **Background Jobs**: Hangfire (VPS, MSSQL storage) veya Quartz.NET. Cron benzeri: yenileme uyarıları, email dispatch, cleanup, exchange rate sync.
- **Caching**: Redis Cache-Aside (Lazy Loading) pattern. Detaylar için [DATA_MODEL.md](./DATA_MODEL.md#-cache-stratejisi) bakınız.

  | Entity             | TTL     | Invalidation             |
  | ------------------ | ------- | ------------------------ |
  | `Resource`         | 1 saat  | Admin CRUD → DEL key     |
  | `EntitlementCache` | 5-15 dk | Webhook → DEL key        |
  | `Category`         | 1 saat  | Admin CRUD → DEL key     |
  | `Provider`         | 1 saat  | Admin CRUD → DEL key     |
  | `ExchangeRate`     | 1 saat  | Background job → refresh |

- **Localization**: DB-driven resource table. [ADR-001](./ADR.md#adr-001-localization-strategy) kararına göre:

  - Client app açılışında `GET /api/resources?lang=TR&since={lastSyncedAt}` ile delta sync
  - Typo fix = DB update → client restart'ta otomatik güncellenir
  - Yeni dil eklemek = sadece DB insert (App Store update gerektirmez)

- **Observability**: OpenTelemetry (traces/logs/metrics) + OTLP exporter; Serilog + JSON; Health Checks `/health` + liveness/readiness; Prometheus format opsiyonel (prom-to-otlp veya node exporter yanına otelcol).
- **API Security**: JWT auth, role/claim-based (plan: free/premium), rate limiting (ASP.NET built-in), input validation, CORS (web + mobile schemes).
- **Hosting**: VPS (Linux), Docker Compose. Detaylar için [DEPLOYMENT_DIAGRAM.md](./diagrams/DEPLOYMENT_DIAGRAM.md) bakınız.
  - `reverse-proxy` (Nginx/Caddy) TLS termination (Let's Encrypt).
  - `api` (ASP.NET), `db` (MSSQL, ideally managed or container with volume), `worker` (Hangfire server), `otel-collector` (opsiyonel), `frontend` (Next.js served via reverse proxy).
- **CDN/Static**: For web assets (optional Cloudflare) and QR static.

---

### 🗓 Milestones & Sequencing

1. **Web MVP**: Auth, subscriptions CRUD, dashboard, email alert (cron), free limit (3).
2. **Premium gating + RevenueCat entegrasyonu**: Web checkout, entitlements doğrulama, paywall.
3. **AI önerileri & raporlama (premium)**.
4. **Flutter app (v1)**: Auth, list, dashboard, paywall link, push.
5. **Mobile push + QR yönlendirme**.

---

### 🌍 Dil Desteği

- EN + TR. Web: next-i18next JSON. Mobile: Flutter Intl (.arb files).
- Backend yanıtları i18n-aware (Accept-Language / profile.locale).
- Email ve AI yanıtları için dil seçimi.

---

### 💸 Fiyatlandırma (Güncel)

- **Freemium**: 3 abonelik, temel dashboard, e-posta uyarısı, AI/rapor yok.
- **Premium (RevenueCat)**:
  - TR: 49 TL / ay, 499 TL / yıl , 699 TL / ömür boyu.
  - Global: $4.99 / mo, $49.99 / yr , $69.99 / life time.
- RevenueCat ürünleri/entitlements:
  - `premium_monthly`, `premium_yearly`, `lifetime`
  - Paywall konfig: web (Stripe), iOS, Android store ürün ID’leri eşlenmiş.

---

### 🔐 Yetki & Limit Mantığı

- Auth: ASP.NET Identity + JWT; Refresh token rotation.
- Free limit: max 3 active subscriptions (archived hariç) → 403 + mesaj.
- AI endpoint: premium check via entitlement (profiles.plan == premium OR RevenueCat active entitlement).
- Rate limiting: IP + user-based limits on write & AI endpoints.

---

### 🧱 Veri Modeli (MSSQL, EF Core)

> **📖 Detaylı Dokümantasyon:** [DATA_MODEL.md](./DATA_MODEL.md)
>
> **📊 ERD Diyagramı:** [diagrams/ERD.md](./diagrams/ERD.md)

#### Özet Tablo Listesi

| Kategori            | Tablolar                                                      |
| ------------------- | ------------------------------------------------------------- |
| **Identity & Auth** | `AspNetUsers`, `profiles`, `refresh_tokens`                   |
| **Core Business**   | `subscriptions`, `categories`, `user_categories`, `providers` |
| **Localization**    | `resources`                                                   |
| **Billing**         | `billing_sessions`, `entitlements_cache`                      |
| **AI & Analytics**  | `ai_suggestions_logs`, `activity_logs`                        |
| **Notifications**   | `notification_settings`, `email_templates`                    |
| **System**          | `exchange_rate_snapshots`                                     |

#### ADR'lerden Gelen Önemli Değişiklikler

**[ADR-001] `resources` tablosu (Yeni):**

- DB-driven localization için
- `page_name`, `name`, `language_code`, `value`
- App Store update gerektirmeden çeviri güncellemesi

**[ADR-004] `categories` tablosu (Güncelleme):**

- `name` alanı kaldırıldı, yerine `slug` kullanılıyor
- Lokalizasyon `resources` tablosundan lookup yapılır

**[ADR-006] `user_categories` tablosu (Yeni):**

- Kullanıcı tanımlı özel kategoriler
- `subscriptions.user_category_id` ile bağlantı

**[ADR-007] `subscriptions` tablosu (Güncelleme):**

- `shared_with_count` eklendi (paylaşım sayısı)
- `category_id` ve `user_category_id` FK'ları eklendi
- `UserShare = Price / SharedWithCount` computed property

**[ADR-008] `exchange_rate_snapshots` tablosu (Yeni):**

- Döviz kuru snapshot'ları
- Background job ile saatlik güncelleme

**[ADR-009] `profiles` tablosu (Güncelleme):**

- `main_currency`, `monthly_budget` eklendi
- `application_theme_color`, `dark_theme` eklendi

**[ADR-010] GUID Generation:**

- `NEWSEQUENTIALID()` kullanımı (clustered index optimization)

**`activity_logs` tablosu (Yeni):**

- Dashboard'da "Son İşlemler" listesi için kullanıcı aktivite logları
- `entity_type`, `action`, `description` alanları
- Audit trail ve UX iyileştirmesi için

### 📌 Abonelik Sağlayıcı Seçimi (Plan Yok)

- Kullanıcı abonelik eklerken:
  - Sağlayıcı listesinden seçim (Netflix, Exxen, Amazon vb.) **veya** serbest metin isim (ör. “MahalleGym”).
  - Sağlayıcı seçilirse: fiyat/para birimi/döngü otomatik dolsun, kullanıcı isterse override edebilsin.
  - Sağlayıcı `is_active = false` ise yeni abonelikte seçilemez; daha önce eklenmiş kayıtlar görüntülenir/raporlanır.
-
- Doğruluk/güvenlik: UI’da “son doğrulanma zamanı” ve “kaynak” metni gösterilir; fiyat uyuşmazsa kullanıcı fiyatı değiştirip kaydedebilir.
- Fiyat güncelleme yöntemleri:
  1. Manuel/admin doğrulama (kaynak URL + last_verified_at güncellenir).
  2. Opsiyonel job/scraper belirli sağlayıcılardan fiyatı çekmeye çalışır ve günceller.

---

### 🌐 API Tasarımı (ASP.NET Core Web API, `/api`)

Auth: Bearer JWT. All endpoints return RFC 7807 ProblemDetails on errors.

1. **AuthController** (`/api/auth`)

   - `POST /register`: Yeni kullanıcı kaydı (Doğrulama maili gönderir).
   - `GET /verify-email`: E-posta doğrulama (Query: userId, code).
   - `POST /resend-confirmation-email`: Doğrulama mailini tekrar gönder.
   - `POST /login`: Giriş (Access + Refresh Token).
   - `POST /refresh-token`: Token yenileme.
   - `POST /logout`: Çıkış (Refresh token revoke).
   - `POST /forgot-password`: Şifre sıfırlama isteği.
   - `POST /reset-password`: Şifre sıfırlama işlemi.
   - `GET /me`: Mevcut kullanıcı bilgilerini getirir.

2. **SubscriptionsController** (`/api/subscriptions`)

   - `GET /`: Listeleme (Filtre: `includeArchived`, `category`).
   - `GET /{id}`: Detay.
   - `POST /`: Ekleme (Freemium limiti kontrolü).
   - `PUT /{id}`: Güncelleme.
   - `DELETE /{id}`: Arşivleme (Soft delete).
   - `GET /upcoming`: Yaklaşan ödemeler.

3. **CategoriesController** (`/api/categories`)

   - `GET /`: Sistem kategorileri (Resource tablosundan).
   - `POST /`: (Opsiyonel) Özel kategori.

4. **ReportsController** (`/api/reports`)

   - `GET /monthly-spend`: Aylık grafik verisi.
   - `GET /category-breakdown`: Kategori dağılımı.
   - `GET /currency-distribution`: Para birimi dağılımı.

5. **AiController** (`/api/ai`)

   - `POST /analyze`: Analiz ve öneri üret (Premium).
   - `GET /history`: Geçmiş öneriler.
   - `POST /feedback`: Geri bildirim.

6. **ProfileController** (`/api/profile`)

   - `GET /`: Profil bilgileri.
   - `PUT /`: Güncelleme.
   - `PUT /notifications`: Bildirim ayarları.
   - `POST /device-token`: Push token kaydı.

7. **PaymentsController** (`/api/payments` & `/api/billing`)

   - `GET /api/payments/status`: Premium durum sorgusu.
   - `POST /api/billing/checkout`: Web ödeme oturumu başlatma (RevenueCat/Stripe).
   - `POST /api/webhooks/revenuecat`: Webhook handler.

8. **SystemController** (`/api/system`)

   - `GET /currencies`: Desteklenen para birimleri.
   - `GET /health`: Health check (Global).

9. **AdminController** (`/api/admin`) - _Require Role: Admin_

   - `GET /users`: Tüm kullanıcıları listele (Sayfalama + Arama).
   - `GET /stats`: Dashboard metrikleri (Toplam kullanıcı, Aktif abonelik, Tahmini gelir).
   - `GET /logs`: Sistem loglarını görüntüle (Son hatalar).
   - `GET /transactions`: Ödeme geçmişini listele (BillingSessions tablosundan).
   - `GET /feedback`: Kullanıcıların AI önerilerine verdiği geri bildirimler.
   - Sadece `Admin` rolüne sahip kullanıcılar erişebilir.

10. **EmailTemplatesController** (`/api/email-templates`)

    - `GET /`: Tüm şablonları listele (sayfalama ve filtreleme destekli).
    - `GET /{id}`: Şablon detaylarını getir.
    - `POST /`: Yeni bir şablon oluştur.
    - `PUT /{id}`: Mevcut bir şablonu güncelle.
    - `DELETE /{id}`: Şablonu sil.
    - Sadece `Admin` rolüne sahip kullanıcılar erişebilir.

11. **ProvidersController** (`/api/providers`)

    - `GET /`: Aktif sağlayıcı listesi (name, slug, logo, currency, price, billing_cycle, region, last_verified_at, source_url).
    - `GET /{id}`: Sağlayıcı detayı.
    - `GET /{id}/pricing-history`: Fiyat değişim logu.

12. **ResourcesController** (`/api/resources`) - **[ADR-001]**

    - `GET /?lang=TR&since={timestamp}`: Delta sync ile localized resources.
    - Public endpoint, rate limited.

13. **ExchangeRatesController** (`/api/exchange-rates`) - **[ADR-008]**

    - `GET /?base=TRY`: Döviz kurları (cached).
    - Public endpoint, rate limited.

14. **ActivityController** (`/api/activity`)
    - `GET /?page=1&pageSize=10`: Son aktiviteler listesi.
    - Dashboard'da "Son İşlemler" gösterimi için.
    - Otomatik log kaydı (subscription, profile, payment, auth işlemleri).

> **📖 Detaylı API Dokümantasyonu:** [API_CONTRACTS.md](./API_CONTRACTS.md)
>
> Request/Response örnekleri, error formatları ve rate limiting detayları için yukarıdaki dokümana bakınız.

---

### 📋 Subify Feature Listesi
1. Authentication & Account
•	E-posta ile kayıt (JWT tabanlı)
•	E-posta ile giriş (JWT tabanlı)
•	E-posta doğrulama (kayıt sonrası zorunlu)
•	Şifre sıfırlama (e-posta ile)
•	Refresh token ile oturum yenileme
•	Çıkış (refresh token revoke)
•	Profil bilgileri görüntüleme ve güncelleme
•	Kullanıcı dil tercihi (locale)
•	Tema rengi ve dark mode tercihi (application_theme_color, dark_theme)
•	Ana para birimi ve aylık bütçe limiti
---
2. Abonelik Yönetimi (Subscriptions)
•	Abonelik ekleme (provider seçimi veya serbest isim)
•	Abonelik güncelleme
•	Abonelik arşivleme (soft delete)
•	Abonelik silme (opsiyonel, MVP’de arşivleme)
•	Abonelik detaylarını görüntüleme
•	Abonelik listesi (aktif/arsivlenmiş filtreleriyle)
•	Yaklaşan ödemeler listesi
•	Paylaşımlı abonelik desteği (shared_with_count)
•	Kullanıcı başına freemium limiti (max 3 aktif abonelik, premiumda limitsiz)
•	Kategoriye göre filtreleme
•	Kategori ve sağlayıcıya göre abonelik ekleme
•	Not ekleme ve güncelleme
•	Son kullanım tarihi takibi (last_used_at)
---
3. Kategori Yönetimi
•	Sistem kategorileri (slug, icon, color, sort)
•	Kategori isimlerinin çoklu dil desteği (resource tablosu üzerinden)
•	Kullanıcı tanımlı özel kategoriler (user_categories)
•	Kategoriye göre raporlama ve filtreleme
---
4. Raporlama & Analiz
•	Aylık/yıllık toplam harcama raporu
•	Kategori bazlı harcama dağılımı (premium)
•	Para birimi dağılımı
•	Yaklaşan ödemeler raporu
•	Dashboard’da özet ve grafikler
•	Premium kullanıcılar için detaylı raporlar (blur/CTA ile gating)
---
5. AI Destekli Analiz (Premium)
•	AI ile abonelik harcama analizi ve öneriler
•	Kullanıcıya özel özet ve tasarruf önerileri
•	Kategori ve kullanım sıklığına göre öneriler
•	AI öneri geçmişi görüntüleme
•	AI önerilerine geri bildirim gönderme
•	Rate limit ve günlük kota kontrolü
---
6. Bildirimler
•	E-posta ile ödeme hatırlatıcıları (tüm kullanıcılar)
•	Mobil push bildirimleri (sadece premium)
•	Bildirim tercihleri yönetimi (e-posta/push, gün sayısı)
•	E-posta şablonlarının admin panelinden yönetimi ve çoklu dil desteği
•	Test e-postası gönderme ve şablon önizleme (admin)
---
7. Ödeme & Premium Yönetimi
•	RevenueCat entegrasyonu (web: Stripe, mobil: Store IAP)
•	Premium plan satın alma ve entitlement kontrolü
•	Web paywall (Stripe checkout, RevenueCat hosted)
•	Mobil paywall (RevenueCat SDK)
•	Premium plan yenileme, iptal ve downgrade işlemleri (webhook ile)
•	Premium avantajlarının gating’i (rapor, AI, push, limitsiz abonelik)
•	Premium durumunun API ve cache üzerinden kontrolü
---
8. Admin Paneli
•	Kullanıcı yönetimi (listeleme, arama, sayfalama)
•	Sistem metrikleri ve dashboard (toplam kullanıcı, aktif abonelik, gelir)
•	Sistem logları ve hata geçmişi
•	Ödeme geçmişi ve transaction listesi
•	AI öneri geri bildirimleri
•	E-posta şablon yönetimi (CRUD, dil bazlı)
•	Kategori, sağlayıcı, resource yönetimi (opsiyonel)
•	Sadece admin rolüne sahip kullanıcılar erişebilir
---
9. Sistem & Altyapı
•	Sağlayıcı yönetimi (aktif sağlayıcılar, fiyat, döngü, bölge, logo)
•	Döviz kuru yönetimi ve cache (exchange rates)
•	Resource tabanlı lokalizasyon (TR/EN, delta sync, Redis cache)
•	Health check endpointleri
•	Rate limiting (IP ve kullanıcı bazlı)
•	OpenTelemetry ile izleme ve loglama
•	Hangfire/Quartz ile background job’lar (yenileme uyarısı, entitlement sync, AI log cleanup)
•	Docker Compose ile dağıtım, reverse proxy, TLS, otomatik migration
---
10. Web & Mobile UX
•	Web: Next.js, App Router, i18n, paywall, dashboard, admin panel
•	Mobil: Flutter, Riverpod, Dio, push, paywall, AI, raporlar, profil yönetimi
•	Responsive ve mobile-first tasarım
•	Free/Premium feature gating (blur + CTA)
•	Çoklu dil desteği (TR/EN), backend ve frontend uyumlu
---
11. Güvenlik & Uyumluluk
•	JWT tabanlı kimlik doğrulama, refresh token rotation
•	Role/claim-based authorization (admin, premium)
•	Input validation (FluentValidation)
•	CORS, HTTPS, output encoding, PII loglama koruması
•	Webhook signature validation (RevenueCat)
•	DB backup, environment secrets, least privilege
---
12. Diğer
•	Aktivite logları (dashboard’da son işlemler)
•	Exchange rate snapshot’ları ve cache
•	Test stratejisi (unit, integration, contract, E2E)
•	CDN/static asset desteği (opsiyonel)
•	App Store update gerektirmeden lokalizasyon güncelleme
---

### 📂 Proje Dizini Yapısı (Backend)

src/
└── Subify.Api/
    ├── Common/                # Ortak yardımcılar, base sınıflar, extensionlar
    ├── Features/
    │   ├── Auth/
    │   │   ├── Register/
    │   │   │   ├── RegisterEndpoint.cs
    │   │   │   ├── RegisterCommand.cs
    │   │   │   ├── RegisterHandler.cs
    │   │   │   └── RegisterValidator.cs
    │   │   ├── Login/
    │   │   ├── Logout/
    │   │   ├── RefreshTokens/
    │   │   ├── ForgotPassword/
    │   │   ├── ResetPassword/
    │   │   ├── VerifyEmail/
    │   │   ├── ResendConfirmation/
    │   │   └── GetCurrentUser/
    │   ├── Subscriptions/
    │   │   ├── CreateSubscription/
    │   │   ├── UpdateSubscription/
    │   │   ├── ArchiveSubscription/
    │   │   ├── GetSubscription/
    │   │   ├── ListSubscriptions/
    │   │   ├── GetUpcomingPayments/
    │   │   └── DeleteSubscription/
    │   ├── Categories/
    │   │   ├── GetCategories/
    │   │   ├── CreateCategories/
    │   │   ├── UpdateCategories/
    │   │   └── DeleteCategory/
    │   ├── UserCategories/
    │   │   ├── GetUserCategories/
    │   │   ├── CreateUserCategories/
    │   │   ├── UpdateUserCategories/
    │   │   └── DeleteUserCategories/
    │   ├── Reports/
    │   │   ├── GetMonthlySpend/
    │   │   ├── GetCategoryBreakdown/
    │   │   └── GetCurrencyDistribution/
    │   ├── Ai/
    │   │   ├── Analyze/
    │   │   ├── GetHistory/
    │   │   └── Feedback/
    │   ├── Profile/
    │   │   ├── GetProfile/
    │   │   ├── UpdateProfile/
    │   │   ├── UpdateNotifications/
    │   │   └── RegisterDeviceToken/
    │   ├── Payments/
    │   │   ├── GetStatus/
    │   │   └── Billing/
    │   │       ├── Checkout/
    │   │       └── Webhooks/
    │   ├── System/
    │   │   ├── GetCurrencies/
    │   │   └── Health/
    │   ├── Admin/
    │   │   ├── ListUsers/
    │   │   ├── GetStats/
    │   │   ├── GetLogs/
    │   │   ├── GetTransactions/
    │   │   └── GetFeedback/
    │   ├── EmailTemplates/
    │   │   ├── ListTemplates/
    │   │   ├── GetTemplate/
    │   │   ├── CreateTemplate/
    │   │   ├── UpdateTemplate/
    │   │   └── DeleteTemplate/
    │   ├── Providers/
    │   │   ├── ListProviders/
    │   │   ├── GetProvider/
    │   │   └── GetPricingHistory/
    │   ├── Resources/
    │   │   └── ListResources/
    │   ├── ExchangeRates/
    │   │   └── ListExchangeRates/
    │   └── Activity/
    │       └── ListActivity/
    ├── Middleware/
    ├── Persistence/           # DbContext, Migrations, Repository
    ├── Services/              # Domain servisleri, dış servis adapterları
    ├── BackgroundJobs/        # Hangfire/Quartz job tanımları
    ├── DTOs/                  # Ortak DTO'lar (isteğe bağlı)
    ├── Validators/            # Ortak validatorlar (isteğe bağlı)
    ├── Program.cs
    └── Subify.Api.csproj

---

### 📱 Flutter Dizin Yapısı

lib/
├── main.dart
├── app.dart                    # Root widget, router, theme
├── features/                   # Feature-based modüller
│   ├── auth/
│   │   ├── data/               # API, repository, model
│   │   ├── domain/             # Service, logic
│   │   ├── presentation/       # Screens, widgets, state
│   │   └── auth_provider.dart
│   ├── subscriptions/
│   │   ├── data/
│   │   ├── domain/
│   │   ├── presentation/
│   │   └── subscription_provider.dart
│   ├── reports/
│   ├── ai/
│   ├── paywall/
│   ├── notifications/
│   ├── profile/
│   ├── admin/
│   └── ... (diğer feature'lar)
├── core/                       # Ortak altyapı (network, theme, utils, error)
│   ├── api/
│   ├── config/
│   ├── constants/
│   ├── exceptions/
│   ├── theme/
│   ├── localization/
│   ├── widgets/                # Ortak UI bileşenleri
│   └── utils/
├── l10n/                       # Flutter Intl .arb dosyaları (TR/EN)
├── routes/                     # GoRouter tanımları
├── services/                   # Ortak servisler (ör: RevenueCat, FCM)
└── models/                     # Ortak modeller (isteğe bağlı)

---

### 🌐 Next.js (App Router) Dizin Yapısı

src/
├── app/                        # Route tabanlı sayfalar (App Router)
│   ├── layout.tsx
│   ├── page.tsx                # Landing page
│   ├── features/               # Public features (tanıtım, pricing vs.)
│   ├── pricing/
│   ├── login/
│   ├── register/
│   ├── forgot-password/
│   ├── reset-password/
│   ├── confirm-email/
│   ├── app/                    # Protected user area
│   │   ├── layout.tsx
│   │   ├── page.tsx            # Dashboard
│   │   ├── subscriptions/
│   │   │   ├── page.tsx
│   │   │   ├── new/
│   │   │   │   └── page.tsx
│   │   │   └── [id]/
│   │   │       └── page.tsx
│   │   ├── reports/
│   │   ├── ai/
│   │   └── settings/
│   │       ├── profile/
│   │       ├── notifications/
│   │       └── billing/
│   ├── admin/                  # Admin panel (Role: Admin)
│   │   ├── layout.tsx
│   │   ├── page.tsx
│   │   ├── users/
│   │   ├── transactions/
│   │   ├── email-templates/
│   │   │   ├── page.tsx
│   │   │   └── [id]/
│   │   │       └── page.tsx
│   │   └── logs/
├── features/                   # Feature-based modüller (UI + logic)
│   ├── auth/
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── api/
│   │   └── utils/
│   ├── subscriptions/
│   ├── reports/
│   ├── ai/
│   ├── paywall/
│   ├── notifications/
│   ├── admin/
│   └── ... (diğer feature'lar)
├── components/                 # Ortak UI bileşenleri (Button, Modal, vs.)
├── lib/                        # Ortak yardımcı fonksiyonlar, API client, config
├── hooks/                      # Ortak custom hooks
├── store/                      # Global state (isteğe bağlı, ör: Zustand)
├── styles/                     # Global ve tema stilleri
├── locales/                    # next-i18next JSON dosyaları (TR/EN)
├── types/                      # Ortak TypeScript tipleri
└── utils/                      # Ortak yardımcı fonksiyonlar

---

### 🤖 AI Prompting (Server-side)

System prompt (en/tr selectable), user prompt template with:

- Monthly total
- Subscriptions list with last_used, category, price, cycle
  Rules:
- Don’t say “iptal et”; use “dondur”, “gözden geçir”.
- Identify unused (>30gün), category duplicates.
- Respond in requested lang (profile.locale or body.lang).
- 3 kısa öneri maddesi.
  Rate limiting: user-level (e.g., 5/min) + daily quota (e.g., 20/day) for cost control.

---

### 🔔 Notifications

- Email:
  - **Auth**: Email Verification, Password Reset (Frontend URL'lerine yönlendiren linkler).
  - **Reminder**: Daily job checks `next_renewal_date <= today + days_before_renewal`; send via SMTP/Resend.
  - Push: Mobile uses FCM tokens; only premium gets push-enabled; link with RevenueCat entitlement.
  - E-posta şablonları admin paneli üzerinden düzenlenebilir.
  - Locale-aware templates (TR/EN).

---

### 🧠 Premium Gating & Paywall

- Free user sees blur + CTA on premium features (reports, AI, push).
- Paywall shows pricing (TL + USD), benefits list, CTA to RevenueCat checkout or native store paywall on mobile.
- Entitlement sync: webhook + on-demand `/api/me` refresh; cache in `entitlements_cache`.

---

### 📱 Mobile (Flutter)

- **Pages**: Auth (Login/Register/Forgot Password), Dashboard, Subscriptions (List/Add/Detail), Reports (Premium), AI Suggestions, Settings (Profile, Notifications, Language, Currency), Paywall.
- **State**: Riverpod (Code generation mode recommended).
- **Networking**: Dio with interceptors.
  - **Auth Interceptor**: Attaches `Bearer` token. Handles `401` by locking request queue (Dio `Lock` or `QueuedInterceptor`), calling `/api/auth/refresh-token`, then retrying.
- **Storage**: `flutter_secure_storage` for Tokens (Access + Refresh).
- **Push**: `firebase_messaging`. Sends FCM token to `/api/profile/device-token` on login.
- **RevenueCat**: `purchases_flutter` SDK. Shows paywall, manages subscriptions.

---

### 🖥️ Web (Next.js)

- **Structure**:
  - `/ (Public)`: Landing Page (Hero, Features, Pricing).
  - `/app (User)`: Dashboard, Subscriptions, Settings (Requires Login).
  - `/admin (Admin)`: User Management, System Stats, Logs (Requires Role='Admin').
- **Tech**: App Router, Server Components, Middleware for Auth/Role protection.
- Auth: JWT stored httpOnly cookie; refresh flow.
- i18n: next-i18next.
- Data fetching: React Query / server actions (careful with cookies).
- Paywall modal and blur states consistent with mobile.
- **Yeni: Admin Paneli**
  - **Email Templates**: Şablonları listeleme, düzenleme, silme ve test e-postası gönderme.
  - **Permissions**: Sadece `Admin` rolüne sahip kullanıcılar erişebilir.

---

### 🔐 Security & Compliance

- HTTPS everywhere (TLS via reverse proxy).
- JWT short-lived access, rotated refresh; revoke on logout.
- Input validation (FluentValidation).
- Output encoding; no PII in logs.
- Rate limiting on write + AI endpoints.
- Webhook signature validation (RevenueCat).
- Least privilege DB user; parameterized queries (EF Core).
- Backups: DB backups daily; secrets via environment variables (VPS: .env + restricted perms; consider 1Password/Key Vault later).
- CORS: allow web origin + mobile schemes.

---

### 🩺 Observability & Ops

- OpenTelemetry: ASP.NET Core instrumentation; OTLP → otel-collector.
- Logging: Serilog JSON; request logging with PII filter.
- Metrics: requests, latency, 5xx, AI call counts, RevenueCat webhook success/failure, job durations.
- Health checks: DB, cache, RevenueCat reachability (optional), SMTP.
- Alerting: basic (e.g., Uptime monitor on /health).

---

### 🧪 Testing

- Unit tests: domain/services, validators.
- Integration tests: WebApplicationFactory + Testcontainers (SQL Server container).
- Contract tests for API (OpenAPI/Swagger + Schemathesis optional).
- Mobile: widget tests for paywall gating; integration for API flows.
- E2E (later): Playwright for web happy paths.

---

### 🚀 Deployment (VPS)

- Docker Compose:
  - `reverse-proxy` (Nginx/Caddy) :80/:443 → `frontend` (Next.js) and `api`.
  - `api`: ASP.NET Core image.
  - `worker`: Hangfire server (same image, env flag).
  - `db`: MSSQL (with volume) — or external managed instance preferred.
  - `otel-collector`: optional.
- CI: build & test (dotnet test, flutter test, next lint/build); docker build; deploy via SSH/Watchtower or GitHub Actions with remote compose up.
- Migrations: `dotnet ef database update` on deploy (run once per release).
- Static assets/CDN: optional Cloudflare in front of proxy.

---

### 🔄 Background Jobs

- Hangfire/Quartz running in `worker`:
  - Daily renewal reminder scan + email dispatch.
  - RevenueCat entitlement reconciliation (safety net).
  - AI log cleanup (if needed).
  - Metrics housekeeping.

---

### 📈 Reports (Premium)

- Charts: category spend (monthly/quarterly), top categories, upcoming renewals.
- For Free: blurred + CTA.

---

### 🧠 AI Öneri Sistemi

- Trigger: user taps “AI önerisi al” (premium); auto monthly digest (optional later).
- Data: server-side fetched subscriptions; monthly total; last_used; categories.
- Response shape: { summary, tips[], estimated_savings }.
- Logging: redact PII where possible; store prompts/responses in `ai_suggestions_logs`.

---

### 🏦 Payments with RevenueCat

- Products:
  - Web (Stripe via RevenueCat): `premium_monthly_tr`, `premium_yearly_tr`, `premium_monthly_usd`, `premium_yearly_usd`.
  - iOS/Android store products mapped to same entitlements.
- Flow Web:
  - `/api/billing/checkout` → RevenueCat hosted checkout (Stripe) → success → webhook → upgrade plan.
- Flow Mobile:
  - Flutter paywall via RevenueCat SDK → purchase → RevenueCat sends webhook → backend updates entitlements_cache + profiles.plan.
- Downgrade/expire: webhook sets plan to free after grace.

---

### 🧭 UX Copy Highlights (TR)

- Paywall Başlık: “Daha Akıllı Abonelik Yönetimi için Premium’a Geç”
- Benefits: Sınırsız abonelik, Detaylı rapor, AI önerileri 🤖, Push bildirim, Öncelikli deneyim.
- CTA: “Premium’a Geç – 49 TL / ay”
- Free limit uyarı: “Free planda en fazla 3 abonelik ekleyebilirsin. Daha fazlası için Premium’a geç.”

(EN metinleri aynı yapıda, fiyat USD.)

---

### 🗂 Pricing Comparison Table (TR)

| Özellik                  | Free     | Premium                  |
| ------------------------ | -------- | ------------------------ |
| Abonelik sayısı          | 3        | Sınırsız                 |
| Kategori raporları       | ❌       | ✔️                       |
| AI önerileri             | ❌       | ✔️ 🤖                    |
| Mobil push               | ❌       | ✔️                       |
| E-posta uyarıları        | ✔️       | ✔️ (gelişmiş)            |
| Aylık/Yıllık grafikleri  | Sınırlı  | Tam                      |
| Yaklaşan ödeme uyarıları | ✔️       | ✔️ (email+push)          |
| Destek                   | Temel    | Öncelikli                |
| Fiyat                    | Ücretsiz | 49 TL/ay veya 499 TL/yıl |

(EN tablosu eşdeğer, fiyat $4.99/mo, $49.99/yr.)

---

### 📌 Risk & Mitigations

- **Payment sync hatası**: RevenueCat webhook + periodic reconciliation job.
- **VPS tekil arıza**: Düzenli yedek, otomatik yeniden başlatma, izleme. (Gelecekte managed DB + multi-AZ.)
- **AI maliyet artışı**: Rate limit + daily quota; model seçimi (gpt-4o-mini/3.5).
- **DB ölçek**: MSSQL indexing; future read replicas (if managed), caching.
- Admin panelinde yanlışlıkla şablonların silinmesi.
  - Silme işlemi için onay modalı ve geri alma mekanizması (soft delete).

---

### ✅ Definition of Done (MVP)

- Web: Auth, subscriptions CRUD, dashboard totals, email reminders, paywall, RevenueCat web checkout, entitlement-based gating.
- Backend: All APIs above, JWT, logging, health checks, rate limiting, EF Core migrations, RevenueCat webhook.
- AI: Prompted suggestions returned and shown in UI (premium).
- Mobile: Auth, list, dashboard, paywall via RevenueCat, entitlement-aware feature gating, push token capture.
- Infra: VPS with TLS, reverse proxy, Docker Compose, migrations applied, basic monitoring/health in place.
