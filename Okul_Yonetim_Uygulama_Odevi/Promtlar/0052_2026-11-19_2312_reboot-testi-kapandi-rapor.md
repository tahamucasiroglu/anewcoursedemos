# 0052 — Pi yeniden başlatma testi kapatıldı · güncel rapor

**Tarih:** 2026-11-19 23:12

## İstek

> pi yi yeniden başlatma testini kapat yapmayacağız. şuan local testteyiz. pi nin yeniden başlaması işlerimi zora sokar o yüzden o testi kapat. onun dışında kalan var mı raporu tekrar hazırla

## Cevap

### Yapılan
- **Test kapatıldı** (`a565530`):
  - Spec'teki AC-3 maddesinden "Pi yeniden başlatıldığında" kısmı çıkarıldı.
  - Yerine "servis açılışta başlayacak şekilde etkin" yazıldı; bu Pi'de zaten gözlendi (`enabled`, `WantedBy=multi-user.target`).
  - Gerekçe (senin sözlerinle) spec'e ve plana (A-5) yazıldı.

### Güncel rapor — spec 0003 VERIFY tablosu
| Kriter | Sonuç | Kanıt |
|---|---|---|
| AC-1 Tarayıcıdan sistem durumu | ✅ | Gerçek tarayıcıda "✔ Sistem çalışıyor" görünüyor (ana sayfa + derin bağlantı). İlk denemede bulunan hata 4. düzeltme turunda giderildi. |
| AC-2 Yalnızca 8080 dışarı açık | ✅ | 5080 ağdan reddediliyor. |
| AC-3 Ayrı kullanıcı, kendini toparlama | ✅ | Kullanıcı `okul`, oturum açamaz. API süreci öldürülünce 5 sn içinde geri geldi. Servis açılışta etkin. |
| AC-4 Kısıtlı veritabanı kullanıcısı | ✅ | Başka veritabanına bağlanma, veritabanı ve kullanıcı açma reddedildi. |
| AC-5 Şifre hiçbir yerde yok | ✅ | 7 kaynakta 0 eşleşme (olumlu kontrolde 1). Dosya `root` 600. |
| AC-6 Tek komutla dağıtım | ✅ | Kilit, disk eşiği ve 2 sürüm saklama çalışıyor. |
| AC-7 Otomatik ve elle geri dönüş | ✅ | Bozuk sürüm → önceki sürüm + migration uyarısı. |
| AC-8 Web ve API ayrı | ✅ | Süreç kimliği (PID) ve web bağlantısı korundu. |
| AC-9 Log dosyası | ✅ | `/var/log/okul/okul-api-YYYYMMDD.log`. |
| AC-10 Mobil kod yolu | ✅ | Ortak istemciden Pi'ye istek → healthy. |
| AC-11 Onaysız kaldırma | ✅ | 4 denemede hiçbir şey silinmedi. |
| AC-12 Tekrar kurulum | ✅ | Şifre dosyası, portlar, servisler ve veritabanları aynı kaldı. |

### Kalan var mı?
Kod işi yok. Kalan iki adım:
1. **Senin kararın:** son incelemenin 3 bilgi notu.
   - T13 belgesinin açıklaması netleştirilir.
   - Plana "`MSYS_NO_PATHCONV` bilerek kaldırıldı" notu düşülür.
   - Üçüncü not için eylem yok.
   - İkisi yalnızca belge değişikliği.
2. **SHIP onayı** → spec 0003 tamamlanır, özet ve yeni feature önerileri.

### Karar (tek)
Son incelemenin 3 bilgi notu için önerilerimi kabul ediyor musun?
