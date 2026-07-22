namespace Subify.Domain.Constants;

/// <summary>
/// Built-in i18n resource strings for Subify OS (task 2.3.7).
/// Pages: Common, Category, Dashboard, Subscription, Error — <b>no Paywall</b> / freemium copy.
/// </summary>
public static class SystemResources
{
    public sealed record Definition(
        string PageName,
        string Name,
        string LanguageCode,
        string Value);

    public static class Pages
    {
        public const string Common = "Common";
        public const string Category = "Category";
        public const string Dashboard = "Dashboard";
        public const string Subscription = "Subscription";
        public const string Error = "Error";
    }

    /// <summary>All seed rows (TR + EN pairs).</summary>
    public static readonly IReadOnlyList<Definition> All = Build();

    private static IReadOnlyList<Definition> Build()
    {
        var list = new List<Definition>(capacity: 120);

        void Pair(string page, string name, string tr, string en)
        {
            list.Add(new Definition(page, name, SupportedLocales.Tr, tr));
            list.Add(new Definition(page, name, SupportedLocales.En, en));
        }

        // --- Common ---
        Pair(Pages.Common, "save", "Kaydet", "Save");
        Pair(Pages.Common, "cancel", "İptal", "Cancel");
        Pair(Pages.Common, "delete", "Sil", "Delete");
        Pair(Pages.Common, "edit", "Düzenle", "Edit");
        Pair(Pages.Common, "add", "Ekle", "Add");
        Pair(Pages.Common, "loading", "Yükleniyor...", "Loading...");
        Pair(Pages.Common, "error", "Bir hata oluştu", "An error occurred");
        Pair(Pages.Common, "success", "Başarılı", "Success");
        Pair(Pages.Common, "search", "Ara", "Search");
        Pair(Pages.Common, "confirm", "Onayla", "Confirm");
        Pair(Pages.Common, "back", "Geri", "Back");
        Pair(Pages.Common, "close", "Kapat", "Close");
        Pair(Pages.Common, "yes", "Evet", "Yes");
        Pair(Pages.Common, "no", "Hayır", "No");

        // --- Category (names match SystemCategories slugs) ---
        Pair(Pages.Category, SystemCategories.Streaming, "Video Akış", "Streaming");
        Pair(Pages.Category, SystemCategories.Music, "Müzik", "Music");
        Pair(Pages.Category, SystemCategories.Productivity, "Üretkenlik", "Productivity");
        Pair(Pages.Category, SystemCategories.Gaming, "Oyun", "Gaming");
        Pair(Pages.Category, SystemCategories.Shopping, "Alışveriş", "Shopping");
        Pair(Pages.Category, SystemCategories.Utilities, "Araçlar", "Utilities");
        Pair(Pages.Category, SystemCategories.Education, "Eğitim", "Education");
        Pair(Pages.Category, SystemCategories.Health, "Sağlık", "Health");
        Pair(Pages.Category, SystemCategories.Cloud, "Bulut", "Cloud");
        Pair(Pages.Category, SystemCategories.Other, "Diğer", "Other");

        // --- Dashboard ---
        Pair(Pages.Dashboard, "title", "Ana Sayfa", "Home");
        Pair(Pages.Dashboard, "monthly_total", "Aylık Toplam", "Monthly Total");
        Pair(Pages.Dashboard, "yearly_total", "Yıllık Toplam", "Yearly Total");
        Pair(Pages.Dashboard, "upcoming_payments", "Yaklaşan Ödemeler", "Upcoming Payments");
        Pair(Pages.Dashboard, "budget_usage", "Bütçe Kullanımı", "Budget Usage");
        Pair(Pages.Dashboard, "recent_activity", "Son İşlemler", "Recent Activity");
        Pair(Pages.Dashboard, "active_subscriptions", "Aktif Abonelikler", "Active Subscriptions");
        Pair(Pages.Dashboard, "no_subscriptions", "Henüz abonelik yok", "No subscriptions yet");

        // --- Subscription ---
        Pair(Pages.Subscription, "title", "Aboneliklerim", "My Subscriptions");
        Pair(Pages.Subscription, "add_new", "Yeni Abonelik", "New Subscription");
        Pair(Pages.Subscription, "name", "Abonelik Adı", "Subscription Name");
        Pair(Pages.Subscription, "price", "Fiyat", "Price");
        Pair(Pages.Subscription, "category", "Kategori", "Category");
        Pair(Pages.Subscription, "provider", "Sağlayıcı", "Provider");
        Pair(Pages.Subscription, "billing_cycle", "Döngü", "Billing Cycle");
        Pair(Pages.Subscription, "monthly", "Aylık", "Monthly");
        Pair(Pages.Subscription, "yearly", "Yıllık", "Yearly");
        Pair(Pages.Subscription, "next_renewal", "Sonraki Ödeme", "Next Payment");
        Pair(Pages.Subscription, "shared_with", "Paylaşım", "Shared With");
        Pair(Pages.Subscription, "persons", "kişi", "persons");
        Pair(Pages.Subscription, "your_share", "Sizin Payınız", "Your Share");
        Pair(Pages.Subscription, "notes", "Notlar", "Notes");
        Pair(Pages.Subscription, "archive", "Arşivle", "Archive");
        Pair(Pages.Subscription, "archived", "Arşivlendi", "Archived");
        Pair(Pages.Subscription, "currency", "Para Birimi", "Currency");
        Pair(Pages.Subscription, "empty", "Henüz abonelik eklemediniz", "You have not added any subscriptions yet");

        // --- Error (OS-safe: no freemium / email-confirm / paywall) ---
        Pair(Pages.Error, "invalid_credentials", "E-posta veya şifre hatalı.", "Invalid email or password.");
        Pair(Pages.Error, "rate_limit", "Çok fazla istek gönderdiniz. Lütfen bekleyin.", "Too many requests. Please wait.");
        Pair(Pages.Error, "unauthorized", "Oturum açmanız gerekiyor.", "You need to sign in.");
        Pair(Pages.Error, "forbidden", "Bu işlem için yetkiniz yok.", "You do not have permission for this action.");
        Pair(Pages.Error, "not_found", "Kayıt bulunamadı.", "Record not found.");
        Pair(Pages.Error, "validation_failed", "Girdiğiniz bilgileri kontrol edin.", "Please check the information you entered.");
        Pair(Pages.Error, "conflict", "Bu kayıt zaten mevcut.", "This record already exists.");
        Pair(Pages.Error, "server_error", "Beklenmeyen bir hata oluştu.", "An unexpected error occurred.");
        Pair(Pages.Error, "setup_required", "Kurulum henüz tamamlanmadı.", "Setup is not complete yet.");

        return list;
    }

    public static bool IsSeededPage(string? pageName) =>
        !string.IsNullOrWhiteSpace(pageName)
        && pageName.Trim() is Pages.Common or Pages.Category or Pages.Dashboard
            or Pages.Subscription or Pages.Error;
}
