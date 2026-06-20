# SubifyProject 🚀

SubifyProject, kişisel ve ailevi aboneliklerinizi (Netflix, Spotify, YouTube vb.), düzenli ödemelerinizi ve finansal harcamalarınızı tek bir yerden güvenle takip edebileceğiniz, açık kaynaklı (Open Source) ve kendi sunucunuzda barındırılabilir (Self-Hosted) modern bir finans yönetim platformudur.

---

## 🎯 Proje Ne Yapar?

* **Merkezi Abonelik Yönetimi:** Tüm dijital ve fiziksel aboneliklerinizi, ödeme periyotlarını ve döngülerini tek bir panelden izlemenizi sağlar.
* **Çoklu Kullanıcı ve Aile Desteği:** Sistemi kuran admin, aile üyelerini veya arkadaşlarını sisteme davet ederek her kullanıcının kendi bütçesini izole bir şekilde yönetmesine imkan tanır.
* **Aydınlık & Karanlık Tema:** Kullanıcı tercihine göre Tailwind CSS tabanlı esnek Light/Dark modu destekler.
* **Akıllı Bildirimler & Yapay Zeka (Geliştirme Aşamasında):** Yaklaşan ödemeler için otomatik e-posta hatırlatmaları gönderir ve ilerleyen aşamalarda harcama alışkanlıklarınızı analiz eden yerel bir AI danışmanı barındırır.
* **Tam Gizlilik ve Tek Tıkla Kurulum:** Verileriniz üçüncü parti bulut şirketlerinde değil, kendi sunucunuzda PostgreSQL üzerinde izole kalır. Docker Compose ile tek komutta ayağa kalkar.

---

## 🛠️ Teknik Altyapı

* **Backend:** ASP.NET Core 8 Web API (Clean Architecture & CQRS)
* **Frontend:** Next.js (App Router) & TypeScript & Tailwind CSS
* **Veritabanı:** PostgreSQL
* **Dağıtım:** Docker & Docker Compose

---

## 📌 Mevcut Durum

Bu proje şu anda **aktif geliştirme aşamasındadır**. İlk kararlı sürüm yayınlandığında kapsamlı kurulum, yerel geliştirme ve kullanım kılavuzları bu dosyaya eklenecektir. Mimari kurallar ve yol haritası için `docs/` klasörünü inceleyebilirsiniz.