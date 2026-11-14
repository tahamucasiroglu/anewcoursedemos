/**
 * TK.production — Konseptten Video ve Üretilenler sayfalarının ortak parçaları (tek yerde, DRY):
 *  - Aşama adları ve gruplar (Konsept inceleniyor → … → Birleştirme → Hazır), grup durumu hesaplama.
 *  - Model etiketleri (Yerel/Online, VRAM, "Sadece Türkçe" gibi dil etiketi, fiyat ipucu) ve ayar özetleri.
 *  - Ayar düzenleyicileri: editor(kind, settings, ctx) → { $el, read() } (kind: code | engine | image | speech | compose).
 *    Modelleri ctx.models'ten, sesleri ctx.loadVoices(modelKey) ile alır.
 *  - rerunStages(eski, yeni): "Yeni sürüm"de hangi aşamaların yeniden çalışacağının tahmini (API'nin girdi özeti kurallarıyla aynı).
 *  - textChanges / diffHtml: yazılış ↔ okunuş farkları (kelime düzeyi).
 *  - duration, mode, isSameId gibi küçük yardımcılar.
 * Tek modül (IIFE); sayfa betiklerinden önce yüklenir.
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    function U() { return TK.util; }

    var PIPELINE = ['Script', 'FactSheet', 'ScenePlan', 'SceneCode', 'Images', 'Normalize', 'Speech', 'Align', 'SceneRender', 'Compose', 'Mux'];
    var STAGE_NAMES = {
        Script: 'Konsept inceleniyor', FactSheet: 'Bilgi özeti', ScenePlan: 'Sahne planı', SceneCode: 'Kod', Images: 'Görseller',
        Normalize: 'Okunuş', Speech: 'Ses', Align: 'Hizalama', SceneRender: 'Render', Compose: 'Altyazı ve birleştirme', Mux: 'Son dosya'
    };
    var GROUPS = [
        { key: 'review', name: 'Konsept inceleniyor', stages: ['Script', 'FactSheet'] },
        { key: 'plan', name: 'Sahne planı', stages: ['ScenePlan'] },
        { key: 'code', name: 'Kod', stages: ['SceneCode'], perScene: true },
        { key: 'images', name: 'Görseller', stages: ['Images'] },
        { key: 'normalize', name: 'Okunuş', stages: ['Normalize'] },
        { key: 'speech', name: 'Ses', stages: ['Speech'] },
        { key: 'align', name: 'Hizalama', stages: ['Align'] },
        { key: 'render', name: 'Render', stages: ['SceneRender'], perScene: true },
        { key: 'mux', name: 'Birleştirme', stages: ['Compose', 'Mux'] }
    ];
    var EFFORTS = [['Low', 'Düşük'], ['Medium', 'Orta'], ['High', 'Yüksek'], ['XHigh', 'Çok yüksek'], ['Max', 'En yüksek']];
    var MODES = [['Off', 'Kapalı'], ['Local', 'Yerel'], ['Online', 'Online']];
    var SUPERTONIC_VOICES = ['M1', 'M2', 'M3', 'M4', 'M5', 'F1', 'F2', 'F3', 'F4', 'F5'];
    var FALLBACK_ENGINES = [
        { key: 'hyperframes', displayName: 'HyperFrames', capability: 'VideoEngine', runsOn: 'Agent', notes: 'Varsayılan' },
        { key: 'remotion', displayName: 'Remotion', capability: 'VideoEngine', runsOn: 'Agent' },
        { key: 'revideo', displayName: 'Revideo', capability: 'VideoEngine', runsOn: 'Agent', notes: 'Deneysel' }
    ];

    /* ---------------- Küçük yardımcılar ---------------- */
    function str(v) { return v === null || v === undefined ? '' : String(v); }
    function lower(v) { return str(v).toLowerCase(); }
    function isSameId(a, b) { return lower(a) === lower(b) && str(a) !== ''; }

    /** Enum değerini metne çevirir (API metin gönderir; sayı gelirse sıradan bulur). */
    function enumName(value, names) {
        if (typeof value === 'number') { return names[value] || ''; }
        var v = lower(value);
        for (var i = 0; i < names.length; i++) { if (lower(names[i]) === v) { return names[i]; } }
        return str(value);
    }
    function mode(v) { return enumName(v, ['Off', 'Local', 'Online']) || 'Off'; }
    function stageName(v) { return enumName(v, PIPELINE); }
    function stageStatus(v) { return lower(enumName(v, ['Pending', 'Running', 'Completed', 'Skipped', 'Failed', 'Cancelled'])); }
    function itemStatus(v) { return lower(enumName(v, ['Waiting', 'Running', 'Paused', 'Done', 'Failed', 'Cancelled'])); }
    function runState(v) { return lower(enumName(v, ['Idle', 'Running', 'Paused'])); }
    function effortName(v) { return enumName(v, ['Low', 'Medium', 'High', 'XHigh', 'Max']); }
    function isVertical(format) { return lower(enumName(format, ['Vertical', 'Horizontal'])) !== 'horizontal'; }

    /** Süreyi okunur yazar: 950 ms → "1 sn", 125000 → "2 dk 05 sn", 3 saat üstü → "3 sa 4 dk". */
    function duration(ms) {
        var s = Math.round(Number(ms || 0) / 1000);
        if (!isFinite(s) || s <= 0) { return ms > 0 ? '<1 sn' : ''; }
        if (s < 60) { return s + ' sn'; }
        var m = Math.floor(s / 60);
        if (m < 60) { return m + ' dk ' + ('0' + (s % 60)).slice(-2) + ' sn'; }
        return Math.floor(m / 60) + ' sa ' + (m % 60) + ' dk';
    }

    /** Saniyeyi video süresi biçiminde yazar (1:05). */
    function clock(sec) {
        var s = Math.max(0, Math.round(Number(sec || 0)));
        return Math.floor(s / 60) + ':' + ('0' + (s % 60)).slice(-2);
    }

    /* ---------------- Modeller ---------------- */
    function capabilityOf(m) { return lower(enumName(m.capability, ['Text', 'Image', 'Speech', 'VideoEngine'])); }
    function isLocal(m) { return lower(enumName(m.runsOn, ['Server', 'Agent'])) === 'agent'; }

    function modelsOf(models, capability, runsLocal) {
        var cap = lower(capability);
        var list = $.grep(models || [], function (m) {
            if (capabilityOf(m) !== cap) { return false; }
            return runsLocal === undefined || isLocal(m) === runsLocal;
        });
        if (cap === 'videoengine' && !list.length) { list = FALLBACK_ENGINES; }
        return list;
    }

    function findModel(models, key) {
        var list = (models || []).concat(FALLBACK_ENGINES);
        for (var i = 0; i < list.length; i++) { if (isSameId(list[i].key, key)) { return list[i]; } }
        return null;
    }

    function modelName(models, key, fallback) {
        if (!key) { return fallback || 'Seçilmedi'; }
        var m = findModel(models, key);
        return m ? (m.displayName || m.key) : key;
    }

    function priceHint(m) {
        if (isLocal(m)) { return 'Ücretsiz'; }
        var p = m.pricing || {};
        var u = U();
        if (p.perImage) { return u.money(p.perImage) + '/görsel'; }
        if (p.perKChars) { return u.money(p.perKChars) + '/1K karakter'; }
        if (p.perMinute) { return u.money(p.perMinute) + '/dk'; }
        if (p.inputPerMTok || p.outputPerMTok) { return u.money(p.inputPerMTok) + ' / ' + u.money(p.outputPerMTok) + ' (1M token)'; }
        return '';
    }

    function gb(mb) { return (Math.round(Number(mb) / 102.4) / 10).toString().replace('.', ','); }

    /** Model rozetleri: Yerel/Online, dil etiketi, VRAM gereksinimi (ajanın VRAM'i yetmiyorsa kırmızı), fiyat. */
    function modelTags(m, agentVramMb) {
        var u = U();
        var tags = [];
        if (m.runsOn !== undefined) { tags.push(isLocal(m) ? '<span class="tk-badge is-info">Yerel</span>' : '<span class="tk-badge is-accent">Online</span>'); }
        if (m.languageLabel) {
            var cls = m.languageLabel === 'Türkçe yok' ? 'is-danger' : m.languageLabel === 'Sadece Türkçe' ? 'is-success' : '';
            tags.push('<span class="tk-badge ' + cls + '">' + u.escape(m.languageLabel) + '</span>');
        }
        if (m.minVramMb) {
            var low = agentVramMb && agentVramMb < m.minVramMb;
            tags.push('<span class="tk-badge ' + (low ? 'is-danger' : 'is-warning') + '" title="Gereken ekran kartı belleği">' +
                (low ? 'VRAM yetersiz · ' : '') + '≥ ' + gb(m.minVramMb) + ' GB VRAM</span>');
        } else if (isLocal(m) && capabilityOf(m) !== 'videoengine') {
            tags.push('<span class="tk-badge" title="Ekran kartı gerekmez">CPU</span>');
        }
        var price = priceHint(m);
        if (price) { tags.push('<span class="tk-badge">' + u.escape(price) + '</span>'); }
        return tags.join(' ');
    }

    /* ---------------- Ayar özetleri ---------------- */
    function effortLabel(v) {
        var e = $.grep(EFFORTS, function (x) { return x[0] === effortName(v); })[0];
        return e ? e[1] : '';
    }
    function summary(kind, s, models) {
        s = s || {};
        if (kind === 'code') { var ef = effortLabel(s.codeEffort); return modelName(models, s.codeModelKey) + (ef ? ' · ' + ef : ''); }
        if (kind === 'engine') { return modelName(models, s.videoEngine); }
        if (kind === 'image') {
            var im = mode(s.imageMode);
            return im === 'Off' ? 'Kapalı (koddan çiz)' : modelName(models, s.imageModelKey) + ' · ' + (im === 'Local' ? 'yerel' : 'online');
        }
        if (kind === 'speech') {
            var sm = mode(s.speechMode);
            if (sm === 'Off') { return 'Kapalı (sessiz)'; }
            return modelName(models, s.speechModelKey) + (s.voiceId ? ' · ' + s.voiceId : '') + (Number(s.speechSpeed || 1) !== 1 ? ' · ' + Number(s.speechSpeed).toFixed(2).replace('.', ',') + '×' : '');
        }
        if (kind === 'compose') { return (s.burnSubtitles ? 'Altyazı var' : 'Altyazı yok') + ' · ' + (s.showProgressBar ? 'çubuk var' : 'çubuk yok'); }
        return '';
    }

    /* ---------------- Ayar düzenleyicileri ---------------- */
    function radioList(name, list, current, agentVramMb, offLabel) {
        var u = U();
        var html = '<div class="tk-list kv-models" role="radiogroup">';
        if (offLabel) {
            html += '<label class="tk-list-item' + (!current ? ' is-selected' : '') + '"><input type="radio" name="' + name + '" value="" ' + (!current ? 'checked' : '') + ' />' +
                '<div class="tk-list-main"><div class="tk-list-title">' + u.escape(offLabel) + '</div></div></label>';
        }
        $.each(list, function (_, m) {
            var sel = isSameId(m.key, current);
            var low = m.minVramMb && agentVramMb && agentVramMb < m.minVramMb;
            html += '<label class="tk-list-item' + (sel ? ' is-selected' : '') + (low ? ' is-disabled' : '') + '">' +
                '<input type="radio" name="' + name + '" value="' + u.escape(m.key) + '" ' + (sel ? 'checked' : '') + ' />' +
                '<div class="tk-list-main"><div class="tk-list-title">' + u.escape(m.displayName || m.key) + '</div>' +
                '<div class="tk-list-sub">' + u.escape([m.platform, m.notes].filter(Boolean).join(' · ')) + '</div>' +
                '<div class="kv-tags">' + modelTags(m, agentVramMb) + '</div></div></label>';
        });
        if (!list.length) { html += u.empty('cpu', 'Model bulunamadı', 'Bu grupta seçilebilir model yok.'); }
        return html + '</div>';
    }

    function bindRadioHighlight($root) {
        $root.on('change', 'input[type=radio]', function () {
            var name = $(this).attr('name');
            $root.find('input[name="' + name + '"]').closest('.tk-list-item').removeClass('is-selected');
            $(this).closest('.tk-list-item').addClass('is-selected');
        });
    }

    function segHtml(current) {
        return '<div class="kv-seg" role="tablist">' + $.map(MODES, function (m) {
            return '<button type="button" data-mode="' + m[0] + '" class="' + (m[0] === current ? 'is-active' : '') + '">' + m[1] + '</button>';
        }).join('') + '</div>';
    }

    function editorCode(s, ctx) {
        var u = U();
        var $el = $('<div></div>');
        var list = modelsOf(ctx.models, 'Text');
        $el.html(radioList('code', list, s.codeModelKey, ctx.agentVramMb) +
            '<div class="tk-field" style="margin-top:14px"><label for="kv-effort">Efor (yüksek efor = daha iyi kod, daha çok token)</label>' +
            '<select class="tk-select" id="kv-effort" name="effort"></select></div>');
        function fillEfforts() {
            var key = $el.find('input[name=code]:checked').val();
            var m = findModel(ctx.models, key) || {};
            var allowed = $.map(m.effortLevels || [], effortName);
            var current = effortName($el.find('[name=effort]').val() || s.codeEffort || m.defaultEffort || 'High');
            var opts = $.grep(EFFORTS, function (e) { return !allowed.length || $.inArray(e[0], allowed) >= 0; });
            if (!opts.length) { opts = EFFORTS; }
            if (!$.grep(opts, function (e) { return e[0] === current; }).length) { current = effortName(m.defaultEffort) || opts[opts.length - 1][0]; }
            $el.find('[name=effort]').html($.map(opts, function (e) {
                return '<option value="' + e[0] + '"' + (e[0] === current ? ' selected' : '') + '>' + u.escape(e[1]) + '</option>';
            }).join(''));
        }
        bindRadioHighlight($el);
        $el.on('change', 'input[name=code]', fillEfforts);
        fillEfforts();
        return {
            $el: $el,
            read: function () { return { codeModelKey: $el.find('input[name=code]:checked').val() || s.codeModelKey, codeEffort: $el.find('[name=effort]').val() || null }; }
        };
    }

    function editorEngine(s, ctx) {
        var $el = $('<div></div>').html('<p class="tk-hint" style="margin:0 0 10px">Sahne kodu bu motora göre yazılır ve bilgisayarınızda render edilir.</p>' +
            radioList('engine', modelsOf(ctx.models, 'VideoEngine'), s.videoEngine, ctx.agentVramMb));
        bindRadioHighlight($el);
        return { $el: $el, read: function () { return { videoEngine: $el.find('input[name=engine]:checked').val() || s.videoEngine }; } };
    }

    /** Görsel ve ses için ortak "Kapalı / Yerel / Online + model listesi" düzenleyicisi. */
    function editorMedia(kind, s, ctx) {
        var isSpeech = kind === 'speech';
        var capability = isSpeech ? 'Speech' : 'Image';
        var modeField = isSpeech ? 'speechMode' : 'imageMode';
        var keyField = isSpeech ? 'speechModelKey' : 'imageModelKey';
        var current = { mode: mode(s[modeField]), key: s[keyField] || null, voice: s.voiceId || null, speed: Number(s.speechSpeed || 1) };
        var $el = $('<div></div>');
        var offText = isSpeech ? 'Ses kapalı: video sessiz üretilir (ses, okunuş ve hizalama aşamaları atlanır).'
            : 'Görsel kapalı: sahneler tamamen koddan çizilir (en ucuz ve en hızlı).';
        $el.html(segHtml(current.mode) + '<div data-role="body"></div>' + (isSpeech ? '<div data-role="voice"></div>' : ''));

        function renderVoices() {
            if (!isSpeech) { return; }
            var u = U();
            var $v = $el.find('[data-role=voice]');
            var key = $el.find('input[name=' + kind + ']:checked').val();
            if (current.mode === 'Off' || !key) { $v.empty(); return; }
            var speedHtml = '<div class="tk-field" style="margin-top:12px"><label for="kv-speed">Konuşma hızı</label><div class="kv-speed">' +
                '<input type="range" id="kv-speed" name="speed" min="0.5" max="2" step="0.05" value="' + current.speed + '" />' +
                '<output for="kv-speed">' + current.speed.toFixed(2).replace('.', ',') + '×</output></div></div>';
            var head = '<div class="tk-field" style="margin:14px 0 6px"><label>Ses</label></div>';
            function grid(voices) {
                if (!voices.length) {
                    return head + '<input class="tk-input" name="voiceText" maxlength="120" placeholder="Ses kimliği (boş = modelin varsayılanı)" value="' + u.escape(current.voice || '') + '" />';
                }
                var hasCurrent = $.grep(voices, function (x) { return isSameId(x.id, current.voice); }).length > 0;
                return head + '<div class="kv-voice-grid" role="radiogroup">' + $.map(voices, function (x) {
                    var sel = hasCurrent ? isSameId(x.id, current.voice) : !!x.isDefault;
                    return '<label class="kv-voice"><input type="radio" name="voice" value="' + u.escape(x.id) + '"' + (sel ? ' checked' : '') + ' />' +
                        '<span>' + u.escape(x.displayName || x.id) + (x.nativeTurkish ? ' <span class="tk-badge is-success">TR</span>' : '') +
                        (x.gender || x.notes ? '<small>' + u.escape([x.gender, x.notes].filter(Boolean).join(' · ')) + '</small>' : '') + '</span></label>';
                }).join('') + '</div>';
            }
            if (key === 'supertonic-3') {
                $v.html(grid($.map(SUPERTONIC_VOICES, function (id) { return { id: id, displayName: id, gender: id.charAt(0) === 'M' ? 'Erkek' : 'Kadın', isDefault: id === 'M1' }; })) + speedHtml);
            } else if (current.mode === 'Online' && ctx.loadVoices) {
                $v.html(head + U().skeleton(1) + speedHtml);
                ctx.loadVoices(key).done(function (voices) {
                    if ($el.find('input[name=' + kind + ']:checked').val() !== key) { return; }
                    $v.html(grid(U().items(voices)) + speedHtml);
                }).fail(function () { $v.html(grid([]) + speedHtml); });
            } else {
                $v.html(grid([]) + speedHtml);
            }
        }

        function renderBody() {
            var $b = $el.find('[data-role=body]');
            $el.find('.kv-seg button').removeClass('is-active').filter('[data-mode=' + current.mode + ']').addClass('is-active');
            if (current.mode === 'Off') {
                $b.html('<div class="tk-alert"><svg class="tk-ic" aria-hidden="true"><use href="/img/icons.svg#i-info"></use></svg><div>' + U().escape(offText) + '</div></div>');
            } else {
                var local = current.mode === 'Local';
                var list = modelsOf(ctx.models, capability, local);
                var key = $.grep(list, function (m) { return isSameId(m.key, current.key); }).length ? current.key : (list[0] && list[0].key);
                $b.html((local ? '<p class="tk-hint" style="margin:0 0 10px">Bilgisayarınızda çalışır (RTX ekran kartı / CPU). Ücretsiz.</p>'
                    : '<p class="tk-hint" style="margin:0 0 10px">Sunucudan sağlayıcıya gider; Ayarlar → API anahtarları gerekir.</p>') +
                    radioList(kind, list, key, ctx.agentVramMb));
            }
            renderVoices();
        }

        bindRadioHighlight($el);
        $el.on('click', '.kv-seg button', function () { current.mode = $(this).attr('data-mode'); renderBody(); });
        $el.on('change', 'input[name=' + kind + ']', function () { current.key = $(this).val(); current.voice = null; renderVoices(); });
        $el.on('change', 'input[name=voice]', function () { current.voice = $(this).val(); });
        $el.on('input', 'input[name=voiceText]', function () { current.voice = $.trim($(this).val()) || null; });
        $el.on('input', 'input[name=speed]', function () {
            current.speed = Number($(this).val());
            $(this).siblings('output').text(current.speed.toFixed(2).replace('.', ',') + '×');
        });
        renderBody();

        return {
            $el: $el,
            read: function () {
                var patch = {};
                var key = $el.find('input[name=' + kind + ']:checked').val() || null;
                patch[modeField] = current.mode;
                patch[keyField] = current.mode === 'Off' ? (s[keyField] || null) : key;
                if (isSpeech) {
                    patch.voiceId = current.mode === 'Off' ? (s.voiceId || null) : ($el.find('input[name=voice]:checked').val() || current.voice || null);
                    patch.speechSpeed = current.speed;
                }
                return patch;
            }
        };
    }

    function editorCompose(s) {
        var $el = $('<div class="tk-row" style="gap:18px"></div>').html(
            '<label class="tk-switch"><input type="checkbox" name="burnSubtitles"' + (s.burnSubtitles ? ' checked' : '') + ' /><span class="tk-switch-track"></span> Altyazı</label>' +
            '<label class="tk-switch"><input type="checkbox" name="showProgressBar"' + (s.showProgressBar ? ' checked' : '') + ' /><span class="tk-switch-track"></span> Üst ilerleme çubuğu</label>');
        return {
            $el: $el,
            read: function () { return { burnSubtitles: $el.find('[name=burnSubtitles]').is(':checked'), showProgressBar: $el.find('[name=showProgressBar]').is(':checked') }; }
        };
    }

    /**
     * Bir ayar grubunun düzenleyicisi. ctx: { models, agentVramMb, loadVoices(modelKey) → promise }.
     * Dönen nesne: { $el, read() → değişen alanlar }.
     */
    function editor(kind, settings, ctx) {
        var s = settings || {};
        if (kind === 'code') { return editorCode(s, ctx); }
        if (kind === 'engine') { return editorEngine(s, ctx); }
        if (kind === 'image' || kind === 'speech') { return editorMedia(kind, s, ctx); }
        return editorCompose(s);
    }

    /* ---------------- Yeni sürüm: yeniden çalışacak aşamalar ---------------- */
    function changedGroups(a, b) {
        a = a || {}; b = b || {};
        var out = [];
        if (!isSameId(a.codeModelKey, b.codeModelKey) || effortName(a.codeEffort) !== effortName(b.codeEffort) || lower(a.videoEngine) !== lower(b.videoEngine) || Number(a.fps || 30) !== Number(b.fps || 30)) { out.push('engine'); }
        var am = mode(a.imageMode), bm = mode(b.imageMode);
        if ((am === 'Off') !== (bm === 'Off')) { out.push('imagesToggle'); } else if (bm !== 'Off' && (am !== bm || lower(a.imageModelKey) !== lower(b.imageModelKey))) { out.push('images'); }
        var sa = mode(a.speechMode), sb = mode(b.speechMode);
        if (sa !== sb || (sb !== 'Off' && (lower(a.speechModelKey) !== lower(b.speechModelKey) || lower(a.voiceId) !== lower(b.voiceId) || Number(a.speechSpeed || 1) !== Number(b.speechSpeed || 1)))) { out.push('speech'); }
        if (!!a.burnSubtitles !== !!b.burnSubtitles || !!a.showProgressBar !== !!b.showProgressBar) { out.push('subtitles'); }
        return out;
    }

    /** Eski ve yeni ayara göre yeniden çalışacak aşamalar (sıra korunur) ve "changed" anahtarları. Mux her zaman çalışır. */
    function rerunStages(a, b) {
        var groups = changedGroups(a, b);
        var set = { Mux: true };
        $.each(groups, function (_, g) {
            var add = {
                engine: ['SceneCode', 'SceneRender', 'Compose'],
                imagesToggle: ['ScenePlan', 'SceneCode', 'Images', 'SceneRender', 'Compose'],
                images: ['Images', 'SceneRender', 'Compose'],
                speech: ['Speech', 'Align', 'Compose'],
                subtitles: ['Compose']
            }[g] || [];
            $.each(add, function (_, st) { set[st] = true; });
        });
        var stages = $.grep(PIPELINE, function (st) { return !!set[st]; });
        var changed = $.map(groups, function (g) { return g === 'imagesToggle' ? 'images' : g; });
        return { stages: stages, changed: changed, reused: $.grep(PIPELINE, function (st) { return !set[st]; }) };
    }

    /* ---------------- Aşama grupları (sıra ilerlemesi) ---------------- */
    function groupState(summaries, group) {
        var parts = $.grep(summaries || [], function (x) { return $.inArray(stageName(x.stage), group.stages) >= 0; });
        var r = { status: 'pending', durationMs: 0, costUsd: 0, progress: 0, scenesDone: 0, scenesTotal: 0, reused: false, agent: false, message: '' };
        if (!parts.length) { return r; }
        var st = $.map(parts, function (x) { return stageStatus(x.status); });
        $.each(parts, function (_, x) {
            r.durationMs += Number(x.durationMs || 0);
            r.costUsd += Number(x.costUsd || 0);
            r.scenesDone += Number(x.scenesDone || 0);
            r.scenesTotal += Number(x.scenesTotal || 0);
            r.reused = r.reused || !!x.reused;
            r.agent = r.agent || lower(enumName(x.location, ['Server', 'Agent'])) === 'agent';
            if (x.message) { r.message = x.message; }
        });
        var prog = 0;
        $.each(parts, function (_, x) { prog += Number(x.progress || 0); });
        r.progress = prog / parts.length;
        if ($.inArray('failed', st) >= 0) { r.status = 'failed'; }
        else if ($.inArray('running', st) >= 0) { r.status = 'running'; }
        else if ($.inArray('cancelled', st) >= 0) { r.status = 'cancelled'; }
        else if ($.grep(st, function (x) { return x === 'skipped'; }).length === st.length) { r.status = 'skipped'; }
        else if ($.grep(st, function (x) { return x === 'completed' || x === 'skipped'; }).length === st.length) { r.status = 'done'; }
        else if ($.grep(st, function (x) { return x === 'completed' || x === 'skipped'; }).length > 0) { r.status = 'running'; }
        return r;
    }

    /* ---------------- Yazılış ↔ okunuş farkları ---------------- */
    function tokens(text) { return $.grep(str(text).split(/\s+/), function (t) { return t !== ''; }); }

    /** Kelime düzeyinde fark: [{ type: 'same'|'change', from, to }] (LCS; uzun metinlerde kaba eşleme). */
    function diff(a, b) {
        var x = tokens(a), y = tokens(b);
        var n = x.length, m = y.length;
        if (n * m > 250000) { return [{ type: 'change', from: x.join(' '), to: y.join(' ') }]; }
        var dp = [], i, j;
        for (i = 0; i <= n; i++) { dp.push(new Array(m + 1).fill(0)); }
        for (i = n - 1; i >= 0; i--) {
            for (j = m - 1; j >= 0; j--) { dp[i][j] = x[i] === y[j] ? dp[i + 1][j + 1] + 1 : Math.max(dp[i + 1][j], dp[i][j + 1]); }
        }
        var out = [], from = [], to = [];
        function flush() { if (from.length || to.length) { out.push({ type: 'change', from: from.join(' '), to: to.join(' ') }); from = []; to = []; } }
        i = 0; j = 0;
        while (i < n && j < m) {
            if (x[i] === y[j]) { flush(); out.push({ type: 'same', from: x[i], to: y[j] }); i++; j++; }
            else if (dp[i + 1][j] >= dp[i][j + 1]) { from.push(x[i]); i++; }
            else { to.push(y[j]); j++; }
        }
        while (i < n) { from.push(x[i++]); }
        while (j < m) { to.push(y[j++]); }
        flush();
        return out;
    }

    function textChanges(display, spoken) {
        return $.map($.grep(diff(display, spoken), function (d) { return d.type === 'change'; }), function (d) { return { original: d.from, replacement: d.to }; });
    }

    /** Okunuş metnini, yazılıştan farklı kelimeler işaretli HTML olarak döner. */
    function diffHtml(display, spoken) {
        var u = U();
        return $.map(diff(display, spoken), function (d) {
            if (d.type === 'same') { return u.escape(d.to); }
            return d.to ? '<mark class="ur-mark" title="' + u.escape(d.from ? 'Yazılış: ' + d.from : 'Eklendi') + '">' + u.escape(d.to) + '</mark>' : '';
        }).join(' ');
    }

    TK.production = {
        PIPELINE: PIPELINE, STAGE_NAMES: STAGE_NAMES, GROUPS: GROUPS, EFFORTS: EFFORTS,
        mode: mode, stageName: stageName, stageStatus: stageStatus, itemStatus: itemStatus, runState: runState, effortName: effortName,
        isVertical: isVertical, isSameId: isSameId, duration: duration, clock: clock,
        modelsOf: modelsOf, findModel: findModel, modelName: modelName, modelTags: modelTags, isLocal: isLocal,
        summary: summary, editor: editor, rerunStages: rerunStages, groupState: groupState,
        textChanges: textChanges, diffHtml: diffHtml
    };
})(window, window.jQuery);
