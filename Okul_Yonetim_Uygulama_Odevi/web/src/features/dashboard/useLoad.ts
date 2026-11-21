import { useEffect, useState } from "react";
import { failureText } from "../admin/adminText";
import type { ApiOutcome } from "../admin/adminApi";

export type LoadState<T> = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; data: T };

type Result<T> = { load: unknown; refresh: number; attempt: number; state: LoadState<T> };

/**
 * Yönetici paneli kartlarının ve listelerinin yükleme durumu (spec 0017 States): yükleniyor → dolu ya da hata; "Tekrar dene" ve "Yenile"
 * yeniden yükler. Her kart kendi isteğini yapar — bir kartın hatası ötekileri bozmaz. `load` (çağıran `useCallback` ile kurar; şube
 * süzgeci değişince yenilenir), `refresh` (Yenile sayacı) ya da deneme değişince sonuç eskir ve "yükleniyor" görünür.
 */
export function useLoad<T>(load: () => Promise<ApiOutcome<T>>, refresh: number): { state: LoadState<T>; retry: () => void } {
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState<Result<T> | null>(null);

  useEffect(() => {
    let active = true;
    load().then((outcome) => {
      if (active) {
        setResult({ load, refresh, attempt, state: outcome.ok ? { kind: "ready", data: outcome.data } : { kind: "error", message: failureText(outcome) } });
      }
    });
    return () => {
      active = false;
    };
  }, [load, refresh, attempt]);

  const fresh = result !== null && result.load === load && result.refresh === refresh && result.attempt === attempt;
  return { state: fresh ? result.state : { kind: "loading" }, retry: () => setAttempt((n) => n + 1) };
}
