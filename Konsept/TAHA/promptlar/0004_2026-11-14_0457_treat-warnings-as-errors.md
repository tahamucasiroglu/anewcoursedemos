# 0004 — TreatWarningsAsErrors

- **Zaman:** 2026-11-14 04:57 (+03:00)

## İstek

TreatWarningsAsErrors da ekle projelere bu arada

## Cevap

Ekledim. `docs/MIMARI.md` ve `memory/kurallar/kod.md` güncellendi: tüm projelerde **TreatWarningsAsErrors=true**. Uyarılar NoWarn ile bastırılmayacak, kod düzeltilecek. Şu an çalışan iskelet adımı bitince `Directory.Build.props` içine eklenip çıkan bütün uyarılar düzeltilecek; sonraki tüm modül ajanları bu kuralla çalışacak.
