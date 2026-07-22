# Subify OS — Detaylı Geliştirme Task Listesi

| Alan | Değer |
| ---- | ----- |
| **Sürüm** | 1.0 |
| **Durum** | Aktif — uygulama sırası |
| **Son güncelleme** | 2026-03-22 |
| **Kaynak** | [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md), [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md) |
| **Kullanım** | Grok’a görev verirken **task numarasını** yaz (ör. `3.2.4` veya `T-3.2.4`) |

---

## Nasıl kullanılır?

1. Aşağıdaki numaralandırma **hiyerarşiktir**: `Faz.Bölüm.Task` (ör. `3.1.2`).
2. Alt adımlar gerekiyorsa `3.1.2.a`, `3.1.2.b` kullanılır.
3. Durum işaretleri:
   - `[ ]` Yapılmadı
   - `[~]` Kısmen yapıldı / iskelet var
   - `[x]` Tamamlandı
4. **Öncelik:** P0 (bloklayıcı) → P1 → P2 → P3 (sonra / opsiyonel).
5. **Bağımlılık:** Bir task’ın “Bağımlı” satırı varsa önce onlar bitmeli.
6. Eski SaaS task listeleri (`SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md`) **geçersizdir**; bu dosya geçerlidir.

---

## İlerleme özeti (yüksek seviye)

| Faz | Konu | Genel durum |
| --- | ---- | ----------- |
| 0 | Repo, dokümantasyon, temiz başlangıç | [x] 0.1 + 0.2 tamam |
| 1 | Core setup (solution, tooling, Scalar) | [~] |
| 2 | Domain, EF, Postgres, seed altyapısı | [~] |
| 3 | Auth, roller, SuperAdmin, multi-user | [~] |
| 4 | Subscription + finansal motor | [ ] |
| 5 | Categories, providers, profile, activity | [ ] |
| 6 | Reports, FX, resources/i18n | [ ] |
| 7 | Admin panel API (users, settings, invites) | [ ] |
| 8 | SMTP, e-posta şablonları, background jobs | [ ] |
| 9 | AI (BYOK) | [ ] |
| 10 | Web (Next.js) UI | [ ] |
| 11 | Docker, release, ops | [ ] |
| 12 | Testler | [ ] |
| 13 | Flutter (Faz 7 — en son) | [ ] |
| 14 | Dokümantasyon & polish | [ ] |

---

# FAZ 0 — Dokümantasyon hiyerarşisi ve repo hijyeni

### 0.1 Doküman önceliği

- [x] **0.1.1** README’yi Subify OS odaklı yeniden yaz  
  **Açıklama:** Self-hosted, open source, freemium/RevenueCat yok; `docker compose` vaadi ve linkler (Manifesto, PRD, bu task list).  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22

- [x] **0.1.2** Eski SaaS PRD’ye LEGACY banner ekle  
  **Açıklama:** `Subify.Web.Uygulamasi.v2.PRD.md` en üste “uygulama için kullanma; OS PRD geçerli” uyarısı.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

- [x] **0.1.3** Eski task listesine LEGACY banner ekle  
  **Açıklama:** `SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md` için aynı uyarı.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

- [x] **0.1.4** DATA_MODEL / API_CONTRACTS / ERROR_CODES OS notu  
  **Açıklama:** Billing, plan, premium limit bölümlerine “OS’ta yok” notu veya ayrı OS patch notları.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

- [x] **0.1.5** LICENSE dosyası ekle  
  **Açıklama:** MIT (veya seçilen OSS lisansı) kök dizine.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22

### 0.2 Repo hijyeni

- [x] **0.2.1** Kök `.gitignore` gözden geçir  
  **Açıklama:** `bin/`, `obj/`, `.env`, `node_modules/`, IDE, user secrets, log dosyaları.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** Eski dosyada `*.sln` ve `*.launchSettings.json` yanlışlıkla ignore ediliyordu (kaldırıldı). `!.env.example`, logs, TestResults, Flutter/Docker data eklendi.

- [x] **0.2.2** API’de kullanılmayan kalıntıları temizle  
  **Açıklama:** `WeatherForecast`, boş Controllers klasörü, örnek dosyalar, çakışan legacy HTTP testleri.  
  **Öncelik:** P1 · **Tamamlandı:** 2026-03-22  
  **Not:** `LoginRequest` (kullanılmıyordu), boş `Repositories`/`RequestEntities` klasörleri, `bin\Debug` artifact kaldırıldı; `.http` host `5240` yapıldı.

- [x] **0.2.3** `Subify.Api.http` dosyasını güncelle  
  **Açıklama:** Scalar ile uyumlu login/register örnek istekleri.  
  **Öncelik:** P2 · **Tamamlandı:** 2026-03-22  
  **Not:** host `5240`, Scalar/OpenAPI GET’leri, register/login (+ duplicate/invalid), named requests, token variable zinciri.

---

# FAZ 1 — Core setup ve API host sertleştirme

### 1.1 Solution ve katmanlar

- [x] **1.1.1** Clean Architecture solution iskeleti  
  **Açıklama:** Domain, Application, Infrastructure, Api projeleri ve referanslar.  
  **Durum:** Mevcut.

- [x] **1.1.2** MediatR + DI registration  
  **Açıklama:** `AddApplicationServices` / `AddInfrastructureServices`.  
  **Durum:** Mevcut (pipeline behavior eksik olabilir → 1.2.x).

- [x] **1.1.3** Result / Error / DomainErrors altyapısı  
  **Açıklama:** `Result<T>`, `Error`, error kodları.  
  **Durum:** Mevcut; OS’a göre kod temizliği 1.2.4.

- [x] **1.1.4** Minimal API endpoint discovery  
  **Açıklama:** `IEndpoint`, `AddEndpoints`, `MapEndpoints`.  
  **Durum:** Mevcut.

- [x] **1.1.5** Scalar / OpenAPI UI  
  **Açıklama:** Development’ta `/scalar/v1`, `/openapi/v1.json`, root redirect.  
  **Durum:** Mevcut.

### 1.2 Cross-cutting API pipeline

- [ ] **1.2.1** FluentValidation MediatR pipeline behavior  
  **Açıklama:** Tüm `IRequest` öncesi validator çalışsın; hatalar `VAL_*` + ProblemDetails.  
  **Öncelik:** P0 · **Bağımlı:** —

- [ ] **1.2.2** Validation exception → ProblemDetails middleware/map  
  **Açıklama:** Pipeline failure’ların HTTP 400 ile RFC 7807 dönmesi.  
  **Öncelik:** P0 · **Bağımlı:** 1.2.1

- [ ] **1.2.3** Global exception handler  
  **Açıklama:** Beklenmeyen exception → `SYS_001`, traceId; development’ta detay opsiyonel.  
  **Öncelik:** P0

- [ ] **1.2.4** DomainErrors OS temizliği  
  **Açıklama:** Premium/limit kodlarını kaldır veya yeniden adlandır (`AI_KEY_MISSING` vb.); `SUBS_001` limit kalksın.  
  **Öncelik:** P0

- [ ] **1.2.5** CORS policy  
  **Açıklama:** Web origin (`localhost:3000` + env); production’da bilinen origin.  
  **Öncelik:** P1

- [ ] **1.2.6** Rate limiting (login/register/forgot/AI)  
  **Açıklama:** ASP.NET rate limiter; brute-force ve AI abuse koruması (plan limiti değil).  
  **Öncelik:** P1

- [ ] **1.2.7** `GET /health` (liveness)  
  **Açıklama:** Basit 200 OK; container healthcheck için.  
  **Öncelik:** P0

- [ ] **1.2.8** `GET /health/ready` (readiness)  
  **Açıklama:** Postgres bağlantı kontrolü.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.x

- [ ] **1.2.9** OpenAPI JWT Bearer security scheme  
  **Açıklama:** Scalar’da Authorize ile Bearer token girebilme.  
  **Öncelik:** P1

- [ ] **1.2.10** ProblemDetails status code map doğrulama  
  **Açıklama:** `ResultExtensions` tüm `ErrorType` için doğru HTTP kodu.  
  **Öncelik:** P1

- [ ] **1.2.11** Request logging / Serilog temel kurulum  
  **Açıklama:** Console + structured log; secret loglanmaz.  
  **Öncelik:** P2

- [ ] **1.2.12** `ICurrentUserService`  
  **Açıklama:** JWT’den `UserId`, email, roller; handler’larda tekrar parse yok.  
  **Öncelik:** P0 · **Bağımlı:** 3.1.x

---

# FAZ 2 — Domain, EF Core, PostgreSQL, seed

### 2.1 Domain model düzeltmeleri

- [ ] **2.1.1** `ApplicationUser.Locate` → `Locale` rename  
  **Açıklama:** Property, migration, TokenService claim, tüm referanslar.  
  **Öncelik:** P0

- [ ] **2.1.2** ApplicationUser profil alanlarını PRD ile hizala  
  **Açıklama:** FullName, Locale, MainCurrency, MonthlyBudget, ApplicationThemeColor, DarkTheme, audit. Plan alanı **eklenmeyecek**.  
  **Öncelik:** P0

- [ ] **2.1.3** Subscription domain metodları güçlendir  
  **Açıklama:** Factory/create kuralları, `UserShare` computed, archive/reactivate, Category XOR UserCategory invariant.  
  **Öncelik:** P0

- [ ] **2.1.4** Provider `Logout` → `LogoUrl` (veya doğru alan adı)  
  **Açıklama:** Typo/isim düzeltmesi + migration.  
  **Öncelik:** P1

- [ ] **2.1.5** SystemSettings singleton modeli netleştir  
  **Açıklama:** Tek satır instance settings; update metodları; secret alanlar.  
  **Öncelik:** P1

- [ ] **2.1.6** RefreshToken entity rotation alanları  
  **Açıklama:** RevokedAt, ReplacedByToken, ReasonRevoked, IsActive helper.  
  **Öncelik:** P0 · **Durum notu:** Kısmen var; gözden geçir.

- [ ] **2.1.7** Invite token entity (yeni)  
  **Açıklama:** `UserInvite`: token hash, email, expires, createdBy, usedAt.  
  **Öncelik:** P1

- [ ] **2.1.8** Device token entity (opsiyonel / sonra)  
  **Açıklama:** Push için; Flutter fazına kadar ertele.  
  **Öncelik:** P3

- [ ] **2.1.9** Soft delete global query filter stratejisi  
  **Açıklama:** `ISoftDeletable` için EF filter (opsiyonel ama tutarlı).  
  **Öncelik:** P2

- [ ] **2.1.10** BaseEntity Id generation politikası  
  **Açıklama:** Postgres uyumlu UUID (v4/v7 veya `gen_random_uuid()`); dokümante et.  
  **Öncelik:** P1

### 2.2 EF Core configurations

- [ ] **2.2.1** `IEntityTypeConfiguration<>` klasör yapısı  
  **Açıklama:** Infrastructure/Persistence/Configurations.  
  **Öncelik:** P0

- [ ] **2.2.2** Subscription configuration  
  **Açıklama:** Index `(UserId, Archived, NextRenewalDate)`, FK, precision decimal, check constraints mümkünse.  
  **Öncelik:** P0

- [ ] **2.2.3** Category / UserCategory / Provider configuration  
  **Açıklama:** Unique slug, indexes, soft delete.  
  **Öncelik:** P0

- [ ] **2.2.4** Resource unique index  
  **Açıklama:** `(PageName, Name, LanguageCode)` unique.  
  **Öncelik:** P1

- [ ] **2.2.5** RefreshToken configuration  
  **Açıklama:** Index user+token hash; uzunluk limitleri.  
  **Öncelik:** P0

- [ ] **2.2.6** ActivityLog / AiSuggestionLog configuration  
  **Açıklama:** Index `(UserId, CreatedAt DESC)`.  
  **Öncelik:** P1

- [ ] **2.2.7** EmailTemplates unique (Name, LanguageCode)  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **2.2.8** ExchangeRateSnapshot index  
  **Açıklama:** `(Base, Target, FetchedAt DESC)`.  
  **Öncelik:** P1

- [ ] **2.2.9** ApplicationUser / Identity table naming  
  **Açıklama:** Postgres naming convention (snake_case opsiyonel); tutarlılık.  
  **Öncelik:** P2

- [ ] **2.2.10** SystemSettings configuration  
  **Açıklama:** Tek kayıt garantisi dokümantasyonu (app-level).  
  **Öncelik:** P1

### 2.3 DbContext, migrate, seed runtime

- [~] **2.3.1** SubifyDbContext DbSet’ler  
  **Açıklama:** Tüm OS entity’ler; billing yok.  
  **Durum:** Büyük ölçüde mevcut.

- [ ] **2.3.2** Startup auto-migrate  
  **Açıklama:** API ayağa kalkarken `Database.Migrate()` + retry Postgres ready.  
  **Öncelik:** P0

- [ ] **2.3.3** `IDataSeeder` / `DbInitializer` arayüzü  
  **Açıklama:** Idempotent seed pipeline.  
  **Öncelik:** P0 · **Bağımlı:** 2.3.2

- [ ] **2.3.4** Role seed  
  **Açıklama:** `SuperAdmin`, `Admin`, `User` Identity rolleri.  
  **Öncelik:** P0

- [ ] **2.3.5** Category seed (10 sistem kategorisi)  
  **Açıklama:** streaming, music, productivity, gaming, shopping, utilities, education, health, cloud, other.  
  **Öncelik:** P0

- [ ] **2.3.6** Provider seed (başlangıç listesi)  
  **Açıklama:** Netflix, Spotify vb. TR/global; LogoUrl opsiyonel.  
  **Öncelik:** P1

- [ ] **2.3.7** Resource seed (TR/EN temel metinler)  
  **Açıklama:** Common, Category, Dashboard, Subscription, Error (paywall metinleri yok).  
  **Öncelik:** P1

- [ ] **2.3.8** Email template seed  
  **Açıklama:** VerifyEmail, ResetPassword, RenewalReminder TR/EN.  
  **Öncelik:** P1

- [ ] **2.3.9** SystemSettings initial empty row  
  **Açıklama:** Singleton boş kayıt oluştur.  
  **Öncelik:** P1

- [ ] **2.3.10** Seed sadece boş tabloya  
  **Açıklama:** Idempotent; ikinci start duplicate üretmesin.  
  **Öncelik:** P0

- [ ] **2.3.11** Development connection string / docker-compose hizası  
  **Açıklama:** appsettings ile `docker/docker-compose.yaml` kullanıcı/şifre/db aynı.  
  **Öncelik:** P0

- [ ] **2.3.12** Migration baseline gözden geçir  
  **Açıklama:** Rename/alan değişikliklerinden sonra yeni migration; gerekirse squash dokümantasyonu.  
  **Öncelik:** P0 · **Bağımlı:** 2.1.x, 2.2.x

### 2.4 ISubifyDbContext genişletme

- [ ] **2.4.1** DbSet’leri interface’e taşı (gerekli olanlar)  
  **Açıklama:** Handler’lar concrete context’e bağımlı olmasın.  
  **Öncelik:** P1

- [ ] **2.4.2** Unit of Work / SaveChanges tek giriş  
  **Açıklama:** Handler sonunda tutarlı save.  
  **Öncelik:** P1

---

# FAZ 3 — Auth, JWT, SuperAdmin, multi-user temel

### 3.1 JWT ve token servisi

- [~] **3.1.1** Access token üretimi  
  **Açıklama:** Sub, email, jti, roles, locale claims.  
  **Durum:** Mevcut; claim isimleri gözden geçir.

- [~] **3.1.2** Refresh token üretimi + hash saklama  
  **Açıklama:** SHA256 hash DB; plain sadece response.  
  **Durum:** Mevcut.

- [ ] **3.1.3** Refresh token rotation implementasyonu  
  **Açıklama:** Eski revoke + yeni token; reuse detection (`theft_detected`).  
  **Öncelik:** P0

- [ ] **3.1.4** Token expiry config  
  **Açıklama:** Access (ör. 15–60 dk) ve refresh (ör. 7 gün) appsettings.  
  **Öncelik:** P0

- [ ] **3.1.5** JWT validation clock skew  
  **Açıklama:** TokenValidationParameters.  
  **Öncelik:** P2

### 3.2 Auth endpoint’leri

- [~] **3.2.1** `POST /api/auth/register`  
  **Açıklama:** FullName, Email, Password; validation; duplicate email 409.  
  **Durum:** Mevcut handler; SuperAdmin yok (3.3.x); e-posta yok.

- [~] **3.2.2** `POST /api/auth/login`  
  **Açıklama:** Email/password; tokens; lockout.  
  **Durum:** Mevcut; EmailConfirmed politikası 3.2.8.

- [ ] **3.2.3** `POST /api/auth/refresh-token`  
  **Açıklama:** Body refreshToken → yeni access+refresh.  
  **Öncelik:** P0 · **Bağımlı:** 3.1.3

- [ ] **3.2.4** `POST /api/auth/logout`  
  **Açıklama:** Refresh revoke; reason `logout`.  
  **Öncelik:** P0

- [ ] **3.2.5** `GET /api/auth/confirm-email`  
  **Açıklama:** userId + code; Identity confirm.  
  **Öncelik:** P1 · **Bağımlı:** 8.x SMTP (veya dev bypass)

- [ ] **3.2.6** `POST /api/auth/resend-confirmation`  
  **Açıklama:** Rate limited.  
  **Öncelik:** P1

- [ ] **3.2.7** `POST /api/auth/forgot-password`  
  **Açıklama:** Email varsa reset mail (enumeration-safe response).  
  **Öncelik:** P1 · **Bağımlı:** 8.x

- [ ] **3.2.8** `POST /api/auth/reset-password`  
  **Açıklama:** Email + code + newPassword.  
  **Öncelik:** P1

- [ ] **3.2.9** EmailConfirmed politikası (self-host)  
  **Açıklama:** SMTP yokken: register sonrası login mümkün (EmailConfirmed=true) **veya** env `REQUIRE_EMAIL_CONFIRMATION=false` default. SMTP sonra enforce opsiyonu.  
  **Öncelik:** P0

- [ ] **3.2.10** Login response’a user özeti ekle  
  **Açıklama:** id, email, fullName, locale, roles (plan yok).  
  **Öncelik:** P0

- [ ] **3.2.11** Register sonrası otomatik NotificationSettings satırı  
  **Açıklama:** defaults: email on, days_before=3.  
  **Öncelik:** P1

- [ ] **3.2.12** Auth endpoint OpenAPI örnekleri / Produces düzelt  
  **Açıklama:** Status kodları doğru.  
  **Öncelik:** P2

- [ ] **3.2.13** Public registration kapatma flag  
  **Açıklama:** `ALLOW_PUBLIC_REGISTRATION` env; false iken sadece invite/admin create. İlk SuperAdmin istisnası.  
  **Öncelik:** P1 · **Bağımlı:** 3.3.1

### 3.3 SuperAdmin bootstrap ve roller

- [ ] **3.3.1** İlk kullanıcı = SuperAdmin  
  **Açıklama:** Transaction + “herhangi SuperAdmin var mı?”; race-safe.  
  **Öncelik:** P0 · **Bağımlı:** 2.3.4

- [ ] **3.3.2** Sonraki public register = User rolü  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 3.3.1

- [ ] **3.3.3** Authorization policies  
  **Açıklama:** `RequireSuperAdmin`, `RequireAdminOrAbove`, `RequireAuthenticatedUser`.  
  **Öncelik:** P0

- [ ] **3.3.4** `[Authorize]` / `.RequireAuthorization()` endpoint’lerde  
  **Açıklama:** Auth public; diğerleri protected.  
  **Öncelik:** P0 · **Bağımlı:** 3.3.3

- [ ] **3.3.5** SuperAdmin transfer (opsiyonel)  
  **Açıklama:** v1 dışı bırakılabilir; dokümante et.  
  **Öncelik:** P3

### 3.4 Identity güvenlik ayarları

- [ ] **3.4.1** Password policy  
  **Açıklama:** Min 8, upper/lower/digit (mevcut); dokümante.  
  **Öncelik:** P1

- [ ] **3.4.2** Lockout ayarları  
  **Açıklama:** Max failed attempts, lockout süresi; AUTH_005.  
  **Öncelik:** P1

- [ ] **3.4.3** Unique email enforce  
  **Açıklama:** Identity + DB.  
  **Öncelik:** P0 · **Durum:** options mevcut; test et.

---

# FAZ 4 — Subscription CRUD ve finansal motor

### 4.1 Application layer — Subscription features

- [ ] **4.1.1** CreateSubscription command/handler/validator  
  **Açıklama:** Name, price>0, currency, cycle, share≥1, category XOR, provider optional, nextRenewal. **Limit yok.**  
  **Öncelik:** P0 · **Bağımlı:** 1.2.12, 3.3.4

- [ ] **4.1.2** Create sonrası ActivityLog  
  **Açıklama:** `subscription.created`.  
  **Öncelik:** P1

- [ ] **4.1.3** GetSubscriptionById query  
  **Açıklama:** Ownership check; 404/403.  
  **Öncelik:** P0

- [ ] **4.1.4** ListSubscriptions query  
  **Açıklama:** includeArchived, category filter, pagination, search.  
  **Öncelik:** P0

- [ ] **4.1.5** List response summary  
  **Açıklama:** monthlyTotal, yearlyTotal, currency (mainCurrency).  
  **Öncelik:** P0 · **Bağımlı:** 4.3.x

- [ ] **4.1.6** UpdateSubscription command  
  **Açıklama:** Ownership; old/new values activity.  
  **Öncelik:** P0

- [ ] **4.1.7** ArchiveSubscription (DELETE soft)  
  **Açıklama:** Archived=true; activity archived.  
  **Öncelik:** P0

- [ ] **4.1.8** ReactivateSubscription (opsiyonel endpoint)  
  **Açıklama:** Archive geri alma.  
  **Öncelik:** P2

- [ ] **4.1.9** UpcomingSubscriptions query  
  **Açıklama:** `days` query; daysUntilRenewal; overdue ayrı işaret.  
  **Öncelik:** P0

- [ ] **4.1.10** DTO’lar (SubscriptionResponse vb.)  
  **Açıklama:** userShare, category, provider nested.  
  **Öncelik:** P0

- [ ] **4.1.11** Provider aktif değilse create reject  
  **Açıklama:** SUB provider not active.  
  **Öncelik:** P1

- [ ] **4.1.12** Category / UserCategory varlık ve ownership doğrulama  
  **Açıklama:** UserCategory başka kullanıcıya ait olamaz.  
  **Öncelik:** P0

### 4.2 API endpoints — Subscriptions

- [ ] **4.2.1** `GET /api/subscriptions`  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **4.2.2** `GET /api/subscriptions/{id}`  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **4.2.3** `POST /api/subscriptions`  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **4.2.4** `PUT /api/subscriptions/{id}`  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **4.2.5** `DELETE /api/subscriptions/{id}`  
  **Açıklama:** Soft archive.  
  **Öncelik:** P0

- [ ] **4.2.6** `GET /api/subscriptions/upcoming`  
  **Açıklama:**  
  **Öncelik:** P0

### 4.3 Finansal hesaplama

- [ ] **4.3.1** UserShare pure function / domain property  
  **Açıklama:** `Price / SharedWithCount`.  
  **Öncelik:** P0

- [ ] **4.3.2** MonthlyEquivalent / YearlyEquivalent  
  **Açıklama:** monthly as-is; yearly/12 ve tersi.  
  **Öncelik:** P0

- [ ] **4.3.3** DashboardTotals service  
  **Açıklama:** Aktif non-archived toplamları.  
  **Öncelik:** P0

- [ ] **4.3.4** Multi-currency convert (basit)  
  **Açıklama:** Snapshot rate ile mainCurrency’ye çevir; rate yoksa orijinal + warning.  
  **Öncelik:** P1 · **Bağımlı:** 6.2.x

- [ ] **4.3.5** Budget exceeded flag  
  **Açıklama:** monthlyTotal > monthlyBudget → response flag.  
  **Öncelik:** P1

- [ ] **4.3.6** Unit testler finansal motor  
  **Açıklama:** share, monthly/yearly, budget.  
  **Öncelik:** P1 · **Bağımlı:** 12.1.x

---

# FAZ 5 — Categories, providers, profile, activity

### 5.1 Categories

- [ ] **5.1.1** `GET /api/categories`  
  **Açıklama:** Sistem kategorileri; Accept-Language veya user locale ile name.  
  **Öncelik:** P0 · **Bağımlı:** 2.3.5

- [ ] **5.1.2** `GET /api/categories/user`  
  **Açıklama:** Kullanıcının özel kategorileri.  
  **Öncelik:** P0

- [ ] **5.1.3** `POST /api/categories/user`  
  **Açıklama:** name, icon, color.  
  **Öncelik:** P0

- [ ] **5.1.4** `PUT /api/categories/user/{id}`  
  **Açıklama:** Ownership.  
  **Öncelik:** P1

- [ ] **5.1.5** `DELETE /api/categories/user/{id}`  
  **Açıklama:** Aktif subscription varsa conflict.  
  **Öncelik:** P1

- [ ] **5.1.6** Category name resource lookup helper  
  **Açıklama:** slug → localized name; fallback slug.  
  **Öncelik:** P1 · **Bağımlı:** 6.3.x

### 5.2 Providers

- [ ] **5.2.1** `GET /api/providers`  
  **Açıklama:** isActive=true; search query opsiyonel.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.6

- [ ] **5.2.2** `GET /api/providers/{id}`  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **5.2.3** Admin provider CRUD (opsiyonel v1)  
  **Açıklama:** SuperAdmin manage catalog.  
  **Öncelik:** P2

### 5.3 Profile

- [ ] **5.3.1** `GET /api/profile`  
  **Açıklama:** Tercihler + email.  
  **Öncelik:** P0

- [ ] **5.3.2** `PUT /api/profile`  
  **Açıklama:** fullName, locale, mainCurrency, budget, theme, darkTheme.  
  **Öncelik:** P0

- [ ] **5.3.3** Theme color whitelist validation  
  **Açıklama:** Preset listesi.  
  **Öncelik:** P1

- [ ] **5.3.4** Currency validation (ISO 4217 basit set)  
  **Açıklama:** TRY, USD, EUR, GBP…  
  **Öncelik:** P1

- [ ] **5.3.5** `PUT /api/profile/notifications`  
  **Açıklama:** emailEnabled, daysBeforeRenewal (push sonra).  
  **Öncelik:** P1

- [ ] **5.3.6** Profile update activity log  
  **Açıklama:**  
  **Öncelik:** P2

### 5.4 Activity

- [ ] **5.4.1** ActivityLog writer service  
  **Açıklama:** Merkezi `IActivityLogger`.  
  **Öncelik:** P1

- [ ] **5.4.2** `GET /api/activity`  
  **Açıklama:** Pagination, entityType filter; sadece kendi logları.  
  **Öncelik:** P1

- [ ] **5.4.3** Login/logout activity (opsiyonel)  
  **Açıklama:** auth entity.  
  **Öncelik:** P2

---

# FAZ 6 — Reports, döviz, resources

### 6.1 Reports

- [ ] **6.1.1** `GET /api/reports/monthly-spend`  
  **Açıklama:** Son N ay; premium yok.  
  **Öncelik:** P1 · **Bağımlı:** 4.x

- [ ] **6.1.2** `GET /api/reports/category-breakdown`  
  **Açıklama:** total, percentage, count, color.  
  **Öncelik:** P1

- [ ] **6.1.3** `GET /api/reports/currency-distribution`  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **6.1.4** Yetersiz veri empty-state response  
  **Açıklama:** Boş array + message; crash yok.  
  **Öncelik:** P1

### 6.2 Exchange rates

- [ ] **6.2.1** Exchange rate provider abstraction  
  **Açıklama:** `IExchangeRateClient` (HTTP).  
  **Öncelik:** P1

- [ ] **6.2.2** Snapshot persist  
  **Açıklama:** Background veya on-demand fetch → DB.  
  **Öncelik:** P1

- [ ] **6.2.3** `GET /api/exchange-rates?base=`  
  **Açıklama:** Son snapshot / cache.  
  **Öncelik:** P1

- [ ] **6.2.4** Background sync job (saatlik)  
  **Açıklama:** HostedService; API key env.  
  **Öncelik:** P2 · **Bağımlı:** 8.4 veya 11.x

- [ ] **6.2.5** Fallback last-known rate  
  **Açıklama:** API down.  
  **Öncelik:** P1

### 6.3 Resources / i18n API

- [ ] **6.3.1** `GET /api/resources?lang=&since=`  
  **Açıklama:** Delta sync.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.7

- [ ] **6.3.2** Resource cache (memory)  
  **Açıklama:** Redis zorunlu değil; IMemoryCache.  
  **Öncelik:** P2

- [ ] **6.3.3** Admin resource CRUD (opsiyonel)  
  **Açıklama:**  
  **Öncelik:** P3

---

# FAZ 7 — Admin: users, invites, SystemSettings API

### 7.1 Users admin

- [ ] **7.1.1** `GET /api/admin/users`  
  **Açıklama:** Sayfalı liste, arama; SuperAdmin/Admin.  
  **Öncelik:** P0 · **Bağımlı:** 3.3.3

- [ ] **7.1.2** `POST /api/admin/users`  
  **Açıklama:** Manuel kullanıcı oluştur (email, temp password veya force change).  
  **Öncelik:** P0

- [ ] **7.1.3** `PATCH /api/admin/users/{id}`  
  **Açıklama:** Lock/unlock, rol Admin/User (SuperAdmin korunur).  
  **Öncelik:** P1

- [ ] **7.1.4** Admin başka kullanıcının subscription’ını **görmez** (v1)  
  **Açıklama:** Explicit non-goal enforce; test.  
  **Öncelik:** P0

- [ ] **7.1.5** Soft disable user  
  **Açıklama:** Login engeli.  
  **Öncelik:** P1

### 7.2 Invites

- [ ] **7.2.1** `POST /api/admin/invites`  
  **Açıklama:** Email + expiry; token üret.  
  **Öncelik:** P1 · **Bağımlı:** 2.1.7

- [ ] **7.2.2** `GET /api/admin/invites`  
  **Açıklama:** Pending list.  
  **Öncelik:** P2

- [ ] **7.2.3** `POST /api/auth/accept-invite`  
  **Açıklama:** Token + password + fullName → User.  
  **Öncelik:** P1

- [ ] **7.2.4** Invite e-posta gönderimi  
  **Açıklama:** SMTP varsa mail; yoksa admin’e raw link response.  
  **Öncelik:** P1 · **Bağımlı:** 8.x

- [ ] **7.2.5** Invite single-use + expiry enforce  
  **Açıklama:**  
  **Öncelik:** P1

### 7.3 SystemSettings API

- [ ] **7.3.1** `GET /api/admin/settings`  
  **Açıklama:** Secret maskeli (smtp password, AI key).  
  **Öncelik:** P0 · **Bağımlı:** 2.1.5, 3.3.3

- [ ] **7.3.2** `PUT /api/admin/settings`  
  **Açıklama:** SMTP + AI key partial update (boş = değiştirme).  
  **Öncelik:** P0

- [ ] **7.3.3** `POST /api/admin/settings/test-smtp`  
  **Açıklama:** Test mail SuperAdmin adresine.  
  **Öncelik:** P1 · **Bağımlı:** 8.1

- [ ] **7.3.4** `POST /api/admin/settings/test-ai`  
  **Açıklama:** Minimal model ping.  
  **Öncelik:** P2 · **Bağımlı:** 9.x

- [ ] **7.3.5** Settings change audit log  
  **Açıklama:** Secret değer loglanmaz.  
  **Öncelik:** P2

### 7.4 Email templates admin (P2)

- [ ] **7.4.1** List/get/update email templates  
  **Açıklama:** SuperAdmin.  
  **Öncelik:** P2

- [ ] **7.4.2** Template preview / test send  
  **Açıklama:**  
  **Öncelik:** P2

---

# FAZ 8 — SMTP, e-posta, background jobs

### 8.1 E-posta altyapısı

- [ ] **8.1.1** `IEmailSender` abstraction  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **8.1.2** SmtpEmailSender (SystemSettings)  
  **Açıklama:** Runtime settings; factory/refresh.  
  **Öncelik:** P1 · **Bağımlı:** 7.3.2

- [ ] **8.1.3** Null/Noop email sender  
  **Açıklama:** SMTP yokken log + safe no-op.  
  **Öncelik:** P1

- [ ] **8.1.4** Template renderer  
  **Açıklama:** `{{FullName}}` placeholder replace.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.8

- [ ] **8.1.5** Locale’e göre template seçimi  
  **Açıklama:**  
  **Öncelik:** P1

### 8.2 Auth e-postaları

- [ ] **8.2.1** Verify email mail  
  **Açıklama:**  
  **Öncelik:** P1 · **Bağımlı:** 3.2.5, 8.1

- [ ] **8.2.2** Reset password mail  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **8.2.3** Invite mail  
  **Açıklama:**  
  **Öncelik:** P1

### 8.3 Yenileme hatırlatma

- [ ] **8.3.1** Renewal reminder job  
  **Açıklama:** Günlük; days_before_renewal; email_enabled.  
  **Öncelik:** P1 · **Bağımlı:** 8.1, 4.x

- [ ] **8.3.2** Duplicate send koruması  
  **Açıklama:** Aynı gün aynı subscription için tekrar mail yok (log/flag).  
  **Öncelik:** P1

- [ ] **8.3.3** Job disabled when SMTP empty  
  **Açıklama:** Warning log.  
  **Öncelik:** P1

### 8.4 Background host

- [ ] **8.4.1** HostedService vs Hangfire kararı implement  
  **Açıklama:** v1 için `BackgroundService` yeterli önerilir; dokümante.  
  **Öncelik:** P1

- [ ] **8.4.2** Job schedule configuration  
  **Açıklama:** Cron benzeri env (ör. daily 08:00).  
  **Öncelik:** P2

- [ ] **8.4.3** Job hata izolasyonu  
  **Açıklama:** Bir user fail tüm job’u öldürmesin.  
  **Öncelik:** P1

---

# FAZ 9 — AI (BYOK)

### 9.1 AI altyapı

- [ ] **9.1.1** `IAiClient` OpenAI-compatible  
  **Açıklama:** Chat completions HTTP.  
  **Öncelik:** P2 · **Bağımlı:** 7.3.2

- [ ] **9.1.2** Key SystemSettings’ten resolve  
  **Açıklama:** Yoksa `AI_KEY_MISSING` anlamlı hata.  
  **Öncelik:** P2

- [ ] **9.1.3** Prompt builder (server-side)  
  **Açıklama:** Kullanıcı abonelik özeti; PII minimize.  
  **Öncelik:** P2

- [ ] **9.1.4** Response parse → tips DTO  
  **Açıklama:** unused, duplicate, yearly, general + savings.  
  **Öncelik:** P2

### 9.2 AI endpoints

- [ ] **9.2.1** `POST /api/ai/analyze`  
  **Açıklama:** Auth user; rate limit; log request/response.  
  **Öncelik:** P2

- [ ] **9.2.2** `GET /api/ai/history`  
  **Açıklama:** Pagination.  
  **Öncelik:** P2

- [ ] **9.2.3** Insufficient data (<1 subscription)  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **9.2.4** AI rate limit (5/min, 20/day öneri)  
  **Açıklama:** Stabilite; plan değil.  
  **Öncelik:** P2

- [ ] **9.2.5** AiSuggestionLog persist  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **9.2.6** Activity log ai_suggestion  
  **Açıklama:**  
  **Öncelik:** P2

---

# FAZ 10 — Web (Next.js)

### 10.1 Web foundation

- [~] **10.1.1** Next.js App Router + TS + Tailwind iskelet  
  **Açıklama:** Mevcut scaffold.  
  **Durum:** Scaffold var.

- [ ] **10.1.2** Design tokens (manifesto colors)  
  **Açıklama:** CSS variables + Tailwind theme light/dark.  
  **Öncelik:** P1 · **Bağımlı:** —

- [ ] **10.1.3** Dark mode (`class` strategy)  
  **Açıklama:** system + user preference.  
  **Öncelik:** P1

- [ ] **10.1.4** Inter font  
  **Açıklama:** next/font.  
  **Öncelik:** P1

- [ ] **10.1.5** API client (fetch/axios) + base URL env  
  **Açıklama:** `NEXT_PUBLIC_API_URL`.  
  **Öncelik:** P0

- [ ] **10.1.6** Auth token storage stratejisi  
  **Açıklama:** httpOnly cookie (BFF) **veya** memory+refresh; XSS notları. Self-host için pratik seçim dokümante.  
  **Öncelik:** P0

- [ ] **10.1.7** Auth context / session provider  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **10.1.8** Protected route middleware/layout  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **10.1.9** shadcn/ui veya temel component set  
  **Açıklama:** Button, Input, Card, Dialog, Toast.  
  **Öncelik:** P1

- [ ] **10.1.10** i18n (TR/EN) web  
  **Açıklama:** next-intl veya benzeri.  
  **Öncelik:** P1

- [ ] **10.1.11** Error toast / ProblemDetails handler  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.1.12** Loading ve empty states  
  **Açıklama:**  
  **Öncelik:** P1

### 10.2 Auth sayfaları

- [ ] **10.2.1** Login sayfası  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 3.2.2, 10.1.5

- [ ] **10.2.2** Register sayfası (ilk kurulum + public)  
  **Açıklama:** İlk kullanıcı SuperAdmin bilgilendirme metni.  
  **Öncelik:** P0

- [ ] **10.2.3** Forgot / reset password sayfaları  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.2.4** Accept invite sayfası  
  **Açıklama:**  
  **Öncelik:** P1 · **Bağımlı:** 7.2.3

- [ ] **10.2.5** Logout  
  **Açıklama:**  
  **Öncelik:** P0

### 10.3 App shell

- [ ] **10.3.1** App layout (sidebar/topnav)  
  **Açıklama:** Dashboard, Subscriptions, Reports, AI, Profile, Admin.  
  **Öncelik:** P0

- [ ] **10.3.2** Responsive mobile nav  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.3.3** Theme toggle  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.3.4** User menu (email, logout)  
  **Açıklama:**  
  **Öncelik:** P0

### 10.4 Dashboard UI

- [ ] **10.4.1** Summary cards (monthly/yearly)  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 4.2.1

- [ ] **10.4.2** Budget progress bar  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.4.3** Upcoming payments list  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **10.4.4** Recent activity list  
  **Açıklama:**  
  **Öncelik:** P1 · **Bağımlı:** 5.4.2

- [ ] **10.4.5** Budget exceeded warning UI  
  **Açıklama:**  
  **Öncelik:** P1

### 10.5 Subscriptions UI

- [ ] **10.5.1** Subscription list/grid  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **10.5.2** Card states: Yakında / Gecikmiş / Normal  
  **Açıklama:** Manifesto border + badge + dark amber glow.  
  **Öncelik:** P0

- [ ] **10.5.3** Create subscription form/modal  
  **Açıklama:** Provider autocomplete, category, share, dates.  
  **Öncelik:** P0

- [ ] **10.5.4** Edit subscription  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **10.5.5** Archive confirmation  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **10.5.6** Filters (category, archived, search)  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.5.7** UserShare display  
  **Açıklama:** “Sizin payınız”.  
  **Öncelik:** P0

### 10.6 Reports UI

- [ ] **10.6.1** Category breakdown chart  
  **Açıklama:**  
  **Öncelik:** P1 · **Bağımlı:** 6.1.2

- [ ] **10.6.2** Monthly spend chart  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.6.3** Empty/error states  
  **Açıklama:**  
  **Öncelik:** P1

### 10.7 AI UI

- [ ] **10.7.1** Analyze CTA + loading  
  **Açıklama:**  
  **Öncelik:** P2 · **Bağımlı:** 9.2.1

- [ ] **10.7.2** Tips cards  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **10.7.3** Key missing admin guidance message  
  **Açıklama:** “SuperAdmin AI key girmeli”.  
  **Öncelik:** P2

- [ ] **10.7.4** History list  
  **Açıklama:**  
  **Öncelik:** P2

### 10.8 Profile UI

- [ ] **10.8.1** Profile form  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 5.3.x

- [ ] **10.8.2** Notification preferences form  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.8.3** Theme color picker  
  **Açıklama:**  
  **Öncelik:** P1

### 10.9 Admin UI

- [ ] **10.9.1** Users table  
  **Açıklama:**  
  **Öncelik:** P0 · **Bağımlı:** 7.1.x · Sadece SuperAdmin/Admin

- [ ] **10.9.2** Create user / invite UI  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **10.9.3** SystemSettings form (SMTP + AI)  
  **Açıklama:** Masked secrets; test buttons.  
  **Öncelik:** P0 · **Bağımlı:** 7.3.x

- [ ] **10.9.4** Admin nav visibility by role  
  **Açıklama:**  
  **Öncelik:** P0

### 10.10 Landing (opsiyonel)

- [ ] **10.10.1** Minimal self-host landing  
  **Açıklama:** Login/Register CTA.  
  **Öncelik:** P2

---

# FAZ 11 — Docker, release, ops

### 11.1 Docker artifacts

- [ ] **11.1.1** API Dockerfile  
  **Açıklama:** multi-stage build, non-root opsiyonel.  
  **Öncelik:** P1 · **Bağımlı:** 2.3.2

- [ ] **11.1.2** Web Dockerfile  
  **Açıklama:** Next standalone output önerilir.  
  **Öncelik:** P1

- [ ] **11.1.3** docker-compose full stack  
  **Açıklama:** postgres + api + web; volume; env sample.  
  **Öncelik:** P0 · **Bağımlı:** 11.1.1, 11.1.2

- [ ] **11.1.4** `.env.example`  
  **Açıklama:** Connection string, JWT secret, URLs, flags.  
  **Öncelik:** P0

- [ ] **11.1.5** Reverse proxy örneği (Caddy/Nginx)  
  **Açıklama:** `/` → web, `/api` → api; TLS notları.  
  **Öncelik:** P2

- [ ] **11.1.6** Healthcheck compose  
  **Açıklama:** api `/health`.  
  **Öncelik:** P1 · **Bağımlı:** 1.2.7

- [ ] **11.1.7** Auto-migrate compose path doğrula  
  **Açıklama:** Cold start empty volume.  
  **Öncelik:** P0

### 11.2 Ops docs

- [ ] **11.2.1** README install (one command)  
  **Açıklama:**  
  **Öncelik:** P0

- [ ] **11.2.2** Backup/restore Postgres prosedürü  
  **Açıklama:** pg_dump örnekleri.  
  **Öncelik:** P1

- [ ] **11.2.3** Upgrade / migration notları  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **11.2.4** Troubleshooting (port, JWT, SMTP)  
  **Açıklama:**  
  **Öncelik:** P2

---

# FAZ 12 — Testler

### 12.1 Backend unit

- [ ] **12.1.1** Test projesi `Subify.Domain.Tests` / `Application.Tests`  
  **Açıklama:** xUnit.  
  **Öncelik:** P1

- [ ] **12.1.2** UserShare / totals unit tests  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **12.1.3** First SuperAdmin race/logic tests  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **12.1.4** Validators unit tests  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **12.1.5** Category XOR rule tests  
  **Açıklama:**  
  **Öncelik:** P1

### 12.2 Integration

- [ ] **12.2.1** WebApplicationFactory setup  
  **Açıklama:** Testcontainers Postgres önerilir.  
  **Öncelik:** P1

- [ ] **12.2.2** Auth flow integration  
  **Açıklama:** register → login → refresh → logout.  
  **Öncelik:** P1

- [ ] **12.2.3** Subscription isolation test  
  **Açıklama:** User A User B verisini göremez.  
  **Öncelik:** P0

- [ ] **12.2.4** Admin authorization tests  
  **Açıklama:** User settings’e 403.  
  **Öncelik:** P1

- [ ] **12.2.5** No subscription limit test  
  **Açıklama:** 4+ create 403 değil.  
  **Öncelik:** P1

### 12.3 Web E2E

- [ ] **12.3.1** Playwright setup  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **12.3.2** E2E: first admin + create subscription  
  **Açıklama:**  
  **Öncelik:** P2

---

# FAZ 13 — Flutter (en son)

### 13.1 Mobile foundation

- [ ] **13.1.1** Flutter proje oluştur  
  **Açıklama:** `mobile/`  
  **Öncelik:** P3 · **Bağımlı:** API+Web stabilize

- [ ] **13.1.2** Dinamik API base URL onboarding  
  **Açıklama:** Self-host zorunlu.  
  **Öncelik:** P3

- [ ] **13.1.3** Dio + JWT interceptor + secure storage  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.1.4** Riverpod (veya seçilen state) + GoRouter  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.1.5** Light/Dark theme manifesto  
  **Açıklama:**  
  **Öncelik:** P3

### 13.2 Mobile screens

- [ ] **13.2.1** Login / Register  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.2** Dashboard  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.3** Subscriptions list + form  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.4** Reports  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.5** AI  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.6** Profile  
  **Açıklama:**  
  **Öncelik:** P3

- [ ] **13.2.7** Push notifications (opsiyonel)  
  **Açıklama:** FCM; premium yok.  
  **Öncelik:** P3

---

# FAZ 14 — Polish, güvenlik, dokümantasyon kapanışı

### 14.1 Güvenlik checklist

- [ ] **14.1.1** Secret masking audit (settings GET)  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **14.1.2** Log redaction (passwords, tokens, API keys)  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **14.1.3** CORS production tight  
  **Açıklama:**  
  **Öncelik:** P1

- [ ] **14.1.4** Security headers (reverse proxy notları)  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **14.1.5** Dependency vulnerability scan  
  **Açıklama:** Örn. Microsoft.OpenApi uyarısı.  
  **Öncelik:** P2

### 14.2 API dokümantasyon

- [ ] **14.2.1** OpenAPI title/version/description  
  **Açıklama:** Subify OS.  
  **Öncelik:** P2

- [ ] **14.2.2** Endpoint summary/description audit  
  **Açıklama:**  
  **Öncelik:** P2

- [ ] **14.2.3** ERROR_CODES OS revizyonu  
  **Açıklama:**  
  **Öncelik:** P2

### 14.3 Task list bakımı

- [ ] **14.3.1** Tamamlanan task’ları `[x]` yap  
  **Açıklama:** Her PR/sprint sonunda.  
  **Öncelik:** P1

- [ ] **14.3.2** Yeni scope task ekleme kuralı  
  **Açıklama:** Manifesto çelişkisi yoksa ekle; çelişki varsa reddet.  
  **Öncelik:** P1

---

## Önerilen uygulama sırası (numara rehberi)

Grok’a verirken pratik sprint paketleri:

| Sıra | Paket | Task aralığı |
| ---- | ----- | ------------ |
| 1 | Pipeline + health + error OS | `1.2.1` → `1.2.12` (kritikler) |
| 2 | Domain fix + EF + migrate/seed | `2.1` → `2.3` |
| 3 | Auth tamam + SuperAdmin | `3.1` → `3.3` |
| 4 | Subscription core | `4.1` → `4.3` |
| 5 | Categories + Profile + Activity | `5.x` |
| 6 | Admin + Settings | `7.x` |
| 7 | Reports + FX | `6.x` |
| 8 | SMTP + jobs | `8.x` |
| 9 | AI | `9.x` |
| 10 | Web foundation + auth + dashboard + subs | `10.1` → `10.5` |
| 11 | Web admin + reports + profile | `10.6` → `10.9` |
| 12 | Docker release | `11.x` |
| 13 | Tests | `12.x` |
| 14 | Flutter | `13.x` |
| 15 | Polish | `0.x`, `14.x` |

---

## Hızlı referans: P0 “MVP dikey dilim” minimum set

Aşağıdakiler self-host demo için minimum:

1. `1.2.1`, `1.2.3`, `1.2.7`, `1.2.12`
2. `2.1.1`, `2.1.2`, `2.2.1`–`2.2.5`, `2.3.2`–`2.3.5`, `2.3.10`, `2.3.11`
3. `3.1.3`, `3.2.3`, `3.2.4`, `3.2.9`, `3.2.10`, `3.3.1`–`3.3.4`
4. `4.1.1`–`4.1.10`, `4.2.1`–`4.2.6`, `4.3.1`–`4.3.3`
5. `5.1.1`–`5.1.3`, `5.3.1`–`5.3.2`
6. `7.1.1`–`7.1.2`, `7.3.1`–`7.3.2`
7. `10.1.5`–`10.1.8`, `10.2.1`–`10.2.2`, `10.3.1`, `10.4.1`, `10.4.3`, `10.5.1`–`10.5.5`, `10.9.1`, `10.9.3`
8. `11.1.3`, `11.1.4`, `11.1.7`, `11.2.1`
9. `12.2.3`

---

## Kullanım örneği

> “**3.3.1** ve **3.3.2** task’larını yap.”  
> “**4.1.1**’den **4.2.6**’ya kadar subscription dilimini implement et.”  
> “**1.2.1**, **1.2.3**, **1.2.7** — pipeline ve health.”

---

*Bu dosya Subify OS geliştirme sırasının tek operasyonel task listesidir. Çelişkide Manifesto > PRD > bu liste > legacy docs.*
