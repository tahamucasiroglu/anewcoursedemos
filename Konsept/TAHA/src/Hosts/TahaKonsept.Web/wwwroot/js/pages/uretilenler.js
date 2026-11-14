/**
 * TK.uretilenler — Üretilen Videolar sayfası.
 *  - Sol: liste (küçük resim, başlık, süre, tarih, ücret, sürüm; arama). Küçük resimler ayrıntıdan arka planda tek tek alınır.
 *  - Orta: HTML5 oynatıcı (ses + tam ekran; adres ajanın yerel sunucusu) ve işlemler: Klasörde aç, Sistem oynatıcısında aç,
 *    Yeniden üret (önce konsept düzenleme penceresi), Yeni sürüm (motor/görsel/ses/altyazı değiştir → yalnızca değişen aşamalar;
 *    hangi aşamaların yeniden çalışacağı canlı gösterilir), Sil (onay penceresi).
 *  - Sağ sekmeler: Seslendirme + sahneler (ekrandaki yazılış ↔ seslendirilen okunuş, farklar işaretli; süreye tıkla → oraya git),
 *    Konsept metni, Ayrıntılar ve ücret (aşama aşama ücret/süre/token, kullanılan modeller, zamanlar, sürüm geçmişi).
 *  - ?video={id} ile doğrudan açılır.
 * MVC uçları: /Uretilenler/{Liste, Detay, KlasordeAc, SistemOynaticisi, YenidenUret, YeniSurum, Sil} ve model/ses listesi için
 * /KonsepttenVideo/{Modeller, Sesler, Ajanlar}. Ortak parçalar: TK.production. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;
    var P = null;
    var state = { videos: [], details: {}, current: null, filter: '', models: null, agentVramMb: null, voices: {}, thumbQueue: [], thumbBusy: false };

    /* ---------------- Liste ---------------- */
    function thumbHtml(v) {
        var d = state.details[String(v.id).toLowerCase()];
        var img = d && d.thumbnailUrl ? '<img src="' + U.escape(d.thumbnailUrl) + '" alt="" loading="lazy" />' : U.icon('play', 'tk-ic-sm');
        return '<span class="tk-thumb ur-thumb' + (P.isVertical(v.format) ? ' is-vertical' : '') + '" data-thumb="' + U.escape(v.id) + '">' + img +
            '<span class="ur-thumb-time">' + U.escape(P.clock(v.durationSec)) + '</span></span>';
    }

    function renderList() {
        var $l = $('#ur-list');
        var f = state.filter.toLocaleLowerCase('tr-TR');
        var list = $.grep(state.videos, function (v) { return !f || String(v.title || '').toLocaleLowerCase('tr-TR').indexOf(f) >= 0; });
        var total = 0;
        $.each(state.videos, function (_, v) { total += Number(v.costUsd || 0); });
        $('#ur-count').text(state.videos.length ? state.videos.length + ' video · toplam ' + U.money(total) : 'Boş');
        if (!list.length) {
            $l.html(state.videos.length ? U.empty('list', 'Eşleşen video yok', 'Arama ifadesini değiştirin.') : U.empty('film', 'Henüz video üretilmedi', 'Konseptten Video sayfasından üretim başlatın.'));
            return;
        }
        var html = '<ul class="tk-list">';
        $.each(list, function (_, v) {
            var sel = state.current && P.isSameId(state.current.id, v.id);
            html += '<li class="tk-list-item ur-item' + (sel ? ' is-selected' : '') + '" data-id="' + U.escape(v.id) + '">' + thumbHtml(v) +
                '<div class="tk-list-main"><div class="tk-list-title">' + U.escape(v.title || 'Adsız video') + '</div>' +
                '<div class="tk-list-sub"><span>' + U.escape(U.dateTime(v.createdAt)) + '</span><span class="ur-cost">' + U.escape(U.money(v.costUsd)) + '</span>' +
                (v.version > 1 ? '<span class="tk-badge">Sürüm ' + U.escape(v.version) + '</span>' : '') + '</div></div></li>';
        });
        $l.html(html + '</ul>');
    }

    function loadList() {
        return TK.api.get('/Uretilenler/Liste', null, { silent: true })
            .done(function (d) {
                state.videos = U.items(d);
                renderList();
                state.thumbQueue = $.map(state.videos.slice(0, 30), function (v) { return v.id; });
                nextThumb();
            })
            .fail(function () { $('#ur-list').html(U.empty('alert', 'Liste alınamadı', 'API şu an yanıt vermiyor.')); });
    }

    /** Küçük resimleri ayrıntıdan sırayla (aynı anda tek istek) yükler. */
    function nextThumb() {
        if (state.thumbBusy || !state.thumbQueue.length) { return; }
        var id = state.thumbQueue.shift();
        if (state.details[String(id).toLowerCase()]) { nextThumb(); return; }
        state.thumbBusy = true;
        TK.api.get('/Uretilenler/Detay/' + encodeURIComponent(id), null, { silent: true }).done(function (d) {
            if (!d || $.isArray(d)) { return; }
            state.details[String(id).toLowerCase()] = d;
            if (!d.agentOnline) { state.thumbQueue = []; }
            if (d.thumbnailUrl) {
                $('[data-thumb="' + id + '"]').find('img, svg').remove().end().prepend($('<img alt="" loading="lazy" />').attr('src', d.thumbnailUrl));
            }
        }).always(function () { state.thumbBusy = false; window.setTimeout(nextThumb, 150); });
    }

    /* ---------------- Oynatıcı ---------------- */
    function renderPlayer(v) {
        var $p = $('#ur-player').empty();
        $('#ur-actions').prop('hidden', !v);
        $('#ur-fullscreen').prop('hidden', true);
        $('#ur-player-bar').empty();
        if (!v) {
            $p.removeClass('is-vertical').html('<div class="tk-player-empty">' + U.icon('film', 'tk-ic-lg') + '<div>Henüz video seçilmedi</div></div>');
            $('#ur-player-sub').text('Soldan bir video seçin');
            return;
        }
        $('#ur-player-sub').text(v.title || '');
        $p.toggleClass('is-vertical', P.isVertical(v.format));
        if (!v.loaded) {
            $p.html('<div class="tk-player-empty">' + U.icon('film', 'tk-ic-lg') + '<div>Yükleniyor…</div></div>');
            return;
        }
        if (v.playbackUrl) {
            $('<video controls playsinline preload="metadata"></video>').attr('src', v.playbackUrl).attr('poster', v.thumbnailUrl || null).appendTo($p)
                .on('error', function () { $p.html('<div class="tk-player-empty">' + U.icon('alert', 'tk-ic-lg') + '<div><strong>Video açılamadı.</strong><br/>Masaüstü programı çalışıyor ve dosya yerinde mi?</div></div>'); });
            $('#ur-fullscreen').prop('hidden', false);
        } else {
            $p.html('<div class="tk-player-empty">' + U.icon('monitor', 'tk-ic-lg') + '<div><strong>' + (v.agentOnline ? 'Video oynatılamıyor.' : 'Masaüstü programına ulaşılamıyor.') + '</strong><br/>' +
                U.escape(v.playbackMessage || 'Video bilgisayarınızda saklanır; bilgisayar açık ve program çalışırken burada oynatılır.') + '</div></div>');
        }
        $('#ur-player-bar').html([
            '<span class="tk-badge">' + U.escape(P.clock(v.durationSec)) + '</span>',
            '<span class="tk-badge">' + (P.isVertical(v.format) ? 'Dikey 9:16' : 'Yatay 16:9') + (v.width ? ' · ' + v.width + '×' + v.height : '') + '</span>',
            v.version > 1 ? '<span class="tk-badge">Sürüm ' + U.escape(v.version) + '</span>' : '',
            '<span class="tk-badge is-accent">' + U.escape(U.money(v.costUsd)) + '</span>',
            v.agentOnline ? '' : '<span class="tk-badge is-warning">Ajan çevrimdışı</span>'
        ].join(' '));
    }

    function fullscreen() {
        var el = $('#ur-player video')[0];
        if (!el) { return; }
        var fn = el.requestFullscreen || el.webkitRequestFullscreen;
        if (fn) { fn.call(el); }
    }

    function seek(sec) {
        var el = $('#ur-player video')[0];
        if (!el) { TK.toast.info('Oynatıcı hazır değil.'); return; }
        el.currentTime = Math.max(0, Number(sec || 0));
        el.play();
    }

    /* ---------------- Sağ sekmeler ---------------- */
    function renderScenes(v) {
        var scenes = v.scenes || [];
        if (!scenes.length) { $('#ur-scenes').html(U.empty('list', 'Sahne bilgisi yok', '')); return; }
        var html = '<p class="tk-hint" style="margin:0 0 6px">Ekranda yazılış, seslendirmede okunuş kullanılır. <mark class="ur-mark">İşaretli</mark> kelimeler okunuşta değişenlerdir.</p>';
        $.each(scenes, function (_, s) {
            var start = Number(s.startSec || 0);
            var display = s.displayText || '';
            var spoken = s.spokenText || '';
            html += '<div class="ur-scene"><div class="ur-scene-head"><span class="kv-scene-no">' + (Number(s.index || 0) + 1) + '</span>' +
                '<strong>' + U.escape(s.title || '') + '</strong>' +
                '<button type="button" class="ur-seek" data-seek="' + start + '" title="Videoda bu sahneye git">' + U.escape(P.clock(start)) + ' – ' + U.escape(P.clock(start + Number(s.durationSec || 0))) + '</button></div>' +
                (s.onScreen ? '<div class="ur-text-label">Ekranda</div><div class="ur-text">' + U.escape(s.onScreen) + '</div>' : '') +
                '<div class="ur-text-label">Yazılış (altyazı)</div><div class="ur-text">' + U.escape(display) + '</div>' +
                '<div class="ur-text-label">Okunuş (seslendirme)</div><div class="ur-text is-spoken">' + (spoken && spoken !== display ? P.diffHtml(display, spoken) : U.escape(spoken || display) + (spoken ? ' <span class="tk-badge">aynı</span>' : '')) + '</div></div>';
        });
        $('#ur-scenes').html(html);
    }

    function renderConcept(v) {
        var d = v.document || {};
        var chips = [];
        chips.push('<span class="tk-badge">' + (P.isVertical(d.format) ? 'Dikey' : 'Yatay') + '</span>');
        if (d.targetDurationSec) { chips.push('<span class="tk-badge">Hedef ' + U.escape(P.clock(d.targetDurationSec)) + '</span>'); }
        $.each([d.tone, d.audience, d.narrativeStyle], function (_, x) { if (x) { chips.push('<span class="tk-badge">' + U.escape(x) + '</span>'); } });
        var html = '<h3 style="margin:0">' + U.escape(d.title || v.title || '') + '</h3>' +
            (d.hook ? '<p class="ur-hook">“' + U.escape(d.hook) + '”</p>' : '') +
            (d.summary ? '<p class="tk-muted" style="margin:6px 0">' + U.escape(d.summary) + '</p>' : '') +
            '<div class="ur-chips">' + chips.join('') + '</div>';
        if ((d.scenes || []).length) {
            html += '<div class="ur-section-title">Sahneler</div>' + $.map(d.scenes, function (s, i) {
                return '<div class="ur-concept-scene"><strong>' + (i + 1) + '. ' + U.escape(s.title || '') + '</strong>' +
                    (s.onScreen ? '<div class="tk-hint">Ekranda: ' + U.escape(s.onScreen) + '</div>' : '') +
                    '<div class="ur-text">' + U.escape(s.narration || '') + '</div></div>';
            }).join('');
        }
        if ((d.sources || []).length) {
            html += '<div class="ur-section-title">Kaynaklar</div><ul style="margin:0;padding-left:18px">' + $.map(d.sources, function (s) {
                var safe = /^https?:\/\//i.test(s.url || '') ? s.url : '';
                return '<li>' + (safe ? '<a href="' + U.escape(safe) + '" target="_blank" rel="noopener noreferrer">' + U.escape(s.title || s.url) + '</a>' : U.escape(s.title || s.url)) + '</li>';
            }).join('') + '</ul>';
        }
        if ((d.tags || []).length) {
            html += '<div class="ur-section-title">Etiketler</div><div class="ur-chips">' + $.map(d.tags, function (t) { return '<span class="tk-badge is-accent">#' + U.escape(t) + '</span>'; }).join('') + '</div>';
        }
        $('#ur-concept').html(html);
    }

    function renderDetails(v) {
        var s = v.settings || {};
        var rows = v.costBreakdown || [];
        var totalMs = 0, tokIn = 0, tokOut = 0, sum = 0;
        var table = '<table class="tk-table ur-cost-table"><thead><tr><th>Aşama</th><th class="is-num">Süre</th><th class="is-num">Token</th><th class="is-num">Ücret</th></tr></thead><tbody>';
        $.each(rows, function (_, r) {
            var name = P.STAGE_NAMES[P.stageName(r.stage)] || P.stageName(r.stage);
            totalMs += Number(r.durationMs || 0); tokIn += Number(r.inputTokens || 0); tokOut += Number(r.outputTokens || 0); sum += Number(r.costUsd || 0);
            table += '<tr><td>' + U.escape(name) + (r.reused ? ' <span class="tk-badge is-success" title="Önceki sürümden kopyalandı">yeniden kullanıldı</span>' : '') + '</td>' +
                '<td class="is-num">' + U.escape(P.duration(r.durationMs) || '—') + '</td>' +
                '<td class="is-num">' + (r.inputTokens || r.outputTokens ? U.escape(fmtInt(r.inputTokens) + ' / ' + fmtInt(r.outputTokens)) : '—') + '</td>' +
                '<td class="is-num">' + U.escape(U.money(r.costUsd)) + '</td></tr>';
        });
        table += '</tbody><tfoot><tr><td>Toplam</td><td class="is-num">' + U.escape(P.duration(totalMs) || '—') + '</td><td class="is-num">' +
            U.escape(fmtInt(tokIn) + ' / ' + fmtInt(tokOut)) + '</td><td class="is-num">' + U.escape(U.money(sum)) + '</td></tr></tfoot></table>';

        var models = [
            ['Video kodu', P.summary('code', s, state.models || [])],
            ['Video motoru', P.summary('engine', s, state.models || [])],
            ['Görsel', P.summary('image', s, state.models || [])],
            ['Ses', P.summary('speech', s, state.models || [])],
            ['Altyazı / çubuk', P.summary('compose', s)],
            ['Kare hızı', (v.fps || s.fps || 30) + ' fps']
        ];
        var wall = v.startedAt && v.finishedAt ? P.duration(new Date(v.finishedAt) - new Date(v.startedAt)) : '';
        var times = [
            ['Sıraya alındı', U.dateTime(v.createdAt)], ['Başladı', U.dateTime(v.startedAt)], ['Bitti', U.dateTime(v.finishedAt)], ['Toplam süre', wall]
        ];
        var html = '<div class="ur-total"><strong>' + U.escape(U.money(v.costUsd)) + '</strong><span class="tk-muted">bu sürümün toplam ücreti</span></div>' +
            '<div class="ur-section-title">Aşama aşama ücret ve süre</div>' + (rows.length ? table : U.empty('chart', 'Ücret dökümü yok', '')) +
            '<div class="ur-section-title">Kullanılan modeller</div><dl class="tk-kv">' + kv(models) + '</dl>' +
            '<div class="ur-section-title">Zamanlar</div><dl class="tk-kv">' + kv(times) + '</dl>' +
            '<div class="ur-section-title">Sürüm geçmişi</div>' + versionsHtml(v);
        $('#ur-details').html(html);
    }

    function kv(rows) {
        return $.map(rows, function (r) { return '<dt>' + U.escape(r[0]) + '</dt><dd>' + U.escape(r[1] || '—') + '</dd>'; }).join('');
    }

    function fmtInt(n) { return Number(n || 0).toLocaleString('tr-TR'); }

    function versionsHtml(v) {
        var list = $.grep(state.videos, function (x) { return P.isSameId(x.conceptId, v.conceptId); });
        if (!$.grep(list, function (x) { return P.isSameId(x.id, v.id); }).length) { list.push(v); }
        list.sort(function (a, b) { return Number(b.version || 1) - Number(a.version || 1) || new Date(b.createdAt) - new Date(a.createdAt); });
        return '<ul class="tk-list ur-versions">' + $.map(list, function (x) {
            var cur = P.isSameId(x.id, v.id);
            return '<li class="tk-list-item' + (cur ? ' is-current is-selected' : '') + '" data-version="' + U.escape(x.id) + '">' +
                '<span class="tk-badge' + (cur ? ' is-accent' : '') + '">Sürüm ' + U.escape(x.version || 1) + '</span>' +
                '<div class="tk-list-main"><div class="tk-list-sub">' + U.escape(U.dateTime(x.createdAt) + ' · ' + P.clock(x.durationSec) + ' · ' + U.money(x.costUsd)) + '</div></div>' +
                (cur ? '<span class="tk-hint">açık</span>' : '') + '</li>';
        }).join('') + '</ul>';
    }

    function renderInfo(v) {
        if (!v || !v.loaded) {
            var msg = v ? U.skeleton(4) : U.empty('info', 'Video seçilmedi', 'Soldaki listeden bir video seçin.');
            $('#ur-scenes, #ur-concept, #ur-details').html(msg);
            return;
        }
        renderScenes(v);
        renderConcept(v);
        renderDetails(v);
    }

    function select(id) {
        var summary = $.grep(state.videos, function (v) { return P.isSameId(v.id, id); })[0] || { id: id };
        var old = $('#ur-player video')[0];
        if (old) { old.pause(); }
        state.current = $.extend({}, summary, { loaded: false });
        renderList();
        renderPlayer(state.current);
        renderInfo(state.current);
        if (window.history && window.history.replaceState) { window.history.replaceState(null, '', '?video=' + encodeURIComponent(id)); }
        TK.api.get('/Uretilenler/Detay/' + encodeURIComponent(id)).done(function (v) {
            if (!v || $.isArray(v) || !state.current || !P.isSameId(state.current.id, id)) { return; }
            state.details[String(id).toLowerCase()] = v;
            state.current = $.extend({}, summary, v, { loaded: true });
            renderPlayer(state.current);
            renderInfo(state.current);
        }).fail(function () {
            state.current = null;
            renderPlayer(null);
            renderInfo(null);
        });
    }

    /* ---------------- Yeni sürüm ---------------- */
    function ensureModels() {
        if (state.models) { return $.Deferred().resolve().promise(); }
        return TK.api.get('/KonsepttenVideo/Modeller').done(function (d) { state.models = U.items(d); });
    }

    function loadVoices(modelKey) {
        if (state.voices[modelKey]) { return $.Deferred().resolve(state.voices[modelKey]).promise(); }
        return TK.api.get('/KonsepttenVideo/Sesler', { modelKey: modelKey }, { silent: true }).done(function (x) { state.voices[modelKey] = x; });
    }

    function chips(stages, kept) {
        return $.map(stages, function (st) { return '<span class="ur-stage-chip' + (kept ? ' is-kept' : '') + '">' + U.escape(P.STAGE_NAMES[st] || st) + '</span>'; }).join('');
    }

    function newVersion() {
        var v = state.current;
        if (!v || !v.loaded) { return; }
        ensureModels().done(function () {
            var base = $.extend({}, v.settings || {});
            var ctx = { models: state.models, agentVramMb: state.agentVramMb, loadVoices: loadVoices };
            var tabs = [['engine', 'Motor ve kod'], ['image', 'Görsel'], ['speech', 'Ses'], ['compose', 'Altyazı ve çubuk']];
            var editors = {};
            var $b = $('<div></div>');
            var $rerun = $('<div class="ur-rerun" aria-live="polite"></div>');
            var $tabs = $('<div class="tk-tabs ur-version-tabs" role="tablist"></div>');
            $b.append('<p class="tk-muted" style="margin:0 0 10px">Değiştirdiğiniz ayara göre yalnızca etkilenen aşamalar yeniden çalışır; diğerleri (LLM çağrıları ve render edilmiş sahneler dahil) yeniden kullanılır.</p>', $rerun, $tabs);
            $.each(tabs, function (i, t) {
                $tabs.append('<button type="button" class="tk-tab' + (i === 0 ? ' is-active' : '') + '" data-vtab="' + t[0] + '">' + t[1] + '</button>');
                var $panel = $('<div class="ur-version-panel"></div>').attr('data-vpanel', t[0]).prop('hidden', i !== 0);
                if (t[0] === 'engine') {
                    var code = P.editor('code', base, ctx);
                    var engine = P.editor('engine', base, ctx);
                    editors.code = code;
                    editors.engine = engine;
                    $panel.append('<div class="tk-label" style="margin-bottom:8px">Video motoru</div>', engine.$el, '<div class="tk-label" style="margin:14px 0 8px">Video kodu modeli</div>', code.$el);
                } else {
                    editors[t[0]] = P.editor(t[0], base, ctx);
                    $panel.append(editors[t[0]].$el);
                }
                $b.append($panel);
            });
            $b.append('<div class="tk-field" style="margin-top:14px"><label for="ur-v-note">Not (isteğe bağlı)</label><input id="ur-v-note" class="tk-input" maxlength="200" placeholder="Ör. Supertonic yerine Chatterbox denendi" /></div>');

            function next() {
                var s = $.extend({}, base);
                $.each(editors, function (_, ed) { $.extend(s, ed.read()); });
                return s;
            }
            function refresh() {
                var plan = P.rerunStages(base, next());
                $rerun.html('<div class="ur-rerun-row"><span class="tk-label">Yeniden çalışacak</span>' + chips(plan.stages) + '</div>' +
                    '<div class="ur-rerun-row"><span class="tk-label">Yeniden kullanılacak</span>' + (plan.reused.length ? chips(plan.reused, true) : '<span class="tk-hint">—</span>') + '</div>' +
                    (plan.changed.length ? '' : '<div class="tk-hint" style="margin-top:8px">Henüz bir ayar değiştirmediniz; yalnızca son dosya yeniden oluşturulur.</div>'));
                return plan;
            }
            $b.on('click', '[data-vtab]', function () {
                var k = $(this).attr('data-vtab');
                $tabs.find('.tk-tab').removeClass('is-active').filter(this).addClass('is-active');
                $b.find('[data-vpanel]').each(function () { $(this).prop('hidden', $(this).attr('data-vpanel') !== k); });
            });
            $b.on('change input', function () { window.setTimeout(refresh, 0); });
            $b.on('click', '.kv-seg button', function () { window.setTimeout(refresh, 0); });
            refresh();

            var $go = $('<button type="button" class="tk-btn tk-btn-primary">' + U.icon('layers') + ' Yeni sürümü üret</button>');
            var h = TK.modal.open({ title: 'Yeni sürüm — ' + (v.title || ''), size: 'lg', body: $b, footer: $go, hint: 'Sürüm ' + (Number(v.version || 1) + 1) + ' olarak sıraya eklenir.' });
            $go.on('click', function () {
                var plan = refresh();
                var body = { settings: next(), changed: plan.changed, note: $.trim($('#ur-v-note').val()) || null };
                U.busy($go, TK.api.post('/Uretilenler/YeniSurum/' + encodeURIComponent(v.id), body)).done(function () {
                    h.close(true);
                    queuedToast('Yeni sürüm üretim sırasına alındı.');
                });
            });
        });
    }

    function queuedToast(text) {
        TK.toast.success(text + ' İlerlemeyi Konseptten Video sayfasında izleyebilirsiniz; sıra duruyorsa "Sırayla üret" deyin.');
    }

    /* ---------------- Yeniden üret (önce düzenle) ---------------- */
    function regenerate() {
        var v = state.current;
        if (!v || !v.loaded) { return; }
        var d = $.extend(true, {}, v.document || { title: v.title, scenes: [] });
        var $b = $('<div></div>');
        $b.append('<p class="tk-muted" style="margin:0 0 12px">Konsepti düzenleyin; video bu metinle <strong>baştan</strong> üretilir (tüm aşamalar yeniden çalışır).</p>');
        function fieldEl(label, name, value, multi, rows) {
            var $f = $('<div class="tk-field"><label></label></div>');
            $f.find('label').text(label);
            var $i = multi ? $('<textarea class="tk-textarea"></textarea>').attr('rows', rows || 2) : $('<input class="tk-input" />');
            $i.attr('name', name).val(value || '');
            return $f.append($i);
        }
        $b.append(fieldEl('Başlık', 'title', d.title), fieldEl('Kanca (ilk cümle)', 'hook', d.hook), fieldEl('Özet', 'summary', d.summary, true, 2));
        $.each(d.scenes || [], function (i, s) {
            var $s = $('<div class="kv-scene"></div>').attr('data-scene', i);
            var $head = $('<div class="kv-scene-head"><span class="kv-scene-no"></span></div>');
            $head.find('.kv-scene-no').text(i + 1);
            $head.append($('<input class="tk-input" name="sceneTitle" aria-label="Sahne başlığı" />').val(s.title || ''));
            $s.append($head, fieldEl('Ekranda', 'onScreen', s.onScreen, true, 2), fieldEl('Seslendirme', 'narration', s.narration, true, 3));
            $b.append($s);
        });
        var $go = $('<button type="button" class="tk-btn tk-btn-primary">' + U.icon('rotate') + ' Düzenle ve yeniden üret</button>');
        var h = TK.modal.open({ title: 'Yeniden üret — ' + (v.title || ''), size: 'lg', body: $b, footer: $go, hint: 'Güncel üretim ayarları (Konseptten Video) kullanılır.' });
        $go.on('click', function () {
            d.title = $.trim($b.find('[name=title]').val());
            d.hook = $.trim($b.find('[name=hook]').val());
            d.summary = $.trim($b.find('[name=summary]').val());
            if (!d.title) { TK.toast.warning('Başlık boş olamaz.'); return; }
            $b.find('[data-scene]').each(function () {
                var i = +$(this).attr('data-scene');
                d.scenes[i].title = $.trim($(this).find('[name=sceneTitle]').val());
                d.scenes[i].onScreen = $(this).find('[name=onScreen]').val();
                d.scenes[i].narration = $(this).find('[name=narration]').val();
            });
            U.busy($go, TK.api.post('/Uretilenler/YenidenUret/' + encodeURIComponent(v.id), { document: d })).done(function () {
                h.close(true);
                queuedToast('Video yeniden üretim sırasına alındı.');
            });
        });
    }

    /* ---------------- İşlemler ---------------- */
    function act(name, $btn) {
        var v = state.current;
        if (!v) { return; }
        var id = encodeURIComponent(v.id);
        if (name === 'folder') { U.busy($btn, TK.api.post('/Uretilenler/KlasordeAc/' + id)).done(function () { TK.toast.info('Klasör bilgisayarınızda açılıyor.'); }); }
        if (name === 'system') { U.busy($btn, TK.api.post('/Uretilenler/SistemOynaticisi/' + id)).done(function () { TK.toast.info('Video sistem oynatıcısında açılıyor.'); }); }
        if (name === 'version') { newVersion(); }
        if (name === 'regenerate') { regenerate(); }
        if (name === 'delete') {
            TK.modal.confirm('Videoyu sil', '"' + (v.title || 'Video') + '" (sürüm ' + (v.version || 1) + ') kalıcı olarak silinecek; dosya bilgisayarınızdan da silinir. Emin misiniz?', 'Sil').done(function () {
                U.busy($btn, TK.api.del('/Uretilenler/Sil/' + id)).done(function () {
                    TK.toast.info('Video silindi.');
                    var old = $('#ur-player video')[0];
                    if (old) { old.pause(); }
                    state.current = null;
                    renderPlayer(null);
                    renderInfo(null);
                    if (window.history && window.history.replaceState) { window.history.replaceState(null, '', window.location.pathname); }
                    loadList();
                });
            });
        }
    }

    function init() {
        if (!$('#tk-uretilenler').length) { return; }
        U = TK.util;
        P = TK.production;
        $('#ur-list').html(U.skeleton(5));
        renderInfo(null);

        $('#ur-list').on('click', '.tk-list-item', function () { select($(this).attr('data-id')); });
        $('#ur-search').on('input', function () { state.filter = $.trim($(this).val()); renderList(); });
        $('#ur-refresh').on('click', function () { U.busy($(this), loadList()); });
        $('#ur-actions').on('click', '[data-act]', function () { act($(this).attr('data-act'), $(this)); });
        $('#ur-fullscreen').on('click', fullscreen);
        $('#ur-scenes').on('click', '[data-seek]', function () { seek($(this).attr('data-seek')); });
        $('#ur-details').on('click', '[data-version]', function () { var id = $(this).attr('data-version'); if (!state.current || !P.isSameId(state.current.id, id)) { select(id); } });
        $('#tk-uretilenler').on('click', '.tk-card .tk-tab[data-tab]', function () {
            var tab = $(this).attr('data-tab');
            $('#tk-uretilenler .tk-tab[data-tab]').removeClass('is-active').filter(this).addClass('is-active');
            $('#tk-uretilenler .tk-tab-panel').removeClass('is-active').filter('[data-panel=' + tab + ']').addClass('is-active');
        });

        TK.api.get('/KonsepttenVideo/Modeller', null, { silent: true }).done(function (d) {
            state.models = U.items(d);
            if (state.current && state.current.loaded) { renderDetails(state.current); }
        });
        TK.api.get('/KonsepttenVideo/Ajanlar', null, { silent: true }).done(function (d) {
            var max = 0;
            $.each(U.items(d), function (_, a) { max = Math.max(max, Number(a.vramMb || (a.gpu && a.gpu.vramMb) || 0)); });
            state.agentVramMb = max || null;
        });
        var initial = new window.URLSearchParams(window.location.search).get('video');
        loadList().always(function () { if (initial) { select(initial); } });
    }

    TK.uretilenler = { init: init };
    $(init);
})(window, window.jQuery);
