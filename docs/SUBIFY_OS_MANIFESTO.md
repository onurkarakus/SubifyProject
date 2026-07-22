Subify Open Source (Subify OS) - Mimari ve Tasarım Anayasası (Manifesto)

Sürüm: 2.0 (Açık Kaynak ve Self-Hosted Özel) Durum: Kabul Edildi (Sıfırdan
Geliştirme Temeli) Hedef Kitle: Bireysel Kullanıcılar, Aileler ve Topluluklar

1. Temel Vizyon ve Strateji

Subify; kapalı kaynaklı, freemium iş modeline sahip bir SaaS ürünü olmaktan
çıkarılarak; topluluk odaklı, tamamen ücretsiz, kendi sunucusunda
barındırılabilir (Self-Hosted) ve açık kaynaklı bir abonelik/finans yönetim
aracına dönüştürülmüştür.
Stratejik Sütunlar

  - Sıfır Teknik Borç (Sıfırdan Başlangıç): Eski kod tabanına ait tüm
    kalıntılar, MSSQL ve SaaS mimarisinin getirdiği karmaşıklıklar temizlenmiş;
    temiz bir sayfa (Greenfield projesi) açılmıştır.
  - Gizlilik ve Kontrol: Kullanıcıların verileri üçüncü parti bulut
    sağlayıcılarında değil, kendi kontrol ettikleri Docker konteynerleri
    içerisinde, kendi yerel veritabanlarında tutulur.
  - Genişletilebilirlik ve Yapay Zeka: Temel finansal takip özellikleri
    tamamlandıktan sonra, kullanıcının kendi sağlayacağı API anahtarı üzerinden
    çalışacak yerel/bulut yapay zeka analiz motoru entegre edilecektir.
2. Teknoloji Yığını (Tech Stack)

| Katman | Teknoloji | Seçim Gerekçesi | | ------ | ------ | ------ | |
Veritabanı | PostgreSQL | Açık kaynak dünyasının standart, performanslı ve
self-hosted uyumlu ilişkisel veritabanı kralı. | | Backend API | ASP.NET Core 8
Web API | Kurumsal seviyede tip güvenliği, yüksek performans, asenkron mimari ve
Clean Architecture / CQRS uyumluluğu. | | Frontend (Web) | Next.js (App Router)
& TypeScript | SEO dostu, hızlı, modern bileşen yönetimi ve esnek sunucu/istemci
taraflı render (SSR/CSR) yetenekleri. | | Frontend (Mobil) | Flutter (Dart) |
Faz 2'ye ertelendi. Tek kod tabanıyla iOS ve Android desteği, self-hosted
dinamik API URL yapılandırmasına tam uyum. | | Dağıtım | Docker & Docker-Compose
| Son kullanıcının altyapı karmaşası yaşamadan tek komutla (docker compose up
-d) tüm sistemi ayağa kaldırabilmesi. |
3. Mimari Değişiklikler (SaaS -> Self-Hosted Dönüşümü)

3.1. Basitleştirilmiş Altyapı (Temizlik)

  - RevenueCat ve Stripe Kaldırıldı: billing_sessions, entitlements_cache
    tabloları, ödeme kontrol webhook'ları ve premium kilit ekranları tamamen
    sistemden sökülmüştür. Ürünün hiçbir noktasında özellik kısıtlaması veya
    limit (eski 3 abonelik sınırı gibi) bulunmayacaktır.
  - Otomatik Veritabanı Yönetimi: Kullanıcının manuel SQL betikleri
    çalıştırmaması için, API konteyneri ayağa kalkarken Entity Framework Core
    aracılığıyla bekleyen tüm Migration yapılarını PostgreSQL veritabanına
    otomatik olarak uygulayacaktır.
3.2. Çoklu Kullanıcı ve Aile Modeli

  - İlk Kurulum Sahibi (Süper Admin): Docker ayağa kalktıktan sonra sisteme
    kayıt olan ilk kullanıcı otomatik olarak "Süper Yönetici" rolünü kazanır.
  - Kullanıcı Davet Sistemi (Multi-User): Yönetici, sistem ayarları veya admin
    paneli üzerinden aile üyelerini ya da arkadaşlarını sisteme manuel olarak
    ekleyebilir veya bir davet bağlantısı oluşturabilir.
  - İzole Veri Yapısı: Her kullanıcı yalnızca kendi aboneliklerini,
    kategorilerini ve harcamalarını görür; finansal gizlilik tam olarak korunur.
3.3. Dinamik Sistem Ayarları (SystemSettings Tablosu)

Açık kaynak dağıtımlarda hassas ayarların sürekli .env dosyalarından
değiştirilmesi zorlayıcı olabilir. Bu nedenle sistemde bir SystemSettings
tablosu yer alacaktır. Süper Admin, Web arayüzündeki panelden şu bilgileri
dinamik olarak yönetir:

  - OpenAI / LLM API Key: Yapay zeka analizlerinin çalışması için gerekli
    anahtar.
  - SMTP Config: Yaklaşan ödemelerin kullanıcılara e-posta ile hatırlatılması
    için gerekli mail sunucu bilgileri (Host, Port, Kullanıcı adı, Şifre).
4. Tasarım Sistemi (Design System) ve UI/UX Temelleri

Subify, hem gece kullanımlarında gözü yormayacak şık bir karanlık temaya hem de
günlük kullanıma uygun temiz bir aydınlık temaya sahip olacaktır. TailwindCSS'in
dark: seçicileriyle tam uyumlu bir dual-theme mimarisi kurulacaktır.

4.1. Renk Paleti (Theme Tokens)

| Renk Amacı | Aydınlık Tema (Light Mode) | Karanlık Tema (Dark Mode) | Arayüz
Uygulaması | | ------ | ------ | ------ | ------ | | Ana Arka Plan (Background)
| #F8FAFC (Slate 50) | #0F172A (Slate 900) | Tüm sayfaların en alt zemin rengi.
| | Yüzey (Surface) | #FFFFFF (White) | #1E293B (Slate 800) | Kartlar, modallar,
form alanları ve açılır menüler. | | Ana Vurgu (Primary Purple) | #7C3AED
(Violet 600) | #8B5CF6 (Violet 500) | Birincil aksiyon butonları, aktif menü
linkleri, neon glow efektleri. | | Ana Metin (Text Primary) | #0F172A
(Slate 900) | #F8FAFC (Slate 50) | Okunması gereken ana başlıklar ve yoğun
metinler. | | İkincil Metin (Text Muted) | #64748B (Slate 500) | #94A3B8
(Slate 400) | Yardımcı açıklamalar, placeholder'lar ve tarihler. | | Başarı
(Success) | #10B981 (Emerald 500) | #34D399 (Emerald 400) | Tasarruf
bildirimleri, grafiklerdeki olumlu dilimler. | | Uyarı (Warning) | #F59E0B
(Amber 500) | #FBBF24 (Amber 400) | Yaklaşan ödemeler (Son 3 gün), sarı kart
border'ları. | | Hata / Tehlike (Danger) | #EF4444 (Red 500) | #F87171 (Red 400)
| Gecikmiş ödemeler, silme/arşivleme butonları, kırmızı border'lar. |
4.2. Tipografi ve Bileşen Kuralları

  - Yazı Tipi (Font): Inter sans-serif ailesi.
  - Hiyerarşi: H1: 32px (Bold), H2: 24px (SemiBold), Body: 16px (Regular).
  - Görsel Durum Varyasyonları:
      - Yaklaşan Ödeme (< 3 Gün): Abonelik kartının etrafında hafif bir uyarı
        renginde border belirir ve "Yakında" etiketi eklenir. Karanlık modda bu
        karta çok hafif bir amber glow efekti uygulanabilir.
      - Gecikmiş Ödeme: Kart kırmızı border ile vurgulanır, "Gecikmiş" rozeti
        alır.
  - Responsive Akış: Next.js tarafında tüm düzen Tailwind Grid/Flex sistemine
    sadık kalınarak mobil ekranlardan ultra geniş monitörlere kadar kusursuz
    esneklikte kodlanacaktır.
5. Sıfırdan Geliştirme Yol Haritası (Roadmap)

Geliştirme süreci, Tech Lead (Yapay Zeka) tarafından sırayla verilecek
adımlarla, kullanıcı tarafından kodlanacak ve GitHub reposu üzerinden review
edilecektir.

Faz 1: Temel Proje Kurulumu ve İskelet (Core Setup)

  - [ ] Boş bir GitHub reposu oluşturulması, .gitignore ve README.md kurulumu.
  - [ ] Kök dizinde api/, web/, docs/ klasörlerinin açılması.
  - [ ] .NET 8 Clean Architecture katmanlarının (Domain, Application,
    Infrastructure, Api) oluşturulması ve referanslarının bağlanması.
  - [ ] Next.js projesinin TypeScript ve TailwindCSS ile başlatılması.
Faz 2: Veritabanı ve Çekirdek Varlıklar (PostgreSQL & Core Domain)

  - [ ] BaseEntity (Guid Id, CreatedAt, UpdatedAt) tasarlanması. (EF Core
    sequential GUID üretimi kuralı ile).
  - [ ] SystemSettings, User, Subscription, Category, Provider entity
    yapılarının Postgres veri tiplerine uygun yazılması.
  - [ ] DbContext kurulumu ve Postgres sağlayıcısının entegre edilmesi.

Faz 3: Kimlik Doğrulama ve Çoklu Kullanıcı Akışı (Identity & Multi-User)

  - [ ] JWT ve ASP.NET Core Identity altyapısının kurulması.
  - [ ] Sisteme kayıt olan ilk kullanıcının otomatik Admin atanması mantığı.
  - [ ] Admin paneli kullanıcı davet/ekleme API endpointlerinin yazılması.
Faz 4: Abonelik Yönetimi ve Finansal Motor (Core Features)

  - [ ] Abonelik oluşturma, listeleme, güncelleme ve silme (CRUD) dikey
    dilimlerinin (Vertical Slice) yazılması.
  - [ ] Sistem kategorilerinin seed verilerinin (Netflix, Spotify vb.) ve döviz
    kuru snapshots yapısının kurulması.
  - [ ] Dashboard finansal toplam hesaplama algoritmaları.

Faz 5: Yapay Zeka ve Bildirim Sistemleri (AI & SMTP)

  - [ ] OpenAI API istemcisinin SystemSettings tablosundaki dinamik anahtarla
    beslenerek kurulması.
  - [ ] Kullanıcının harcama alışkanlıklarına göre AI öneri motorunun yazılması.
  - [ ] Arka planda çalışacak e-posta hatırlatma işlerinin (Hangfire veya
    BackgroundService) kurulması.
Faz 6: Dockerization ve Dağıtım Hazırlığı (Release)

  - [ ] API ve Web için ayrı Dockerfile yapılarının yazılması.
  - [ ] postgres imajını da içeren, tek tıkla kurulumu sağlayan ana
    docker-compose.yml dosyasının yazılması.
  - [ ] EF Core otomatik veritabanı migration tetikleyicisinin API ayağa
    kalkışına eklenmesi.

Faz 7: Mobil Macera (Faz 2 - Flutter)

  - [ ] Web ve API stabilizasyonundan sonra Flutter ile mobil uygulamanın
    sıfırdan geliştirilmesi.
