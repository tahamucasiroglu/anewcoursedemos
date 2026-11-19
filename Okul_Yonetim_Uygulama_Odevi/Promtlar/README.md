# Promtlar — sohbet kaydı

Bu klasör, Okul Yönetim Platformu projesindeki kullanıcı ↔ AI sohbetinin yedeğidir.
ANEW'in kendi `prompts/` klasöründen **ayrıdır**; ANEW iş akışları bu klasörü kullanmaz.

## Dosya adı düzeni

`NNNN_YYYY-AA-GG_SSDD_kisa-konu.md`

- `NNNN` — sıra numarası (0001, 0002, …) — dosyalar bununla sıralanır
- `YYYY-AA-GG_SSDD` — isteğin tarihi ve saati
- `kisa-konu` — isteğin kısa özeti

Her dosyada iki bölüm vardır: **İstek** (kullanıcının mesajı, aynen) ve **Cevap** (AI'nın verdiği cevap ve yaptığı işlerin özeti).
