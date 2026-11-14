/**
 * TK.settingsProviders — Ayarlar sayfasının sağlayıcı bölümleri: API anahtarları (sunucudan gelen platform listesi; kaydet,
 * değiştir, sil, test et; yalnızca son 4 hane; Azure Speech için bölge alanı) ve bütçe limiti (aylık limit, uyarı eşiği,
 * aşınca yeni üretimi durdur) + bu ayın harcaması (platform kırılımı). MVC uçları: /Ayarlar/{ApiAnahtarlari, ApiAnahtari,
 * ApiAnahtariSil/{platform}, ApiAnahtariTest/{platform}, Butce}. Yerel LLM satırında kök adres
 * alanı (zorunlu) ve isteğe bağlı anahtar vardır, /Harcama/Aylik. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};
    var U = null;

    /* API cevap vermezse gösterilecek platform listesi (sunucudaki ProviderPlatformDirectory ile aynı kimlikler). */
    var FALLBACK = [
        { platform: 'anthropic', displayName: 'Anthropic (Claude)', description: 'Konsept, video kodu, konu önerisi' },
        { platform: 'openai', displayName: 'OpenAI', description: 'GPT-6, gpt-image, gpt-4o-mini-tts' },
        { platform: 'openrouter', displayName: 'OpenRouter', description: 'Çok sağlayıcılı LLM geçidi' },
        { platform: 'zai', displayName: 'Z.ai (GLM)', description: 'GLM-5.3, GLM-Image, CogView-4' },
        { platform: 'moonshot', displayName: 'Moonshot (Kimi)', description: 'Kimi K3' },
        { platform: 'google', displayName: 'Google Gemini', description: 'Nano Banana, Gemini Flash TTS' },
        { platform: 'elevenlabs', displayName: 'ElevenLabs', description: 'Online ses' },
        { platform: 'azure-speech', displayName: 'Azure Speech', description: 'Neural TTS (bölge gerekir)', requiresRegion: true },
        { platform: 'cartesia', displayName: 'Cartesia', description: 'Online ses' },
        { platform: 'fal', displayName: 'fal.ai', description: 'Online görsel' },
        { platform: 'bfl', displayName: 'Black Forest Labs', description: 'FLUX görsel' },
        { platform: 'replicate', displayName: 'Replicate', description: 'Online görsel' },
        { platform: 'local-openai', displayName: 'Yerel LLM (OpenAI uyumlu)', description: 'Ollama / LM Studio / llama.cpp server', requiresBaseUrl: true, keyOptional: true }
    ];

    var names = {};

    /* ---------------- API anahtarları ---------------- */
    function statusBadge(k) {
        if (!k.configured) { return '<span class="tk-badge">Yok</span>'; }
        var html = '<span class="tk-badge is-success">' + U.icon('check', 'tk-ic-sm') + (k.last4 ? ' ••••' + U.escape(k.last4) : ' Kayıtlı') + '</span>';
        if (k.lastTestSucceeded === true) { html += ' <span class="tk-badge is-success" title="' + U.escape(U.dateTime(k.lastTestedAtUtc)) + '">Test: geçerli</span>'; }
        if (k.lastTestSucceeded === false) { html += ' <span class="tk-badge is-danger" title="' + U.escape(U.dateTime(k.lastTestedAtUtc)) + '">Test: başarısız</span>'; }
        if (k.region) { html += '<div class="tk-hint">Bölge: ' + U.escape(k.region) + '</div>'; }
        if (k.baseUrl) { html += '<div class="tk-hint" title="Kök adres">' + U.escape(k.baseUrl) + '</div>'; }
        return html;
    }

    function renderKeys(rows) {
        var list = rows && rows.length ? rows : FALLBACK;
        var html = '<table class="tk-table"><thead><tr><th>Sağlayıcı</th><th>Durum</th><th style="width:40%">Anahtar</th><th></th></tr></thead><tbody>';
        $.each(list, function (_, k) {
            names[k.platform] = k.displayName || k.platform;
            html += '<tr data-platform="' + U.escape(k.platform) + '"><td><strong>' + U.escape(k.displayName || k.platform) + '</strong><div class="tk-hint">' + U.escape(k.description || '') + '</div></td>' +
                '<td>' + statusBadge(k) + '</td>' +
                '<td><div class="tk-row tk-wrap">' +
                (k.requiresBaseUrl ? '<input type="url" class="tk-input tk-grow" data-field="baseUrl" maxlength="300" spellcheck="false" placeholder="Kök adres (http://localhost:11434/v1)" value="' + U.escape(k.baseUrl || '') + '" aria-label="Kök adres (Base URL)" />' : '') +
                '<input type="password" class="tk-input tk-grow" data-field="key" autocomplete="off" spellcheck="false" placeholder="' +
                (k.keyOptional ? (k.configured && k.last4 ? 'Anahtar kayıtlı (boş = koru)' : 'Anahtar (isteğe bağlı)') : (k.configured ? 'Değiştirmek için yeni anahtar' : 'Anahtarı yapıştırın')) + '" aria-label="' + U.escape(k.displayName) + ' anahtarı" />' +
                (k.requiresRegion ? '<input type="text" class="tk-input" data-field="region" style="width:140px" maxlength="40" placeholder="Bölge (westeurope)" value="' + U.escape(k.region || '') + '" aria-label="Bölge" />' : '') +
                '</div></td>' +
                '<td style="text-align:right;white-space:nowrap"><button type="button" class="tk-btn tk-btn-secondary tk-btn-sm" data-act="save">Kaydet</button>' +
                (k.configured ? ' <button type="button" class="tk-btn tk-btn-ghost tk-btn-sm" data-act="test" title="Ücretsiz bir çağrıyla anahtarı dener">Test et</button>' +
                    ' <button type="button" class="tk-mini-btn is-danger" data-act="delete" title="Sil" aria-label="Sil">' + U.icon('trash') + '</button>' : '') +
                '</td></tr>';
        });
        $('#st-keys').html(html + '</tbody></table>');
    }

    function loadKeys() {
        TK.api.get('/Ayarlar/ApiAnahtarlari', null, { silent: true })
            .done(function (d) { renderKeys(U.items(d)); })
            .fail(function () { renderKeys([]); });
    }

    function saveKey($btn, $tr, platform) {
        var key = $.trim($tr.find('[data-field=key]').val());
        var $base = $tr.find('[data-field=baseUrl]');
        var optional = !!$base.length;
        if (optional ? /\s/.test(key) : (key.length < 8 || /\s/.test(key))) {
            TK.toast.warning(optional ? 'Anahtar boşluk içeremez.' : 'Geçerli bir anahtar girin (en az 8 karakter, boşluksuz).');
            $tr.find('[data-field=key]').trigger('focus');
            return;
        }
        var $region = $tr.find('[data-field=region]');
        var body = { platform: platform, apiKey: key };
        if (optional) {
            body.baseUrl = $.trim($base.val());
            if (!/^https?:\/\/\S+$/i.test(body.baseUrl)) { TK.toast.warning('Kök adres gerekli (ör. http://localhost:11434/v1).'); $base.trigger('focus'); return; }
        }
        if ($region.length) {
            body.region = $.trim($region.val());
            if (!body.region) { TK.toast.warning('Bölge gerekli (ör. westeurope).'); $region.trigger('focus'); return; }
        }
        U.busy($btn, TK.api.put('/Ayarlar/ApiAnahtari', body)).done(function () {
            TK.toast.success((names[platform] || platform) + ' anahtarı kaydedildi.');
            loadKeys();
        });
    }

    function testKey($btn, platform) {
        U.busy($btn, TK.api.post('/Ayarlar/ApiAnahtariTest/' + encodeURIComponent(platform))).done(function (r) {
            if (r && r.success) { TK.toast.success(r.message || 'Anahtar geçerli.'); } else { TK.toast.error((r && r.message) || 'Anahtar doğrulanamadı.'); }
            loadKeys();
        });
    }

    function deleteKey(platform) {
        var name = names[platform] || platform;
        TK.modal.confirm('Anahtarı sil', name + ' anahtarı silinecek; bu sağlayıcının modelleri kullanılamaz.', 'Sil').done(function () {
            TK.api.del('/Ayarlar/ApiAnahtariSil/' + encodeURIComponent(platform)).done(function () { TK.toast.info('Anahtar silindi.'); loadKeys(); });
        });
    }

    /* ---------------- Bütçe + harcama ---------------- */
    function loadBudget() {
        TK.api.get('/Ayarlar/Butce', null, { silent: true }).done(function (b) {
            if (!b) { return; }
            $('#st-budget').val(b.monthlyLimitUsd ? b.monthlyLimitUsd : '');
            $('#st-budget-warn').val(b.warnAtPercent || 80);
            $('#st-budget-form [name=stopOnExceed]').prop('checked', b.stopOnExceed !== false);
        });
    }

    function renderUsage(u) {
        var $u = $('#st-usage');
        if (!u) { $u.empty(); return; }
        var limit = Number(u.limitUsd || 0);
        var cls = u.exceeded ? 'is-danger' : (u.warning ? 'is-warning' : '');
        var html = '<div class="tk-alert ' + cls + '">' + U.icon(u.exceeded ? 'alert' : 'info') + '<span>' +
            '<strong>' + U.escape(u.month || '') + '</strong> harcaması: ' + U.money(u.totalUsd) +
            (limit > 0 ? ' / ' + U.money(limit) : ' (limitsiz)') +
            (u.exceeded ? (u.stopOnExceed ? ' — limit aşıldı, yeni üretim durduruldu.' : ' — limit aşıldı (yalnızca uyarı).') : (u.warning ? ' — uyarı eşiğine ulaşıldı.' : '')) +
            '</span></div>';
        var rows = U.items(u.byPlatform);
        if (rows.length) {
            html += '<table class="tk-table" style="margin-top:10px"><thead><tr><th>Platform</th><th style="text-align:right">Çağrı</th><th style="text-align:right">Tutar</th></tr></thead><tbody>';
            $.each(rows, function (_, r) {
                html += '<tr><td>' + U.escape(names[r.platform] || r.platform) + '</td><td style="text-align:right">' + U.escape(r.calls) + '</td><td style="text-align:right">' + U.money(r.costUsd) + '</td></tr>';
            });
            html += '</tbody></table>';
        }
        $u.html(html);
    }

    function loadUsage() {
        TK.api.get('/Harcama/Aylik', null, { silent: true }).done(renderUsage).fail(function () { renderUsage(null); });
    }

    function saveBudget($form) {
        var limit = parseFloat($('#st-budget').val());
        var warn = U.clampInt($('#st-budget-warn').val(), 1, 100, 80);
        U.busy($form.find('[type=submit]'), TK.api.put('/Ayarlar/Butce', {
            monthlyLimitUsd: isNaN(limit) || limit < 0 ? 0 : limit,
            warnAtPercent: warn,
            stopOnExceed: $form.find('[name=stopOnExceed]').is(':checked')
        })).done(function () {
            TK.toast.success('Bütçe limiti kaydedildi.');
            loadUsage();
            if (TK.layout) { TK.layout.refreshSpend(); }
        });
    }

    function bind() {
        $('#st-keys').on('click', '[data-act]', function () {
            var $btn = $(this);
            var $tr = $btn.closest('tr');
            var platform = $tr.attr('data-platform');
            var act = $btn.attr('data-act');
            if (act === 'save') { saveKey($btn, $tr, platform); }
            if (act === 'test') { testKey($btn, platform); }
            if (act === 'delete') { deleteKey(platform); }
        });
        $('#st-keys').on('keydown', '[data-field=key]', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); $(this).closest('tr').find('[data-act=save]').trigger('click'); }
        });
        $('#st-budget-form').on('submit', function (e) { e.preventDefault(); saveBudget($(this)); });
    }

    function init() {
        if (!$('#st-keys').length) { return; }
        U = TK.util;
        $('#st-keys').html('<div style="padding:16px">' + U.skeleton(4) + '</div>');
        bind();
        loadKeys();
        loadBudget();
        loadUsage();
    }

    TK.settingsProviders = { init: init, reloadKeys: loadKeys, reloadUsage: loadUsage };
    $(init);
})(window, window.jQuery);
