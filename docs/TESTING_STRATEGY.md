# Subify - Test Stratejisi

Bu doküman, Subify projesinin test yaklaşımını ve coverage hedeflerini tanımlar.

> **Referanslar:**
>
> - [API_CONTRACTS.md](./API_CONTRACTS.md)
> - [SEQUENCE_DIAGRAMS.md](./diagrams/SEQUENCE_DIAGRAMS.md)
> - Operasyonel task checklist: [SUBIFY_OS_TASK_LIST.md](./SUBIFY_OS_TASK_LIST.md) · Faz 12

---

## Subify OS — mevcut test düzeni (2026-08-01)

> Aşağıdaki “legacy freemium” örnekleri tarihseldir. **OS ürününde subscription limiti yok.**

| Proje | Rol | Komut |
| ----- | --- | ----- |
| `api/Subify.Domain.Tests` | Domain pure logic (math, XOR, seeds) | `dotnet test api/Subify.Domain.Tests` |
| `api/Subify.Application.Tests` | FluentValidation unit | `dotnet test api/Subify.Application.Tests` |
| `api/Subify.Api.Tests` | Handler harness + `Integration/` WAF | `dotnet test api/Subify.Api.Tests` |
| Full API solution | Hepsi | `dotnet test api/Subify.slnx` |
| `web/e2e` | Playwright smoke (P2, live stack) | `cd web && npx playwright test` |

**Integration factory:** `SubifyWebApplicationFactory` — env `Testing`, SQLite shared memory, hosted services off, `EnsureCreated` + role/settings seed.  
**Kritik akışlar:** auth (setup→login→refresh→logout), subscription isolation, admin 403, no freemium limit.

---

## 📋 Test Piramidi

```
                    ┌─────────────────┐
                    │   E2E Tests     │  ← Az sayıda, kritik akışlar
                    │   (Playwright)  │
                    ├─────────────────┤
                    │ Integration     │  ← Orta sayıda, API sözleşmeleri
                    │ Tests (xUnit)   │
                    ├─────────────────┤
                    │   Unit Tests    │  ← Çok sayıda, iş mantığı
                    │   (xUnit/Jest)  │
                    └─────────────────┘
```

---

## 🎯 Coverage Hedefleri

| Katman                    | Hedef Coverage | Öncelik   |
| ------------------------- | -------------- | --------- |
| **Domain/Business Logic** | 80%+           | 🔴 Kritik |
| **Services**              | 70%+           | 🟠 Yüksek |
| **Controllers**           | 60%+           | 🟡 Orta   |
| **Infrastructure**        | 50%+           | 🟢 Düşük  |

---

## 1️⃣ Unit Tests

### Backend (C# / xUnit)

#### Test Edilecek Alanlar

| Alan                       | Dosya                    | Test Senaryoları                  |
| -------------------------- | ------------------------ | --------------------------------- |
| **Subscription Limit**     | `SubscriptionService.cs` | Free limit (3), Premium unlimited |
| **User Share Calculation** | `Subscription.cs`        | `Price / SharedWithCount`         |
| **Token Validation**       | `TokenService.cs`        | Expiry, revocation, rotation      |
| **AI Rate Limiting**       | `AiService.cs`           | 5/min, 20/day limits              |
| **Budget Warning**         | `BudgetService.cs`       | `monthlyTotal > budget`           |
| **Validators**             | `*Validator.cs`          | FluentValidation rules            |

#### Örnek Unit Test

```csharp
// SubscriptionServiceTests.cs
public class SubscriptionServiceTests
{
    [Fact]
    public async Task AddSubscription_FreeUser_WithThreeSubscriptions_ShouldThrowLimit()
    {
        // Arrange
        var mockRepo = new Mock<ISubscriptionRepository>();
        mockRepo.Setup(r => r.CountActiveByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(3);

        var service = new SubscriptionService(mockRepo.Object);
        var request = new CreateSubscriptionRequest { Name = "Netflix", Price = 149.99m };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionLimitException>(
            () => service.CreateAsync(freeUserId, request)
        );

        Assert.Equal("subscription_limit_reached", exception.ErrorCode);
    }

    [Fact]
    public async Task AddSubscription_PremiumUser_ShouldSucceed()
    {
        // Arrange
        var mockRepo = new Mock<ISubscriptionRepository>();
        mockRepo.Setup(r => r.CountActiveByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(10); // Already has 10

        var service = new SubscriptionService(mockRepo.Object);
        var request = new CreateSubscriptionRequest { Name = "Spotify", Price = 59.99m };

        // Act
        var result = await service.CreateAsync(premiumUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Spotify", result.Name);
    }

    [Theory]
    [InlineData(100, 4, 25)]    // 100 TL / 4 kişi = 25 TL
    [InlineData(149.99, 1, 149.99)]  // Paylaşım yok
    [InlineData(200, 5, 40)]    // 200 TL / 5 kişi = 40 TL
    public void UserShare_ShouldCalculateCorrectly(decimal price, int sharedWith, decimal expected)
    {
        // Arrange
        var subscription = new Subscription
        {
            Price = price,
            SharedWithCount = sharedWith
        };

        // Act & Assert
        Assert.Equal(expected, subscription.UserShare);
    }
}
```

### Frontend - Flutter (Dart / flutter_test)

```dart
// subscription_provider_test.dart
void main() {
  group('SubscriptionProvider', () {
    test('should calculate monthly total correctly', () {
      // Arrange
      final subscriptions = [
        Subscription(price: 149.99, billingCycle: 'monthly', sharedWithCount: 4),
        Subscription(price: 59.99, billingCycle: 'monthly', sharedWithCount: 1),
        Subscription(price: 240, billingCycle: 'yearly', sharedWithCount: 1),
      ];

      // Act
      final monthlyTotal = calculateMonthlyTotal(subscriptions);

      // Assert
      // 149.99/4 + 59.99 + 240/12 = 37.50 + 59.99 + 20 = 117.49
      expect(monthlyTotal, closeTo(117.49, 0.01));
    });

    test('should filter by category correctly', () {
      // Arrange
      final subscriptions = [
        Subscription(name: 'Netflix', categorySlug: 'streaming'),
        Subscription(name: 'Spotify', categorySlug: 'music'),
        Subscription(name: 'Disney+', categorySlug: 'streaming'),
      ];

      // Act
      final streaming = filterByCategory(subscriptions, 'streaming');

      // Assert
      expect(streaming.length, 2);
      expect(streaming.map((s) => s.name), containsAll(['Netflix', 'Disney+']));
    });
  });
}
```

### Frontend - Next.js (Jest)

```typescript
// utils/calculations.test.ts
describe("Subscription Calculations", () => {
  it("should calculate user share correctly", () => {
    expect(calculateUserShare(100, 4)).toBe(25);
    expect(calculateUserShare(149.99, 1)).toBe(149.99);
    expect(calculateUserShare(200, 5)).toBe(40);
  });

  it("should convert yearly to monthly", () => {
    expect(yearlyToMonthly(120)).toBe(10);
    expect(yearlyToMonthly(499)).toBeCloseTo(41.58, 2);
  });

  it("should format currency correctly", () => {
    expect(formatCurrency(149.99, "TRY")).toBe("₺149,99");
    expect(formatCurrency(4.99, "USD")).toBe("$4.99");
  });
});
```

---

## 2️⃣ Integration Tests

### API Integration Tests (xUnit + WebApplicationFactory)

```csharp
// AuthControllerIntegrationTests.cs
public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new { email = "test@example.com", password = "SecureP@ss123", fullName = "Test User" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(content?.UserId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var request = new { email = "existing@example.com", password = "SecureP@ss123", fullName = "Test User" };
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange - First register and confirm email
        // ... setup code ...

        var request = new { email = "user@example.com", password = "SecureP@ss123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(content?.AccessToken);
        Assert.NotNull(content?.RefreshToken);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/subscriptions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

### Webhook Integration Tests

```csharp
// RevenueCatWebhookTests.cs
public class RevenueCatWebhookTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task InitialPurchase_ShouldUpgradeUserToPremium()
    {
        // Arrange
        var userId = await CreateTestUser();
        var webhook = CreateWebhookPayload("INITIAL_PURCHASE", userId, "premium_monthly_tr");

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/revenuecat", webhook);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await GetProfile(userId);
        Assert.Equal("premium", profile.Plan);
    }

    [Fact]
    public async Task Expiration_ShouldDowngradeUserToFree()
    {
        // Arrange
        var userId = await CreatePremiumUser();
        var webhook = CreateWebhookPayload("EXPIRATION", userId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks/revenuecat", webhook);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await GetProfile(userId);
        Assert.Equal("free", profile.Plan);
    }
}
```

---

## 3️⃣ End-to-End Tests

### Playwright (Web)

```typescript
// e2e/auth.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Authentication Flow", () => {
  test("should register a new user", async ({ page }) => {
    await page.goto("/register");

    await page.fill('[data-testid="email"]', "newuser@example.com");
    await page.fill('[data-testid="password"]', "SecureP@ss123");
    await page.fill('[data-testid="fullName"]', "Test User");
    await page.click('[data-testid="submit"]');

    await expect(page).toHaveURL("/confirm-email");
    await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
  });

  test("should login and see dashboard", async ({ page }) => {
    await page.goto("/login");

    await page.fill('[data-testid="email"]', "existing@example.com");
    await page.fill('[data-testid="password"]', "SecureP@ss123");
    await page.click('[data-testid="submit"]');

    await expect(page).toHaveURL("/app");
    await expect(page.locator('[data-testid="welcome-message"]')).toContainText(
      "Merhaba"
    );
  });
});

// e2e/subscription.spec.ts
test.describe("Subscription Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsTestUser(page);
  });

  test("should add a new subscription", async ({ page }) => {
    await page.goto("/app/subscriptions");
    await page.click('[data-testid="add-subscription"]');

    await page.fill('[data-testid="name"]', "Netflix");
    await page.fill('[data-testid="price"]', "149.99");
    await page.selectOption('[data-testid="category"]', "streaming");
    await page.click('[data-testid="save"]');

    await expect(
      page
        .locator('[data-testid="subscription-card"]')
        .filter({ hasText: "Netflix" })
    ).toBeVisible();
  });

  test("free user should see limit warning at 4th subscription", async ({
    page,
  }) => {
    // Add 3 subscriptions first
    await addSubscription(page, "Netflix");
    await addSubscription(page, "Spotify");
    await addSubscription(page, "Disney+");

    // Try to add 4th
    await page.click('[data-testid="add-subscription"]');
    await page.fill('[data-testid="name"]', "HBO Max");
    await page.fill('[data-testid="price"]', "79.99");
    await page.click('[data-testid="save"]');

    await expect(page.locator('[data-testid="paywall-modal"]')).toBeVisible();
    await expect(page.locator('[data-testid="limit-message"]')).toContainText(
      "Premium"
    );
  });
});
```

### Flutter Integration Tests

```dart
// integration_test/app_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('Authentication Flow', () {
    testWidgets('should login and navigate to dashboard', (tester) async {
      await tester.pumpWidget(const MyApp());
      await tester.pumpAndSettle();

      // Login
      await tester.enterText(find.byKey(Key('email')), 'test@example.com');
      await tester.enterText(find.byKey(Key('password')), 'SecureP@ss123');
      await tester.tap(find.byKey(Key('loginButton')));
      await tester.pumpAndSettle();

      // Verify dashboard
      expect(find.text('Ana Sayfa'), findsOneWidget);
      expect(find.byKey(Key('monthlyTotal')), findsOneWidget);
    });
  });

  group('Subscription Flow', () {
    testWidgets('should add subscription', (tester) async {
      await loginAsTestUser(tester);

      await tester.tap(find.byKey(Key('addSubscription')));
      await tester.pumpAndSettle();

      await tester.enterText(find.byKey(Key('subscriptionName')), 'Netflix');
      await tester.enterText(find.byKey(Key('price')), '149.99');
      await tester.tap(find.byKey(Key('saveButton')));
      await tester.pumpAndSettle();

      expect(find.text('Netflix'), findsOneWidget);
    });
  });
}
```

---

## 🧪 Test Ortamları

### Local Development

```bash
# Backend
cd src/Subify.Api
dotnet test --filter "Category=Unit"

# Frontend (Next.js)
cd src/web
npm run test

# Flutter
cd src/mobile
flutter test
```

### CI/CD Pipeline

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports: ["1433:1433"]
        env:
          SA_PASSWORD: YourStrong@Passw0rd
          ACCEPT_EULA: Y
      redis:
        image: redis:7
        ports: ["6379:6379"]

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"
      - run: dotnet restore
      - run: dotnet test --collect:"XPlat Code Coverage"
      - uses: codecov/codecov-action@v3

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: "20"
      - run: npm ci
      - run: npm run test:coverage

  e2e-tests:
    runs-on: ubuntu-latest
    needs: [backend-tests, frontend-tests]
    steps:
      - uses: actions/checkout@v4
      - run: docker-compose up -d
      - run: npx playwright install
      - run: npx playwright test
```

---

## 📊 Test Metrikleri

| Metrik                    | Hedef   | Ölçüm             |
| ------------------------- | ------- | ----------------- |
| Unit Test Coverage        | 80%+    | Codecov           |
| Integration Test Coverage | 70%+    | Codecov           |
| E2E Critical Paths        | 100%    | Playwright Report |
| Test Execution Time       | < 5 min | CI/CD Pipeline    |
| Flaky Test Rate           | < 1%    | CI/CD History     |

---

## ✅ Test Checklist

### MVP İçin Zorunlu

- [ ] Auth flow unit tests
- [ ] Subscription limit tests
- [ ] Token refresh tests
- [ ] Webhook handler tests
- [ ] Login E2E test
- [ ] Add subscription E2E test

### Nice to Have

- [ ] AI rate limiting tests
- [ ] Cache invalidation tests
- [ ] Push notification tests
- [ ] Full E2E payment flow
- [ ] Performance tests
- [ ] Security tests (OWASP)
