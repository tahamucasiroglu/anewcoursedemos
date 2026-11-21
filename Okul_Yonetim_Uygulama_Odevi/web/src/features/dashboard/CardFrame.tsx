import type { ReactNode } from "react";
import { t } from "../../i18n/t";
import type { LoadState } from "./useLoad";

interface Props<T> {
  id: string;
  title: string;
  state: LoadState<T>;
  onRetry: () => void;
  children: (data: T) => ReactNode;
}

/**
 * Günlük özet kartının çerçevesi (spec 0017 States, Accessibility): başlık öğesi (h3), yükleniyor / hata + "Tekrar dene" (role=alert) / dolu.
 * Her kart kendi durumunu çizer; bir kartın hatası ötekileri etkilemez.
 */
export function CardFrame<T>({ id, title, state, onRetry, children }: Props<T>) {
  return (
    <section className="summary-card" aria-labelledby={id}>
      <h3 id={id}>{title}</h3>
      {state.kind === "loading" && <p className="notice" aria-busy="true">{t("dashboard.loading")}</p>}
      {state.kind === "error" && (
        <div>
          <p className="form-error" role="alert">{state.message}</p>
          <div className="actions">
            <button type="button" onClick={onRetry}>{t("dashboard.retry")}</button>
          </div>
        </div>
      )}
      {state.kind === "ready" && children(state.data)}
    </section>
  );
}
