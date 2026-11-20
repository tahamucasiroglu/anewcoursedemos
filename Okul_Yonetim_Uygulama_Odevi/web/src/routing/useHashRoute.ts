import { useCallback, useSyncExternalStore } from "react";

// Bağımlılıksız küçük adres yönlendirmesi (spec 0005, plan Risks 3): "#/yonetim" gibi adresler. Sayfa yenilenince ve
// geri/ileri tuşlarında aynı ekran gelir; nginx ayarı değişmez.
function subscribe(onChange: () => void) {
  window.addEventListener("hashchange", onChange);
  return () => window.removeEventListener("hashchange", onChange);
}

function current() {
  return window.location.hash.replace(/^#/, "") || "/";
}

export function useHashRoute() {
  const route = useSyncExternalStore(subscribe, current, () => "/");
  const navigate = useCallback((to: string) => {
    window.location.hash = to;
  }, []);
  return { route, navigate };
}
