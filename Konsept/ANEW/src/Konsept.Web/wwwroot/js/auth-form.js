// Giriş/kayıt formları (spec 0002 Req 10, AC-16, AC-18): gönderim sürerken düğme pasif ve "Gönderiliyor…" görünür; ikinci
// gönderim (çift tıklama, Enter tekrarı) engellenir — iki hesap açılmaz, iki deneme sayılmaz. Hata sonrası odak ilk hatalı
// alana gider. Formlar normal POST'tur (istek API'si kullanılmaz); metinler resx'ten (data-text-sending).

/**
 * Bir formun gönderim korumasını bağlar.
 * @param {HTMLFormElement} form Form.
 */
export function bindAuthForm(form) {
  form.addEventListener('submit', (event) => {
    if (form.dataset.submitting === 'true') {
      event.preventDefault();
      return;
    }

    form.dataset.submitting = 'true';
    form.setAttribute('aria-busy', 'true');
    const button = form.querySelector('button[type="submit"]');
    if (button) {
      button.disabled = true;
      button.setAttribute('aria-disabled', 'true');
      if (form.dataset.textSending) {
        button.textContent = form.dataset.textSending;
      }
    }
  });
}

/**
 * Sayfadaki tüm kimlik formlarını bağlar.
 * @param {Document} doc Belge.
 */
export function bindAuthForms(doc) {
  doc.querySelectorAll('form[data-auth-form]').forEach((form) => bindAuthForm(form));
}

/**
 * İlk hatalı alana odaklanır (sunucu autofocus'u desteklemeyen tarayıcılar için).
 * @param {Document} doc Belge.
 * @returns {Element|null} Odaklanan alan.
 */
export function focusFirstInvalid(doc) {
  const field = doc.querySelector('form[data-auth-form] [aria-invalid="true"]') || doc.querySelector('form[data-auth-form] [autofocus]');
  if (field && typeof field.focus === 'function') {
    field.focus();
  }
  return field;
}
