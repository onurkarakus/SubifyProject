# Subify - Architectural Decision Records (ADR)

Bu doküman, PRD dışında alınan teknik ve mimari kararları kayıt altına alır.

---

## ADR-001: Localization Strategy

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Flutter ARB dosyaları ile lokalizasyon yapmak, her text değişikliğinde app store update gerektiriyor.

### Karar

DB-driven flat resource table kullanılacak:

- **Tablo:** `resources` (PageName, Name, LanguageCode, Value)
- **Sync:** Client app açılışında `GET /api/resources? lang=TR&since={lastSyncedAt}` çağrısı yapar
- **Cache:** Client LocalStorage'da tutar, delta sync ile günceller
- **Backend Cache:** Redis (TTL: 1 saat, invalidation on write)

### Alternatifler

| Alternatif                    | Değerlendirme                           |
| ----------------------------- | --------------------------------------- |
| Flutter ARB files             | ❌ Her değişiklikte app update gerekir  |
| . resx files (backend)        | ❌ Compile-time, runtime değişiklik yok |
| Complex ResourceGroup pattern | ❌ Over-engineering for MVP             |

### Sonuçlar

- Typo fix = DB update → client restart'ta otomatik güncellenir
- Yeni dil eklemek = sadece DB insert
- App Store review gerektirmez

---

## ADR-002: Cache Strategy

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Resource ve EntitlementCache entity'leri read-heavy, write-rare karakteristiğe sahip.

### Karar

Redis Cache-Aside (Lazy Loading) pattern kullanılacak:

| Entity             | Redis Cache | TTL         | Invalidation             |
| ------------------ | ----------- | ----------- | ------------------------ |
| `Resource`         | ✅          | 1 saat      | Admin CRUD → DEL key     |
| `EntitlementCache` | ✅          | 5-15 dakika | Webhook → DEL key        |
| `Category`         | ✅          | 1 saat      | Admin CRUD → DEL key     |
| `Provider`         | ✅          | 1 saat      | Admin CRUD → DEL key     |
| `ExchangeRate`     | ✅          | 1 saat      | Background job → refresh |

### Cache Key Patterns

```
resources:{languageCode}           → JSON array
entitlement:{userId}               → JSON object
categories:all                     → JSON array
providers:active                   → JSON array
exchange-rates:{baseCurrency}      → JSON object
```

---

## ADR-003: Entity Base Classes

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

ASP.NET Core Identity'nin `IdentityUser<TKey>` sınıfı kendi `Id` property'sini tanımlıyor.

### Karar

- **Domain Entities:** `BaseEntity` kullanır (Id, CreatedAt, UpdatedAt)
- **ApplicationUser:** `IdentityUser<Guid>` kullanır, BaseEntity almaz (diamond inheritance prevention)
- **Soft Delete:** `ISoftDeletable` interface ile marker pattern

### BaseEntity

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }  // No default - EF Core generates
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

### Sonuçlar

- Identity framework constraint'e uyum
- Audit fields hem BaseEntity hem ApplicationUser'da manuel tanımlı
- `sealed` keyword tüm domain entities için önerilen (JIT optimization)

---

## ADR-004: Category i18n via Resource Table

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Category entity'sinde çoklu dil desteği gerekiyor (TR/EN).

### Karar

Category tablosunda name tutulmayacak. Localization için Resource tablosu kullanılacak:

- **Category. Slug:** `streaming`, `music`, `productivity`
- **Resource lookup:** `PageName='Category', Name=Slug, LanguageCode='TR'`

### Örnek

```
Category: { Slug: 'streaming', Icon: 'play-circle', Color: '#E50914' }
Resource: { PageName: 'Category', Name: 'streaming', LanguageCode: 'TR', Value: 'Video Akış' }
Resource: { PageName: 'Category', Name: 'streaming', LanguageCode: 'EN', Value: 'Streaming' }
```

### Sonuçlar

- Tek localization source of truth
- Category entity sadece metadata (icon, color, sort) tutar
- Resource tablosu ile tüm UI text'leri merkezi yönetilir

---

## ADR-005: Mobile Framework - Flutter

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Expo/React Native vs Flutter değerlendirmesi yapıldı.

### Karar

Flutter tercih edildi.

### Gerekçe

| Kriter             | Flutter                              | Expo/React Native                |
| ------------------ | ------------------------------------ | -------------------------------- |
| **IAP Stability**  | ✅ Official `in_app_purchase` plugin | Community package, bazen sorunlu |
| **Type Safety**    | ✅ Dart ↔ C# benzeri type sistem     | JS/TS farklı paradigma           |
| **Performance**    | ✅ Native ARM compile                | JavaScript bridge overhead       |
| **UI Consistency** | ✅ Pixel-perfect cross-platform      | Platform farklılıkları           |

---

## ADR-006: User-Defined Custom Categories

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Kullanıcılar sistem kategorileri dışında kendi kategorilerini (örn: 'Gym', 'VPN') tanımlamak istiyor.

### Karar

Ayrı `UserCategory` entity'si oluşturulacak:

- **System categories:** `Category` tablosu (IsDefault=true, global)
- **User categories:** `UserCategory` tablosu (user-specific)
- **Subscription:** Either `CategoryId` OR `UserCategoryId` referansı (mutually exclusive)

### Alternatifler

| Alternatif                     | Değerlendirme                           |
| ------------------------------ | --------------------------------------- |
| Category tablosuna UserId ekle | ❌ Null UserId = system, data pollution |
| Tek tablo, flag ile ayır       | ❌ Query complexity, index inefficiency |
| Ayrı UserCategory tablosu      | ✅ Clean separation, clear ownership    |

### Sonuçlar

- System ve user kategorileri tamamen izole
- User silme/data export operasyonları basitleşir
- Reporting'de ayrı aggregation yapılabilir

---

## ADR-007: Subscription Shared Cost Model

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Kullanıcılar aile planlarını veya arkadaşlarıyla paylaştıkları abonelikleri (Netflix, Spotify Family) takip etmek istiyor.

### Karar

`Subscription` entity'sine `SharedWithCount` alanı eklendi:

- **SharedWithCount = 1:** Paylaşım yok, kullanıcı tam fiyat öder
- **SharedWithCount = 3:** 3 kişi paylaşıyor, kullanıcı Price/3 öder
- **UserShare:** Computed property (DB'de saklanmaz)

### Hesaplama

```csharp
public decimal UserShare => SharedWithCount > 0 ? Price / SharedWithCount : Price;
```

### Dashboard Totals

- **Aylık Toplam:** Tüm aktif aboneliklerin `UserShare` toplamı
- **Yıllık Toplam:** Monthly → \*12, Yearly → as-is

### Sonuçlar

- Basit ve anlaşılır model
- Paylaşan kişilerin bilgisi tutulmuyor (MVP scope)
- İleride PaymentRecord'da "kim ödedi" tracking eklenebilir

---

## ADR-008: Exchange Rate Strategy

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Kullanıcı MainCurrency=TRY olabilir ama USD/EUR abonelik ekleyebilir. Dashboard'da tüm abonelikler MainCurrency'e çevrilmeli.

### Karar

**Backend-driven exchange rate** yaklaşımı:

1. **Background Job:** Saatlik `exchangerate-api.com` API çağrısı
2. **Redis Cache:** `exchange-rates:{baseCurrency}` key ile cache (TTL: 1 saat)
3. **DB Snapshot:** `ExchangeRateSnapshot` tablosuna audit/fallback için yazılır
4. **API Endpoint:** `GET /api/exchange-rates?base=TRY` → cached rates döner

### Flow

```
Client Dashboard Request
    ↓
GET /api/subscriptions (includes currency)
GET /api/exchange-rates?base={user.MainCurrency}
    ↓
Client-side calculation: subscription.UserShare * rate[subscription.Currency]
    ↓
Display in MainCurrency
```

### Alternatifler

| Alternatif                 | Değerlendirme                           |
| -------------------------- | --------------------------------------- |
| Client-side API call       | ❌ CORS issues, rate limits, no caching |
| DB'de converted price      | ❌ Stale data, recalculation complexity |
| Backend provides converted | ✅ Centralized, cached, consistent      |

### Sonuçlar

- Tek source of truth: Backend
- Client calculation basit: `amount * rate`
- Rate limit ve API key yönetimi backend'de

---

## ADR-009: Profile UI Preferences

**Tarih:** 2025-12-07  
**Durum:** Kabul Edildi

### Bağlam

Kullanıcılar tema rengi ve dark/light mode tercihlerini kaydedebilmeli.

### Karar

`Profile` entity'sine UI preference alanları eklendi:

- **ApplicationThemeColor:** Accent color name (e.g., 'Royal Purple')
- **DarkTheme:** Boolean dark mode flag
- **MainCurrency:** ISO 4217 currency code
- **MonthlyBudget:** Budget limit for warnings

### Theme Color Values

Taslak projeden alınan değerler:

- Royal Purple, Ocean Blue, Forest Green, Sunset Orange, Cherry Red, Golden Yellow

### Budget Warning Logic

```csharp
if (user.MonthlyBudget > 0 && monthlyTotal > user.MonthlyBudget)
{
    // Trigger budget exceeded warning
}
```

### Sonuçlar

- UI state backend'de persist edilir
- Cross-device sync sağlanır
- Budget tracking opsiyonel (0 = disabled)

---

## ADR-010: GUID Generation Strategy

**Tarih:** 2025-12-07  
**Durum:** Beklemede (Implementation Required)

### Bağlam

`BaseEntity. Id = Guid.NewGuid()` default assignment, her entity instantiation'da gereksiz allocation yaratıyor.

### Karar

Default assignment kaldırılacak, GUID generation EF Core configuration'da yapılacak:

```csharp
// BaseEntity - No default
public Guid Id { get; set; }

// EF Core Configuration
builder.Property(e => e.Id)
    .HasDefaultValueSql("NEWSEQUENTIALID()");
```

### Gerekçe

- **NEWSEQUENTIALID():** Clustered index fragmentation'ı minimize eder
- **No runtime allocation:** EF Core materialization sırasında gereksiz GUID üretilmez

### Sonuçlar

- Insert performance iyileşir
- Index maintenance maliyeti düşer
- Unit test'lerde entity'lere manuel ID atanması gerekir

---

## Changelog

| Tarih      | ADR     | Değişiklik                         |
| ---------- | ------- | ---------------------------------- |
| 2025-12-07 | ADR-001 | Initial - Localization Strategy    |
| 2025-12-07 | ADR-002 | Initial - Cache Strategy           |
| 2025-12-07 | ADR-003 | Initial - Entity Base Classes      |
| 2025-12-07 | ADR-004 | Initial - Category i18n            |
| 2025-12-07 | ADR-005 | Initial - Mobile Framework         |
| 2025-12-07 | ADR-006 | Initial - User Custom Categories   |
| 2025-12-07 | ADR-007 | Initial - Shared Cost Model        |
| 2025-12-07 | ADR-008 | Initial - Exchange Rate Strategy   |
| 2025-12-07 | ADR-009 | Initial - Profile UI Preferences   |
| 2025-12-07 | ADR-010 | Initial - GUID Generation Strategy |
