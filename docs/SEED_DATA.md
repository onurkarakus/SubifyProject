# Subify - Seed Data

Bu doküman, uygulamanın ilk çalışması için gerekli başlangıç verilerini içerir.

> **Referanslar:**
>
> - [DATA_MODEL.md](./DATA_MODEL.md)
> - [API_CONTRACTS.md](./API_CONTRACTS.md)

---

## 📋 İçindekiler

1. [System Categories](#1-system-categories)
2. [Providers (Subscription Services)](#2-providers)
3. [Resources (Localizations)](#3-resources)
4. [Email Templates](#4-email-templates)
5. [Admin User](#5-admin-user)

---

## 1. System Categories

Sistem tarafından tanımlanan varsayılan kategoriler. Kullanıcılar bunlara ek olarak özel kategoriler oluşturabilir.

```sql
INSERT INTO categories (id, slug, icon, color, sort_order, is_default, is_active, created_at, updated_at)
VALUES
  (NEWID(), 'streaming', 'play-circle', '#E50914', 1, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'music', 'music-note', '#1DB954', 2, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'productivity', 'briefcase', '#0078D4', 3, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'gaming', 'gamepad', '#9146FF', 4, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'shopping', 'shopping-cart', '#FF9900', 5, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'utilities', 'tool', '#6C757D', 6, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'education', 'book-open', '#00A86B', 7, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'health', 'heart', '#FF6B6B', 8, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'cloud', 'cloud', '#4285F4', 9, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'other', 'more-horizontal', '#8E8E93', 99, 1, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
```

### Kategori Detayları

| Slug           | İkon            | Renk                     | Açıklama                    |
| -------------- | --------------- | ------------------------ | --------------------------- |
| `streaming`    | play-circle     | #E50914 (Netflix Red)    | Video akış servisleri       |
| `music`        | music-note      | #1DB954 (Spotify Green)  | Müzik servisleri            |
| `productivity` | briefcase       | #0078D4 (Microsoft Blue) | İş/Üretkenlik araçları      |
| `gaming`       | gamepad         | #9146FF (Twitch Purple)  | Oyun servisleri             |
| `shopping`     | shopping-cart   | #FF9900 (Amazon Orange)  | Alışveriş abonelikleri      |
| `utilities`    | tool            | #6C757D (Gray)           | VPN, Depolama vb.           |
| `education`    | book-open       | #00A86B (Green)          | Eğitim platformları         |
| `health`       | heart           | #FF6B6B (Red)            | Sağlık/Fitness uygulamaları |
| `cloud`        | cloud           | #4285F4 (Google Blue)    | Bulut servisleri            |
| `other`        | more-horizontal | #8E8E93 (Gray)           | Diğer                       |

---

## 2. Providers

Popüler abonelik sağlayıcıları. Kullanıcılar abonelik eklerken bu listeden seçebilir.

```sql
INSERT INTO providers (id, name, slug, logo_url, currency, price, billing_cycle, region, source_url, is_active, created_at, updated_at)
VALUES
  -- Video Streaming
  (NEWID(), 'Netflix', 'netflix', 'https://cdn.subify.app/logos/netflix.png', 'TRY', 149.99, 'monthly', 'TR', 'https://www.netflix.com/tr/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Disney+', 'disney-plus', 'https://cdn.subify.app/logos/disney-plus.png', 'TRY', 134.99, 'monthly', 'TR', 'https://www.disneyplus.com/tr-tr', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Amazon Prime Video', 'amazon-prime-video', 'https://cdn.subify.app/logos/amazon-prime.png', 'TRY', 39.00, 'monthly', 'TR', 'https://www.primevideo.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'BluTV', 'blutv', 'https://cdn.subify.app/logos/blutv.png', 'TRY', 84.90, 'monthly', 'TR', 'https://www.blutv.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Exxen', 'exxen', 'https://cdn.subify.app/logos/exxen.png', 'TRY', 104.90, 'monthly', 'TR', 'https://www.exxen.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Gain', 'gain', 'https://cdn.subify.app/logos/gain.png', 'TRY', 49.90, 'monthly', 'TR', 'https://www.gain.tv/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'YouTube Premium', 'youtube-premium', 'https://cdn.subify.app/logos/youtube.png', 'TRY', 79.99, 'monthly', 'TR', 'https://www.youtube.com/premium', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'HBO Max', 'hbo-max', 'https://cdn.subify.app/logos/hbo-max.png', 'USD', 15.99, 'monthly', 'US', 'https://www.max.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

  -- Music
  (NEWID(), 'Spotify', 'spotify', 'https://cdn.subify.app/logos/spotify.png', 'TRY', 59.99, 'monthly', 'TR', 'https://www.spotify.com/tr/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Apple Music', 'apple-music', 'https://cdn.subify.app/logos/apple-music.png', 'TRY', 34.99, 'monthly', 'TR', 'https://www.apple.com/tr/apple-music/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Deezer', 'deezer', 'https://cdn.subify.app/logos/deezer.png', 'TRY', 29.99, 'monthly', 'TR', 'https://www.deezer.com/tr/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Fizy', 'fizy', 'https://cdn.subify.app/logos/fizy.png', 'TRY', 24.99, 'monthly', 'TR', 'https://fizy.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

  -- Productivity
  (NEWID(), 'ChatGPT Plus', 'chatgpt-plus', 'https://cdn.subify.app/logos/openai.png', 'USD', 20.00, 'monthly', 'GLOBAL', 'https://chat.openai.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Microsoft 365', 'microsoft-365', 'https://cdn.subify.app/logos/microsoft-365.png', 'TRY', 129.99, 'monthly', 'TR', 'https://www.microsoft.com/tr-tr/microsoft-365', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Notion', 'notion', 'https://cdn.subify.app/logos/notion.png', 'USD', 10.00, 'monthly', 'GLOBAL', 'https://www.notion.so/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Canva Pro', 'canva-pro', 'https://cdn.subify.app/logos/canva.png', 'TRY', 149.99, 'monthly', 'TR', 'https://www.canva.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Grammarly', 'grammarly', 'https://cdn.subify.app/logos/grammarly.png', 'USD', 12.00, 'monthly', 'GLOBAL', 'https://www.grammarly.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

  -- Gaming
  (NEWID(), 'Xbox Game Pass', 'xbox-game-pass', 'https://cdn.subify.app/logos/xbox.png', 'TRY', 109.00, 'monthly', 'TR', 'https://www.xbox.com/tr-TR/xbox-game-pass', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'PlayStation Plus', 'playstation-plus', 'https://cdn.subify.app/logos/playstation.png', 'TRY', 159.00, 'monthly', 'TR', 'https://www.playstation.com/tr-tr/ps-plus/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Nintendo Switch Online', 'nintendo-switch-online', 'https://cdn.subify.app/logos/nintendo.png', 'TRY', 69.00, 'monthly', 'TR', 'https://www.nintendo.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'EA Play', 'ea-play', 'https://cdn.subify.app/logos/ea.png', 'TRY', 49.99, 'monthly', 'TR', 'https://www.ea.com/ea-play', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

  -- Cloud & Utilities
  (NEWID(), 'iCloud+', 'icloud', 'https://cdn.subify.app/logos/icloud.png', 'TRY', 12.99, 'monthly', 'TR', 'https://www.apple.com/tr/icloud/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Google One', 'google-one', 'https://cdn.subify.app/logos/google-one.png', 'TRY', 19.99, 'monthly', 'TR', 'https://one.google.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Dropbox Plus', 'dropbox-plus', 'https://cdn.subify.app/logos/dropbox.png', 'USD', 11.99, 'monthly', 'GLOBAL', 'https://www.dropbox.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'NordVPN', 'nordvpn', 'https://cdn.subify.app/logos/nordvpn.png', 'USD', 12.99, 'monthly', 'GLOBAL', 'https://nordvpn.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

  -- Education
  (NEWID(), 'Coursera Plus', 'coursera-plus', 'https://cdn.subify.app/logos/coursera.png', 'USD', 59.00, 'monthly', 'GLOBAL', 'https://www.coursera.org/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Udemy', 'udemy', 'https://cdn.subify.app/logos/udemy.png', 'TRY', 99.00, 'monthly', 'TR', 'https://www.udemy.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
  (NEWID(), 'Duolingo Plus', 'duolingo-plus', 'https://cdn.subify.app/logos/duolingo.png', 'TRY', 399.99, 'yearly', 'TR', 'https://www.duolingo.com/', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
```

### Provider Özet Tablosu

| Kategori         | Provider                                                                    | Fiyat (TRY/USD) |
| ---------------- | --------------------------------------------------------------------------- | --------------- |
| **Streaming**    | Netflix, Disney+, Prime Video, BluTV, Exxen, Gain, YouTube Premium, HBO Max | 39-150 TRY      |
| **Music**        | Spotify, Apple Music, Deezer, Fizy                                          | 25-60 TRY       |
| **Productivity** | ChatGPT, Microsoft 365, Notion, Canva, Grammarly                            | $10-20          |
| **Gaming**       | Xbox Game Pass, PlayStation Plus, Nintendo, EA Play                         | 50-160 TRY      |
| **Cloud**        | iCloud, Google One, Dropbox, NordVPN                                        | 13-20 TRY       |
| **Education**    | Coursera, Udemy, Duolingo                                                   | 99-400 TRY      |

---

## 3. Resources

UI metin lokalizasyonları (TR/EN).

```sql
-- Common
INSERT INTO resources (id, page_name, name, language_code, value, created_at, updated_at) VALUES
-- TR
(NEWID(), 'Common', 'save', 'tr', 'Kaydet', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'cancel', 'tr', 'İptal', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'delete', 'tr', 'Sil', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'edit', 'tr', 'Düzenle', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'add', 'tr', 'Ekle', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'loading', 'tr', 'Yükleniyor...', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'error', 'tr', 'Bir hata oluştu', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'success', 'tr', 'Başarılı', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
-- EN
(NEWID(), 'Common', 'save', 'en', 'Save', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'cancel', 'en', 'Cancel', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'delete', 'en', 'Delete', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'edit', 'en', 'Edit', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'add', 'en', 'Add', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'loading', 'en', 'Loading...', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'error', 'en', 'An error occurred', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Common', 'success', 'en', 'Success', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Categories
INSERT INTO resources (id, page_name, name, language_code, value, created_at, updated_at) VALUES
-- TR
(NEWID(), 'Category', 'streaming', 'tr', 'Video Akış', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'music', 'tr', 'Müzik', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'productivity', 'tr', 'Üretkenlik', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'gaming', 'tr', 'Oyun', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'shopping', 'tr', 'Alışveriş', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'utilities', 'tr', 'Araçlar', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'education', 'tr', 'Eğitim', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'health', 'tr', 'Sağlık', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'cloud', 'tr', 'Bulut', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'other', 'tr', 'Diğer', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
-- EN
(NEWID(), 'Category', 'streaming', 'en', 'Streaming', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'music', 'en', 'Music', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'productivity', 'en', 'Productivity', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'gaming', 'en', 'Gaming', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'shopping', 'en', 'Shopping', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'utilities', 'en', 'Utilities', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'education', 'en', 'Education', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'health', 'en', 'Health', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'cloud', 'en', 'Cloud', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Category', 'other', 'en', 'Other', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Dashboard
INSERT INTO resources (id, page_name, name, language_code, value, created_at, updated_at) VALUES
-- TR
(NEWID(), 'Dashboard', 'title', 'tr', 'Ana Sayfa', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'monthly_total', 'tr', 'Aylık Toplam', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'yearly_total', 'tr', 'Yıllık Toplam', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'upcoming_payments', 'tr', 'Yaklaşan Ödemeler', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'budget_usage', 'tr', 'Bütçe Kullanımı', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'recent_activity', 'tr', 'Son İşlemler', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
-- EN
(NEWID(), 'Dashboard', 'title', 'en', 'Home', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'monthly_total', 'en', 'Monthly Total', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'yearly_total', 'en', 'Yearly Total', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'upcoming_payments', 'en', 'Upcoming Payments', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'budget_usage', 'en', 'Budget Usage', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Dashboard', 'recent_activity', 'en', 'Recent Activity', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Subscriptions
INSERT INTO resources (id, page_name, name, language_code, value, created_at, updated_at) VALUES
-- TR
(NEWID(), 'Subscription', 'title', 'tr', 'Aboneliklerim', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'add_new', 'tr', 'Yeni Abonelik', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'name', 'tr', 'Abonelik Adı', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'price', 'tr', 'Fiyat', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'category', 'tr', 'Kategori', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'billing_cycle', 'tr', 'Döngü', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'monthly', 'tr', 'Aylık', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'yearly', 'tr', 'Yıllık', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'next_renewal', 'tr', 'Sonraki Ödeme', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'shared_with', 'tr', 'Paylaşım', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'persons', 'tr', 'kişi', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'your_share', 'tr', 'Sizin Payınız', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
-- EN
(NEWID(), 'Subscription', 'title', 'en', 'My Subscriptions', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'add_new', 'en', 'New Subscription', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'name', 'en', 'Subscription Name', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'price', 'en', 'Price', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'category', 'en', 'Category', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'billing_cycle', 'en', 'Cycle', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'monthly', 'en', 'Monthly', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'yearly', 'en', 'Yearly', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'next_renewal', 'en', 'Next Payment', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'shared_with', 'en', 'Shared With', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'persons', 'en', 'persons', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Subscription', 'your_share', 'en', 'Your Share', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Paywall
INSERT INTO resources (id, page_name, name, language_code, value, created_at, updated_at) VALUES
-- TR
(NEWID(), 'Paywall', 'title', 'tr', 'Premium''a Geç', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'subtitle', 'tr', 'Daha Akıllı Abonelik Yönetimi', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_unlimited', 'tr', 'Sınırsız abonelik ekle', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_ai', 'tr', 'AI önerileri', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_reports', 'tr', 'Detaylı raporlar', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_push', 'tr', 'Push bildirimleri', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_support', 'tr', 'Öncelikli destek', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'monthly_price', 'tr', '₺49/ay', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'yearly_price', 'tr', '₺499/yıl', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'yearly_savings', 'tr', '2 Ay Bedava', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'lifetime_price', 'tr', '₺699 bir kez', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'cta', 'tr', 'Premium''a Geç', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'cancel_anytime', 'tr', 'İstediğin zaman iptal et', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
-- EN
(NEWID(), 'Paywall', 'title', 'en', 'Go Premium', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'subtitle', 'en', 'Smarter Subscription Management', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_unlimited', 'en', 'Unlimited subscriptions', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_ai', 'en', 'AI suggestions', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_reports', 'en', 'Detailed reports', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_push', 'en', 'Push notifications', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'benefit_support', 'en', 'Priority support', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'monthly_price', 'en', '$4.99/mo', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'yearly_price', 'en', '$49.99/yr', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'yearly_savings', 'en', '2 Months Free', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'lifetime_price', 'en', '$69.99 once', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'cta', 'en', 'Go Premium', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Paywall', 'cancel_anytime', 'en', 'Cancel anytime', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Errors
INSERT INTO resources (id, page_name, name, language_code, value, created_at, updated_at) VALUES
-- TR
(NEWID(), 'Error', 'subscription_limit', 'tr', 'Free planda en fazla 3 abonelik ekleyebilirsin. Daha fazlası için Premium''a geç.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'invalid_credentials', 'tr', 'E-posta veya şifre hatalı.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'email_not_verified', 'tr', 'Lütfen önce e-postanızı doğrulayın.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'premium_required', 'tr', 'Bu özellik Premium üyelere özeldir.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'rate_limit', 'tr', 'Çok fazla istek gönderdiniz. Lütfen bekleyin.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
-- EN
(NEWID(), 'Error', 'subscription_limit', 'en', 'You can add up to 3 subscriptions on Free plan. Upgrade to Premium for more.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'invalid_credentials', 'en', 'Invalid email or password.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'email_not_verified', 'en', 'Please verify your email first.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'premium_required', 'en', 'This feature requires Premium subscription.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
(NEWID(), 'Error', 'rate_limit', 'en', 'Too many requests. Please wait.', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
```

---

## 4. Email Templates

Sistem tarafından gönderilen e-posta şablonları.

```sql
INSERT INTO email_templates (id, name, language_code, subject, body, created_at, updated_at) VALUES

-- Verify Email - TR
(NEWID(), 'VerifyEmail', 'tr', 'E-postanızı Doğrulayın - Subify',
'<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"></head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
  <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
    <h1 style="color: #6B46C1;">Merhaba {{FullName}},</h1>
    <p>Subify''ye hoş geldiniz! Hesabınızı aktifleştirmek için aşağıdaki butona tıklayın:</p>
    <p style="text-align: center; margin: 30px 0;">
      <a href="{{VerificationUrl}}" style="background: #6B46C1; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">E-postamı Doğrula</a>
    </p>
    <p>Bu link 24 saat geçerlidir.</p>
    <p>Eğer bu hesabı siz oluşturmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
    <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
    <p style="color: #888; font-size: 12px;">Subify - Aboneliklerinizi Tek Yerden Yönetin</p>
  </div>
</body>
</html>',
SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Verify Email - EN
(NEWID(), 'VerifyEmail', 'en', 'Verify Your Email - Subify',
'<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"></head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
  <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
    <h1 style="color: #6B46C1;">Hello {{FullName}},</h1>
    <p>Welcome to Subify! Click the button below to activate your account:</p>
    <p style="text-align: center; margin: 30px 0;">
      <a href="{{VerificationUrl}}" style="background: #6B46C1; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">Verify My Email</a>
    </p>
    <p>This link is valid for 24 hours.</p>
    <p>If you did not create this account, you can ignore this email.</p>
    <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
    <p style="color: #888; font-size: 12px;">Subify - Manage Your Subscriptions in One Place</p>
  </div>
</body>
</html>',
SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Password Reset - TR
(NEWID(), 'ResetPassword', 'tr', 'Şifre Sıfırlama - Subify',
'<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"></head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
  <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
    <h1 style="color: #6B46C1;">Şifre Sıfırlama</h1>
    <p>Merhaba {{FullName}},</p>
    <p>Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
    <p style="text-align: center; margin: 30px 0;">
      <a href="{{ResetUrl}}" style="background: #6B46C1; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">Şifremi Sıfırla</a>
    </p>
    <p>Bu link 1 saat geçerlidir.</p>
    <p>Eğer bu isteği siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
    <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
    <p style="color: #888; font-size: 12px;">Subify - Aboneliklerinizi Tek Yerden Yönetin</p>
  </div>
</body>
</html>',
SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Password Reset - EN
(NEWID(), 'ResetPassword', 'en', 'Password Reset - Subify',
'<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"></head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
  <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
    <h1 style="color: #6B46C1;">Password Reset</h1>
    <p>Hello {{FullName}},</p>
    <p>Click the button below to reset your password:</p>
    <p style="text-align: center; margin: 30px 0;">
      <a href="{{ResetUrl}}" style="background: #6B46C1; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">Reset My Password</a>
    </p>
    <p>This link is valid for 1 hour.</p>
    <p>If you did not request this, you can ignore this email.</p>
    <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
    <p style="color: #888; font-size: 12px;">Subify - Manage Your Subscriptions in One Place</p>
  </div>
</body>
</html>',
SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Renewal Reminder - TR
(NEWID(), 'RenewalReminder', 'tr', 'Ödeme Hatırlatması - {{SubscriptionName}}',
'<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"></head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
  <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
    <h1 style="color: #6B46C1;">Ödeme Hatırlatması 💳</h1>
    <p>Merhaba {{FullName}},</p>
    <p><strong>{{SubscriptionName}}</strong> aboneliğinizin yenileme tarihi yaklaşıyor:</p>
    <div style="background: #F7F7F7; padding: 20px; border-radius: 8px; margin: 20px 0;">
      <p style="margin: 0;"><strong>Abonelik:</strong> {{SubscriptionName}}</p>
      <p style="margin: 10px 0 0;"><strong>Tutar:</strong> {{Amount}} {{Currency}}</p>
      <p style="margin: 10px 0 0;"><strong>Tarih:</strong> {{RenewalDate}}</p>
    </div>
    <p style="text-align: center; margin: 30px 0;">
      <a href="{{AppUrl}}" style="background: #6B46C1; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">Aboneliklerimi Görüntüle</a>
    </p>
    <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
    <p style="color: #888; font-size: 12px;">Subify - Aboneliklerinizi Tek Yerden Yönetin</p>
  </div>
</body>
</html>',
SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Renewal Reminder - EN
(NEWID(), 'RenewalReminder', 'en', 'Payment Reminder - {{SubscriptionName}}',
'<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"></head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
  <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
    <h1 style="color: #6B46C1;">Payment Reminder 💳</h1>
    <p>Hello {{FullName}},</p>
    <p>Your <strong>{{SubscriptionName}}</strong> subscription renewal date is approaching:</p>
    <div style="background: #F7F7F7; padding: 20px; border-radius: 8px; margin: 20px 0;">
      <p style="margin: 0;"><strong>Subscription:</strong> {{SubscriptionName}}</p>
      <p style="margin: 10px 0 0;"><strong>Amount:</strong> {{Amount}} {{Currency}}</p>
      <p style="margin: 10px 0 0;"><strong>Date:</strong> {{RenewalDate}}</p>
    </div>
    <p style="text-align: center; margin: 30px 0;">
      <a href="{{AppUrl}}" style="background: #6B46C1; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;">View My Subscriptions</a>
    </p>
    <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
    <p style="color: #888; font-size: 12px;">Subify - Manage Your Subscriptions in One Place</p>
  </div>
</body>
</html>',
SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
```

### Email Template Placeholders

| Template            | Placeholder            | Açıklama        |
| ------------------- | ---------------------- | --------------- |
| **VerifyEmail**     | `{{FullName}}`         | Kullanıcı adı   |
|                     | `{{VerificationUrl}}`  | Doğrulama linki |
| **ResetPassword**   | `{{FullName}}`         | Kullanıcı adı   |
|                     | `{{ResetUrl}}`         | Sıfırlama linki |
| **RenewalReminder** | `{{FullName}}`         | Kullanıcı adı   |
|                     | `{{SubscriptionName}}` | Abonelik adı    |
|                     | `{{Amount}}`           | Ödeme tutarı    |
|                     | `{{Currency}}`         | Para birimi     |
|                     | `{{RenewalDate}}`      | Yenileme tarihi |
|                     | `{{AppUrl}}`           | Uygulama linki  |

---

## 5. Admin User

İlk admin kullanıcısı (migration veya seed script ile oluşturulur):

```csharp
// Seed Admin User (C# - Program.cs veya DbInitializer)
public static async Task SeedAdminUser(IServiceProvider services)
{
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    // Rolleri oluştur
    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    // Admin kullanıcısını oluştur
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@subify.app";
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "SecureAdminP@ss123";

    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");

            // Profile oluştur
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Profiles.Add(new Profile
            {
                Id = admin.Id,
                Email = adminEmail,
                FullName = "System Admin",
                Locale = "tr-TR",
                Plan = "premium",
                MainCurrency = "TRY"
            });
            await context.SaveChangesAsync();
        }
    }
}
```

---

## ✅ Seed Data Checklist

| Kategori             | Kayıt Sayısı | Durum |
| -------------------- | ------------ | ----- |
| Categories           | 10           | ✅    |
| Providers            | 27           | ✅    |
| Resources (TR)       | ~50          | ✅    |
| Resources (EN)       | ~50          | ✅    |
| Email Templates (TR) | 3            | ✅    |
| Email Templates (EN) | 3            | ✅    |
| Admin User           | 1            | ✅    |
| Roles                | 2            | ✅    |
