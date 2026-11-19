# Spec 0002 VERIFY kasıtlı bozmaları: repo DIŞINDA kopyada; kök node_modules repo'ya bağlantı (junction), salt okunur kullanılır.
set -u
SRC="C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Okul_Yonetim_Uygulama_Odevi"
W="C:/Users/taham/AppData/Local/Temp/claude/09d252a19f5bfcd1/2fc78961-704e-4a2f-9b3f-d7c2e5ec5024/scratchpad/verify2"
WWIN='C:\Users\taham\AppData\Local\Temp\claude\09d252a19f5bfcd1\2fc78961-704e-4a2f-9b3f-d7c2e5ec5024\scratchpad\verify2'
SRCWIN='C:\Dosyalar\Projects\engin_demirog\anewcoursedemos\Okul_Yonetim_Uygulama_Odevi'

fresh() {
  [ -d "$W" ] && cmd //c "rmdir $WWIN\\node_modules" >/dev/null 2>&1
  rm -rf "$W"; mkdir -p "$W"
  (cd "$SRC" && tar --exclude='web/node_modules/.vite' --exclude='web/node_modules/.tmp' -cf - package.json packages web mobile) | (cd "$W" && tar -xf -)
  cmd //c "mklink /J $WWIN\\node_modules $SRCWIN\\node_modules" >/dev/null
}
vt() { (cd "$W/$1" && npx vitest run "$2" 2>&1 | grep -E "^ +(×|✓) |Tests " | grep -E "×|Tests " | head -4); }
jt() { (cd "$W/mobile" && npx jest --ci --watchAll=false --maxWorkers=1 "$1" 2>&1 | grep -E "✕|Tests:" | head -4); }
py() { python -I -c "import io,sys;p,a,b=sys.argv[1:4];s=io.open(p,encoding='utf-8').read();assert a in s,a;io.open(p,'w',encoding='utf-8',newline='\n').write(s.replace(a,b,1))" "$@"; }

echo "=== B1 (AC-9) koyu soluk metin rengi düşük kontrast (#cbd5e1 -> #64748b) ==="
fresh; py "$W/packages/theme/tokens.json" '"textMuted": "#cbd5e1"' '"textMuted": "#64748b"'; vt packages/theme src/contrast.test.ts

echo "=== B2 (AC-1) web koyu ana renk tokens.json'dan farklı ==="
fresh; py "$W/web/src/theme/theme.css" '--color-primary: #93c5fd;' '--color-primary: #60a5fa;'; vt web src/theme/cssVariables.test.ts

echo "=== B3 (AC-8, T11) ön betik cihaz modunu yok sayıyor ==="
fresh; py "$W/web/index.html" '(deviceDark ? "dark" : "light")' '"light"'; vt web src/theme/prePaint.test.ts

echo "=== B4 (AC-1) web bileşeninde renk değeri ==="
fresh; py "$W/web/src/theme/ThemeSwitcher.tsx" 'className="theme-switcher"' 'style={{ color: "#ffffff" }} className="theme-switcher"'; (cd "$W/web" && npx eslint src/theme/ThemeSwitcher.tsx --max-warnings 0 2>&1 | grep -E "error|problem" | head -2)

echo "=== B5 (AC-6) web cihaz değişimi dinlenmiyor ==="
fresh; py "$W/web/src/theme/useThemePreference.ts" '    media.addEventListener("change", onChange);' '    void onChange;'; vt web src/theme/ThemeSwitcher.test.tsx

echo "=== B6 (AC-5) web seçimi saklamıyor ==="
fresh; py "$W/web/src/theme/useThemePreference.ts" '    window.localStorage.setItem(THEME_STORAGE_KEY, preference);' '    void preference;'; vt web src/theme/ThemeSwitcher.test.tsx

echo "=== B7 (AC-7 sınır) mobil zaman aşımı 500 -> 2000 ms ==="
fresh; py "$W/mobile/src/theme/ThemeProvider.tsx" 'const STORAGE_TIMEOUT_MS = 500;' 'const STORAGE_TIMEOUT_MS = 2000;'; jt src/theme/ThemeProvider.test.tsx

echo "=== B8 (AC-6) mobil cihaz modunu yok sayıyor ==="
fresh; py "$W/mobile/src/theme/ThemeProvider.tsx" 'device === "dark" || device === "light" ? device : null' 'null'; jt src/theme/ThemeProvider.test.tsx

echo "=== B9 (AC-11) web seçili durumu ekran okuyucuya bildirilmiyor (aria-pressed yok) ==="
fresh; py "$W/web/src/theme/ThemeSwitcher.tsx" '          aria-pressed={preference === option.value}' '          data-x={preference === option.value}'; vt web src/theme/ThemeSwitcher.test.tsx

cmd //c "rmdir $WWIN\\node_modules" >/dev/null 2>&1; rm -rf "$W"
echo "=== bitti; kopya silindi: $([ -d "$W" ] && echo HAYIR || echo evet) ==="
