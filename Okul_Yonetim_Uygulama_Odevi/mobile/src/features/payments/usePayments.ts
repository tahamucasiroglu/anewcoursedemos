import { useEffect, useState } from "react";
import { callApi, type AuthSession, type ChildAccount, type MyPayments } from "@okul/api-client";
import { t } from "../../i18n/t";

type Client = AuthSession["client"];
type Failure = { unreachable: true } | { unreachable: false; message: string | null };

export type PaymentsState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; view: MyPayments };
export type ChildPaymentsState = { kind: "loading" } | { kind: "gone" } | { kind: "error"; message: string } | { kind: "ready"; child: ChildAccount };

function failure(outcome: Failure): string {
  return outcome.unreachable ? t("academics.unreachable") : outcome.message ?? t("payments.loadError");
}

/** Velinin "Ödemeler" listesi (spec 0015 gereksinim 10): `GET api/my/payments`; yükleniyor / hata + tekrar dene / hazır. */
export function useMyPayments(client: Client) {
  const [state, setState] = useState<PaymentsState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    void callApi(client.GET("/api/my/payments")).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", view: outcome.data } : { kind: "error", message: failure(outcome) });
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

/** Bir çocuğun ödeme ayrıntısı (`GET api/my/payments/{id}`; bağ yoksa 404 → "artık görüntülenemiyor"). */
export function useChildPayments(client: Client, studentId: string) {
  const [state, setState] = useState<ChildPaymentsState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    void callApi(client.GET("/api/my/payments/{studentId}", { params: { path: { studentId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", child: outcome.data });
      } else if (!outcome.unreachable && outcome.status === 404) {
        setState({ kind: "gone" });
      } else {
        setState({ kind: "error", message: failure(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, studentId, reload]);

  const retry = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  return { state, retry };
}
