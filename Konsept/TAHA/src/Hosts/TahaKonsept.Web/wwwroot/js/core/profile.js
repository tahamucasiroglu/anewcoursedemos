/**
 * TK.profile — kanal profili ekleme / düzenleme penceresi (üst bardaki ✏️ ve ➕).
 * Alanlar: kanal adı, video dili, biçim (dikey/yatay), hedef süre, kanal içeriği; "Gelişmiş" altında ton / hedef izleyici / anlatım.
 * ➕ penceresinde hazır şablon seçici (/Profil/Sablonlar) seçilen şablonla alanları doldurur. Yeni profil kaydedilince aktif olur.
 * Pencere dışına tıklanınca, ✕/Esc ya da Kaydet ile kapanırken değişiklik varsa kaydeder (POST /Profil/Ekle, PUT /Profil/Guncelle/{id}).
 * ✏️ penceresinde "Sil" (DELETE /Profil/Sil/{id}) vardır. Kaydet/sil sonrası document üzerinde 'tk:profile-saved' tetiklenir
 * (TK.layout listeyi yeniler ve 'tk:profile' ile sayfa verisini tazeler). Tek modül (IIFE); istekler yalnızca TK.api ile.
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    var LANGS = [['tr', 'Türkçe'], ['en', 'İngilizce'], ['de', 'Almanca'], ['fr', 'Fransızca'], ['es', 'İspanyolca'],
        ['it', 'İtalyanca'], ['pt', 'Portekizce'], ['ru', 'Rusça'], ['ar', 'Arapça'], ['az', 'Azerbaycan Türkçesi'], ['ja', 'Japonca']];
    var DEFAULTS = { language: 'tr', format: 'Vertical', targetDurationSec: 60 };

    function option(value, text) { return $('<option></option>').val(value).text(text); }

    function field(id, label, control, hint) {
        var $f = $('<div class="tk-field"></div>').append($('<label></label>').attr('for', id).text(label), control);
        if (hint) { $f.append($('<span class="tk-hint"></span>').text(hint)); }
        return $f;
    }

    function formHtml(withTemplates) {
        var $form = $('<form class="tk-profile-form" autocomplete="off" novalidate></form>');

        if (withTemplates) {
            var $tpl = $('<select id="pf-template" name="templateKey" class="tk-select"></select>').append(option('', 'Şablonsuz başla'));
            $form.append(field('pf-template', 'Hazır şablon', $tpl, 'Şablon seçmek alanları örnek içerikle doldurur; sonra dilediğiniz gibi değiştirebilirsiniz.'));
        } else {
            $form.append('<input type="hidden" name="templateKey" />');
        }

        var $lang = $('<select id="pf-lang" name="language" class="tk-select"></select>');
        $.each(LANGS, function (_, l) { $lang.append(option(l[0], l[1])); });
        var $format = $('<select id="pf-format" name="format" class="tk-select"></select>')
            .append(option('Vertical', 'Dikey (9:16 — Shorts/Reels)'), option('Horizontal', 'Yatay (16:9 — YouTube)'));

        $('<div class="tk-form-grid"></div>').append(
            field('pf-name', 'Kanal adı', '<input id="pf-name" name="name" class="tk-input" maxlength="120" required placeholder="Ör. Bizans Günlükleri" />'),
            field('pf-lang', 'Video dili', $lang),
            field('pf-format', 'Biçim', $format),
            field('pf-duration', 'Hedef süre (sn)', '<input id="pf-duration" name="targetDurationSec" type="number" min="15" max="1800" step="5" class="tk-input" value="60" />')
        ).appendTo($form);

        $form.append(field('pf-content', 'Kanal içeriği',
            '<textarea id="pf-content" name="channelDescription" class="tk-textarea" rows="6" maxlength="4000" placeholder="Odak, tarih aralığı, kaçınılacak konular, doğruluk beklentisi… (konsept üretiminde sistem talimatına kısaltılarak eklenir)"></textarea>'));

        var $adv = $('<details class="tk-profile-advanced" style="margin-top:8px"></details>')
            .append('<summary style="cursor:pointer;font-weight:600;margin-bottom:8px">Gelişmiş: ton, hedef izleyici, anlatım</summary>');
        $('<div class="tk-form-grid"></div>').append(
            field('pf-tone', 'Ton', '<input id="pf-tone" name="defaultTone" class="tk-input" maxlength="200" placeholder="Ör. merak uyandıran" />'),
            field('pf-audience', 'Hedef izleyici', '<input id="pf-audience" name="defaultAudience" class="tk-input" maxlength="200" placeholder="Ör. genel izleyici" />'),
            field('pf-style', 'Anlatım biçimi', '<input id="pf-style" name="defaultNarrativeStyle" class="tk-input" maxlength="200" placeholder="Ör. kronolojik hikâye" />')
        ).appendTo($adv);
        $form.append($adv);
        return $form;
    }

    function val($f, name) { return $.trim($f.find('[name=' + name + ']').val() || ''); }

    function read($f) {
        return {
            name: val($f, 'name'),
            language: $f.find('[name=language]').val() || 'tr',
            format: $f.find('[name=format]').val() || 'Vertical',
            targetDurationSec: TK.util.clampInt($f.find('[name=targetDurationSec]').val(), 15, 1800, 60),
            channelDescription: val($f, 'channelDescription'),
            defaultTone: val($f, 'defaultTone') || null,
            defaultAudience: val($f, 'defaultAudience') || null,
            defaultNarrativeStyle: val($f, 'defaultNarrativeStyle') || null,
            templateKey: $f.find('[name=templateKey]').val() || null
        };
    }

    function ensureLanguage($f, code) {
        var $sel = $f.find('[name=language]');
        if (code && !$sel.find('option').filter(function () { return this.value === code; }).length) {
            $sel.append(option(code, code));
        }
        $sel.val(code || 'tr');
    }

    function fill($f, p, includeName) {
        if (!p) { return; }
        if (includeName) { $f.find('[name=name]').val(p.name || ''); }
        ensureLanguage($f, p.language);
        $f.find('[name=format]').val(p.format || 'Vertical');
        $f.find('[name=targetDurationSec]').val(p.targetDurationSec || 60);
        $f.find('[name=channelDescription]').val(p.channelDescription || '');
        $f.find('[name=defaultTone]').val(p.defaultTone || '');
        $f.find('[name=defaultAudience]').val(p.defaultAudience || '');
        $f.find('[name=defaultNarrativeStyle]').val(p.defaultNarrativeStyle || '');
        if (p.defaultTone || p.defaultAudience || p.defaultNarrativeStyle) { $f.find('.tk-profile-advanced').prop('open', true); }
        if (p.templateKey !== undefined) { $f.find('[name=templateKey]').val(p.templateKey || ''); }
    }

    /** Şablonları kategoriye göre gruplayarak seçiciye doldurur; seçim alanları doldurur. */
    function bindTemplates($f) {
        var $sel = $f.find('select[name=templateKey]');
        if (!$sel.length) { return; }
        var templates = [];
        var lastName = '';
        TK.api.get('/Profil/Sablonlar', null, { silent: true }).done(function (data) {
            templates = TK.util.items(data);
            var groups = {};
            $.each(templates, function (_, t) {
                var cat = t.category || 'Diğer';
                if (!groups[cat]) { groups[cat] = $('<optgroup></optgroup>').attr('label', cat).appendTo($sel); }
                groups[cat].append(option(t.key, t.title || t.name));
            });
        });
        $sel.on('change', function () {
            var key = $(this).val();
            var t = $.grep(templates, function (x) { return String(x.key) === key; })[0];
            if (!t) { return; }
            var current = val($f, 'name');
            var replaceName = !current || current === lastName;
            fill($f, $.extend({}, t, { templateKey: key }), replaceName);
            lastName = t.name || '';
        });
    }

    function deleteButton(id, getHandle) {
        var $btn = $('<button type="button" class="tk-btn tk-btn-danger tk-btn-sm"></button>').html(TK.util.icon('trash') + ' Sil');
        $btn.on('click', function () {
            TK.modal.confirm('Profili sil', 'Bu profil kalıcı olarak silinecek. Aktif profilse en yeni diğer profil aktif olur.', 'Sil').done(function () {
                TK.util.busy($btn, TK.api.del('/Profil/Sil/' + encodeURIComponent(id))).done(function () {
                    TK.toast.success('Profil silindi.');
                    getHandle().close(true);
                    $(document).trigger('tk:profile-saved', [null]);
                });
            });
        });
        return $btn;
    }

    function open(options) {
        var $f = formHtml(options.withTemplates);
        bindTemplates($f);
        fill($f, options.initial, true);
        var snapshot = JSON.stringify(read($f));
        var h = null;

        var $saveBtn = $('<button type="button" class="tk-btn tk-btn-primary"></button>').html(TK.util.icon('check') + ' Kaydet');
        var $footer = $('<div class="tk-row" style="margin-left:auto"></div>');
        if (options.id) { $footer.append(deleteButton(options.id, function () { return h; })); }
        $footer.append($saveBtn);

        h = TK.modal.open({
            title: options.title,
            body: $f,
            size: 'lg',
            footer: $footer,
            hint: 'Dışarı tıklayınca da kaydedilir.',
            onClose: function () {
                var data = read($f);
                if (JSON.stringify(data) === snapshot) { return true; }
                if (!data.name) {
                    TK.toast.warning('Kanal adı boş olamaz.');
                    $f.find('[name=name]').addClass('is-invalid').trigger('focus');
                    return false;
                }
                return options.save(data).done(function (res) {
                    TK.toast.success('Profil kaydedildi.');
                    $(document).trigger('tk:profile-saved', [res || data]);
                });
            }
        });
        $f.on('input', '[name=name]', function () { $(this).removeClass('is-invalid'); });
        $saveBtn.on('click', function () { h.close(); });
        $f.on('submit', function (e) { e.preventDefault(); h.close(); });
        return h;
    }

    TK.profile = {
        /** ➕ Yeni profil (şablon seçicili); kaydedilen profil aktif olur. */
        create: function () {
            return open({
                title: 'Yeni profil',
                withTemplates: true,
                initial: $.extend({ name: '', templateKey: '' }, DEFAULTS),
                save: function (data) { return TK.api.post('/Profil/Ekle', data); }
            });
        },
        /** ✏️ Var olan profili düzenler. */
        edit: function (id) {
            TK.api.get('/Profil/Getir/' + encodeURIComponent(id)).done(function (p) {
                open({
                    title: 'Profili düzenle',
                    id: id,
                    withTemplates: false,
                    initial: p,
                    save: function (data) { return TK.api.put('/Profil/Guncelle/' + encodeURIComponent(id), data); }
                });
            });
        }
    };
})(window, window.jQuery);
