# Subify OS

**Açık kaynaklı, self-hosted abonelik ve kişisel finans takip platformu.**

Subify OS; Netflix, Spotify, spor salonu, VPN ve benzeri aboneliklerinizi kendi sunucunuzda yönetmenizi sağlar. Verileriniz üçüncü parti SaaS bulutlarında değil, sizin PostgreSQL veritabanınızda kalır.

> **Model:** Tamamen ücretsiz · Özellik limiti yok · Freemium / RevenueCat / Stripe **yok**  
> **Hedef:** Bireysel kullanıcılar, aileler ve küçük topluluklar

---

## Ne yapar?

| Özellik | Açıklama |
| ------- | -------- |
| **Abonelik yönetimi** | CRUD, arşivleme, yaklaşan / gecikmiş ödemeler |
| **Paylaşımlı maliyet** | Aile planı: `UserShare = Price / SharedWithCount` |
| **Dashboard** | Aylık / yıllık toplam, bütçe, son işlemler |
| **Çoklu kullanıcı** | İlk kayıt = Super Admin; davet ile aile üyeleri; veri izolasyonu |
| **Tema** | Light / Dark (Tailwind), Inter tipografi |
| **E-posta hatırlatma** | Super Admin’in girdiği SMTP ile (opsiyonel) |
| **AI analiz** | Super Admin’in girdiği kendi LLM API key’i ile BYOK (opsiyonel) |

---

## Tech stack

| Katman | Teknoloji |
| ------ | --------- |
| API | ASP.NET Core (Clean Architecture + CQRS / MediatR) |
| Web | Next.js (App Router) + TypeScript + Tailwind CSS |
| DB | **PostgreSQL** |
| Deploy | Docker + Docker Compose |
| Mobil | Flutter — **son faz** (Web + API stabilize olduktan sonra) |
| API docs (dev) | Scalar UI → `/scalar/v1` |

---

## Doküman hiyerarşisi (önemli)

Çelişen bilgilerde sıra:

1. [`docs/SUBIFY_OS_MANIFESTO.md`](docs/SUBIFY_OS_MANIFESTO.md) — Anayasa  
2. [`docs/SUBIFY_OS_PRD.md`](docs/SUBIFY_OS_PRD.md) — Ürün gereksinimleri  
3. [`docs/SUBIFY_OS_TASK_LIST.md`](docs/SUBIFY_OS_TASK_LIST.md) — Uygulama task listesi  
4. Diğer `docs/*` (ADR, DATA_MODEL, API örnekleri vb.) — yardımcı; SaaS kalıntılarına dikkat  

**Legacy (uygulama kaynağı değil):**

- `docs/Subify.Web.Uygulamasi.v2.PRD.md` — eski freemium SaaS PRD  
- `docs/SUBIFY_DEVELOPMENT_TASK_LIST_NEW.md` — eski task list  
- `docs/REVENUECAT_CONFIG.md` — ödeme; OS’ta yok  

---

## Hızlı başlangıç (geliştirme)

### 1. PostgreSQL

```bash
cd docker
docker compose up -d
```

Varsayılanlar **API appsettings ile hizalı** (task 2.3.11):

| | |
| - | - |
| Host / Port | `localhost:5432` |
| Database | `subify_db` |
| User | `subify_admin` |
| Password | `SecretPassword123!` |

Detay: [`docker/README.md`](docker/README.md) · örnek env: [`docker/.env.example`](docker/.env.example)

### 2. API

```bash
cd api/Subify.Api
dotnet run --launch-profile http
```

API start’ta EF Core **auto-migrate** + **seed** çalışır (Postgres ready olana kadar retry). Manuel `dotnet ef database update` gerekmez.

- API: http://localhost:5240  
- **Scalar (test UI):** http://localhost:5240/scalar/v1  
- OpenAPI JSON: http://localhost:5240/openapi/v1.json  
- Health: http://localhost:5240/health · readiness: http://localhost:5240/health/ready

### 3. Web (iskelet)

```bash
cd web
npm install
npm run dev
```

Web henüz auth/dashboard aşamasındadır; API önce olgunlaşmaktadır.

---

## Kurulum vaadi (hedef release)

Stabil sürümde tek komut:

```bash
docker compose up -d
```

API ayağa kalkarken EF Core migration’lar otomatik uygulanacak; seed (roller, kategoriler) yüklenecek.  
*(Full compose + auto-migrate henüz geliştirme yol haritasında — bkz. task list Faz 11.)*

---

## Repo yapısı

```
SubifyProject/
├── api/                 # ASP.NET Clean Architecture
│   ├── Subify.Api/
│   ├── Subify.Application/
│   ├── Subify.Domain/
│   └── Subify.Infrastructure/
├── web/                 # Next.js
├── mobile/              # Flutter (sonra)
├── docker/              # docker-compose (Postgres)
├── docs/                # Manifesto, PRD, task list, diyagramlar
├── LICENSE
└── README.md
```

---

## Lisans

[MIT](./LICENSE) — özgürce kullanın, değiştirin, self-host edin.

---

## Katkı / durum

Proje **aktif geliştirme** aşamasındadır. Yapılacak işler ve sıra:

→ [`docs/SUBIFY_OS_TASK_LIST.md`](docs/SUBIFY_OS_TASK_LIST.md)

Mimari kararlar:

→ [`docs/SUBIFY_OS_MANIFESTO.md`](docs/SUBIFY_OS_MANIFESTO.md)  
→ [`docs/SUBIFY_OS_PRD.md`](docs/SUBIFY_OS_PRD.md)
