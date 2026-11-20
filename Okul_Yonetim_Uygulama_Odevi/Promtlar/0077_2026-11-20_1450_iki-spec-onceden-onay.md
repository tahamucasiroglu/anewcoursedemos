# 0077 — İki spec, önceden verilen onayla uçtan uca

## İstek

2 ayrı spec olarak ilerleyelim. plan kısmına kadar gelelim bu seferlik review de bulduğun bulgularda karar sende olacak fakat tavsiyem bilgi dışındakileri düzeltmen. buna göre bana bir new feature komutu yazacak mısın başka şekilde mi çözeceğiz

(Netleştirme sorusu: "Plan kısmına kadar gelelim" nasıl anlaşılsın? → Cevap: **Uçtan uca** — açık soruların cevabı, spec ve
plan onayı önerilerimle önceden verilmiş sayılır; build → review → düzeltme → verify; ship için kullanıcı beklenir.)

## Cevap

Komut gerekmiyor; bu mesaj ve seçim önceden verilen onayın kaydıdır (yalnızca bu iki spec için, "bu seferlik").

- 0006 = Şifre işlemleri ve e-posta + Profilim; 0007 = Platform yönetim paneli. Sırayla, `/new-feature` ile.
- Clarify cevapları = önerilerim ("Dokümanda istenen" notlarıyla spec'e yazılır); spec ve plan onayı önceden verilmiş
  (dosyalara "önceden verilen onay, Promtlar 0077" diye işlenir).
- Triage kararı bende: bilgi (Info) dışındaki bulgular düzeltilir, bilgi notları backlog'a ya da nota. Hassas alan
  kuralları aynen: her düzeltme turundan sonra yeniden review, her kriter kasıtlı bozmayla kanıt, en fazla 3 tur (4.
  tur gerekirse durur, not bırakırım).
- Ship, 0004 gözlemi ve Pi kullanıcıda; Pi'ye dokunulmaz.
- 0005 VERIFY arka planda sürüyor; 0006'nın kodu ona karışmasın diye 0006 build'i 0005 VERIFY bitince başlar.
