# 📋 Subify Development Task List (ASP.NET Core + Expo)

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

---

## 2. 📱 Mobile (Expo / React Native)

### 2.1 Kurulum
- [ ] `npx create-expo-app@latest subify-mobile --template blank-typescript`
- [ ] Klasör yapısı: `app`, `components`, `services`, `store`.
- [ ] React Native Paper veya NativeWind kurulumu.

### 2.2 Auth Flow
- [ ] Login Screen & Register Screen tasarımları.
- [ ] Axios Interceptor kurulumu (JWT'yi header'a ekle, 401 gelirse logout yap).
- [ ] SecureStore ile Token saklama.

### 2.3 Dashboard & Abonelikler
- [ ] Dashboard UI: Toplam harcama kartı, liste.
- [ ] "Add Subscription" Modal (Bottom Sheet).
- [ ] Abonelik Detay ekranı.

### 2.4 Premium Features
- [ ] Paywall Modalı tasarımı (Upgrade to Premium).
- [ ] AI Suggestion Ekranı (Loading state + Sonuç kartları).
- [ ] Push Notification izinleri ve testi.

---

## 3. 🌐 Landing Page (Next.js)

### 3.1 Basit Tanıtım Sitesi
- [ ] Hero Section: "Aboneliklerini Cepten Yönet".
- [ ] App Store / Play Store butonları (veya "Coming Soon" formu).
- [ ] Features Section.
- [ ] Pricing Table.