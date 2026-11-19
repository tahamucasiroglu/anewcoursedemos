import { t } from "../../i18n/t";
import { useSystemStatus } from "./useSystemStatus";

/**
 * Sistem durumu kartı (spec 0001): yükleniyor / çalışıyor / hizmet dışı / sunucuya ulaşılamıyor.
 * Son ikisinde "Tekrar dene" düğmesi var. Durum ekran okuyucuya duyurulur (role=status) ve yalnızca renkle değil
 * metinle anlatılır.
 */
export function SystemStatus() {
  const { state, retry } = useSystemStatus();

  const text =
    state.kind === "loading"
      ? t("status.loading")
      : state.kind === "unreachable"
        ? t("status.unreachable")
        : state.message;
  const canRetry = state.kind === "unavailable" || state.kind === "unreachable";

  return (
    <section className={`status status--${state.kind}`} role="status" aria-live="polite" aria-busy={state.kind === "loading"}>
      <p>{state.kind === "healthy" ? "✔ " : ""}{text}</p>
      {canRetry && (
        <button type="button" onClick={retry}>
          {t("status.retry")}
        </button>
      )}
    </section>
  );
}
