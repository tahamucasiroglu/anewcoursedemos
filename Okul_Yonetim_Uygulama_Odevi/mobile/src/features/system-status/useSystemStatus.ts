import { useCallback, useEffect, useState } from "react";
import { createOkulApiClient, fetchSystemHealth, type SystemHealthResult } from "@okul/api-client";
import { apiBaseUrl } from "../../config";

export type SystemStatusState = { kind: "loading" } | SystemHealthResult;

const client = createOkulApiClient(apiBaseUrl);

/** Sistem durumunu açılışta sorgular; "Tekrar dene" (retry) yeniden yükleniyor durumuna döner ve tekrar sorgular. */
export function useSystemStatus() {
  const [state, setState] = useState<SystemStatusState>({ kind: "loading" });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    fetchSystemHealth(client).then((result) => {
      if (active) {
        setState(result);
      }
    });
    return () => {
      active = false;
    };
  }, [attempt]);

  const retry = useCallback(() => {
    setState({ kind: "loading" });
    setAttempt((n) => n + 1);
  }, []);

  return { state, retry };
}
