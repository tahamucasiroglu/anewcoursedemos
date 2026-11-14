// Testlerde _Layout.cshtml'deki alt şerit ve kontrollerin en küçük karşılığı (metinler resx'teki değerler).
import $ from 'jquery';

export function installJquery() {
  globalThis.jQuery = $;
  return $;
}

export function layoutHtml() {
  return `
    <meta name="csrf-token" content="test-token" />
    <button id="theme-toggle" type="button">tema</button>
    <button id="passive" type="button" class="passive" aria-disabled="true" aria-label="Ayarlar" data-tooltip="Yakında">⚙</button>
    <div id="journal" data-level="info"><span id="journal-text">Henüz kayıt yok</span></div>
    <div id="system-status"
         data-text-loading="Yükleniyor…" data-text-api-ok="API ✓" data-text-api-down="API erişilemiyor"
         data-text-db-ok="DB ✓" data-text-db-down="DB erişilemiyor" data-text-agent-connected="Ajan bağlı"
         data-text-agent-disconnected="Ajan bağlı değil" data-text-journal-empty="Henüz kayıt yok">
      <span id="status-version"></span><span id="status-api"></span><span id="status-db"></span><span id="status-agent"></span>
    </div>
    <button id="agent-test-button" data-text-sending="Gönderiliyor…" data-text-failed="Ajana ulaşılamadı"
            data-text-agent-not-connected="Ajan bağlı değil" data-text-agent-timeout="Zaman aşımı" data-text-api-unavailable="API erişilemiyor">Ajanı test et</button>
    <span id="agent-test-result"></span>`;
}

/** jQuery'nin $.ajax'ı yerine geçen, done/fail destekli sahte cevap. */
export function ajaxResolving($, body) {
  return $.Deferred().resolve(body).promise();
}

export function ajaxRejecting($, status, body) {
  return $.Deferred().reject({ status, responseJSON: body }).promise();
}
