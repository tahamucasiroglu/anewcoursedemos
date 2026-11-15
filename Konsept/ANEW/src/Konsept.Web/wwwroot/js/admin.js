// Admin iskeleti (spec 0011 Req 4): özet /admin/ozet'ten $.ajax ile (api.js, K-6) yüklenir. Bölüm yoksa boş durum metni
// kalır; yetki reddedilirse "Bu işlem için yetkiniz yok", başka hatada genel metin gösterilir (metinler resx'ten,
// bölümün data-text-* öznitelikleriyle).
import { get } from './api.js';

function showMessage(section, text) {
  const message = section.querySelector('#admin-message');
  message.textContent = text;
  message.hidden = false;
}

/**
 * Özeti yükler ve bölümleri gösterir.
 * @param {Document} doc Belge.
 * @returns {Promise<void>}
 */
export async function loadAdminOverview(doc) {
  const section = doc.getElementById('admin');
  if (!section) {
    return;
  }

  try {
    const overview = await get(section.dataset.overviewUrl);
    const sections = (overview && overview.sections) || [];
    const list = section.querySelector('#admin-sections');
    list.replaceChildren(...sections.map((name) => {
      const item = doc.createElement('li');
      item.textContent = name;
      return item;
    }));
    list.hidden = sections.length === 0;
    section.querySelector('#admin-empty').hidden = sections.length > 0;
  } catch (failure) {
    const forbidden = failure && failure.status === 403;
    showMessage(section, forbidden ? section.dataset.textForbidden : section.dataset.textFailed);
  }
}
