// Konseptten Video (spec 0006): model seçimi özeti + modalı, Altyazı / İlerleme çubuğu, onaylanan konsept listesi + salt okunur ayrıntı,
// üretim sırası (Yukarı / Aşağı / ×), Sırayla üret / Durdur / Kaldığı yerden devam et. Kurallar (seçilebilir model, efor, pasiflik nedeni,
// dil uyarısı) sunucudadır; her işlem profilin güncel durumunu döndürür ve sayfa onu çizer. Tüm istekler api.js ($.ajax, K-6) ile
// /uretim ve /konseptler uçlarına. Konsept metni güvenilmeyen veridir: yalnızca textContent. Metinler resx'ten data-text-* ile gelir.
// 0017: iş satırında aşamalar (metin + ikon, yalnızca renk değil), maliyet ve sade hata; "Ajan bağlı değil" ve "sıra durdu" uyarıları; iş
// çalışırken 3 sn'de bir yoklama (durum ≤5 sn'de görünür); sağ sütunda Üretilenler listesi ve kesik çizginin sağında izleme alanı (ajanın
// kısa ömürlü yerel akışı).
import { get, lastCorrelationId, post } from './api.js';

const FOCUSABLE = 'a[href], button:not([disabled]), select:not([disabled]), input:not([disabled])';
const RUNNING = ['producing', 'waiting_for_agent'];
const RESUMABLE = ['stopped', 'failed'];
export const POLL_MS = 3000;
const STAGE_TEXT = { analysis: 'textStageAnalysis', code: 'textStageCode', images: 'textStageImages', voice: 'textStageVoice', render: 'textStageRender', merge: 'textStageMerge' };
const STATE_TEXT = { pending: 'textStatePending', running: 'textStateRunning', done: 'textStateDone', skipped: 'textStateSkipped', failed: 'textStateFailed' };
const STATE_ICON = { pending: '○', running: '◔', done: '✓', skipped: '–', failed: '✕' };

/**
 * Maliyet metni (USD; sentin altı da görünür).
 * @param {number} usd Tutar.
 * @returns {string} Metin.
 */
export function costText(usd) {
  const value = Number(usd || 0);
  return `$${value.toFixed(value > 0 && value < 0.01 ? 4 : 2).replace('.', ',')}`;
}

function fill(template, ...args) {
  return String(template || '').replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
}

/**
 * Bir seçeneğin görünen metni: "Ad — yerel" ve varsa "Key yok", "Yalnız Türkçe", "Lisans uyarısı" etiketleri (Req 3, 4).
 * @param {object} model Model.
 * @param {DOMStringMap} text Metinler.
 * @returns {string} Metin.
 */
export function optionLabel(model, text) {
  const tags = [model.local ? text.textLocal : text.textOnline];
  const availability = { no_key: text.textNoKey, no_permission: text.textNoPermission, unreadable: text.textUnreadable }[model.availability];
  if (availability) {
    tags.push(availability);
  }
  if (model.languageOnly === 'tr') {
    tags.push(text.textOnlyTurkish);
  } else if (model.languageOnly === 'en') {
    tags.push(text.textOnlyEnglish);
  }
  if (model.licenseWarning) {
    tags.push(text.textLicense);
  }
  return `${model.name} — ${tags.join(' · ')}`;
}

/**
 * Sayfayı başlatır (sayfada #production yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @returns {object|undefined} Denetleyici (testler için).
 */
export function initProduction(doc, timers = globalThis) {
  const root = doc.getElementById('production');
  if (!root) {
    return undefined;
  }
  const text = root.dataset;
  const el = (id) => doc.getElementById(id);
  const modal = el('production-models-modal');
  const state = { profileId: null, data: null, page: 1, checked: new Set(), opener: null, busy: false, approved: [], poll: null, live: '' };

  function node(tag, className, content) {
    const element = doc.createElement(tag);
    if (className) {
      element.className = className;
    }
    if (content !== undefined) {
      element.textContent = content;
    }
    return element;
  }

  function setStatus(message) {
    el('production-status').textContent = message || '';
  }

  function failText(error) {
    const message = error && error.body && (error.body.message || error.body.title);
    if (message && error.status && error.status < 500) {
      return message;
    }
    const correlation = lastCorrelationId();
    return correlation ? `${text.textFailed} ${fill(text.textCorrelation, correlation)}` : text.textFailed;
  }

  // --- Durum çizimi ---------------------------------------------------------------------------------------------------------------
  function modelName(id, fallback) {
    const model = (state.data.models || []).find((m) => m.id === id);
    return model ? model.name : id || fallback;
  }

  function renderSettings() {
    const s = state.data.settings;
    const audioOff = !s.audioModel;
    const subtitles = el('production-subtitles');
    subtitles.checked = Boolean(s.subtitles) && !audioOff;
    subtitles.disabled = audioOff; // Ses kapalıyken altyazı pasif; kayıtlı değer korunur (Req 5, AC-5).
    el('production-subtitles-note').hidden = !audioOff;
    el('production-progress').checked = Boolean(s.progressBar);
    el('production-progress').disabled = false;
    const code = s.codeModel ? `${modelName(s.codeModel)}${s.codeEffort ? ` ${s.codeEffort.charAt(0).toUpperCase()}${s.codeEffort.slice(1)}` : ''}` : text.textNotSelected;
    el('production-summary-code').textContent = code;
    el('production-summary-engine').textContent = modelName(s.engine);
    el('production-summary-image').textContent = s.imageModel ? modelName(s.imageModel) : text.textImageOff;
    el('production-summary-audio').textContent = s.audioModel ? modelName(s.audioModel) : text.textAudioOff;
    el('production-models-open').disabled = false;
    const warnings = state.data.languageWarnings || [];
    for (const id of ['production-language-warning', 'production-models-warning']) {
      el(id).textContent = warnings.join(' · ');
      el(id).hidden = warnings.length === 0;
    }
  }

  function moveButton(label, job, direction, disabled) {
    const button = node('button', 'icon-button production-queue__move', direction === 'up' ? '↑' : '↓');
    button.type = 'button';
    button.disabled = disabled;
    button.setAttribute('aria-label', fill(label, job.title));
    button.title = fill(label, job.title);
    button.addEventListener('click', () => command(`/uretim/is/${encodeURIComponent(job.id)}/tasi`, { direction }));
    return button;
  }

  function renderQueue() {
    const jobs = state.data.jobs || [];
    el('production-queue-empty').hidden = jobs.length > 0;
    el('production-queue').replaceChildren(...jobs.map((job, index) => {
      const running = RUNNING.includes(job.status);
      const item = node('li', 'production-queue__item');
      item.dataset.status = job.status;
      const label = node('span', 'production-queue__title', `${job.number}. ${job.title}`);
      const status = node('span', `production-queue__status production-queue__status--${job.status}`, text[`textStatus${job.status.replace(/(^|_)(\w)/g, (_, __, c) => c.toUpperCase())}`] || job.status);
      item.append(label, status);
      if ((job.stages || []).length) {
        const stages = node('ol', 'production-stages');
        stages.setAttribute('aria-label', fill(text.textStages, job.title));
        stages.append(...job.stages.map((stage) => {
          const row = node('li', `production-stages__item production-stages__item--${stage.state}`);
          const icon = node('span', 'production-stages__icon', STATE_ICON[stage.state] || '○');
          icon.setAttribute('aria-hidden', 'true');
          row.append(icon, node('span', '', `${text[STAGE_TEXT[stage.stage]] || stage.stage}: ${text[STATE_TEXT[stage.state]] || stage.state}`));
          return row;
        }));
        item.append(stages, node('span', 'production-queue__cost', fill(text.textCost, costText(job.costUsd))));
      }
      if (job.error) {
        item.append(node('p', 'production-queue__error', job.error)); // Sade açıklama sunucudan (iç ayrıntı yok, Req 11).
      }
      if (RESUMABLE.includes(job.status)) {
        const resume = node('button', 'button production-queue__resume', text.textResume);
        resume.type = 'button';
        resume.addEventListener('click', () => command(`/uretim/is/${encodeURIComponent(job.id)}/devam`, {}));
        item.append(resume);
      }
      // Üretimdeki iş taşınamaz/çıkarılamaz; uçlarda Yukarı/Aşağı pasif (Req 11, AC-11, AC-13).
      const neighbourRunning = (offset) => jobs[index + offset] && RUNNING.includes(jobs[index + offset].status);
      item.append(
        moveButton(text.textMoveUp, job, 'up', running || index === 0 || neighbourRunning(-1)),
        moveButton(text.textMoveDown, job, 'down', running || index === jobs.length - 1 || neighbourRunning(1)));
      const remove = node('button', 'icon-button production-queue__remove', '×');
      remove.type = 'button';
      remove.disabled = running;
      remove.setAttribute('aria-label', fill(text.textRemove, job.title));
      remove.title = fill(text.textRemove, job.title);
      remove.addEventListener('click', () => command(`/uretim/is/${encodeURIComponent(job.id)}/cikar`, {}, true));
      item.append(remove);
      return item;
    }));
    const start = el('production-start');
    start.disabled = Boolean(state.data.startBlock) || state.busy;
    el('production-block').textContent = state.data.startBlockMessage || '';
    el('production-stop').hidden = !state.data.runningHere;
    const offline = el('production-agent-offline');
    if (offline) {
      offline.hidden = state.data.agentConnected !== false || !jobs.some((j) => j.status === 'waiting_for_agent'); // BR-11, Req 15.
    }
    const halted = el('production-halted');
    if (halted) {
      halted.hidden = !state.data.queueHalted;
    }
    announce(jobs);
  }

  // Çalışan işin aşaması değişince ekran okuyucuya kibarca duyurulur.
  function announce(jobs) {
    const live = el('production-live');
    const running = jobs.find((j) => RUNNING.includes(j.status));
    const stage = running && (running.stages || []).find((s) => s.state === 'running');
    const message = stage ? `${running.title}: ${text[STAGE_TEXT[stage.stage]] || stage.stage}` : '';
    if (live && message !== state.live) {
      live.textContent = message;
    }
    state.live = message;
  }

  function render(data) {
    const before = state.data;
    state.data = data;
    renderSettings();
    renderQueue();
    if (state.modalOpen) {
      fillModal();
    }
    const finished = before && (before.jobs || []).some((j) => RUNNING.includes(j.status) && !(data.jobs || []).some((n) => n.id === j.id));
    if (finished) {
      loadVideos(); // Biten iş Üretilenler'e düşer.
    }
    schedulePoll();
  }

  // Üretim tarayıcıdan bağımsızdır; sayfa açıkken çalışan iş varsa durum 3 sn'de bir yenilenir (Req 6, 8).
  function schedulePoll() {
    if (state.poll !== null) {
      timers.clearTimeout(state.poll);
      state.poll = null;
    }
    if (!state.data || !(state.data.jobs || []).some((j) => RUNNING.includes(j.status))) {
      return;
    }
    const profileId = state.profileId;
    state.poll = timers.setTimeout(async () => {
      state.poll = null;
      if (!root.isConnected || profileId !== state.profileId || state.busy) {
        if (root.isConnected && profileId === state.profileId) {
          schedulePoll();
        }
        return;
      }
      try {
        const data = await get(`/uretim/${encodeURIComponent(profileId)}`);
        if (profileId === state.profileId && !state.busy) {
          render(data);
        }
      } catch {
        schedulePoll(); // Geçici hata: bir sonraki yoklamada yeniden denenir.
      }
    }, POLL_MS);
  }

  // --- Üretilenler ve izleme alanı (0017 Req 20, 21) ---------------------------------------------------------------------------------
  async function loadVideos() {
    const list = el('production-videos');
    if (!list) {
      return;
    }
    const profileId = state.profileId;
    el('production-videos-status').textContent = text.textLoading;
    el('production-videos-empty').hidden = true;
    try {
      const videos = await get(`/uretim/${encodeURIComponent(profileId)}/videolar`);
      if (profileId !== state.profileId) {
        return;
      }
      el('production-videos-status').textContent = '';
      el('production-videos-empty').hidden = videos.length > 0;
      list.replaceChildren(...videos.map((video) => {
        const item = node('li', 'production-videos__item');
        const open = node('button', 'production-videos__open', video.title);
        open.type = 'button';
        open.setAttribute('aria-label', fill(text.textWatch, video.title));
        open.addEventListener('click', () => watch(video));
        item.append(open);
        return item;
      }));
    } catch (error) {
      if (profileId === state.profileId) {
        el('production-videos-status').textContent = failText(error);
      }
    }
  }

  async function watch(video) {
    const player = el('production-player');
    const status = el('production-viewer-status');
    status.textContent = text.textLoading;
    try {
      const result = await post(`/uretim/video/${encodeURIComponent(video.id)}/izle`, {});
      player.src = result.url;
      player.setAttribute('aria-label', fill(text.textPlayer, video.title));
      player.hidden = false;
      status.textContent = '';
    } catch (error) {
      player.hidden = true;
      player.removeAttribute('src');
      status.textContent = failText(error); // "Videoyu izlemek için ajan bağlı olmalı" · "Video dosyası bulunamadı".
    }
  }

  // --- Onaylanan liste ve ayrıntı ---------------------------------------------------------------------------------------------------
  function renderApproved(result) {
    const items = result.items || [];
    state.approved = items;
    el('production-approved-empty').hidden = items.length > 0;
    el('production-approved').replaceChildren(...items.map((item) => {
      const row = node('li', 'concept-list__item');
      const box = node('input');
      box.type = 'checkbox';
      box.checked = state.checked.has(item.id);
      box.setAttribute('aria-label', fill(text.textSelect, item.title));
      box.addEventListener('change', () => {
        if (box.checked) {
          state.checked.add(item.id);
        } else {
          state.checked.delete(item.id);
        }
        el('production-enqueue').disabled = state.checked.size === 0;
      });
      const open = node('button', 'concept-list__open', item.title);
      open.type = 'button';
      open.dataset.open = item.id;
      open.addEventListener('click', () => showDetail(item.id));
      row.append(box, open);
      return row;
    }));
    const pages = Math.max(1, Math.ceil((result.total || 0) / (result.pageSize || 50)));
    el('production-pager').hidden = pages <= 1;
    el('production-page').textContent = fill(text.textPage, state.page, pages);
    el('production-prev').disabled = state.page <= 1;
    el('production-next').disabled = state.page >= pages;
    el('production-enqueue').disabled = state.checked.size === 0;
  }

  async function loadApproved() {
    const profileId = state.profileId;
    el('production-approved-status').textContent = text.textLoading;
    try {
      const result = await get(`/konseptler/liste/${encodeURIComponent(profileId)}?durumlar=approved&sayfa=${state.page}`);
      if (profileId === state.profileId) {
        el('production-approved-status').textContent = '';
        renderApproved(result);
      }
    } catch (error) {
      if (profileId === state.profileId) {
        el('production-approved-status').textContent = failText(error);
      }
    }
  }

  function hideDetail() {
    el('production-detail').hidden = true;
    el('production-detail-hint').hidden = false;
  }

  async function showDetail(id) {
    try {
      const concept = await get(`/konseptler/detay/${encodeURIComponent(id)}`);
      const scenes = node('ol', 'concept-detail__scenes');
      (concept.scenes || []).forEach((scene, index) => {
        const item = node('li', 'concept-detail__scene');
        item.append(
          node('h4', 'concept-detail__scene-title', fill(text.textSceneHeading, index + 1, scene.title)),
          node('p', '', `${text.textSceneVisual}: ${scene.visual}`),
          node('p', '', `${text.textSceneNarration}: ${scene.narration}`),
          node('p', '', fill(text.textSceneDuration, scene.durationSeconds)));
        scenes.append(item);
      });
      const edit = node('a', 'button', text.textEditInConcepts);
      edit.href = `/?konsept=${encodeURIComponent(concept.id)}`; // Düzenleme yalnızca Konseptler'de (0015, Ç-11).
      el('production-detail').replaceChildren(node('h3', 'concept-detail__title', concept.title), scenes, edit);
      el('production-detail').hidden = false;
      el('production-detail-hint').hidden = true;
    } catch (error) {
      setStatus(failText(error));
    }
  }

  // --- Komutlar --------------------------------------------------------------------------------------------------------------------
  async function command(url, body, reloadApproved) {
    if (state.busy) {
      return false;
    }
    state.busy = true;
    setStatus(text.textSaving);
    try {
      render(await post(url, body));
      setStatus('');
      if (reloadApproved) {
        await loadApproved();
      }
      return true;
    } catch (error) {
      setStatus(failText(error));
      if (state.data) {
        renderSettings(); // Reddedilen değişiklik geri alınır; kayıtlı değer görünür.
      }
      return false;
    } finally {
      state.busy = false;
      if (state.data) {
        renderQueue();
      }
    }
  }

  function currentSettings() {
    const s = state.data.settings;
    return {
      subtitles: s.subtitles, progressBar: s.progressBar, codeModel: s.codeModel, codeEffort: s.codeEffort,
      engine: s.engine, imageModel: s.imageModel, audioModel: s.audioModel,
    };
  }

  function saveSettings(changes) {
    return command(`/uretim/${encodeURIComponent(state.profileId)}/ayarlar`, { ...currentSettings(), ...changes });
  }

  el('production-subtitles').addEventListener('change', (event) => saveSettings({ subtitles: event.target.checked }));
  el('production-progress').addEventListener('change', (event) => saveSettings({ progressBar: event.target.checked }));
  el('production-start').addEventListener('click', () => command('/uretim/baslat', { profileId: state.profileId }));
  el('production-stop').addEventListener('click', () => command('/uretim/durdur', { profileId: state.profileId }));
  el('production-enqueue').addEventListener('click', async () => {
    // Listedeki görünüş sırasıyla (Req 10).
    // Seçim yalnızca görünen sayfadadır (sayfa değişince temizlenir; review r1 M-1); başarısızlıkta seçim korunur.
    const ids = state.approved.map((item) => item.id).filter((id) => state.checked.has(id));
    if (!(await command('/uretim/sira', { profileId: state.profileId, conceptIds: ids }))) {
      return;
    }
    state.checked.clear();
    el('production-enqueue').disabled = true;
    hideDetail();
    await loadApproved();
  });
  function changePage(delta) {
    state.page += delta;
    state.checked.clear();
    el('production-enqueue').disabled = true;
    loadApproved();
  }
  el('production-prev').addEventListener('click', () => changePage(-1));
  el('production-next').addEventListener('click', () => changePage(1));

  // --- Model seçimi modalı -------------------------------------------------------------------------------------------------------
  function options(select, kind, current, offLabel) {
    const models = (state.data.models || []).filter((m) => m.kind === kind);
    const nodes = models.map((m) => {
      const option = node('option', '', optionLabel(m, text));
      option.value = m.id;
      option.disabled = m.availability === 'no_permission' && m.id !== current; // Yetkisiz model seçilemez (AC-4).
      return option;
    });
    if (offLabel !== undefined) {
      const off = node('option', '', offLabel);
      off.value = '';
      nodes.unshift(off);
    } else if (!current) {
      const none = node('option', '', text.textNotSelected);
      none.value = '';
      nodes.unshift(none);
    }
    select.replaceChildren(...nodes);
    select.value = current || '';
  }

  function fillEffort(effort) {
    const model = (state.data.models || []).find((m) => m.id === el('production-model-code').value);
    const levels = model ? model.effortLevels || [] : [];
    el('production-effort-field').hidden = levels.length === 0; // Efor yalnızca desteklenen seviyeler (AC-2).
    el('production-model-effort').replaceChildren(...levels.map((l) => {
      const option = node('option', '', l.charAt(0).toUpperCase() + l.slice(1));
      option.value = l;
      return option;
    }));
    if (levels.length) {
      el('production-model-effort').value = effort && levels.includes(effort) ? effort : (levels.includes(model.defaultEffort) ? model.defaultEffort : levels[0]);
    }
  }

  function fillModal() {
    const s = state.data.settings;
    el('production-models-error').hidden = !state.data.modelsFailed; // Liste yüklenemedi (AC-22).
    options(el('production-model-code'), 'text', s.codeModel);
    fillEffort(s.codeEffort);
    options(el('production-model-engine'), 'video_engine', s.engine);
    options(el('production-model-image'), 'image', s.imageModel, text.textImageOff);
    options(el('production-model-audio'), 'audio', s.audioModel, text.textAudioOff);
  }

  function readModal() {
    const code = el('production-model-code').value || null;
    return {
      codeModel: code,
      codeEffort: code && !el('production-effort-field').hidden ? el('production-model-effort').value : null,
      engine: el('production-model-engine').value || null,
      imageModel: el('production-model-image').value || null,
      audioModel: el('production-model-audio').value || null,
    };
  }

  function focusables() {
    return [...modal.querySelectorAll(FOCUSABLE)].filter((n) => !n.closest('[hidden]'));
  }

  function openModal() {
    if (!state.data) {
      return;
    }
    state.opener = doc.activeElement;
    state.modalOpen = true;
    fillModal();
    state.snapshot = JSON.stringify(readModal());
    modal.hidden = false;
    focusables()[0].focus(); // Odak modala geçer (AC-21).
  }

  async function closeModal() {
    if (!state.modalOpen) {
      return;
    }
    const changed = JSON.stringify(readModal()) !== state.snapshot;
    state.modalOpen = false;
    modal.hidden = true;
    const opener = state.opener;
    if (opener && typeof opener.focus === 'function' && doc.contains(opener)) {
      opener.focus(); // Odak açan kontrole döner (AC-21).
    }
    if (changed && !state.data.modelsFailed) {
      await saveSettings(readModal()); // Kapanınca seçim kaydedilir (Req 2, AC-1).
    }
  }

  el('production-models-open').addEventListener('click', openModal);
  el('production-models-close').addEventListener('click', closeModal);
  modal.addEventListener('click', (event) => {
    if (event.target === modal) {
      closeModal();
    }
  });
  el('production-model-code').addEventListener('change', () => fillEffort(null)); // Model değişince varsayılan efor (AC-2).
  modal.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      event.preventDefault();
      closeModal();
    } else if (event.key === 'Tab') {
      const nodes = focusables();
      const index = nodes.indexOf(doc.activeElement);
      if (event.shiftKey && index <= 0) {
        event.preventDefault();
        nodes[nodes.length - 1].focus();
      } else if (!event.shiftKey && index === nodes.length - 1) {
        event.preventDefault();
        nodes[0].focus();
      }
    }
  });

  // --- Yükleme ---------------------------------------------------------------------------------------------------------------------
  async function load(profileId) {
    state.profileId = profileId;
    state.page = 1;
    state.checked.clear();
    state.data = null;
    if (state.poll !== null) {
      timers.clearTimeout(state.poll);
      state.poll = null;
    }
    const player = el('production-player');
    if (player) {
      player.hidden = true;
      player.removeAttribute('src');
    }
    hideDetail();
    el('production-models-open').disabled = true;
    el('production-start').disabled = true;
    setStatus(text.textLoading);
    try {
      const data = await get(`/uretim/${encodeURIComponent(profileId)}`);
      if (profileId === state.profileId) {
        render(data);
        setStatus('');
      }
    } catch (error) {
      if (profileId === state.profileId) {
        setStatus(failText(error));
      }
    }
    await Promise.all([loadApproved(), loadVideos()]);
  }

  doc.addEventListener('profile:selected', (event) => {
    if (root.isConnected) {
      load(event.detail.id); // Sayfadan kaldırılmış eski denetleyici yeni profili yüklemez.
    }
  });
  const bar = el('profile-bar');
  if (bar && bar.dataset.selectedId) {
    load(bar.dataset.selectedId);
  }
  return { load, state, openModal, closeModal };
}
