import { useEffect, useState } from "react";
import { callApi, type AuthSession, type ChildRow } from "@okul/api-client";
import { t } from "../../i18n/t";

export type ChildrenState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; children: ChildRow[] };

/**
 * Velinin "Çocuklarım" listesini (spec 0009) yükler: yükleniyor → dolu/boş ya da hata (sunucu mesajı veya "sunucuya
 * ulaşılamıyor"); <c>retry</c> yeniden ister (kart "artık görüntülenemiyor" deyince liste de yenilenir).
 */
export function useChildren(client: AuthSession["client"]) {
  const [state, setState] = useState<ChildrenState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/my/children")).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", children: outcome.data });
      } else {
        setState({ kind: "error", message: outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("children.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  return { state, retry };
}
