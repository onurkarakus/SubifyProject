# Subify SaaS Geçiş PRD (OS → Cloud)

| Alan | Değer |
| ---- | ----- |
| **Doküman** | Subify SaaS Transition PRD |
| **Sürüm** | 1.2 |
| **Durum** | Taslak — ileride uygulama rehberi (şimdi implementasyon yok) |
| **Son güncelleme** | 2026-08-02 |
| **İlgili** | [SUBIFY_OS_MANIFESTO.md](./SUBIFY_OS_MANIFESTO.md), [SUBIFY_OS_PRD.md](./SUBIFY_OS_PRD.md), [SUBIFY_OS_TASK_LIST.md](./SUBIFY_OS_TASK_LIST.md), [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md), [DATA_MODEL.md](./DATA_MODEL.md), [OPS.md](./OPS.md) |
| **Task listesi** | [SUBIFY_SAAS_TRANSITION_TASK_LIST.md](./SUBIFY_SAAS_TRANSITION_TASK_LIST.md) |

---

## 0. Amaç ve okuma rehberi

Bu doküman, **bugün çalışan (veya hedefi self-host olan) Subify OS** kod tabanından, **kapalı kaynak / senin işlettiğin Subify Cloud (SaaS)** ürününe geçişin ürün, mimari, veri, ödeme ve operasyon gereklerini tanımlar.

**Neden yazıldı?**

- OS ve SaaS bilinçli olarak farklı ürün varsayımlarına sahip (manifesto: freemium/SaaS/ödeme bilerek söküldü).
- İleride “open source’u SaaS’a çevirelim” dendiğinde kararlar unutulmasın; yeniden keşif maliyeti olmasın.
- Geçiş **tek seferlik big-bang** değil; fazlı, geri alınabilir ve OS hattını bozmayacak şekilde tasarlanabilsin.

**Bu PRD ne değildir?**

- Aktif sprint backlog’u değildir (task listesi ayrı dosyada).
- “Hemen kod yaz” emri değildir.
- OS’i öldürme kararı değildir — **çift hat (dual track)** varsayılır.

---

## 1. Ürün stratejisi: Dual track

### 1.1 İki ürün, bir çekirdek

| | **Subify OS** | **Subify Cloud (SaaS)** |
| --- | --- | --- |
| Dağıtım | Müşteri Docker / self-host | Senin yönettiğin multi-tenant cloud |
| Gelir | Yok (veya destek/sponsor) | Abonelik (RevenueCat + store/Stripe) |
| Admin | Instance SuperAdmin | PlatformAdmin (sen) + OrgAdmin (müşteri) |
| Ayarlar | SMTP/AI BYOK, setup wizard | **SMTP + AI anahtarı yalnızca platform** (müşteriden istenmez) |
| Limit | Bilinçli olarak yok | Plan / entitlement |
| Kaynak | Açık veya kapalı seçilebilir | Kapalı kaynak varsayılan |
| Kod | `OrganizationId` **yok** (şu an) | `OrganizationId` **zorunlu** |

### 1.2 Kod stratejisi seçenekleri

| Seçenek | Açıklama | Öneri |
| ------- | -------- | ----- |
| **A. Feature flags / compile symbols** | `SUBIFY_SAAS` ile billing, tenant filter | Orta vadede karmaşıklaşır |
| **B. Shared libraries + iki host** | Domain/Application ortak; `Subify.Api` vs `Subify.Cloud.Api` | **Önerilen** |
| **C. Fork** | OS dondurulur, Cloud fork | Drift riski yüksek; son çare |

**Öneri (v1 Cloud):** Shared Domain + Application çekirdeği; **Infrastructure ve Api** katmanında SaaS-specific paketler (`Billing`, `Tenancy`, `PlatformAdmin`). OS build’i billing’i referans etmez.

### 1.3 Marka ve lisans

- Cloud: proprietary, private repo veya private package.
- OS: mevcut manifesto ile uyumlu kalabilir (MIT/Apache vb. — avukat onayı).
- Aynı “Subify” adı: ToS’ta “Cloud vs Self-Hosted” ayrımı net olmalı.
- Eski SaaS kalıntı dokümanları (`REVENUECAT_CONFIG.md`, mockup paywall) **Cloud** için yeniden aktif referans olur; OS task listesine **geri sokulmaz**.

---

## 2. Mevcut durum (AS-IS) — OS özeti

Kod ve ürün varsayımları (2026-08 itibarıyla hedef OS mimarisi):

| Alan | Durum |
| ---- | ----- |
| Kiracılık | Yok — tek instance, veri çoğunlukla `UserId` |
| Roller | SuperAdmin, Admin, User |
| Setup | First-run wizard; `SystemSettings` singleton |
| Abonelik | Kullanıcıya özel; limit yok |
| Provider | Instance kataloğu + seed; SuperAdmin import |
| E-posta | SMTP BYOK; şablon motoru; reminder job |
| AI | BYOK OpenAI-compatible |
| FX | Background sync; snapshot; dual display |
| Ödeme | **Yok** (RevenueCat/Stripe bilerek kaldırıldı) |
| Mobil | Flutter ertelenmiş; RC dokümanı legacy/Cloud hazırlığı |
| Yedek | Host `pg_dump`; in-app restore yok |

**Teknik borç / bilinçli boşluklar (Cloud için):**

1. Tüm sorgularda tenant boundary yok.
2. `SystemSettings` tek satır — platform + org ayırımı yok.
3. Invite “instance user” modeline yakın; org membership değil.
4. Entitlement / plan enforcement yok.
5. Web setup wizard Cloud’da istenmez.
6. Platform-wide abuse, rate limit, e-posta doğrulama Cloud’da şart.

---

## 3. Hedef durum (TO-BE) — Subify Cloud

### 3.1 Persona ve roller

#### Platform (sen)

| Rol | Kimlik | Yetki |
| --- | ------ | ----- |
| **PlatformOwner** | Sen / kurucu hesap | Tüm tenant’lar, plan, global catalog, feature flag, imporsonation (audit’li), disable tenant |
| **PlatformSupport** | Destek | Read-mostly tenant, sınırlı impersonation, ticket notları |

Platform paneli müşteri UI’sından **ayrı route / ayrı host** önerilir: `platform.subify.app` veya `/platform/*` + güçlü role gate.

#### Müşteri organizasyonu

| Rol | Yetki (v1 önerisi) |
| --- | ------------------ |
| **OrgOwner** | Billing, silme, member yönet, org ayarları, tüm abonelikler (paylaşım modeline göre) |
| **OrgAdmin** | Member davet, katalog/ayar (billing hariç) |
| **OrgMember** | Kendi veya paylaşılan abonelikler; rapor (kapsama göre) |
| **OrgViewer** (opsiyonel v1.1) | Salt okunur |

#### Son kullanıcı akışı (senin tarifin)

1. Kullanıcı kaydolur → **Organization** + **OrgOwner** oluşur.
2. Owner alt kullanıcı davet eder (e-posta / link).
3. Üyeler sisteme girer; plan limitine kadar member.
4. Hazır **global provider** listesinden seçer veya **custom** ekler.

### 3.2 Workspace / paylaşım modeli (ürün kararı — kilit)

İki ana model; **Cloud v1’de birini seç**, ikisini birden yapma.

#### Model A — “Hane / Team shared ledger” (önerilen senin senaryona)

- Abonelikler **Organization** mülkiyetinde.
- Member’lar role’e göre görür / düzenler.
- “Kim ekledi” audit alanı: `CreatedByUserId`.
- Aylık toplam org bazlı.

**Artı:** Gerçek aile/ekip kullanımı.  
**Eksi:** Yetki matrisi, silme çatışmaları, gizlilik.

#### Model B — “Kişisel cüzdan + rızalı özet”

- Her abonelik bir `UserId`’ye ait.
- Org sadece billing + invite container.
- İsteğe bağlı aggregate (iptal edilen family budget’a benzer).

**Artı:** Daha az sızıntı.  
**Eksi:** “Alt kullanıcı ortak liste” zayıf.

**PRD varsayılanı (değiştirilebilir):** **Model A** — Cloud v1 shared ledger, basit roller (Owner/Admin/Member), v1’de Viewer yok.

### 3.3 Plan ve limitler (entitlement)

OS’te limit yoktu. Cloud’da **RevenueCat entitlement** → API enforcement.

| Plan (örnek) | Entitlement ID | Abonelik # | Member # | AI (canlı LLM) | Özel |
| ------------ | -------------- | ---------- | -------- | -------------- | ---- |
| Free | `free` (veya entitlement yok) | 10 | 1 | **0** canlı / gün (veya 1 cache-only demo) | Soft paywall |
| Plus | `plus` | 50 | 3 | **1 canlı / gün** (org veya user — aşağıda) | Aynı gün tekrar = cache |
| Pro | `pro` | Sınırsız* | 10 | **1 canlı / gün** (v1 aynı; v1.1’de N/gün planla) | Aynı gün tekrar = cache |
| Lifetime | `pro` (non-renewing) | Pro ile aynı | | Aynı | |

\* “Sınırsız” app subscription sayısı için; AI **günlük frekans limiti** ile sınırlıdır (abuse).

#### 3.3.1 AI günlük öneri kuralı (abuse + maliyet) — **kilit**

> **Amaç:** Platform LLM key’i ile maliyeti kontrol etmek; “Yeniden öner” spam’ini engellemek.  
> **Kural (Cloud v1):** Takvim günü başına **en fazla bir canlı AI analyze** (LLM çağrısı).  
> Aynı gün içinde tekrar istek gelirse **yeni LLM çağrısı yapılmaz**; o gün için DB’de saklanan son başarılı öneri **aynı yanıt olarak** döner.

| Kavram | Tanım |
| ------ | ----- |
| **Canlı öneri** | Platform LLM’e giden `analyze` (token maliyeti var) |
| **Cache hit / replay** | Aynı gün, daha önce kaydedilmiş `AiSuggestionLog` (veya günlük snapshot) tekrar sunulur; **UsageCounter artmaz** |
| **Gün sınırı** | Org’un `TimeZoneId` (yoksa `Europe/Istanbul` / UTC — D11) ile `LocalDate` |
| **Yeni gün** | `LocalDate` değişince bir sonraki istek **canlı** olabilir (plan izin veriyorsa) |

**Akış (pseudo):**

```
POST /api/ai/analyze  (veya mevcut analyze endpoint)
  1. Auth + org + entitlement (AI feature açık mı?)
  2. dayKey = today in org timezone
  3. existing = DB'de (OrganizationId, UserId?, dayKey, kind=DailyAnalyze) son başarılı kayıt
  4. if existing != null:
       return existing.Response  // 200, header/flag: fromCache=true
  5. if Free ve AI kapalı → BILL_004 / paywall
  6. call LLM (platform key)
  7. persist AiSuggestionLog + DailyAiSuggestion (dayKey, response, model, createdAt)
  8. return response  // fromCache=false
```

**Kapsam birimi (D12 — seçim gerekli):**

| Seçenek | Anlam | Öneri |
| ------- | ----- | ----- |
| **Per Organization** | Org günde 1 canlı analyze (tüm member’lar paylaşır) | Daha ucuz; aile/workspace için mantıklı |
| **Per User** | Her member günde 1 canlı | Daha cömert; maliyet ↑ |

**PRD varsayılanı:** **Per Organization** (shared ledger ile uyumlu; abuse org başına).

**UI kopyası (TR örnek):**

- İlk istek: “AI önerilerin hazır.”
- Aynı gün tekrar: “Bugünün önerisi (önbellek). Yarın yeni analiz alabilirsin.” + `fromCache: true`
- Force refresh **yok** (v1); v1.1 Pro “+1 ekstra / gün” entitlement opsiyonel.

**History:**

- `GET /api/ai/history` mevcut log’lar kalır; günlük kural history’yi silmez.
- Cache hit de history listesinde **tek satır** olarak kalabilir (aynı id dönülür) veya her replay loglanmaz (öneri: **replay loglanmaz**, aynı log id).

**Rapor commentary (`report-commentary`):**

- Aynı günlük kural **ayrı kind** ile uygulanır: `DailyReportCommentary` — günde 1 canlı (org).  
- Veya v1’de yalnızca `analyze` sınırlanır; commentary de aynı bucket’a alınabilir.  
  **Varsayılan v1:** her iki endpoint de **ayrı daily slot** (abuse hâlâ sınırlı: 2 LLM/gün/org max).

**OS farkı:** Self-host’ta bu limit **yok** (kullanıcı kendi key’i / kendi sunucusu). Yalnız Cloud.

**Not:** “Subscription” kelimesi çift anlamlı:

| Terim | Anlam |
| ----- | ----- |
| **App Subscription** | Netflix vb. kullanıcı kaydı (`Subscriptions` tablosu) |
| **Billing Subscription** | Subify Cloud üyelik (Stripe/App Store/Play) |

Doküman ve kodda Cloud billing için: `BillingPlan`, `CustomerEntitlement`, `RevenueCatEvent` kullan.

### 3.4 Provider kataloğu

| Kaynak | `OrganizationId` | Kim yönetir | Kullanım |
| ------ | ---------------- | ----------- | -------- |
| Global catalog | `NULL` | PlatformAdmin | Tüm org’lar okur |
| Org catalog | org id | OrgAdmin | Sadece o org |
| Free-text only | — | Her member | Provider satırı yok; `Subscription.Name` |

Kurallar:

- Global fiyat **öneri**; gerçek ödeme tutarı her zaman app subscription satırında.
- Logo URL opsiyonel; CDN senin kontrolünde.
- Import JSON (`provider-catalog.sample.json` benzeri) → **yalnız PlatformAdmin**.
- Org, global slug ile çakışan custom slug açamaz (namespace: org-local slug unique per org).

### 3.5 Kimlik ve güvenlik

| Konu | Cloud kuralı |
| ---- | ------------ |
| E-posta confirm | **Cloud’da önerilir** (OS’te bilinçli yoktu). Abuse ve şifre sıfırlama için. |
| Public register | Açık; captcha / rate limit |
| MFA | v1.1 PlatformAdmin zorunlu; OrgOwner opsiyonel |
| Session | JWT + refresh; org claim; org switch |
| Impersonation | Sadece PlatformSupport; audit log zorunlu |
| Data export | OrgOwner GDPR export (JSON/CSV) |
| Data delete | Org silme + soft grace (30 gün) |

### 3.6 E-posta ve AI (operasyon modeli) — **kilit karar**

> **Cloud v1 (ve varsayılan ürün politikası):**  
> **SMTP ve AI API key’i yalnızca platform sahibi (sen) sağlar.**  
> Müşteri / org admin’den SMTP host-şifre veya LLM API key **istenmez, UI’da gösterilmez, kaydedilmez.**

Bu, OS’teki BYOK modelinin **bilinçli tersine çevrilmesidir**.

| Servis | Kim yapılandırır | Müşteri ne görür |
| ------ | ---------------- | ---------------- |
| Transactional mail (invite, reset, confirm) | **Platform** (SES / SendGrid / SMTP pool) | “E-posta geldi” — ayar ekranı yok |
| Renewal reminder / report summary mail | **Platform** aynı pool | Bildirim tercihi (aç/kapa, gün) — **sunucu ayarı yok** |
| AI analiz / report commentary | **Platform** OpenAI-compatible key + model | “Analiz et” + **kota** (plan); key alanı yok |
| FX sync | **Platform** background job | Kurlar “çalışıyor” — provider key yok |
| Org “System settings → SMTP/AI” | **Yok** | Menüde yok; 403 if legacy route |

#### PlatformSettings (sende)

| Alan | Açıklama |
| ---- | -------- |
| `Smtp*` veya managed mail provider config | Tek global gönderen (From, domain, SPF/DKIM senin DNS’inde) |
| `AiApiKey` (encrypted) | Tek (veya env) platform key |
| `AiProvider` / `AiModel` / `AiBaseUrl` | Platform seçimi; müşteri override etmez |
| `AiEnabled` | Bakımda AI kapatma |
| `MailEnabled` | Bakımda mail kapatma |

#### Müşteri tarafı (org / user)

| İzin verilir | İzin verilmez |
| ------------ | ------------- |
| E-posta bildirimi açık/kapalı (tercih) | SMTP host/port/user/password |
| Hatırlatma günü (daysBefore) | From adresi / domain |
| AI özelliğini kullanmak (kota içinde) | Kendi API key / base URL / model |
| Plan yükseltince daha çok AI | “Kendi OpenAI hesabımı bağla” (v1 **yok**) |

#### Maliyet ve abuse

- LLM ve mail maliyeti **senin P&L**’inde → fiyata ve **UsageCounter** kotasına gömülür.
- Sert limit: `BILL_003` AI quota; rate limit; şüpheli org suspend (`S6.2.2`).
- Prompt’a PII politikası + ToS’ta “veri 3. parti LLM’e gidebilir” (platform subprocessors).

#### İleride (bilinçli erteleme — varsayılan kapalı)

- **S9.4 Org-level AI BYOK** ve org SMTP BYOK: yalnızca enterprise talep gelirse; v1 PRD kapsamı **dışı**.  
- Bu özellikler açılırsa ayrı güvenlik review + “getirdiğin key senin sorumluluğun” ToS maddesi gerekir.

#### OS → Cloud UI farkı

| OS | Cloud |
| -- | ----- |
| Setup: SMTP + AI adımları | Yok |
| SuperAdmin System settings → SMTP/AI sekmeleri | **Platform console** → yalnızca sen |
| Müşteri AI key missing mesajı | “AI geçici kapalı” veya “kotan doldu” / plan upgrade |

### 3.7 Multi-region ve veri yerleşimi

v1: **tek region** (ör. `eu-central-1` veya seçtiğin VPS).  
v2: EU/US split — `Organization.DataRegion` + ayrı DB (büyük iş).

---

## 4. Mimari hedef

### 4.1 Mantıksal bileşenler

```
                    ┌─────────────────────┐
                    │  Platform Console   │  (sen)
                    └──────────┬──────────┘
                               │
┌──────────────┐    ┌──────────▼──────────┐    ┌─────────────────┐
│  Web App     │───▶│  Subify Cloud API   │───▶│  PostgreSQL     │
│  (Next.js)   │    │  + Tenancy filter   │    │  shared schema  │
└──────────────┘    │  + Entitlement gate │    └─────────────────┘
                    └──────────┬──────────┘
┌──────────────┐               │
│  Flutter App │───────────────┤
└──────────────┘               │
                    ┌──────────▼──────────┐
                    │ RevenueCat          │
                    │  ├─ Stripe (Web)    │
                    │  ├─ App Store       │
                    │  └─ Play Billing    │
                    └─────────────────────┘
```

### 4.2 Tenancy modeli (v1)

**Shared database, shared schema, discriminator column `OrganizationId`.**

- Global tablolar: `Organizations`, `Plans` cache, `PlatformSettings`, `GlobalProviders` (`OrganizationId IS NULL`), `RevenueCatEvents`, `EmailSendLog` (platform).
- Tenant tabloları: `Subscriptions`, `UserCategories`, `Tags`, `ActivityLog`, `OrgMembers`, `OrgInvites`, `OrgSettings`, org-scoped providers.

**Global query filter (EF):**

```csharp
// Pseudocode
builder.Entity<Subscription>().HasQueryFilter(s =>
    s.OrganizationId == _tenant.CurrentOrganizationId);
```

`IgnoreQueryFilters()` yalnız platform admin / migration / job.

### 4.3 Identity modeli

```
ApplicationUser (global identity)
  └── OrganizationMember (UserId, OrganizationId, Role)
        └── optional: default OrganizationId on user profile
```

- Bir user birden fazla org’ta olabilir (v1.1); v1: **tek org** basitleştirmesi kabul edilebilir.
- JWT:

```json
{
  "sub": "<userId>",
  "email": "...",
  "org_id": "<organizationId>",
  "org_role": "Owner|Admin|Member",
  "entitlements": ["plus"],
  "rc_app_user_id": "<stable id>"
}
```

### 4.4 Entitlement enforcement noktaları

| Aksiyon | Gate |
| ------ | ---- |
| Create app subscription | count < plan.maxSubscriptions |
| Invite member | count < plan.maxMembers |
| AI analyze (canlı) | Günde 1 / org (D12); platform key; aşımda **cache replay** (yeni LLM yok) |
| AI analyze (aynı gün tekrar) | DB’deki bugünkü öneri; `fromCache=true`; kota tüketmez |
| Report commentary (canlı) | Günde 1 / org (ayrı kind); platform key |
| Report email summary | feature flag + plan; **platform SMTP** |
| Custom AI base URL / customer API key | **v1 yok** (S9.4 ertelenmiş) |

**Kaynak of truth:**

1. RevenueCat webhook → `CustomerEntitlement` / cache tablosu güncelle.
2. API her kritik yolda cache’e bak (TTL kısa + webhook ile invalidate).
3. Client paywall sadece UX; **asıl kilit API**.

### 4.5 Ödeme altyapısı — RevenueCat merkezli

Legacy doküman: [REVENUECAT_CONFIG.md](./REVENUECAT_CONFIG.md). Cloud’da yeniden bağlanır ve **genişletilir**.

#### Neden RevenueCat?

- Tek entitlement modeli: Web (Stripe), iOS, Android.
- Receipt validation, sandbox, grace period, unsubscribe analytics.
- Flutter + Web için tek “customer info” dili.

#### Bileşenler

| Bileşen | Görev |
| ------- | ----- |
| **RevenueCat project** | Entitlement + product mapping |
| **Stripe** | Web checkout (RC Stripe bilink veya Custom) |
| **App Store Connect / Play Console** | IAP ürünleri |
| **Webhook → Cloud API** | `POST /api/billing/revenuecat/webhook` |
| **CustomerInfo sync** | Login sonrası / app resume |
| **Customer portal** | Stripe Customer Portal (web iptal/upgrade) |

#### App User ID stratejisi

- **Stable:** `user.Id` (GUID string) veya `org_{organizationId}` eğer faturalama org bazlıysa.

**Kritik ürün kararı:** Billing **user** mı **organization** mı?

| | User-billed | Org-billed (önerilen) |
| --- | --- | --- |
| Kim öder | Kayıt olan kişi | OrgOwner |
| Member limit | Kişisel | Org |
| Transfer | Zor | Org ownership transfer |
| RC app_user_id | `userId` | `orgId` (tercih) |

**PRD varsayılanı:** **Organization-billed** — `app_user_id = organizationId`. Owner değişince billing org’da kalır.

#### Webhook olayları (işle)

- `INITIAL_PURCHASE`, `RENEWAL`, `PRODUCT_CHANGE`
- `CANCELLATION`, `UNCANCELLATION`
- `EXPIRATION`, `BILLING_ISSUE`
- `SUBSCRIPTION_PAUSED` (Play)

Her event: idempotent `EventId` store; entitlement satırı upsert; audit.

#### Sandbox / prod

- Ayrı RC API keys.
- Ayrı Stripe test mode.
- PlatformAdmin’de “simulate entitlement” sadece Development.

### 4.6 OS’ten fark — setup ve settings

| OS | Cloud |
| -- | ----- |
| Setup wizard | Yok (sen deploy) |
| `SystemSettings` singleton | `PlatformSettings` + `OrganizationSettings` |
| SuperAdmin = ilk kuran | PlatformOwner seed (sen) |
| Allow public registration instance flag | Her zaman register; abuse controls |

### 4.7 API yüzey farkları (özet)

Yeni / değişen:

```
POST   /api/auth/register              → org + owner + trial entitlement
POST   /api/billing/checkout-session   → web Stripe/RC
POST   /api/billing/revenuecat/webhook → imza doğrulamalı
GET    /api/billing/entitlements       → current org
POST   /api/org/invites
GET    /api/org/members
PATCH  /api/org/members/{id}
DELETE /api/org/members/{id}
GET    /api/platform/tenants           → PlatformAdmin
POST   /api/platform/providers/import
GET    /api/providers                  → global ∪ org custom
```

Kalan abonelik/rapor/AI path’leri: header veya claim’den `org_id` zorunlu.

---

## 5. Veri modeli (hedef eklemeler)

### 5.1 Yeni entity’ler (özet)

| Entity | Amaç |
| ------ | ---- |
| `Organization` | Tenant; Name, Slug, CreatedAt, Status (Active/Suspended/Deleted), DataRegion |
| `OrganizationMember` | UserId, OrganizationId, Role, JoinedAt, InvitedBy |
| `OrganizationInvite` | Token hash, email, role, expires (OS UserInvite’den türet/ayır) |
| `OrganizationSettings` | DefaultLocale, DefaultCurrency, Theme defaults, feature toggles |
| `PlatformSettings` | **Zorunlu:** platform AI key (encrypted), SMTP/mail provider, From identity, maintenance. Müşteri erişemez. |
| `CustomerEntitlement` | OrganizationId, EntitlementId, ProductId, ExpiresAt, Source (RC), Raw JSON ref |
| `RevenueCatWebhookEvent` | EventId (unique), Type, Payload, ProcessedAt |
| `UsageCounter` | OrganizationId, Metric (AiLiveCalls), Period (yyyy-MM-dd **veya** yyyy-MM), Count — günlük live sayacı için |
| `DailyAiSuggestion` *(veya AiSuggestionLog genişletme)* | OrganizationId, optional UserId, `Day` (date), `Kind` (Analyze \| ReportCommentary), ResponsePayload, Model, SourceLogId, CreatedAt; **unique (Org, Day, Kind)** [+ User if D12=user] |
| `Provider` değişikliği | `OrganizationId?` null = global |

**Not:** Mevcut OS `AiSuggestionLog` history için kalır. Cloud daily cache ya:

1. Log üzerine `SuggestionDay` + unique index, veya  
2. Ayrı `DailyAiSuggestion` satırı (önerilen — net “bugünün kartı”).

### 5.2 Mevcut entity’lere ek kolonlar

| Entity | Ek |
| ------ | --- |
| `Subscription` | `OrganizationId` (required), `CreatedByUserId` |
| `UserCategory` | `OrganizationId` |
| `ActivityLog` | `OrganizationId` |
| `AiSuggestionLog` | `OrganizationId`; opsiyonel `SuggestionDay`, `Kind`, `IsLiveCall` |
| `SubscriptionPriceHistory` | `OrganizationId` (veya subscription join) |
| `EmailSendLog` | `OrganizationId?` (platform mail null olabilir) |
| `ApplicationUser` | `DefaultOrganizationId?`, `EmailConfirmed` Cloud politikası |

### 5.3 Migrasyon stratejisi (OS instance → Cloud)

Self-host müşteriyi Cloud’a taşımak **nadir**; yine de:

1. Export tool (OS): subscriptions + categories JSON.
2. Cloud import: yeni org + owner mapping.
3. Şifreler taşınmaz — reset mail.
4. Provider global list Cloud’da yeniden seed; custom name korunur.

**Tek OS DB’yi multi-tenant’a çevirmek** (kendi sunucunda Cloud modu):

1. `Organizations` oluştur: “Default” org.
2. Tüm user’ları member yap (Owner = eski SuperAdmin).
3. Tüm satırlara `OrganizationId = default`.
4. SuperAdmin → PlatformOwner flag ayrı tabloda.
5. Feature flag `CloudMode=true`.

Bu “in-place upgrade” task listesinde **S2** altında.

---

## 6. UX / ekranlar (Cloud)

### 6.1 Public

- Landing + pricing
- Register / Login
- Forgot / reset password
- Accept invite

### 6.2 App (org context)

- Dashboard (org totals)
- Subscriptions CRUD + provider picker (global + custom)
- Reports / AI (entitlement gate)
- Members & invites
- Org settings (locale, currency)
- Billing / plan (Customer Info + upgrade)
- Profile (kişisel tema; org’dan ayrı)

### 6.3 Platform console (sen)

- Tenant list (search, status, plan, member count)
- Suspend / unsuspend
- Global provider catalog + import
- Entitlement override (destek; audit)
- System health (FX, mail success rate, job)
- Usage / revenue snapshot (RC metrics link)

### 6.4 Paywall

- Soft: banner “Plus’a geç”
- Hard: 403 `BILL_001 PlanLimitExceeded` + web modal
- Mobile: RevenueCat paywall UI / offering

Mockup referans (legacy): `docs/mockups/mobile_paywall_*.png`.

---

## 7. Uyumluluk, hukuk, gizlilik

| Madde | Not |
| ----- | --- |
| KVKK / GDPR | DPA, işleme envanteri, silme/export |
| Çerez | Cookie banner (web analytics varsa) |
| Ödeme | Stripe/Apple/Google şartları; fiyatlandırma vergisi |
| AI | Kullanıcı abonelik verisi 3. parti LLM’e gider — açık rıza / ToS |
| Subprocessors | Liste: hosting, SES, OpenAI, RC, Stripe |
| OS vs Cloud ToS | Ayrı belgeler |

---

## 8. Operasyon (Cloud OPS)

OS [OPS.md](./OPS.md) müşteri self-host içindi. Cloud OPS ekleri:

| Alan | Cloud |
| ---- | ----- |
| Deploy | CI/CD, blue-green veya rolling |
| Backup | Otomatik günlük + PITR; müşteri script görmez |
| Secrets | Vault / cloud secret manager |
| Observability | OpenTelemetry + error tracking (Sentry) |
| On-call | Sen / ekip |
| Cost | LLM token, mail, DB, egress — plan fiyatına yedir |

---

## 9. Riskler ve mitigasyon

| Risk | Etki | Mitigasyon |
| ---- | ---- | ---------- |
| Tenant filter unutulması | Veri sızıntısı | EF global filter + integration test “user A cannot read B” |
| Entitlement cache stale | Ücretsiz premium / haksız kilit | Webhook + short TTL + login refresh |
| Org vs User billing karışıklığı | Muhasebe cehennemi | Tek model: org-billed; dokümante |
| OS/Cloud kod drift | Çift bakım | Shared core; thin hosts |
| App Store 30% | Marj | Web’e yönlendirme kurallarına uy (Apple guideline) |
| AI cost blowup | Zarar | Sert kota + model seçimi + caching |
| SuperAdmin zihniyeti Cloud UI’da kalır | Güvenlik | Route ayrımı + pentest checklist |

---

## 10. Başarı metrikleri (Cloud launch)

| Metri | Hedef (örnek) |
| ----- | ------------- |
| Sign-up → first subscription logged | < 10 dk |
| Paid conversion (trial 14g) | izlenecek |
| API p95 latency | < 300 ms read paths |
| Tenant isolation tests | 100% critical paths |
| Backup restore drill | 90 günde bir başarılı |
| Support: wrong-plan tickets | azalan trend |

---

## 11. Faz özeti (yüksek seviye)

Detaylı task’lar: [SUBIFY_SAAS_TRANSITION_TASK_LIST.md](./SUBIFY_SAAS_TRANSITION_TASK_LIST.md).

| Faz | Ad | Çıktı |
| --- | -- | ----- |
| **S0** | Karar & envanter | Bu PRD kilit kararlar imzalı; gap list |
| **S1** | Tenancy foundation | OrganizationId, filters, register→org |
| **S2** | Members & invites | Alt kullanıcı |
| **S3** | Entitlements + RC | Webhook, gate, paywall API |
| **S4** | Web billing UX | Checkout, portal, soft/hard paywall |
| **S5** | Provider global/org | Catalog model + platform import |
| **S6** | Platform console | Senin admin |
| **S7** | Mobile RC | Flutter offerings (OS mobile ile paralel) |
| **S8** | Hardening & launch | GDPR, load, legal, prod cutover |

OS feature geliştirmesi (tag, vb.) **S1 öncesi veya shared core’da** `OrganizationId` nullable ile ilerleyebilir; S1’de required yapılır.

---

## 12. Kilit karar kaydı (doldurulacak)

> Geçişe başlamadan önce bu tabloyu güncelle.

| # | Karar | Seçenekler | Seçim | Tarih |
| - | ----- | ---------- | ----- | ----- |
| D1 | Paylaşım modeli | A shared / B personal | **A (öneri)** | |
| D2 | Billing subject | User / Org | **Org** | |
| D3 | Free plan | Var / yok | | |
| D4 | E-posta confirm | Zorunlu / değil | **Zorunlu (Cloud)** | |
| D5 | AI + SMTP | **Platform-only (müşteriden key yok)** / org BYOK | **Platform-only** | 2026-08-02 |
| D11 | AI “gün” timezone | Org TZ / UTC / Europe/Istanbul | **Org TZ, fallback Europe/Istanbul** | 2026-08-02 |
| D12 | Daily AI scope | Per org / per user | **Per organization** | 2026-08-02 |
| D13 | Aynı gün 2. istek | Cache replay / 429 hard deny | **Cache replay** (`fromCache`) | 2026-08-02 |
| D6 | Multi-org per user | v1 yok / var | **v1 yok** | |
| D7 | Kod ayrımı | Flag / dual host / fork | **Dual host** | |
| D8 | OS devam | Evet paralel / dondur | **Paralel** | |
| D9 | RC entitlement set | free/plus/pro | | |
| D10 | Region | tek EU / … | | |

---

## 13. Çelişki çözüm sırası

1. **Bu SaaS Transition PRD** (Cloud kapsamı)
2. **OS Manifesto** (yalnız OS hattı; Cloud’da freemium/ödeme **bilinçli istisna**)
3. OS PRD / task list (self-host)
4. Legacy SaaS docs (`REVENUECAT_CONFIG`, web PRD v2) — referans, körü körüne kopyalama

Cloud geliştirirken OS task listesine “RevenueCat ekle” yazma; **SaaS task listesine** yaz.

---

## 14. Ek: OS → Cloud özellik matris

| Özellik | OS | Cloud v1 |
| ------- | -- | -------- |
| Setup wizard | Var | Yok |
| SuperAdmin instance | Var | PlatformOwner |
| Unlimited subs | Evet | Plan’a göre |
| SMTP BYOK (müşteri) | Var | **Yok** — yalnızca platform SMTP/SES |
| AI BYOK (müşteri) | Var | **Yok** — yalnızca platform LLM key |
| AI frekans | Limitsiz (kendi key) | **1 canlı öneri / gün / org**; aynı gün = DB cache |
| Platform SMTP/AI ayarı | SuperAdmin UI | Platform console (sen) |
| FX dual | Var | Var |
| Price history | Var | Var |
| Provider import | SuperAdmin | PlatformAdmin |
| Family budget | İptal | Workspace paylaşımlı model |
| LastUsedAt | İptal | İsteğe bağlı v2 |
| Tags | Backlog OS | Cloud’da org-scoped |
| Docker compose müşteri | Var | Yok (sen) |
| RevenueCat | Yok | Var |
| Paywall | Yok | Var |

---

### Değişiklik geçmişi (PRD)

| Tarih | Sürüm | Not |
| ----- | ----- | --- |
| 2026-08-02 | 1.0 | İlk SaaS geçiş PRD |
| 2026-08-02 | 1.1 | **D5 kilit:** SMTP + AI yalnızca platform; müşteri BYOK v1 yok |
| 2026-08-02 | 1.2 | **D11–D13:** günde 1 canlı AI; aynı gün DB replay (`fromCache`) |

*Sonuç: Open source / self-host Subify OS korunabilir; Cloud ayrı ürün disiplini, tenancy ve RevenueCat ile gelir. **Mail ve AI altyapısı senin sorumluluğunda (managed).** Bu PRD geçişin “neden / ne / nasıl” belleğidir; iş kalemleri task listesindedir.*
