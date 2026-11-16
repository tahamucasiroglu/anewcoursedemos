// Tarayıcının tek istek yolu: jQuery $.ajax (K-6 — başka istek API'si yasak). Tarayıcı → MVC controller → API.
// POST isteklerinde CSRF belirteci başlıkta gönderilir (docs/security.md). Oturum düştüyse (401) işlem sessizce
// başarısız olmaz: tarayıcı "Oturumunuz sona erdi" bilgisiyle giriş sayfasına gider (spec 0002 Req 13, AC-13/14).
// Yetkisiz işlem (403) yönlendirilmez; hata "Bu işlem için yetkiniz yok" metniyle döner (spec 0011 Req 3).

const TOKEN_HEADER = 'RequestVerificationToken';
const DEFAULT_TIMEOUT_MS = 15000;

let navigation = globalThis.location;
let lastCorrelation = '';

/**
 * Son başarısız isteğin korelasyon numarası ("Hata bildir" modalı ekler, spec 0022 Req 1); yoksa boş.
 * @returns {string} Numara.
 */
export function lastCorrelationId() {
  return lastCorrelation;
}

function jq() {
  const $ = globalThis.jQuery;
  if (!$) {
    throw new Error('jQuery yüklenmedi');
  }
  return $;
}

/**
 * Yönlendirmede kullanılacak konumu değiştirir (testler için; varsayılan window.location).
 * @param {{ pathname: string, search?: string, assign: (url: string) => void }} target Konum.
 */
export function setNavigation(target) {
  navigation = target;
}

/**
 * Oturum düşünce gidilecek giriş adresi: bulunulan sayfa dönüş adresi olur.
 * @param {{ pathname: string, search?: string }} location Bulunulan konum.
 * @returns {string} Giriş adresi.
 */
export function loginRedirectUrl(location) {
  const back = `${location.pathname || '/'}${location.search || ''}`;
  return `/giris?oturum=bitti&returnUrl=${encodeURIComponent(back)}`;
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

/**
 * Yetkisiz işlem metni (spec 0011 Req 3): resx'ten, düzenin meta etiketiyle gelir.
 * @param {Document} doc Belge.
 * @returns {string} "Bu işlem için yetkiniz yok" (yoksa boş).
 */
export function forbiddenText(doc = globalThis.document) {
  const meta = doc.querySelector('meta[name="text-action-forbidden"]');
  return meta ? meta.getAttribute('content') || '' : '';
}

function failureBody(status, xhr) {
  const body = xhr && xhr.responseJSON ? xhr.responseJSON : null;
  if (status !== 403) {
    return body;
  }
  // Yetkisiz işlem sessizce başarısız olmaz; yönlendirme de yapılmaz: çağıran "Bu işlem için yetkiniz yok" gösterir.
  return { ...(body || {}), code: 'auth.forbidden', message: forbiddenText() || (body && body.message) || '' };
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
      .fail((xhr) => {
        const status = xhr ? xhr.status : 0;
        const correlation = xhr && xhr.responseJSON && xhr.responseJSON.correlationId;
        if (correlation) {
          lastCorrelation = correlation;
        }
        if (status === 401 && navigation) {
          navigation.assign(loginRedirectUrl(navigation));
        }
        reject({ status, body: failureBody(status, xhr) });
      });
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
