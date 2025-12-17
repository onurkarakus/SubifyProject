# 📋 Subify Development Task List (ASP.NET Core + Flutter)

## 1. 🔙 Backend (ASP.NET Core Web API)

### 1.1 Proje Kurulumu
- [ ] Solution oluştur: `dotnet new sln -n Subify`
- [ ] Web API projesi: `dotnet new webapi -n Subify.API`
- [ ] Class Library (Core/Entities): `dotnet new classlib -n Subify.Core`
- [ ] Class Library (Data/EF): `dotnet new classlib -n Subify.Data`
- [ ] Docker Support ekle (Linux mode)

### 1.2 Database & Entity Framework
- [ ] Supabase'den Connection String al (`User ID`, `Password` ile).
- [ ] Entity'leri oluştur: `User`, `Subscription`, `NotificationSetting`, `AiLog`.
- [ ] DbContext konfigürasyonu (`SubifyDbContext`).
- [ ] Migration oluştur ve uygula: `dotnet ef migrations add InitialCreate`, `dotnet ef database update`.

### 1.3 Auth (Identity + JWT)
- [ ] `Microsoft.AspNetCore.Identity` entegrasyonu.
- [ ] JWT Ayarları (`appsettings.json` içine Issuer, Audience, Secret).
- [ ] Auth Controller: Login, Register, RefreshToken endpointleri.
- [ ] Password Hashing (Identity zaten halleder ama custom logic varsa ayarla).

### 1.4 Subscription CRUD (Controller)
- [ ] `GET /api/subscriptions`: Listeleme.
- [ ] `POST /api/subscriptions`: Ekleme (Freemium logic: User.Subscriptions.Count >= 3 ise 403 dön).
- [ ] `GET /api/subscriptions/{id}`: Detay.
- [ ] `PUT /api/subscriptions/{id}`: Güncelleme.
- [ ] `DELETE /api/subscriptions/{id}`: Soft delete (`IsDeleted = true`).

### 1.5 AI & Raporlama
- [ ] OpenAI Service entegrasyonu (`SemanticKernel` veya direkt `HttpClient`).
- [ ] `POST /api/ai/suggestions`: Kullanıcı verisini topla -> Prompt oluştur -> OpenAI'a at -> Cevabı dön.
- [ ] Cron Job (Hangfire veya Quartz.NET): Günlük ödeme kontrolü ve mail gönderimi.

### 1.6 Admin Modülü (Backend)
- [ ] `AdminController` oluştur (`[Authorize(Roles = "Admin")]`).
- [ ] `GET /api/admin/users`: Kullanıcı listesi ve arama.
- [ ] `GET /api/admin/stats`: Basit istatistikler (Count sorguları).
- [ ] `GET /api/admin/transactions`: Ödeme geçmişi listesi (`billing_sessions` join `users`).

---

## 2. 📱 Mobile (Flutter)

### 2.1 Kurulum
- [ ] Flutter projesi oluştur: `flutter create subify_mobile --org com.subify.app`
- [ ] Klasör yapısı: `lib/core`, `lib/features`, `lib/shared`.
- [ ] Paketleri ekle: `dio`, `flutter_riverpod`, `go_router`, `flutter_secure_storage`, `purchases_flutter`.

### 2.2 Auth Flow
- [ ] Login, Register ve Forgot Password ekranları.
- [ ] Dio Interceptor kurulumu (JWT header ekleme, 401 refresh token rotation logic).
- [ ] Secure Storage servisi yazımı.

### 2.3 Dashboard & Abonelikler
- [ ] Dashboard UI: Toplam harcama kartı, SliverList yapısı.
- [ ] "Add Subscription" Modal (Bottom Sheet).
- [ ] Abonelik Detay ekranı.

### 2.4 Premium Features
- [ ] Paywall Modalı tasarımı (Upgrade to Premium).
- [ ] AI Suggestion Ekranı (Loading state + Sonuç kartları).
- [ ] Firebase Cloud Messaging (FCM) kurulumu ve izinler.

---

## 3. 🌐 Web App (Next.js)

### 3.1 Landing Page (Public)
- [ ] Hero Section: "Aboneliklerini Cepten Yönet".
- [ ] App Store / Play Store butonları (veya "Coming Soon" formu).
- [ ] Features Section.
- [ ] Pricing Table.

### 3.2 User App (Protected)
- [ ] Auth Middleware (Login kontrolü).
- [ ] Dashboard UI (Mobile benzeri grid yapı).
- [ ] Subscription Management (Table view).

### 3.3 Admin Panel (Role: Admin)
- [ ] Admin Middleware (Role kontrolü).
- [ ] Users Table (Listeleme, Yasaklama/Silme).
- [ ] Transactions Table (Kim, Ne Zaman, Ne Kadar Ödedi?).
- [ ] Revenue Chart (Basit grafik).
- [ ] Error Logs Viewer (Basit liste).