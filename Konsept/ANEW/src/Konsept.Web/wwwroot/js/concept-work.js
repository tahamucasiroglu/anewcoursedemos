// Konseptler sayfasının dört alanı (spec 0014): Konudan üret (öneri listesi + Seçtiklerini üret), Fikirden üret, Üretilenler (filtre,
// sayfalama, ✓ onay, toplu onay) ve salt okunur detay. Üretim sunucuda sürer; süren iş varken /konsept-uretim/{profil} 2 sn'de bir
// yoklanır (tarayıcıda SignalR yok). Tüm istekler api.js ($.ajax, K-6). Model çıktısı güvenilmeyen veridir: yalnızca textContent ile
// yazılır, HTML olarak yorumlanmaz (AC-25). Kurallar (adet, fikir, hazır olma, sahiplik) sunucudadır; burada ön doğrulama ve gösterim.
import { get, post } from './api.js';

const POLL_MS = 2000;
const MAX_COUNT = 100;
const MAX_IDEA = 2000;

/**
 * Adet: yalnızca 1–100 arası tam sayı (AC-2); aksi halde null.
 * @param {string|number|null|undefined} value Girilen değer.
 * @returns {number|null} Adet.
 */
export function parseCount(value) {
  const text = String(value ?? '').trim();
  if (!/^\d+$/.test(text)) {
    return null;
  }
  const count = Number(text);
  return count >= 1 && count <= MAX_COUNT ? count : null;
}

/**
 * Fikir metni sorunu (AC-9): boş/yalnızca boşluk → 'required', 2.000 karakteri aşan → 'tooLong', geçerli → null.
 * @param {string|null|undefined} text Metin.
 * @returns {'required'|'tooLong'|null} Sorun.
 */
export function ideaIssue(text) {
  const value = String(text ?? '').trim();
  if (!value) {
    return 'required';
  }
  return value.length > MAX_IDEA ? 'tooLong' : null;
}

/**
 * Resx kalıbını doldurur ({0}, {1}…); değerdeki özel karakterler kalıp olarak yorumlanmaz.
 * @param {string} template Kalıp.
 * @param {...any} args Değerler.
 * @returns {string} Metin.
 */
export function fill(template, ...args) {
  return String(template || '').replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
}

/**
 * Bir işin ilerleme metni ("Üretiliyor: 3/10", "Durduruldu: …", başarısızsa sade mesaj).
 * @param {object|null} job İş.
 * @param {DOMStringMap} text Metinler.
 * @returns {string} Metin.
 */
export function progressText(job, text) {
  if (!job) {
    return '';
  }
  const topics = job.kind === 'topics';
  switch (job.status) {
    case 'running':
      return topics ? text.textTopicsRunning : fill(text.textRunning, job.processed, job.total);
    case 'stopped':
      return fill(text.textStopped, job.succeeded, job.total);
    case 'failed':
      return job.message || text.textRequestFailed;
    default:
      return topics ? fill(text.textTopicsDone, job.succeeded) : fill(text.textDone, job.succeeded, job.total);
  }
}

/**
 * İşin bilgi satırları: biçim dışı, tekrar, anlamca denetim yapılamadı, yeni öneri yok, üretilemeyenler (AC-3, AC-6, AC-8, AC-11, AC-12).
 * @param {object|null} job İş.
 * @param {DOMStringMap} text Metinler.
 * @returns {string[]} Satırlar.
 */
export function jobNotes(job, text) {
  if (!job || job.status === 'running') {
    return [];
  }
  const notes = [];
  if (job.formatRejected > 0) {
    notes.push(fill(text.textFormatRejected, job.formatRejected));
  }
  if (job.duplicates > 0) {
    notes.push(fill(job.kind === 'topics' ? text.textTopicDuplicates : text.textIdeaDuplicates, job.duplicates));
  }
  if (job.dedupeUnavailable) {
    notes.push(text.textDedupeUnavailable);
  }
  if (job.kind === 'topics' && job.status === 'completed' && job.succeeded === 0) {
    notes.push(text.textNoNewTopics);
  }
  if (job.failures && job.failures.length && job.status !== 'failed') {
    notes.push(fill(text.textFailures, job.failures.length));
    for (const failure of job.failures) {
      const label = job.kind === 'from_idea' ? fill(text.textFailureIdea, failure.label) : failure.label;
      notes.push(`${label}: ${failure.message}`);
    }
  }
  return notes;
}

function errorMessage(error, text) {
  const body = error && error.body;
  return (body && body.message) || text.textRequestFailed;
}

function fieldError(error, field) {
  const errors = error && error.body && error.body.errors;
  if (!errors) {
    return null;
  }
  const key = Object.keys(errors).find((k) => k.toLowerCase() === field.toLowerCase());
  return key && errors[key] && errors[key][0] ? errors[key][0] : null;
}

/**
 * Alanları başlatır (sayfada #concept-work yoksa hiçbir şey yapmaz).
 * @param {Document} doc Belge.
 * @param {{ setTimer?: Function, clearTimer?: Function }} [timers] Zamanlayıcı (testler için).
 * @returns {object|undefined} Denetleyici.
 */
export function initConceptWork(doc, timers = {}) {
  const root = doc.getElementById('concept-work');
  if (!root) {
    return undefined;
  }
  const setTimer = timers.setTimer || ((fn, ms) => globalThis.setTimeout(fn, ms));
  const clearTimer = timers.clearTimer || ((id) => globalThis.clearTimeout(id));
  const text = root.dataset;
  const el = (id) => doc.getElementById(id);
  const state = { profileId: null, work: null, page: 1, timer: null, checkedTopics: new Set(), checkedConcepts: new Set(), lastStatus: {} };

  // --- Yardımcılar -------------------------------------------------------------------------------------------------------------------
  function node(tag, className, content) {
    const element = doc.createElement(tag);
    if (className) {
      element.className = className;
    }
    if (content !== undefined) {
      element.textContent = content; // Güvenilmeyen metin yalnızca düz metin olarak (AC-25).
    }
    return element;
  }

  function setError(id, message) {
    el(id).textContent = message || '';
  }

  function running(lane) {
    const job = state.work && state.work[`${lane}Job`];
    return Boolean(job && job.status === 'running');
  }

  // --- Üretim durumu ------------------------------------------------------------------------------------------------------------------
  function renderSuggestions(suggestions) {
    const list = el('topic-list');
    const ids = suggestions.map((s) => s.id);
    for (const id of [...state.checkedTopics]) {
      if (!ids.includes(id)) {
        state.checkedTopics.delete(id);
      }
    }
    list.replaceChildren(...suggestions.map((s) => {
      const item = node('li', 'suggestion-list__item');
      const label = node('label', 'suggestion-list__label');
      const box = node('input');
      box.type = 'checkbox';
      box.value = s.id;
      box.checked = state.checkedTopics.has(s.id);
      label.append(box, node('span', 'suggestion-list__title', s.title));
      item.append(label, node('p', 'suggestion-list__summary', s.summary));
      return item;
    }));
    el('topic-empty').hidden = suggestions.length > 0;
  }

  function renderJob(lane, job) {
    el(`${lane}-progress`).textContent = progressText(job, text);
    el(`${lane}-progress`).dataset.kind = job && job.status === 'failed' ? 'error' : '';
    el(`${lane}-stop`).hidden = !(job && job.status === 'running');
    el(`${lane}-notes`).replaceChildren(...jobNotes(job, text).map((line) => node('li', '', line)));
  }

  function renderButtons() {
    const readiness = (state.work && state.work.readiness) || {};
    for (const hint of root.querySelectorAll('[data-not-ready]')) {
      hint.hidden = !state.work || Boolean(readiness[hint.dataset.notReady]);
    }
    el('topic-generate').disabled = !readiness.topics || running('topic');
    el('idea-generate').disabled = !readiness.idea || running('idea');
    // Hiç öneri seçili değilken "Seçtiklerini üret" pasif (AC-7).
    el('topic-produce').disabled = !readiness.fromTopics || running('topic') || state.checkedTopics.size === 0;
  }

  function renderWork(work) {
    const previous = state.lastStatus;
    state.work = work;
    renderSuggestions(work.suggestions || []);
    renderJob('topic', work.topicJob);
    renderJob('idea', work.ideaJob);
    renderButtons();
    state.lastStatus = { topic: work.topicJob && work.topicJob.status, idea: work.ideaJob && work.ideaJob.status };
    // Bir konsept işi bitince Üretilenler yenilenir.
    const finished = ['topic', 'idea'].some((lane) => previous[lane] === 'running' && state.lastStatus[lane] !== 'running');
    if (finished) {
      loadList();
    }
    schedule();
  }

  function schedule() {
    if (state.timer) {
      clearTimer(state.timer);
      state.timer = null;
    }
    if (running('topic') || running('idea')) {
      state.timer = setTimer(() => {
        state.timer = null;
        loadWork();
      }, POLL_MS);
    }
  }

  async function loadWork() {
    const profileId = state.profileId;
    if (!profileId) {
      return;
    }
    try {
      const work = await get(`/konsept-uretim/${encodeURIComponent(profileId)}`);
      if (profileId === state.profileId) {
        renderWork(work);
      }
    } catch (error) {
      if (profileId === state.profileId) {
        el('topic-progress').textContent = errorMessage(error, text);
        schedule();
      }
    }
  }

  async function command(url, body, lane, fields) {
    try {
      renderWork(await post(url, { profileId: state.profileId, ...body }));
    } catch (error) {
      let shown = false;
      for (const [field, target] of fields) {
        const message = fieldError(error, field);
        if (message) {
          setError(target, message);
          shown = true;
        }
      }
      if (!shown) {
        el(`${lane}-progress`).textContent = errorMessage(error, text); // Sade Türkçe mesaj (AC-8).
        el(`${lane}-progress`).dataset.kind = 'error';
      }
    }
  }

  el('topic-form').addEventListener('submit', (event) => {
    event.preventDefault();
    const count = parseCount(el('topic-count').value);
    setError('topic-count-error', count ? '' : text.textCountInvalid);
    if (!count) {
      return; // Reddedilen istekte çağrı yok (AC-2).
    }
    state.checkedTopics.clear();
    command('/konsept-uretim/konu', { count }, 'topic', [['Count', 'topic-count-error']]);
  });

  el('topic-list').addEventListener('change', (event) => {
    if (event.target.matches('input[type="checkbox"]')) {
      if (event.target.checked) {
        state.checkedTopics.add(event.target.value);
      } else {
        state.checkedTopics.delete(event.target.value);
      }
      renderButtons();
    }
  });

  el('topic-produce').addEventListener('click', () => {
    const suggestionIds = [...state.checkedTopics];
    if (!suggestionIds.length) {
      return;
    }
    state.checkedTopics.clear();
    setError('topic-produce-error', '');
    command('/konsept-uretim/secilenler', { suggestionIds }, 'topic', [['SuggestionIds', 'topic-produce-error']]);
  });

  el('idea-form').addEventListener('submit', (event) => {
    event.preventDefault();
    const issue = ideaIssue(el('idea-text').value);
    const count = parseCount(el('idea-count').value);
    setError('idea-text-error', issue === 'required' ? text.textIdeaRequired : issue === 'tooLong' ? text.textIdeaTooLong : '');
    setError('idea-count-error', count ? '' : text.textCountInvalid);
    if (issue || !count) {
      return; // Reddedilen istekte çağrı yok (AC-9).
    }
    command('/konsept-uretim/fikir', { idea: el('idea-text').value, count }, 'idea', [['Idea', 'idea-text-error'], ['Count', 'idea-count-error']]);
  });

  for (const lane of ['topic', 'idea']) {
    el(`${lane}-stop`).addEventListener('click', () => command('/konsept-uretim/durdur', { lane }, lane, []));
  }

  for (const opener of root.querySelectorAll('[data-open-models]')) {
    opener.addEventListener('click', () => {
      const button = el('concept-models-open');
      if (button) {
        button.click();
      }
    });
  }

  // --- Üretilenler ----------------------------------------------------------------------------------------------------------------------
  function statusLabel(status) {
    // data-text-status-video_ready → dataset.textStatusVideo_ready.
    return text[`textStatus${status.charAt(0).toUpperCase()}${status.slice(1)}`] || status;
  }

  function badges(concept) {
    const list = [node('span', `badge badge--status badge--${concept.status}`, statusLabel(concept.status))];
    if (concept.wordRepetitionWarning) {
      list.push(node('span', 'badge badge--warning', text.textWarningWords));
    }
    if (concept.durationMismatchWarning) {
      list.push(node('span', 'badge badge--warning', text.textWarningDuration));
    }
    return list;
  }

  function renderList(result) {
    const items = result.items || [];
    el('concepts-status').textContent = '';
    el('concepts-empty').hidden = items.length > 0;
    const ids = items.map((c) => c.id);
    for (const id of [...state.checkedConcepts]) {
      if (!ids.includes(id)) {
        state.checkedConcepts.delete(id);
      }
    }
    el('concept-list').replaceChildren(...items.map((concept) => {
      const row = node('li', 'concept-row');
      row.dataset.id = concept.id;
      const box = node('input', 'concept-row__select');
      box.type = 'checkbox';
      box.value = concept.id;
      box.disabled = concept.status !== 'generated'; // Toplu onay yalnızca "Üretildi" (S-1).
      box.checked = state.checkedConcepts.has(concept.id);
      box.setAttribute('aria-label', fill(text.textSelect, concept.title));
      const open = node('button', 'concept-row__title', concept.title);
      open.type = 'button';
      open.dataset.open = concept.id;
      row.append(box, open, ...badges(concept));
      if (concept.status === 'generated' || concept.status === 'approved') {
        const approved = concept.status === 'approved';
        const toggle = node('button', 'icon-button concept-row__approve', '✓');
        toggle.type = 'button';
        toggle.dataset.approve = approved ? 'undo' : 'do';
        toggle.setAttribute('aria-pressed', approved ? 'true' : 'false');
        toggle.setAttribute('aria-label', fill(approved ? text.textUnapprove : text.textApprove, concept.title));
        toggle.title = toggle.getAttribute('aria-label');
        row.append(toggle);
      }
      return row;
    }));
    const pages = Math.max(1, Math.ceil((result.total || 0) / (result.pageSize || 50)));
    el('concepts-pager').hidden = pages <= 1;
    el('concepts-page').textContent = fill(text.textPage, result.page || 1, pages);
    el('concepts-prev').disabled = state.page <= 1;
    el('concepts-next').disabled = state.page >= pages;
    el('concepts-approve-selected').disabled = state.checkedConcepts.size === 0; // Hiç işaret yokken pasif (AC-27).
  }

  function statuses() {
    return [...el('concept-filter').querySelectorAll('input:checked')].map((box) => box.value).join(',');
  }

  async function loadList() {
    const profileId = state.profileId;
    if (!profileId) {
      return;
    }
    el('concepts-status').textContent = text.textLoading;
    try {
      const result = await get(`/konseptler/liste/${encodeURIComponent(profileId)}?durumlar=${encodeURIComponent(statuses())}&sayfa=${state.page}`);
      if (profileId === state.profileId) {
        renderList(result);
      }
    } catch {
      if (profileId === state.profileId) {
        el('concepts-status').textContent = text.textLoadFailed;
      }
    }
  }

  async function changeApproval(ids, undo) {
    el('concepts-status').textContent = text.textApproving;
    try {
      await post(undo ? '/konseptler/onayi-geri-al' : '/konseptler/onayla', { ids });
      for (const id of ids) {
        state.checkedConcepts.delete(id);
      }
      await loadList();
    } catch (error) {
      el('concepts-status').textContent = (error && error.body && error.body.message) || text.textApproveFailed; // Durum değişmez.
    }
  }

  el('concept-list').addEventListener('click', (event) => {
    const approve = event.target.closest('[data-approve]');
    if (approve) {
      changeApproval([approve.closest('[data-id]').dataset.id], approve.dataset.approve === 'undo');
      return;
    }
    const open = event.target.closest('[data-open]');
    if (open) {
      openDetail(open.dataset.open);
    }
  });

  el('concept-list').addEventListener('change', (event) => {
    if (event.target.matches('.concept-row__select')) {
      if (event.target.checked) {
        state.checkedConcepts.add(event.target.value);
      } else {
        state.checkedConcepts.delete(event.target.value);
      }
      el('concepts-approve-selected').disabled = state.checkedConcepts.size === 0;
    }
  });

  el('concepts-approve-selected').addEventListener('click', () => {
    if (state.checkedConcepts.size) {
      changeApproval([...state.checkedConcepts], false);
    }
  });

  el('concept-filter').addEventListener('change', () => {
    state.page = 1;
    loadList();
  });
  el('concepts-prev').addEventListener('click', () => {
    state.page = Math.max(1, state.page - 1);
    loadList();
  });
  el('concepts-next').addEventListener('click', () => {
    state.page += 1;
    loadList();
  });

  // --- Detay (salt okunur, düz metin; AC-25) ---------------------------------------------------------------------------------------------
  function renderDetail(concept) {
    const article = el('concept-detail');
    const scenes = node('ol', 'concept-detail__scenes');
    concept.scenes.forEach((scene, index) => {
      const item = node('li', 'concept-detail__scene');
      item.append(
        node('h4', 'concept-detail__scene-title', fill(text.textSceneHeading, index + 1, scene.title)),
        node('p', '', `${text.textSceneVisual}: ${scene.visual}`),
        node('p', '', `${text.textSceneNarration}: ${scene.narration}`),
        node('p', 'concept-detail__duration', fill(text.textSceneDuration, scene.durationSeconds)));
      scenes.append(item);
    });
    const head = node('div', 'concept-detail__badges');
    head.append(...badges(concept));
    article.replaceChildren(node('h3', 'concept-detail__title', concept.title), head, scenes);
    article.hidden = false;
    el('concept-detail-hint').hidden = true;
  }

  async function openDetail(id) {
    try {
      renderDetail(await get(`/konseptler/detay/${encodeURIComponent(id)}`));
      el('concept-detail').focus();
    } catch {
      el('concept-detail').hidden = true;
      el('concept-detail-hint').hidden = false;
      el('concept-detail-hint').textContent = text.textDetailFailed;
    }
  }

  function select(profileId) {
    state.profileId = profileId;
    state.work = null;
    state.page = 1;
    state.lastStatus = {};
    state.checkedTopics.clear();
    state.checkedConcepts.clear();
    el('concept-detail').hidden = true;
    el('concept-detail-hint').hidden = false;
    renderButtons();
    loadWork();
    loadList();
  }

  doc.addEventListener('profile:selected', (event) => select(event.detail.id));
  doc.addEventListener('concept-settings:changed', () => loadWork()); // Model seçimi değişince hazır olma yenilenir (AC-1).
  const bar = el('profile-bar');
  if (bar && bar.dataset.selectedId) {
    select(bar.dataset.selectedId);
  }
  return { state, loadWork, loadList, openDetail };
}
