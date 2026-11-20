import { useEffect, useRef } from "react";
import { t } from "../../i18n/t";

interface Props {
  message: string;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Onay iletişimi (spec 0005): "Emin misiniz?" — Vazgeç / Onayla. Açılınca odak "Vazgeç"te; Esc kapatır. Ekran okuyucu
 * için iletişim rolü ve modal işareti taşır.
 */
export function ConfirmDialog({ message, busy = false, onConfirm, onCancel }: Props) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    cancelRef.current?.focus();
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onCancel();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onCancel]);

  return (
    <div className="dialog-backdrop">
      <div className="card dialog" role="dialog" aria-modal="true" aria-labelledby="confirm-text">
        <p id="confirm-text">{message}</p>
        <div className="actions">
          <button type="button" className="secondary" ref={cancelRef} onClick={onCancel}>
            {t("confirm.cancel")}
          </button>
          <button type="button" onClick={onConfirm} disabled={busy} aria-busy={busy}>
            {t("confirm.ok")}
          </button>
        </div>
      </div>
    </div>
  );
}
