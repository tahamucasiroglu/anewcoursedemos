import { useEffect, useRef, type KeyboardEvent } from "react";
import { t } from "../../i18n/t";

interface Props {
  message: string;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Onay iletişimi (spec 0005): "Emin misiniz?" — Vazgeç / Onayla. Açılınca odak bir kez "Vazgeç"e gelir; Tab iletişim
 * içinde döner; Esc kapatır; kapanınca odak iletişimi açan düğmeye döner (review L-4). Ekran okuyucu için iletişim rolü
 * ve modal işareti taşır.
 */
export function ConfirmDialog({ message, busy = false, onConfirm, onCancel }: Props) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const cancelRef = useRef<HTMLButtonElement>(null);
  const onCancelRef = useRef(onCancel);
  useEffect(() => {
    onCancelRef.current = onCancel;
  }, [onCancel]);

  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    cancelRef.current?.focus();
    const onKey = (event: globalThis.KeyboardEvent) => {
      if (event.key === "Escape") {
        onCancelRef.current();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("keydown", onKey);
      if (opener?.isConnected) {
        opener.focus();
      }
    };
  }, []);

  const trapTab = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== "Tab" || !dialogRef.current) {
      return;
    }

    const buttons = Array.from(dialogRef.current.querySelectorAll<HTMLButtonElement>("button:not([disabled])"));
    if (buttons.length === 0) {
      return;
    }

    const first = buttons[0];
    const last = buttons[buttons.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  };

  return (
    <div className="dialog-backdrop">
      <div className="card dialog" role="dialog" aria-modal="true" aria-labelledby="confirm-text" ref={dialogRef} onKeyDown={trapTab}>
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
