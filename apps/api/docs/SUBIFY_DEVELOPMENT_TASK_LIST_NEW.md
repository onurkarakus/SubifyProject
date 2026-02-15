# 📋 Subify Development Task List (ASP.NET Core + Flutter + Next.js)

---

## 1. 🔐 Auth & User Management (Backend + Web + Mobile)

### 1.1 Backend (ASP.NET Core Web API)
- [ ] **Kullanıcı Kaydı**
  - `POST /api/auth/register`: Yeni kullanıcı kaydı, e-posta doğrulama maili gönderimi
  - Kullanıcıya doğrulama linki içeren e-posta şablonu (TR/EN)
  - Kullanıcı kaydı sonrası profil oluşturma (varsayılan plan: free)
- [ ] **E-posta Doğrulama**
  - `GET /api/auth/confirm-email`: userId ve code ile e-posta doğrulama
  - Doğrulama sonrası kullanıcı aktif/pasif flag güncelleme
- [ ] **Doğrulama Mailini Tekrar Gönder**
  - `POST /api/auth/resend-confirmation-email`: Kullanıcıya yeni doğrulama maili gönderimi
  - Rate limit ve abuse kontrolü
- [ ] **Giriş & Token Yönetimi**
  - `POST /api/auth/login`: Giriş, access + refresh token üretimi
  - JWT ayarları (issuer, audience, secret, expiry)
  - Refresh token rotation ve güvenli saklama
- [ ] **Token Yenileme**
  - `POST /api/auth/refresh-token`: Refresh token ile yeni access token üretimi
  - Token zinciri ve revoke işlemleri
- [ ] **Çıkış**
  - `POST /api/auth/logout`: Refresh token revoke, oturum sonlandırma
- [ ] **Şifre Sıfırlama**
  - `POST /api/auth/forgot-password`: Şifre sıfırlama isteği, e-posta ile token gönderimi
  - `POST /api/auth/reset-password`: Token ile yeni şifre belirleme
  - Şifre sıfırlama e-posta şablonu (TR/EN)
- [ ] **Hata Yönetimi**
  - Tüm endpointlerde RFC 7807 ProblemDetails ile hata dönüşü
  - Giriş/şifre sıfırlama/aktivasyon hatalarında detaylı mesajlar
- [ ] **EF Core & Identity**
  - Kullanıcı, profil, refresh token tabloları ve migration’ları
  - Index ve güvenlik ayarları (unique email, rate limit, brute force koruması)
- [ ] **Testler**
  - Unit ve integration testler: Auth akışları, token rotation, e-posta doğrulama, şifre sıfırlama

### 1.2 Web Frontend (Next.js)
- [ ] **Kayıt Formu**
  - E-posta, şifre, şifre tekrar alanları
  - Kayıt sonrası doğrulama maili gönderildi ekranı
- [ ] **E-posta Doğrulama Ekranı**
  - userId ve code parametreli doğrulama sonucu mesajı
- [ ] **Doğrulama Mailini Tekrar Gönder**
  - Tekrar gönder butonu, rate limit uyarısı
- [ ] **Giriş Formu**
  - E-posta, şifre alanları, JWT httpOnly cookie yönetimi
  - Hatalı girişte detaylı mesajlar
- [ ] **Şifre Sıfırlama**
  - Şifre sıfırlama isteği formu (e-posta gir)
  - Şifre sıfırlama linki ile yeni şifre belirleme ekranı
- [ ] **Çıkış**
  - Oturum temizliği, yönlendirme
- [ ] **UI/UX**
  - Auth akışlarında loading, hata ve bilgi mesajları
  - Dil desteği (TR/EN), i18n ile metinler

### 1.3 Mobile (Flutter)
- [ ] **Auth Ekranları**
  - Login, Register, Forgot Password sayfaları
  - Şifre sıfırlama linki ile yeni şifre belirleme
- [ ] **Dio Interceptor**
  - JWT header ekleme, 401 durumunda refresh token rotation
- [ ] **Secure Storage**
  - Access ve refresh token’ların güvenli saklanması
- [ ] **Doğrulama ve Şifre Sıfırlama**
  - E-posta doğrulama ve şifre sıfırlama akışları için yönlendirme ve mesajlar
- [ ] **Testler**
  - Widget ve entegrasyon testleri: Auth akışları, token yenileme

---

## 2. 📦 Subscription Management

### 2.1 Backend
- [ ] **Subscription CRUD**
  - `GET /api/subscriptions`: Listeleme (filtre: includeArchived, category)
  - `POST /api/subscriptions`: Ekleme (freemium limiti: max 3 aktif)
  - `GET /api/subscriptions/{id}`: Detay
  - `PUT /api/subscriptions/{id}`: Güncelleme
  - `DELETE /api/subscriptions/{id}`: Soft delete (archived flag)
  - `GET /api/subscriptions/upcoming`: Yaklaşan ödemeler
- [ ] **Veri Modeli**
  - Subscription entity, archived, category, price, currency, billing_cycle, next_renewal_date alanları
  - Indexler: user_id, archived, next_renewal_date
- [ ] **Testler**
  - CRUD ve limit logic için unit/integration testler

### 2.2 Web Frontend
- [ ] **Dashboard**
  - Aktif abonelik listesi, toplam harcama kartı
  - Abonelik ekleme/güncelleme/archivleme UI
  - Free kullanıcıda 3 abonelik limiti ve CTA
- [ ] **Subscription Table/Detail**
  - Table view, detay modalı, kategori ve döngü gösterimi

### 2.3 Mobile (Flutter)
- [ ] **Dashboard & List**
  - Toplam harcama kartı, SliverList ile abonelikler
- [ ] **Add/Edit Subscription**
  - Modal/bottom sheet ile ekleme/güncelleme
- [ ] **Detay Ekranı**
  - Abonelik detayları, arşivleme

---

## 3. 💎 Premium Gating & Payments

### 3.1 Backend
- [ ] **RevenueCat Entegrasyonu**
  - Web checkout başlatma: `POST /api/billing/checkout`
  - Webhook handler: `POST /api/webhooks/revenuecat`
  - Entitlements_cache güncelleme, plan sync
- [ ] **Premium Kontrolü**
  - Endpointlerde entitlement ve plan kontrolü (free/premium)
  - Paywall logic: premium gerektiren endpointlerde 403/CTA
- [ ] **Payments Endpointleri**
  - `GET /api/payments/status`: Premium durum sorgusu

### 3.2 Web Frontend
- [ ] **Paywall Modalı**
  - Premium özelliklerde blur + CTA
  - Fiyatlandırma, avantajlar, RevenueCat checkout linki
- [ ] **Entitlement UI**
  - Premium durumuna göre UI güncelleme

### 3.3 Mobile (Flutter)
- [ ] **Paywall**
  - purchases_flutter SDK ile paywall gösterimi
  - Premium avantajları ve fiyatlandırma
- [ ] **Entitlement Kontrolü**
  - RevenueCat SDK ile entitlement sync

---

## 4. 🤖 AI & Reporting

### 4.1 Backend
- [ ] **OpenAI API Entegrasyonu**
  - Server-side prompt, rate limit, logging
  - `POST /api/ai/analyze`: Analiz ve öneri üret (premium)
  - `GET /api/ai/history`: Geçmiş öneriler
  - `POST /api/ai/feedback`: Geri bildirim
- [ ] **Raporlama Endpointleri**
  - `GET /api/reports/monthly-spend`: Aylık harcama grafiği
  - `GET /api/reports/category-breakdown`: Kategori dağılımı
  - `GET /api/reports/currency-distribution`: Para birimi dağılımı

### 4.2 Web Frontend
- [ ] **AI Öneri UI**
  - AI’dan analiz al butonu, sonuç kartları, loading state
  - Free kullanıcıda paywall/blur
- [ ] **Raporlar**
  - Kategori, aylık, para birimi grafikleri

### 4.3 Mobile (Flutter)
- [ ] **AI Suggestion Ekranı**
  - Analiz tetikleme, loading, sonuç kartları
- [ ] **Raporlar**
  - Kategori ve aylık harcama grafikleri

---

## 5. 🔔 Notifications

### 5.1 Backend
- [ ] **E-posta Bildirimleri**
  - SMTP/Resend ile ödeme hatırlatma, doğrulama, şifre sıfırlama
  - Locale-aware şablonlar (TR/EN)
- [ ] **Push Bildirimleri**
  - FCM entegrasyonu, premium kontrolü
  - Device token kaydı: `POST /api/profile/device-token`
- [ ] **Background Jobs**
  - Hangfire/Quartz ile günlük ödeme kontrolü ve bildirim gönderimi

### 5.2 Mobile (Flutter)
- [ ] **FCM Kurulumu**
  - firebase_messaging ile push izinleri ve token yönetimi
  - Push notification ayarları

---

## 6. 🛠️ Admin & System

### 6.1 Backend
- [ ] **AdminController**
  - `[Authorize(Roles = "Admin")]` ile koruma
  - `GET /api/admin/users`: Kullanıcı listesi, arama
  - `GET /api/admin/stats`: Kullanıcı, abonelik, gelir istatistikleri
  - `GET /api/admin/transactions`: Ödeme geçmişi
  - `GET /api/admin/logs`: Sistem logları
  - `GET /api/admin/feedback`: AI öneri geri bildirimleri
- [ ] **SystemController**
  - `GET /api/system/currencies`: Döviz kurları
  - `GET /api/system/health`: Health check

### 6.2 Web Frontend
- [ ] **Admin Panel**
  - Kullanıcı tablosu, yasaklama/silme
  - Transactions tablosu, revenue chart, error log viewer

---

## 7. 🩺 Observability, Security & DevOps

### 7.1 Backend & Infra
- [ ] **Security**
  - HTTPS, JWT kısa ömür, refresh rotation, revoke
  - Input validation (FluentValidation), output encoding, CORS
  - Webhook signature validation (RevenueCat)
  - DB backup, secrets yönetimi (.env, perms)
- [ ] **Observability**
  - OpenTelemetry ile tracing/logging/metrics
  - Serilog JSON log formatı, Prometheus/otel-collector entegrasyonu
  - Health checks: DB, cache, RevenueCat, SMTP
- [ ] **DevOps**
  - Docker Compose: reverse-proxy, api, db, worker, otel-collector, frontend
  - CI: build & test (dotnet, flutter, next), docker build, deploy
  - EF Core migration otomasyonu
  - Uptime monitor kurulumu

---

## 8. 🧪 Testing & Documentation

- [ ] **Backend**
  - Unit, integration, contract testler (OpenAPI/Swagger, Schemathesis)
- [ ] **Web**
  - Component, e2e testler (Playwright)
- [ ] **Mobile**
  - Widget, API entegrasyon testleri
- [ ] **Dokümantasyon**
  - API dokümantasyonu (Swagger/OpenAPI)
  - Kullanıcı ve admin rehberleri
  - Deployment ve migration talimatları

---