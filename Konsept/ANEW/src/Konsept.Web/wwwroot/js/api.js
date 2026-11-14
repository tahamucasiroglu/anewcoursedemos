// Tarayıcının tek istek yolu: jQuery $.ajax (K-6 — başka istek API'si yasak). Tarayıcı → MVC controller → API.
// POST isteklerinde CSRF belirteci başlıkta gönderilir (docs/security.md).

const TOKEN_HEADER = 'RequestVerificationToken';
const DEFAULT_TIMEOUT_MS = 15000;

function jq() {
  const $ = globalThis.jQuery;
  if (!$) {
    throw new Error('jQuery yüklenmedi');
  }
  return $;
}

/**
 * Sayfadaki CSRF belirtecini okur.
 * @param {Document} doc Belge.
 * @returns {string} Belirteç (yoksa boş).
 */
export function csrfToken(doc = globalThis.document) {
  const meta = doc.querySelector('meta[name="csrf-token"]');
  return meta ? meta.getAttribute('content') || '' : '';
}

function request(method, url, data) {
  const $ = jq();
  const settings = {
    url,
    method,
    dataType: 'json',
    timeout: DEFAULT_TIMEOUT_MS,
    headers: method === 'GET' ? {} : { [TOKEN_HEADER]: csrfToken() },
  };
  if (data !== undefined) {
    settings.data = JSON.stringify(data);
    settings.contentType = 'application/json';
  }
  return new Promise((resolve, reject) => {
    $.ajax(settings)
      .done((body) => resolve(body))
      .fail((xhr) => reject({ status: xhr ? xhr.status : 0, body: xhr && xhr.responseJSON ? xhr.responseJSON : null }));
  });
}

/**
 * GET isteği.
 * @param {string} url Adres.
 * @returns {Promise<any>} JSON gövde.
 */
export function get(url) {
  return request('GET', url);
}

/**
 * POST isteği (CSRF belirteciyle).
 * @param {string} url Adres.
 * @param {any} [data] Gövde.
 * @returns {Promise<any>} JSON gövde.
 */
export function post(url, data) {
  return request('POST', url, data);
}
