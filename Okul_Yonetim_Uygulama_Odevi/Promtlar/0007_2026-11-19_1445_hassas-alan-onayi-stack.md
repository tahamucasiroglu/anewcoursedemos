# 0007 — Konu 3 onayı (kişisel veriler eklendi) · Konu 4/12: Stack ve sürümler

**Tarih:** 2026-11-19 14:45

## İstek

> telefon, tc, şifre ve başka özel verileride ekle. ekleme sonrası onaylıyorum

## Cevap

**Konu 3 — Hassas alanlar: EKLEMEYLE ONAYLANDI.** Son hali:

| # | Hassas alan | Kapsam |
|---|---|---|
| S1 | Kiracı ve şube izolasyonu | Kurum/şube sınırı, sorgu filtreleri, yetkili şube kontrolü |
| S2 | **Kişisel ve özel nitelikli veri (herkes için: öğrenci, veli, personel)** | **TC kimlik no, telefon, e-posta, adres, doğum tarihi, ad-soyad + öğrenci eşleşmesi**, sağlık notu, alerji, fotoğraf, teslim alabilecek kişiler |
| S3 | Kimlik, rol ve yetki + **gizli bilgiler** | **Şifre (yalnızca güçlü hash olarak saklanır, asla düz metin/log olmaz), 2FA sırları, oturum/JWT/yenileme token'ları, API anahtarları**, rol ataması, veli-öğrenci bağı |
| S4 | Para ve tahsilat + **finansal veri** | Taksit, indirim/burs, tahsilat, makbuz, cari, **kart bilgisi (sistemde asla saklanmaz, ödeme sağlayıcıda kalır), IBAN** |
| S5 | Veliye giden iletişim ve AI çıktıları | Duyuru, mesaj, AI taslağı, otomatik bildirim |
| S6 | KVKK rıza, audit log, saklama süresi | Rıza kayıtları, erişim logu, anonimleştirme |

Bu alanların teknik kuralları (şifreleme, maskeleme, loglama yasağı) Konu 9 Güvenlik'te `docs/security.md` için ayrıca sorulacak.

**Konu 4/12 — Stack ve sürümler** (`docs/architecture.md`)

Makinede bulunanlar: **.NET SDK 10.0.403/404**, **Node.js 26.13**, **SQL Server 2025 araçları** (170). Docker, PostgreSQL ve Flutter yok.

| Katman | Önerim | Gerekçe |
|---|---|---|
| Backend | **.NET 10 (LTS) + ASP.NET Core Web API, C# 14** | Doküman "nArchitecture tabanı" diyor; nArchitecture bir .NET Clean Architecture projesi. .NET 10 LTS makinede kurulu. |
| Veri erişimi | **EF Core 10** | nArchitecture deseni; migration desteği |
| Veritabanı | **SQL Server 2025** (geliştirme: yerel instance) | nArchitecture varsayılanı, makinede kurulu; ek kurulum gerektirmez |
| Web paneli (Yönetici, Muhasebe, Öğretmen-web) | **React 19 + TypeScript + Vite** | Yaygın, mobil ile aynı dil (TS) |
| Mobil (Veli + Öğretmen, tek uygulama) | **React Native + Expo (TypeScript)** | iOS + Android tek kod; web ile tip/istemci kodu paylaşılır; Flutter kurulu değil |
| Dış servisler (ödeme, SMS, WhatsApp, push, AI/ses-metin) | **Port arkasında, MVP'de sahte uygulama** | Konu 1 kararı; AI sağlayıcısı da aynı şekilde sonradan seçilir |

Önerim: Yukarıdaki tablo. Kesin paket sürümleri (EF Core, React, Expo SDK) walking skeleton planında, o günün kararlı sürümleriyle sabitlenir. Mimari desen (katmanlar, CQRS, nArchitecture Core paketlerinin kullanılıp kullanılmayacağı) Konu 5'te sorulacak.

Alternatifler: PostgreSQL (açık kaynak, barındırma maliyeti düşük; ama kurulum gerekir), Angular (web için), Flutter (mobil için; Dart, ayrı dil).

**Dokümanda istenen:**
- "Multi-tenant SaaS, tenant bazlı veri izolasyonu (**nArchitecture tabanı**)"
- "Mobil: veli ve öğretmen için tek uygulama (iOS + Android)"
- Kanallar: Kurucu ve Muhasebe → Web; Öğretmen → Mobil + Web; Veli → Mobil
- "Push bildirim, kurum logosuyla markalı açılış"
- "Yedekleme, Türkiye'de barındırma seçeneği"

Cevap bekleniyor: Stack tablosu uygun mu? Özellikle veritabanı (SQL Server mi PostgreSQL mi) ve mobil (Expo mu Flutter mı).
