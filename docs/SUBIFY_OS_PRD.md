# Subify OS — Product Requirements Document (PRD)

| Alan | Değer |
| ---- | ----- |
| **Ürün adı** | Subify Open Source (**Subify OS**) |
| **Sürüm** | 1.0 |
| **Durum** | Kabul Edildi — Uygulama kaynağı |
| **Son güncelleme** | 2026-03-22 |
| **Hedef kitle** | Bireysel kullanıcılar, aileler, küçük topluluklar |
| **Dağıtım modeli** | Açık kaynak, self-hosted, tamamen ücretsiz |
| **Öncelik sırası** | Web → API/Core → Docker Release → Flutter (sonra) |

> **Doküman hiyerarşisi (çelişki halinde):**
>
> 1. **[SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md)** — Anayasa (en yüksek öncelik)
> 2. **Bu PRD (`SUBIFY_OS_PRD.md`)** — Ürün ve uygulama sözleşmesi
> 3. Diğer `docs/*` dosyaları (ADR, DATA_MODEL, API_CONTRACTS, eski SaaS PRD vb.)
>
> Eski SaaS / freemium / RevenueCat / MSSQL kararları **geçersizdir**. Bu PRD, manifesto ile hizalı tek “nasıl ürün olacak?” belgesidir.

---

## 1. TL;DR — Ürün tek cümlede

**Subify OS**, kullanıcının kendi sunucusunda Docker ile ayağa kaldırdığı; aboneliklerini (Netflix, Spotify, spor salonu, VPN vb.), paylaşım maliyetlerini, bütçesini ve yaklaşan ödemelerini yönettiği; isteğe bağlı kendi API anahtarıyla AI analiz ve SMTP ile e-posta hatırlatma sunan **açık kaynaklı, limit’siz, self-hosted** bir abonelik/finans takip platformudur.

---

## 2. Vizyon, misyon ve strateji

### 2.1 Vizyon

Herkesin abonelik harcamalarını, verisini üçüncü parti bulutlara vermeden, kendi altyapısında ve ailesiyle güvenle yönetebilmesi.

### 2.2 Misyon

- Kurulumu tek komuta indirmek (`docker compose up -d`).
- SaaS freemium karmaşıklığını (ödeme, paywall, limit) tamamen çıkarmak.
- Temiz Clean Architecture kod tabanı ile sürdürülebilir açık kaynak ürün sunmak.
- Temel finans takibini tamamladıktan sonra, kullanıcının kendi LLM anahtarıyla AI’ı açmak.

### 2.3 Stratejik sütunlar

| Sütun | Anlamı |
| ----- | ------ |
| **Sıfır teknik borç / Greenfield** | SaaS kalıntıları (MSSQL, RevenueCat, premium gating) temizlenir; net domain ve dikey dilimler |
| **Gizlilik ve kontrol** | Veri kullanıcının Postgres’inde; harici zorunlu bulut yok |
| **Tamamen ücretsiz** | Hiçbir özellik abonelik planına kilitlenmez; limit yok |
| **Genişletilebilirlik** | SystemSettings üzerinden SMTP + LLM; ileride plugin/entegrasyon alanı |
| **Aile / multi-user** | Tek instance, birden fazla izole kullanıcı, ilk kullanıcı = Super Admin |

### 2.4 Bilinçli redler (Non-Goals)

Aşağıdakiler **ürün kapsamında değildir** (MVP ve v1):

| Non-goal | Gerekçe |
| -------- | ------- |
| Freemium / Premium planlar | Manifesto: özellik kısıtı yok |
| RevenueCat, Stripe, IAP, paywall | Ödeme altyapısı kaldırıldı |
| Multi-tenant SaaS (müşteri başına izolasyon) | Tek self-hosted instance modeli |
| Otomatik kredi kartı çekimi / open banking | Sadece **takip**; ödeme aracı değil |
| Kurumsal team SSO / SCIM | Aile ve küçük topluluk odaklı |
| Zorunlu bulut AI | AI opsiyonel; key instance admin’inde |
| Mobile-first v1 | Flutter **Faz 7**; önce Web + API |
| Kullanıcı verisinin Subify bulutuna toplanması | Self-hosted gizlilik |

---

## 3. Problem ve fırsat

### 3.1 Problem

- Abonelikler dağınık; aylık toplam ve yenileme tarihleri unutuluyor.
- Aile planlarında “benim payım ne?” hesabı elle yapılıyor.
- SaaS abonelik takip uygulamaları veri ve ücret dayatıyor.
- Self-host çözümler ya eksik ya da kurulumu zor.

### 3.2 Fırsat

- Docker + Postgres + modern web ile “tek komut” self-host deneyimi.
- Aile içinde herkes kendi bütçesini görür; admin davet eder.
- Açık kaynak topluluk: seed provider listeleri, dil paketleri, temalar.

---

## 4. Persona ve kullanıcı rolleri

### 4.1 Persona’lar

| Persona | İhtiyaç |
| ------- | ------- |
| **Bireysel self-hoster** | Kendi VPS/NAS’ında aboneliklerini takip etmek, veri kontrolü |
| **Aile yöneticisi** | Eş/çocuk için hesap açmak, herkesin kendi listesini görmesi |
| **Gizlilik bilincili** | Üçüncü parti SaaS’a abonelik listesi vermemek |
| **Power user** | Çoklu para birimi, paylaşım, bütçe, AI ile tasarruf önerisi |

### 4.2 Roller (RBAC)

| Rol | Kim | Yetkiler |
| --- | --- | -------- |
| **SuperAdmin** | Instance’a **ilk kayıt olan** kullanıcı (otomatik) | Tüm admin yetkileri + SystemSettings (LLM key, SMTP) + kullanıcı davet/yönetim + seed/admin operasyonları |
| **Admin** | SuperAdmin tarafından yükseltilen kullanıcı (opsiyonel v1+) | Kullanıcı davet/listeleme; SystemSettings SuperAdmin’e özel kalabilir |
| **User** | Davet veya admin eklemesiyle gelen standart kullanıcı | Kendi profili, abonelikleri, kategorileri, raporları, AI (instance key varsa) |

**Kural:** Finansal veri **kullanıcı bazında izole**. User A, User B’nin aboneliklerini göremez. Admin de başkasının abonelik detayını varsayılan olarak göremez (v1); sadece kullanıcı hesabı yönetimi yapar. (İleride “aile bütçe özeti” ayrı özellik olarak değerlendirilebilir — v1 dışı.)

---

## 5. Ürün prensipleri

1. **Limit yok:** Abonelik sayısı, AI çağrısı, rapor — plan kilidi yok. Rate limit sadece kötüye kullanım / stabilite içindir.
2. **Self-host first:** Env + SystemSettings; harici zorunlu servis yok.
3. **Privacy by design:** JWT, hash’li refresh token, kullanıcı izolasyonu, secret’ların admin panelinde maskelenmesi.
4. **Progressive complexity:** Önce CRUD + dashboard; sonra SMTP; sonra AI; sonra mobil.
5. **Clean vertical slices:** Feature = Command/Query + Handler + Validator + Endpoint.
6. **Dual theme:** Light ve Dark birinci sınıf; kullanıcı tercihi profilde saklanır.
7. **TR + EN:** UI ve e-posta dil desteği (locale).

---

## 6. Başarı metrikleri (self-hosted bağlamında)

SaaS “MRR / conversion” metrikleri geçerli değildir. Bunun yerine:

| Metrik | Açıklama |
| ------ | -------- |
| Time-to-first-value | `docker compose up` → ilk abonelik ekleme süresi |
| Setup friction | Manuel SQL / migration gerekmeden ayağa kalkma |
| Multi-user adoption | Instance başına ortalama kullanıcı sayısı |
| Reminder effectiveness | Gönderilen yenileme e-postası / açılma (opsiyonel analytics yoksa kaba log) |
| AI opt-in | SystemSettings’te LLM key tanımlı instance oranı |
| Community health | GitHub stars, issue, PR, self-host başarı raporları |

---

## 7. Kullanıcı yolculukları (end-to-end)

### 7.1 İlk kurulum (Instance bootstrap)

```
1. Kullanıcı docker compose up -d çalıştırır
2. postgres + api + web ayağa kalkar
3. API start: EF Core pending migrations otomatik uygulanır
4. Seed: roller, sistem kategorileri, providers, temel resources (TR/EN)
5. Web açılır → "İlk kullanıcı kaydı" (henüz SuperAdmin yok)
6. İlk Register → SuperAdmin rolü atanır
7. SuperAdmin → SystemSettings (SMTP / AI key opsiyonel)
8. SuperAdmin → aile üyelerini davet eder veya ekler
```

### 7.2 Günlük kullanım (User)

```
Login → Dashboard (aylık/yıllık toplam, bütçe, yaklaşan ödemeler, son işlemler)
     → Abonelik ekle/düzenle/arşivle
     → Kategori (sistem + özel)
     → Raporlar (kategori dağılımı, aylık trend)
     → AI analiz (key tanımlıysa)
     → Profil (locale, currency, tema, bütçe, bildirim tercihi)
```

### 7.3 Yenileme hatırlatma

```
Background job (günlük) → next_renewal_date - days_before_renewal
  → notification_settings.email_enabled
  → SystemSettings SMTP dolu mu?
  → EmailTemplates (locale) → e-posta gönder
  → activity_log kaydı (opsiyonel)
```

---

## 8. Fonksiyonel gereksinimler

### 8.1 Kimlik doğrulama ve oturum (Auth)

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| AUTH-01 | E-posta + şifre ile kayıt | P0 |
| AUTH-02 | E-posta doğrulama (SMTP yoksa: SuperAdmin onayı veya dev’de bypass flag — dokümante edilmeli) | P0/P1 |
| AUTH-03 | Login: access JWT + refresh token (rotation, revoke) | P0 |
| AUTH-04 | Refresh token endpoint | P0 |
| AUTH-05 | Logout: refresh revoke | P0 |
| AUTH-06 | Forgot / reset password (SMTP bağımlı) | P1 |
| AUTH-07 | **İlk kullanıcı = SuperAdmin** (race-safe: transaction + “admin var mı?” kontrolü) | P0 |
| AUTH-08 | Public self-registration kapatılabilir (sadece davet) — SystemSettings veya env `ALLOW_PUBLIC_REGISTRATION` | P1 |
| AUTH-09 | Account lockout / güçlü şifre (Identity defaults) | P0 |
| AUTH-10 | Hatalar RFC 7807 ProblemDetails + domain error kodları | P0 |

**Token politikası (önerilen varsayılan):**

- Access token: kısa ömür (ör. 15 dk)
- Refresh token: 7 gün, hash’lenerek DB’de, IP/user-agent audit, rotation
- Logout / theft: revoke + reason

### 8.2 Multi-user ve admin

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| MU-01 | SuperAdmin kullanıcı listesi (sayfalı) | P0 |
| MU-02 | Kullanıcıyı manuel ekleme (email, temp password veya invite-only) | P0 |
| MU-03 | Davet linki üretme (tek kullanımlık / süreli token) | P1 |
| MU-04 | Kullanıcı devre dışı bırakma / kilitleme | P1 |
| MU-05 | Rol atama (User / Admin) — SuperAdmin tek ve transfer opsiyonel | P1 |
| MU-06 | Veri izolasyonu: tüm subscription/category sorgularında `user_id` filter | P0 |

### 8.3 SystemSettings (instance yapılandırması)

SuperAdmin panelinden yönetilir; hassas alanlar API response’ta maskelenir (`****` / null write-only).

| Anahtar grubu | Alanlar | Kullanım |
| ------------- | ------- | -------- |
| **LLM** | `AIApiKey` (ve ileride provider/model) | AI analiz |
| **SMTP** | Host, Port, User, Password, FromName, FromEmail | Doğrulama, reset, yenileme hatırlatma |

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| SYS-01 | GET/PUT SystemSettings (SuperAdmin only) | P0 |
| SYS-02 | SMTP test e-postası gönder | P1 |
| SYS-03 | AI key test (opsiyonel ping) | P2 |
| SYS-04 | Runtime’da key değişince client/cache yenileme (singleton spoof etmeden factory) | P1 |

### 8.4 Abonelik yönetimi (Core)

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| SUB-01 | CRUD abonelik (create, list, get, update, archive) | P0 |
| SUB-02 | Soft delete = `archived = true` (+ opsiyonel `DeletedAt`) | P0 |
| SUB-03 | `shared_with_count` ≥ 1; `UserShare = Price / SharedWithCount` (computed) | P0 |
| SUB-04 | `billing_cycle`: monthly \| yearly | P0 |
| SUB-05 | `currency` ISO benzeri (TRY, USD, EUR…) | P0 |
| SUB-06 | `provider_id` opsiyonel; provider seçilince name/price ön-doldurma | P1 |
| SUB-07 | Kategori: **ya** `category_id` **ya** `user_category_id` (mutually exclusive) | P0 |
| SUB-08 | `next_renewal_date`, `last_used_at`, `notes` | P0 |
| SUB-09 | Upcoming: N gün içindeki yenilemeler | P0 |
| SUB-10 | Listede filtre: archived, category, arama | P1 |
| SUB-11 | **Abonelik sayısı limiti YOK** | P0 (kural) |

**Kart UI durumları (frontend):**

| Durum | Koşul | Görsel |
| ----- | ----- | ------ |
| Yakında | `0 ≤ daysUntil ≤ 3` | Warning border + “Yakında” badge; dark’ta hafif amber glow |
| Gecikmiş | `daysUntil < 0` | Danger border + “Gecikmiş” badge |
| Normal | diğer | Standart surface kart |

### 8.5 Kategoriler ve sağlayıcılar

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| CAT-01 | Sistem kategorileri seed (streaming, music, productivity, gaming, shopping, utilities, education, health, cloud, other) | P0 |
| CAT-02 | Sistem kategori adı i18n: Resource lookup (`PageName=Category`, `Name=slug`) | P1 |
| CAT-03 | Kullanıcı özel kategori CRUD (kendi user_id) | P0 |
| PRV-01 | Provider seed (Netflix, Spotify, vb. TR/global) | P1 |
| PRV-02 | Admin provider aktif/pasif (opsiyonel) | P2 |

### 8.6 Dashboard ve finansal motor

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| DASH-01 | Aylık toplam (aktif aboneliklerin UserShare; yearly → /12) | P0 |
| DASH-02 | Yıllık toplam (monthly → ×12, yearly as-is) | P0 |
| DASH-03 | Ana para birimine çeviri: exchange rate snapshot + client/backend convert | P1 |
| DASH-04 | Bütçe progress: `monthlyTotal / monthlyBudget` (budget null/0 = kapalı) | P0 |
| DASH-05 | Yaklaşan ödemeler listesi | P0 |
| DASH-06 | Son işlemler (`activity_logs`) | P1 |

**Hesaplama notu:**

```
UserShare = SharedWithCount > 0 ? Price / SharedWithCount : Price

MonthlyEquivalent(sub) =
  BillingCycle == Monthly ? UserShare : UserShare / 12

YearlyEquivalent(sub) =
  BillingCycle == Yearly ? UserShare : UserShare * 12

DashboardMonthly = Σ MonthlyEquivalent(active, non-archived)  // main currency'ye çevrilmiş
```

### 8.7 Raporlar

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| REP-01 | Kategori bazlı breakdown (tutar, %, adet) | P1 |
| REP-02 | Aylık harcama trendi (N ay) | P1 |
| REP-03 | Para birimi dağılımı | P2 |
| REP-04 | Premium kilidi **YOK** — tüm kullanıcılar erişir | P0 (kural) |

### 8.8 AI analiz (opsiyonel, instance key)

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| AI-01 | `POST /api/ai/analyze` — kullanıcı aboneliklerini özetle + öneriler | P2 (Faz 5) |
| AI-02 | Prompt server-side; key SystemSettings’ten | P2 |
| AI-03 | Key yoksa anlamlı hata: “Admin AI anahtarı yapılandırmalı” | P2 |
| AI-04 | Rate limit (stabilite; plan değil) örn. 5/dk, 20/gün **instance veya user bazlı** | P2 |
| AI-05 | `ai_suggestions_logs` audit | P2 |
| AI-06 | Tip önerileri: unused, duplicate category, yearly switch, general | P2 |

### 8.9 Bildirimler

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| NOT-01 | Kullanıcı `notification_settings`: email_enabled, days_before_renewal | P1 |
| NOT-02 | Push (FCM) — **mobil fazında**; web v1’de opsiyonel / yok | P3 |
| NOT-03 | Background job yenileme e-postası | P1 (Faz 5) |
| NOT-04 | Email templates DB’den (TR/EN): Verify, Reset, RenewalReminder | P1 |
| NOT-05 | SuperAdmin template düzenleme (admin panel) | P2 |

### 8.10 Profil ve tercihler

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| PRO-01 | fullName, locale (tr/en), mainCurrency | P0 |
| PRO-02 | monthlyBudget (nullable) | P0 |
| PRO-03 | applicationThemeColor (preset list) + darkTheme | P0 |
| PRO-04 | Profil alanları `ApplicationUser` üzerinde veya 1:1 profile — tutarlı tek model | P0 |

**Tema renk preset’leri (kullanıcı accent):**

- Royal Purple, Ocean Blue, Forest Green, Sunset Orange, Cherry Red, Golden Yellow  
- Primary UI token’ları manifesto violet paleti ile; accent kullanıcı tercihi olarak kart/buton vurgusu olabilir.

### 8.11 Lokalizasyon (resources)

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| i18n-01 | `resources` tablosu: page_name, name, language_code, value | P1 |
| i18n-02 | Delta sync: `GET /api/resources?lang=&since=` | P1 |
| i18n-03 | Web: next-intl / benzeri + API resource hibrit (MVP’de static JSON + kategori resource) | P1 |
| i18n-04 | Backend validation/error mesajları Accept-Language veya user.locale | P2 |

### 8.12 Döviz kurları

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| FX-01 | `exchange_rate_snapshots` + background sync (exchangerate-api veya benzeri) | P1 |
| FX-02 | `GET /api/exchange-rates?base=` | P1 |
| FX-03 | API key env veya SystemSettings (tercih env — dış servis) | P2 |
| FX-04 | Offline/fallback: son snapshot | P1 |

### 8.13 Activity log

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| ACT-01 | Abonelik create/update/archive → activity_log | P1 |
| ACT-02 | Dashboard “Son İşlemler” | P1 |
| ACT-03 | Kullanıcı sadece kendi loglarını görür | P0 |

### 8.14 Sağlık ve sistem

| ID | Gereksinim | Öncelik |
| -- | ---------- | ------- |
| HLTH-01 | `GET /health` (liveness), DB readiness | P0 |
| HLTH-02 | OpenAPI/Swagger dev ortamında | P0 |
| HLTH-03 | Versiyon endpoint (opsiyonel) | P2 |

---

## 9. Fonksiyonel olmayan gereksinimler (NFR)

| Alan | Hedef |
| ---- | ----- |
| **Performans** | Dashboard listesi < 300ms p95 (makul veri, local network) |
| **Ölçek** | Aile instance: onlarca kullanıcı, binlerce abonelik yeterli |
| **Güvenlik** | HTTPS reverse-proxy arkası; JWT; secret mask; SQL injection yok (EF); CORS bilinçli |
| **Gizlilik** | Veri instance içinde; log’larda şifre/token yok |
| **Kurulum** | Tek `docker compose up -d`; auto-migrate |
| **Yedekleme** | Postgres volume; kullanıcıya backup dokümantasyonu |
| **Gözlemlenebilirlik** | Serilog console/file; health; ileride OTel opsiyonel |
| **Erişilebilirlik** | Semantik HTML, kontrast (light/dark), klavye focus |
| **Responsive** | Mobile browser → ultrawide |
| **Lisans** | Açık kaynak (repo LICENSE netleştirilecek — örn. MIT/Apache-2.0) |

---

## 10. Teknoloji yığını

| Katman | Teknoloji | Not |
| ------ | --------- | --- |
| Veritabanı | **PostgreSQL** | Self-host standart; MSSQL yok |
| Backend | **ASP.NET Core 8** Web API | Clean Architecture + CQRS (MediatR) |
| Validation | FluentValidation | Command başına |
| Auth | ASP.NET Identity + JWT | Refresh rotation |
| ORM | EF Core + Npgsql | Auto-migrate on startup |
| Background | Hangfire **veya** `BackgroundService` / Quartz | E-posta + FX sync |
| Cache | Redis **opsiyonel** (v1’de memory/no-cache kabul) | Resources/providers için |
| Web | **Next.js (App Router) + TypeScript + Tailwind CSS** | Dual theme `dark:` |
| Mobil | **Flutter** | Faz 7; dinamik API base URL |
| Deploy | **Docker + Docker Compose** | api, web, postgres (+ reverse-proxy önerilir) |
| AI | OpenAI-uyumlu HTTP API | Key SystemSettings |
| E-posta | SMTP | SystemSettings |

### 10.1 Repo yapısı (hedef)

```
SubifyProject/
├── api/
│   ├── Subify.Api/
│   ├── Subify.Application/
│   ├── Subify.Domain/
│   ├── Subify.Infrastructure/
│   └── Subify.sln / .slnx
├── web/                 # Next.js
├── mobile/              # Flutter (Faz 7)
├── docker/
│   └── docker-compose.yml
├── docs/
│   ├── SUBIFY_OS_MANIFESTO.md
│   ├── SUBIFY_OS_PRD.md          ← bu dosya
│   └── ...
└── README.md
```

### 10.2 Backend katman kuralları

```
Api            → HTTP, endpoints, DI composition, middleware
Application    → Commands/Queries, handlers, validators, interfaces
Domain         → Entities, enums, Result/Error, domain rules
Infrastructure → EF, Identity, JWT, email, AI client, background jobs
```

- Domain dışarıya framework sızdırmaz (Identity user istisnası pratikte kabul).
- Handler’lar `Result<T>` döner; API ProblemDetails map eder.
- Feature klasörleri: `Features/Subscriptions/Create/...` (vertical slice).

---

## 11. Veri modeli (özet — OS uyumlu)

### 11.1 Dahil tablolar / entity’ler

| Grup | Entity | Amaç |
| ---- | ------ | ---- |
| Identity | AspNetUsers (+ roller), ApplicationUser alanları | Auth + profil tercihleri |
| Auth | refresh_tokens | JWT refresh |
| Core | subscriptions | Abonelikler |
| Core | categories | Sistem kategorileri |
| Core | user_categories | Kullanıcı kategorileri |
| Core | providers | Sağlayıcı kataloğu |
| System | system_settings | LLM + SMTP |
| i18n | resources | DB metinleri |
| AI | ai_suggestions_logs | AI audit |
| Analytics | activity_logs | Son işlemler |
| Notify | notification_settings, email_templates | Bildirim |
| FX | exchange_rate_snapshots | Kur |

### 11.2 Bilinçli olarak **olmayan** tablolar

| Kaldırılan | Neden |
| ---------- | ----- |
| `billing_sessions` | Ödeme yok |
| `entitlements_cache` | Premium yok |
| `profiles.plan` / `plan_renews_at` | Freemium yok |

### 11.3 ApplicationUser (profil alanları — hedef)

| Alan | Tip | Default | Not |
| ---- | --- | ------- | --- |
| FullName | string | - | |
| Locale | string | `tr` | `tr` / `en` (typo `Locate` düzeltilmeli) |
| MainCurrency | string | `TRY` | |
| MonthlyBudget | decimal? | null | null/0 = kapalı |
| ApplicationThemeColor | string | preset | |
| DarkTheme | bool | false | |
| CreatedAt / UpdatedAt | DateTimeOffset | | |

### 11.4 Subscription (çekirdek)

| Alan | Kurallar |
| ---- | -------- |
| UserId | Zorunlu, izolasyon |
| ProviderId | Opsiyonel |
| CategoryId XOR UserCategoryId | İkisi birden dolu olamaz |
| Name, Price > 0, Currency | |
| BillingCycle | monthly / yearly |
| SharedWithCount ≥ 1 | |
| NextRenewalDate | |
| LastUsedAt, Notes | Opsiyonel |
| Archived | Soft delete |

### 11.5 SystemSettings

| Alan | Açıklama |
| ---- | -------- |
| AIApiKey | LLM anahtarı (gizli) |
| SmtpHost, SmtpPort, SmtpUser, SmtpPassword | SMTP |
| SmtpFromName, SmtpFromEmail | Gönderen |

Tek satır (singleton row) veya key-value store — implementasyonda tek kayıt tercih edilir.

### 11.6 İlişki özeti

```
User 1──* Subscription
User 1──* UserCategory
User 1──1 NotificationSetting
User 1──* RefreshToken
User 1──* ActivityLog
User 1──* AiSuggestionLog

Subscription *──0..1 Provider
Subscription *──0..1 Category
Subscription *──0..1 UserCategory

SystemSettings (instance singleton)
Resources (global)
Categories, Providers (global seed)
EmailTemplates (global, admin editable)
ExchangeRateSnapshots (global)
```

PostgreSQL tipleri: `uuid`, `timestamptz`, `date`, `numeric`, `text`, `boolean`. GUID stratejisi: EF / `uuid_generate` veya client Guid; Postgres’te sequential için `uuidv7` veya app-side tercih — implementasyon notu.

---

## 12. API yüzeyi (sözleşme özeti)

Base: `/api`  
Auth: `Authorization: Bearer <access_token>`  
Hata: RFC 7807 ProblemDetails + `type` içinde error code.

### 12.1 Auth

| Method | Path | Auth | Açıklama |
| ------ | ---- | ---- | -------- |
| POST | `/auth/register` | Public* | Kayıt; ilk kullanıcı SuperAdmin |
| POST | `/auth/login` | Public | Tokens + user |
| POST | `/auth/refresh-token` | Public | Rotation |
| POST | `/auth/logout` | Auth | Revoke |
| GET | `/auth/confirm-email` | Public | Query userId+code |
| POST | `/auth/forgot-password` | Public | |
| POST | `/auth/reset-password` | Public | |
| POST | `/auth/resend-confirmation` | Public | Rate limited |

\* Public registration SystemSettings/env ile kapatılabilir.

### 12.2 Users / Admin

| Method | Path | Rol | Açıklama |
| ------ | ---- | --- | -------- |
| GET | `/admin/users` | SuperAdmin/Admin | Liste |
| POST | `/admin/users` | SuperAdmin/Admin | Manuel kullanıcı |
| POST | `/admin/invites` | SuperAdmin/Admin | Davet |
| PATCH | `/admin/users/{id}` | SuperAdmin | Rol / lock |
| GET/PUT | `/admin/settings` | SuperAdmin | SystemSettings |
| POST | `/admin/settings/test-smtp` | SuperAdmin | |

### 12.3 Subscriptions

| Method | Path | Açıklama |
| ------ | ---- | -------- |
| GET | `/subscriptions` | Liste + summary (monthly/yearly) |
| GET | `/subscriptions/{id}` | Detay |
| POST | `/subscriptions` | Oluştur |
| PUT | `/subscriptions/{id}` | Güncelle |
| DELETE | `/subscriptions/{id}` | Archive |
| GET | `/subscriptions/upcoming?days=` | Yaklaşan |

### 12.4 Categories / Providers

| Method | Path | Açıklama |
| ------ | ---- | -------- |
| GET | `/categories` | Sistem kategorileri (i18n name) |
| GET/POST/PUT/DELETE | `/categories/user`… | Özel kategoriler |
| GET | `/providers` | Aktif sağlayıcılar |

### 12.5 Reports / AI / Profile / Activity / FX / Resources

| Method | Path | Not |
| ------ | ---- | --- |
| GET | `/reports/monthly-spend` | Limit yok |
| GET | `/reports/category-breakdown` | Limit yok |
| POST | `/ai/analyze` | Key gerekli |
| GET | `/ai/history` | |
| GET/PUT | `/profile` | |
| PUT | `/profile/notifications` | |
| GET | `/activity` | |
| GET | `/exchange-rates` | |
| GET | `/resources` | Delta sync |
| GET | `/health` | |

**Kaldırılan endpoint aileleri:** `/payments/*`, `/billing/*`, `/webhooks/revenuecat`.

---

## 13. Web uygulaması (Next.js) — ekranlar

### 13.1 Public

| Ekran | Açıklama |
| ----- | -------- |
| Landing (opsiyonel self-host) | Kısa “Subify OS” tanıtım veya doğrudan login |
| Login | E-posta / şifre |
| Register | İlk kurulum veya public reg açıksa |
| Confirm email / Reset password | Token sayfaları |

### 13.2 Authenticated (User)

| Ekran | Bileşenler |
| ----- | ---------- |
| **Dashboard** | Selamlama, aylık/yıllık kartlar, bütçe bar, upcoming, recent activity |
| **Subscriptions** | Liste/kart grid, filtre, arama, archive toggle |
| **Subscription form** | Provider autocomplete, kategori, fiyat, cycle, share, tarih, not |
| **Reports** | Kategori pie/bar, aylık line chart |
| **AI** | “Analiz al”, tips kartları, history |
| **Profile / Settings** | Locale, currency, budget, theme, dark mode, notifications |
| **Logout** | |

### 13.3 SuperAdmin

| Ekran | İçerik |
| ----- | ------ |
| Users | Liste, ekle, davet, lock |
| System Settings | SMTP form, AI key form, test butonları |
| Email templates (P2) | CRUD önizleme |
| Providers (P2) | Seed yönetimi |

### 13.4 Navigasyon

- Desktop: sol sidebar veya top nav  
- Mobile web: bottom nav veya hamburger  
- Tema: light/dark toggle + system preference opsiyonu  

### 13.5 Design system (manifesto token’ları)

| Token | Light | Dark | Kullanım |
| ----- | ----- | ---- | -------- |
| Background | `#F8FAFC` | `#0F172A` | Sayfa zemini |
| Surface | `#FFFFFF` | `#1E293B` | Kart / modal |
| Primary | `#7C3AED` | `#8B5CF6` | CTA, aktif nav |
| Text primary | `#0F172A` | `#F8FAFC` | Başlık |
| Text muted | `#64748B` | `#94A3B8` | İkincil |
| Success | `#10B981` | `#34D399` | Tasarruf |
| Warning | `#F59E0B` | `#FBBF24` | Yaklaşan |
| Danger | `#EF4444` | `#F87171` | Gecikmiş / sil |

- Font: **Inter**  
- H1 32 Bold / H2 24 SemiBold / Body 16 Regular  
- Tailwind `dark:` class strategy  

---

## 14. Mobil (Flutter) — Faz 7

**Zamanlama:** Web + API + Docker stabilize olduktan sonra.

| Gereksinim | Detay |
| ---------- | ----- |
| Dinamik API URL | İlk açılışta instance base URL girişi (self-host zorunlu) |
| Ekranlar | Login, Dashboard, Subscriptions, Reports, AI, Profile |
| State | Riverpod (veya eşdeğeri) |
| HTTP | Dio + JWT interceptor + refresh |
| Secure storage | Token’lar |
| Tema | Light/Dark manifesto paleti |
| Push | Opsiyonel FCM (SMTP e-posta yeterli değilse) |

RevenueCat / IAP **yok**.

---

## 15. Altyapı ve dağıtım

### 15.1 Docker Compose (hedef servisler)

| Servis | Port (ör.) | Görev |
| ------ | ---------- | ----- |
| `postgres` | 5432 | Veri |
| `api` | 5000/8080 | ASP.NET |
| `web` | 3000 | Next.js |
| `reverse-proxy` (önerilen) | 80/443 | TLS, route `/` → web, `/api` → api |
| `redis` | opsiyonel | Cache |
| `worker` | opsiyonel | Hangfire ayrı process |

### 15.2 API startup

1. Wait for Postgres (retry)  
2. `Database.Migrate()`  
3. Seed (roles, categories, providers, resources, email templates) if empty  
4. Listen  

### 15.3 Ortam değişkenleri (örnek)

```
ConnectionStrings__DefaultConnection=...
JwtOptions__SecretKey=...
JwtOptions__Issuer=...
JwtOptions__Audience=...
ALLOW_PUBLIC_REGISTRATION=true
ASPNETCORE_ENVIRONMENT=Production
NEXT_PUBLIC_API_URL=https://subify.example.com/api
```

SMTP ve AI key tercihen **SystemSettings** (UI); JWT secret **env** (güvenlik).

### 15.4 Yedekleme (dokümantasyon gereksinimi)

- `pg_dump` volume backup prosedürü README’de  
- Restore adımları  

---

## 16. Güvenlik gereksinimleri

| ID | Kural |
| -- | ----- |
| SEC-01 | Şifreler Identity hash; düz metin asla loglanmaz |
| SEC-02 | Refresh token hash saklanır |
| SEC-03 | SystemSettings secret alanları GET’te maskeli |
| SEC-04 | Authorization her resource’ta user_id ownership |
| SEC-05 | SuperAdmin endpoint’leri role policy |
| SEC-06 | CORS: bilinen web origin |
| SEC-07 | Rate limit: login, register, AI, forgot-password |
| SEC-08 | İlk SuperAdmin race condition’a karşı tek transaction |
| SEC-09 | Production HTTPS reverse-proxy |
| SEC-10 | Dependency güncellemeleri / güvenli image tag’leri |

---

## 17. Hata modeli

- RFC 7807 ProblemDetails  
- Domain kodları: `AUTH_*`, `SUB_*` (limit kodu **kullanılmaz** / `SUB_001` limit kaldırıldı), `AI_*` (premium yerine “key missing”), `PRO_*`, `SYS_*`, `VAL_*`, `USER_*`, `CAT_*`, `PROV_*`  
- Eski `SUB_001 Subscription Limit Reached` ve `AI_001 Premium Required` **OS modelinde kaldırılır veya yeniden anlamlandırılır:**  
  - AI key yok → `AI_KEY_MISSING` / 503 veya 400  
  - Limit → **yok**

---

## 18. Seed verisi (özet)

| Veri | İçerik |
| ---- | ------ |
| Roles | SuperAdmin, Admin, User |
| Categories | 10 sistem slug + icon + color |
| Providers | Popüler TR/global servisler (Netflix, Spotify, …) |
| Resources | Common, Category, Dashboard, Subscription, Error (TR/EN) |
| Email templates | VerifyEmail, ResetPassword, RenewalReminder (TR/EN) |

Detay için (eski seed doc OS’a uyarlanarak) [SEED_DATA.md](./SEED_DATA.md) — plan/premium metinleri temizlenmeli.

---

## 19. Test stratejisi (özet)

| Katman | Araç | Odak |
| ------ | ---- | ---- |
| Unit | xUnit | UserShare, monthly totals, first-user admin, validators |
| Integration | WebApplicationFactory | Auth, subscription isolation, settings authz |
| E2E | Playwright | Register first admin → invite → CRUD → dashboard |
| Manual | Docker | compose up cold start + migrate |

Coverage hedefleri: Domain/business logic yüksek; infrastructure orta.

---

## 20. Geliştirme yol haritası (manifesto ile birebir)

### Faz 1 — Core Setup

- [ ] Repo, `.gitignore`, README (self-host odaklı)
- [ ] `api/`, `web/`, `docs/` iskeleti
- [ ] .NET 8 Clean Architecture solution
- [ ] Next.js + TS + Tailwind

### Faz 2 — PostgreSQL & Domain

- [ ] BaseEntity + GUID politikası
- [ ] User, Subscription, Category, Provider, SystemSettings (+ diğer OS entity’ler)
- [ ] DbContext + Npgsql
- [ ] Billing/entitlement entity **yok**

### Faz 3 — Identity & Multi-User

- [ ] JWT + Identity
- [ ] First-user SuperAdmin
- [ ] Admin invite/add endpoints
- [ ] Register/login/refresh/logout

### Faz 4 — Subscriptions & Financial Engine

- [ ] Subscription vertical slices
- [ ] Seed categories/providers
- [ ] Exchange rate snapshots
- [ ] Dashboard totals + budget + upcoming

### Faz 5 — AI & SMTP

- [ ] SystemSettings UI + API
- [ ] OpenAI client (dynamic key)
- [ ] AI analyze + logs
- [ ] Background e-posta hatırlatma

### Faz 6 — Dockerization & Release

- [ ] Dockerfile api + web
- [ ] docker-compose (postgres + api + web)
- [ ] Auto-migrate on API start
- [ ] README: one-command install

### Faz 7 — Flutter

- [ ] Dinamik API URL
- [ ] Auth + core screens parity with web

---

## 21. SaaS → OS geçiş kontrol listesi (implementasyon için)

| Eski SaaS | OS kararı |
| --------- | --------- |
| Free 3 abonelik limiti | **Kaldır** |
| Premium gating (AI, reports, push) | **Kaldır** |
| RevenueCat / Stripe / webhooks | **Kaldır** |
| `billing_sessions`, `entitlements_cache` | **Kaldır** |
| `plan`, `plan_renews_at` | **Kaldır** |
| Paywall UI | **Kaldır** |
| MSSQL | **PostgreSQL** |
| Mobile-first | **Web-first**; Flutter sonra |
| Multi-tenant SaaS | **Single instance multi-user** |
| Zorunlu bulut AI | **Opsiyonel BYOK (bring your own key)** |
| İlk kullanıcı | **SuperAdmin** |
| Env-only secrets | **SystemSettings (SMTP/AI) + env (JWT/DB)** |

---

## 22. Kullanıcı hikayeleri (kabul kriterli örnekler)

### US-01 İlk kurulum

**Olarak** self-hoster, **istiyorum** ki compose ile sistemi ayağa kaldırıp ilk hesabı oluşturabileyim, **böylece** SuperAdmin olayım.  
**Kabul:** Boş DB’de register → rol SuperAdmin; ikinci register → User (public reg açıksa).

### US-02 Abonelik paylaşımı

**Olarak** kullanıcı, **istiyorum** Netflix’i 4 kişi paylaşıyor diye gireyim, **böylece** dashboard’da payım görünsün.  
**Kabul:** price 400, share 4 → UserShare 100; monthly total’a 100 yansır.

### US-03 Aile daveti

**Olarak** SuperAdmin, **istiyorum** e-posta ile aile üyesi ekleyeyim, **böylece** kendi aboneliklerini girebilsin.  
**Kabul:** Yeni user login olur; admin’in aboneliklerini göremez.

### US-04 Yaklaşan ödeme görseli

**Olarak** kullanıcı, **istiyorum** 2 gün sonra yenilenecek aboneliği uyarı border’ı ile göreyim.  
**Kabul:** daysUntil ≤ 3 → warning state + “Yakında”.

### US-05 SMTP hatırlatma

**Olarak** SuperAdmin SMTP girdiysem, **istiyorum** kullanıcılar yenileme öncesi mail alsın.  
**Kabul:** Job, `days_before_renewal` ve template locale ile mail üretir; SMTP yoksa job no-op + log.

### US-06 AI BYOK

**Olarak** SuperAdmin AI key girdiysem, **istiyorum** her user analiz alabilsin.  
**Kabul:** Key yok → anlaşılır hata; key var → tips + log kaydı.

### US-07 Limit yok

**Olarak** kullanıcı, **istiyorum** istediğim kadar abonelik ekleyeyim.  
**Kabul:** 3’ten fazla eklemede 403/limit hatası **yok**.

---

## 23. Riskler ve mitigasyon

| Risk | Mitigasyon |
| ---- | ---------- |
| İlk SuperAdmin race | DB unique constraint / serializable transaction |
| SMTP/AI yanlış config | Test butonları + net hata mesajları |
| Secret sızıntısı admin API | Masking, audit log, HTTPS |
| Self-host destek yükü | Güçlü README, health checks, örnek compose |
| Eski SaaS kod/doc karışıklığı | Bu PRD + manifesto öncelik; eski doc “legacy” işaretle |
| FX API rate limit / down | Snapshot fallback |
| Public registration abuse | Kapatılabilir reg + rate limit + invite-only |

---

## 24. Açık kararlar / netleştirme notları

Aşağıdakiler manifesto ile çelişmez; implementasyon sırasında kilitlenebilir:

1. **E-posta doğrulama zorunlu mu** SMTP yokken? (Öneri: SuperAdmin için dev flag; production’da SMTP sonrası enforce.)  
2. **Public registration default** true mu false mu? (Öneri: ilk kullanıcıdan sonra default `false` veya env.)  
3. **Hangfire vs BackgroundService** (küçük instance için HostedService yeterli olabilir.)  
4. **Redis zorunlu mu?** (v1 hayır.)  
5. **Profil: ayrı tablo vs ApplicationUser** (tek model seçilip tutarlı kullanılmalı.)  
6. **Admin başkasının verisini görür mü?** (v1: hayır.)  
7. **Açık kaynak lisansı** (MIT önerilir.)  

---

## 25. İlgili dokümanlar

| Doküman | Rol | Not |
| ------- | --- | --- |
| [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md) | Anayasa | En yüksek öncelik |
| **SUBIFY_OS_PRD.md** | Bu dosya — ürün sözleşmesi | |
| [ADR.md](./ADR.md) | Teknik kararlar | SaaS maddeleri geçersiz; localization/shared cost/FX hâlâ yararlı |
| [DATA_MODEL.md](./DATA_MODEL.md) | Detay şema | Billing/plan bölümleri ignore |
| [API_CONTRACTS.md](./API_CONTRACTS.md) | Request/response örnekleri | Premium/payment ignore |
| [SEED_DATA.md](./SEED_DATA.md) | Seed SQL/C# | Paywall resource metinleri temizlenmeli |
| [ERROR_CODES.md](./ERROR_CODES.md) | Hata kataloğu | Limit/premium kodları güncellenmeli |
| [UI_MOCKUPS.md](./UI_MOCKUPS.md) | Ekran referansı | Renk token’ları manifesto ile override |
| [TESTING_STRATEGY.md](./TESTING_STRATEGY.md) | Test | Premium senaryoları çıkar |
| [LOGGING_MONITORING.md](./LOGGING_MONITORING.md) | Log | Geçerli |
| [Subify.Web.Uygulamasi.v2.PRD.md](./Subify.Web.Uygulamasi.v2.PRD.md) | **LEGACY SaaS** | Tarihsel; uygulama için kullanma |
| [SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md](./SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md) | Eski task list | OS roadmap ile yeniden hizalanmalı |
| diagrams/* | Diyagramlar | MSSQL/RevenueCat notları güncellenmeli |

---

## 26. Tek bakışta mimari

```
                    ┌─────────────────────┐
                    │  Browser (Next.js)  │
                    │  Light / Dark UI    │
                    └──────────┬──────────┘
                               │ HTTPS
                    ┌──────────▼──────────┐
                    │  Reverse Proxy      │  (önerilen)
                    └───┬────────────┬────┘
                        │            │
              ┌─────────▼──┐   ┌─────▼─────────┐
              │  Web :3000 │   │  API :8080    │
              └────────────┘   │  Clean Arch   │
                               │  JWT Identity │
                               │  Auto-Migrate │
                               └───────┬───────┘
                         ┌─────────────┼─────────────┐
                         │             │             │
                  ┌──────▼─────┐ ┌─────▼────┐ ┌──────▼──────┐
                  │ PostgreSQL │ │ SMTP     │ │ LLM API     │
                  │ (volume)   │ │ (opt.)   │ │ (opt. key)  │
                  └────────────┘ └──────────┘ └─────────────┘

  İlk User ──► SuperAdmin ──► SystemSettings + davet
  Her User ──► sadece kendi Subscription / Category / Activity
  Özellik kısıtı / ödeme / paywall ──► YOK
```

---

## 27. Özet: Bu PRD’den hatırlanması gerekenler

1. **Subify OS = open source + self-hosted + free + multi-user family.**  
2. **Ödeme, freemium, RevenueCat, paywall, abonelik limiti yok.**  
3. **PostgreSQL + ASP.NET Core 8 CQRS + Next.js + Docker; Flutter en son.**  
4. **İlk kayıt SuperAdmin; veri kullanıcıya izole.**  
5. **SystemSettings: SMTP + AI key (BYOK).**  
6. **Core değer: abonelik CRUD, UserShare, dashboard, bütçe, yaklaşan/gecikmiş UI.**  
7. **Design: dual theme, violet primary, Inter, manifesto renk token’ları.**  
8. **Auto-migrate + seed + tek komut compose = ürün vaadi.**  
9. **Çelişkide manifesto > bu PRD > legacy docs.**  
10. **Geliştirme fazları 1→7 sırayla; feature flag’li “premium” düşünme.**

---

*Bu belge, Subify OS ürününün tek kapsamlı PRD’sidir. Yeni özellik eklenmeden önce manifesto ilkeleri ve bu PRD ile uyum kontrol edilmelidir.*
