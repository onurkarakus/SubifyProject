# Subify - RevenueCat Konfigürasyonu

Bu doküman, RevenueCat ödeme entegrasyonu için gerekli ürün ve entitlement yapılandırmasını detaylandırır.

> **Durum (2026-08):** Subify **OS** hattında ödeme **yok** (manifesto). Bu dosya **legacy + gelecekteki Subify Cloud (SaaS)** referansıdır.  
> Cloud geçiş planı: [SUBIFY_SAAS_TRANSITION_PRD.md](./SUBIFY_SAAS_TRANSITION_PRD.md) · task’lar: [SUBIFY_SAAS_TRANSITION_TASK_LIST.md](./SUBIFY_SAAS_TRANSITION_TASK_LIST.md) (`S3` RevenueCat).  
> Cloud’da önerilen model: **organization-billed**, `app_user_id = organizationId`; entitlement set Free/Plus/Pro (yalnız `premium` değil) — PRD ile güncellenecek.

> **Referanslar:**
>
> - [Ana PRD (legacy web)](./Subify.Web.Uygulamasi.v2.PRD.md)
> - [SaaS Transition PRD](./SUBIFY_SAAS_TRANSITION_PRD.md)
> - [SEQUENCE_DIAGRAMS.md](./diagrams/SEQUENCE_DIAGRAMS.md)

---

## 📋 Genel Bakış

RevenueCat, Subify'ın ödeme altyapısını yönetir:

- **Web**: Stripe entegrasyonu üzerinden
- **iOS**: App Store In-App Purchase
- **Android**: Google Play Billing

---

## 🎯 Entitlements

Entitlement'lar, kullanıcının erişim haklarını tanımlar.

| Entitlement ID | Açıklama       | Aktifleyen Ürünler  |
| -------------- | -------------- | ------------------- |
| `premium`      | Premium üyelik | Tüm premium ürünler |

```json
{
  "entitlements": {
    "premium": {
      "id": "premium",
      "display_name": "Premium Access",
      "products": [
        "premium_monthly_tr",
        "premium_yearly_tr",
        "premium_lifetime_tr",
        "premium_monthly_usd",
        "premium_yearly_usd",
        "premium_lifetime_usd"
      ]
    }
  }
}
```

---

## 💰 Products (Ürünler)

### Türkiye Pazarı (TRY)

| Product ID            | Tip          | Fiyat | Döngü        |
| --------------------- | ------------ | ----- | ------------ |
| `premium_monthly_tr`  | Subscription | ₺49   | Aylık        |
| `premium_yearly_tr`   | Subscription | ₺499  | Yıllık       |
| `premium_lifetime_tr` | Non-Renewing | ₺699  | Tek Seferlik |

### Global Pazar (USD)

| Product ID             | Tip          | Fiyat  | Döngü        |
| ---------------------- | ------------ | ------ | ------------ |
| `premium_monthly_usd`  | Subscription | $4.99  | Aylık        |
| `premium_yearly_usd`   | Subscription | $49.99 | Yıllık       |
| `premium_lifetime_usd` | Non-Renewing | $69.99 | Tek Seferlik |

---

## 🍎 App Store (iOS) Konfigürasyonu

### Product IDs Mapping

| RevenueCat Product     | App Store Product ID             |
| ---------------------- | -------------------------------- |
| `premium_monthly_tr`   | `com.subify.premium.monthly.tr`  |
| `premium_yearly_tr`    | `com.subify.premium.yearly.tr`   |
| `premium_lifetime_tr`  | `com.subify.premium.lifetime.tr` |
| `premium_monthly_usd`  | `com.subify.premium.monthly`     |
| `premium_yearly_usd`   | `com.subify.premium.yearly`      |
| `premium_lifetime_usd` | `com.subify.premium.lifetime`    |

### App Store Connect Ayarları

```
1. App Store Connect > Monetization > Subscriptions
   └── Subscription Group: "Subify Premium"
       ├── premium.monthly.tr (₺49/ay)
       ├── premium.yearly.tr (₺499/yıl)
       ├── premium.monthly (Tier 2 - $4.99)
       └── premium.yearly (Tier 25 - $49.99)

2. App Store Connect > Monetization > In-App Purchases
   ├── premium.lifetime.tr (₺699)
   └── premium.lifetime ($69.99)
```

### Subscription Group Benefits

- Tek subscription group içinde upgrade/downgrade
- Grace period: 16 gün (billing retry)
- Renewal öncesi uyarı: 24 saat

---

## 🤖 Google Play (Android) Konfigürasyonu

### Product IDs Mapping

| RevenueCat Product     | Play Store Product ID |
| ---------------------- | --------------------- |
| `premium_monthly_tr`   | `premium_monthly_tr`  |
| `premium_yearly_tr`    | `premium_yearly_tr`   |
| `premium_lifetime_tr`  | `premium_lifetime_tr` |
| `premium_monthly_usd`  | `premium_monthly`     |
| `premium_yearly_usd`   | `premium_yearly`      |
| `premium_lifetime_usd` | `premium_lifetime`    |

### Google Play Console Ayarları

```
1. Google Play Console > Monetization > Subscriptions
   ├── Base Plans
   │   ├── premium_monthly_tr
   │   │   └── Offer: monthly-offer-tr (₺49/ay)
   │   ├── premium_yearly_tr
   │   │   └── Offer: yearly-offer-tr (₺499/yıl)
   │   ├── premium_monthly
   │   │   └── Offer: monthly-offer ($4.99/mo)
   │   └── premium_yearly
   │       └── Offer: yearly-offer ($49.99/yr)

2. Google Play Console > Monetization > In-app products
   ├── premium_lifetime_tr (₺699)
   └── premium_lifetime ($69.99)
```

---

## 💳 Stripe (Web) Konfigürasyonu

RevenueCat'in Stripe entegrasyonu kullanılır.

### Price IDs

| RevenueCat Product     | Stripe Price ID             |
| ---------------------- | --------------------------- |
| `premium_monthly_tr`   | `price_1ABC...monthly_tr`   |
| `premium_yearly_tr`    | `price_1ABC...yearly_tr`    |
| `premium_lifetime_tr`  | `price_1ABC...lifetime_tr`  |
| `premium_monthly_usd`  | `price_1ABC...monthly_usd`  |
| `premium_yearly_usd`   | `price_1ABC...yearly_usd`   |
| `premium_lifetime_usd` | `price_1ABC...lifetime_usd` |

### Stripe Dashboard Ayarları

```
1. Stripe > Products
   └── Subify Premium
       ├── Price: ₺49/month (TRY, recurring)
       ├── Price: ₺499/year (TRY, recurring)
       ├── Price: ₺699 one-time (TRY)
       ├── Price: $4.99/month (USD, recurring)
       ├── Price: $49.99/year (USD, recurring)
       └── Price: $69.99 one-time (USD)

2. Stripe > Developers > Webhooks
   └── Endpoint: https://api.subify.app/api/webhooks/stripe
       Events: checkout.session.completed, invoice.paid, customer.subscription.*
```

---

## 🔔 Webhook Konfigürasyonu

### RevenueCat Webhook

```
URL: https://api.subify.app/api/webhooks/revenuecat
Auth: Bearer Token veya Shared Secret
```

### Dinlenecek Events

| Event              | Aksiyon                                               |
| ------------------ | ----------------------------------------------------- |
| `INITIAL_PURCHASE` | profiles.plan = 'premium', entitlements_cache insert  |
| `RENEWAL`          | plan_renews_at güncelle, entitlements_cache güncelle  |
| `CANCELLATION`     | Hiçbir şey (subscription aktif kalır expiry'ye kadar) |
| `EXPIRATION`       | profiles.plan = 'free', entitlements_cache delete     |
| `BILLING_ISSUE`    | Opsiyonel: Kullanıcıya email gönder                   |
| `PRODUCT_CHANGE`   | entitlements_cache güncelle                           |

### Webhook Payload Örneği

```json
{
  "api_version": "1.0",
  "event": {
    "type": "INITIAL_PURCHASE",
    "id": "evt_123456",
    "app_user_id": "user-guid-here",
    "product_id": "premium_monthly_tr",
    "entitlement_ids": ["premium"],
    "purchased_at_ms": 1704067200000,
    "expiration_at_ms": 1706745600000,
    "store": "APP_STORE",
    "environment": "PRODUCTION"
  }
}
```

### Webhook Handler Pseudocode

```csharp
[HttpPost("api/webhooks/revenuecat")]
public async Task<IActionResult> HandleRevenueCatWebhook([FromBody] RevenueCatEvent payload)
{
    // 1. Validate webhook signature
    if (!ValidateSignature(Request.Headers))
        return Unauthorized();

    // 2. Find user
    var user = await _userManager.FindByIdAsync(payload.AppUserId);
    if (user == null) return NotFound();

    // 3. Process event
    switch (payload.Event.Type)
    {
        case "INITIAL_PURCHASE":
        case "RENEWAL":
            await UpdateUserToPremium(user, payload);
            break;

        case "EXPIRATION":
            await DowngradeUserToFree(user);
            break;
    }

    // 4. Invalidate cache
    await _cache.RemoveAsync($"entitlement:{user.Id}");

    return Ok();
}
```

---

## 📱 Flutter SDK Entegrasyonu

### Paketi Ekle

```yaml
# pubspec.yaml
dependencies:
  purchases_flutter: ^6.0.0
```

### Başlatma

```dart
// main.dart
import 'package:purchases_flutter/purchases_flutter.dart';

Future<void> initRevenueCat() async {
  await Purchases.setLogLevel(LogLevel.debug);

  PurchasesConfiguration configuration;
  if (Platform.isIOS) {
    configuration = PurchasesConfiguration('appl_XXXXXXXX');
  } else if (Platform.isAndroid) {
    configuration = PurchasesConfiguration('goog_XXXXXXXX');
  }

  await Purchases.configure(configuration);
}
```

### Kullanıcı Tanımlama

```dart
// Login sonrası
Future<void> identifyUser(String userId) async {
  await Purchases.logIn(userId);
}

// Logout
Future<void> logoutUser() async {
  await Purchases.logOut();
}
```

### Paywall Gösterme

```dart
Future<void> showPaywall() async {
  try {
    final offerings = await Purchases.getOfferings();
    final current = offerings.current;

    if (current != null) {
      // Paywall UI göster
      showModalBottomSheet(
        context: context,
        builder: (_) => PaywallWidget(offering: current),
      );
    }
  } catch (e) {
    print('Error fetching offerings: $e');
  }
}
```

### Satın Alma

```dart
Future<bool> purchasePackage(Package package) async {
  try {
    final result = await Purchases.purchasePackage(package);

    if (result.customerInfo.entitlements.all['premium']?.isActive ?? false) {
      // Premium aktif - UI güncelle
      return true;
    }
    return false;
  } on PurchasesErrorCode catch (e) {
    if (e != PurchasesErrorCode.purchaseCancelledError) {
      // Error handling
    }
    return false;
  }
}
```

### Entitlement Kontrolü

```dart
Future<bool> isPremium() async {
  try {
    final customerInfo = await Purchases.getCustomerInfo();
    return customerInfo.entitlements.all['premium']?.isActive ?? false;
  } catch (e) {
    return false;
  }
}
```

---

## 🌐 Next.js SDK Entegrasyonu

### Paketi Ekle

```bash
npm install @revenuecat/purchases-js
```

### Başlatma

```typescript
// lib/revenuecat.ts
import Purchases from "@revenuecat/purchases-js";

export async function initRevenueCat() {
  Purchases.configure("rcb_XXXXXXXX"); // Web API Key
}
```

### Checkout Başlatma

```typescript
// API Route: /api/billing/checkout
export async function POST(req: Request) {
  const { plan } = await req.json();
  const userId = getCurrentUserId();

  // RevenueCat API ile checkout session oluştur
  const response = await fetch(
    "https://api.revenuecat.com/v1/subscribers/" + userId + "/checkout_url",
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${REVENUECAT_API_KEY}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        price_id: plan, // e.g., 'premium_monthly_tr'
        success_url: `${APP_URL}/app?checkout=success`,
        cancel_url: `${APP_URL}/app?checkout=cancelled`,
      }),
    }
  );

  const { checkout_url } = await response.json();
  return Response.json({ checkoutUrl: checkout_url });
}
```

---

## ✅ Konfigürasyon Checklist

### RevenueCat Dashboard

- [ ] Project oluştur
- [ ] App Store Connect entegrasyonu
- [ ] Google Play Console entegrasyonu
- [ ] Stripe entegrasyonu
- [ ] Entitlement tanımla (premium)
- [ ] Products ekle (6 ürün)
- [ ] Offerings oluştur
- [ ] Webhook URL ekle
- [ ] API keys al

### App Store Connect

- [ ] Subscription Group oluştur
- [ ] Subscription products ekle
- [ ] In-App Purchase products ekle
- [ ] Shared Secret oluştur
- [ ] RevenueCat'e bağla

### Google Play Console

- [ ] Subscriptions oluştur
- [ ] In-app products ekle
- [ ] Service Account oluştur
- [ ] RevenueCat'e bağla

### Stripe

- [ ] Products oluştur
- [ ] Prices ekle
- [ ] Webhook endpoint ekle
- [ ] RevenueCat'e bağla

### Backend

- [ ] Webhook handler implement et
- [ ] Signature verification ekle
- [ ] Cache invalidation ekle
- [ ] Error logging ekle

### Mobile

- [ ] purchases_flutter paketi ekle
- [ ] RevenueCat başlat
- [ ] User identification
- [ ] Paywall UI
- [ ] Entitlement check

### Web

- [ ] API route'ları implement et
- [ ] Checkout flow
- [ ] Success/Cancel handling
