import { useState } from "react";
import { t } from "../../i18n/t";

interface Props {
  password: string;
  onDone: () => void;
}

/**
 * Geçici şifre sonucu (spec 0005): şifre seçilebilir metin olarak bir kez gösterilir; "bir daha gösterilmeyecek"
 * uyarısı; "Kopyala" panoya alır ve "Kopyalandı" duyurulur. Sayfa kapanırsa şifre kaybolur (kurucu yeniden sıfırlar).
 */
export function TempPasswordResult({ password, onDone }: Props) {
  const [copied, setCopied] = useState(false);
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(password);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  };

  return (
    <section className="card" aria-labelledby="temp-title">
      <h3 id="temp-title">{t("temp.title")}</h3>
      <p className="temp-password">
        <code>{password}</code>
      </p>
      <p className="notice">{t("temp.warning")}</p>
      <div className="actions">
        <button type="button" className="secondary" onClick={copy}>
          {t("temp.copy")}
        </button>
        <button type="button" onClick={onDone}>
          {t("temp.done")}
        </button>
      </div>
      <p role="status">{copied ? t("temp.copied") : ""}</p>
    </section>
  );
}
