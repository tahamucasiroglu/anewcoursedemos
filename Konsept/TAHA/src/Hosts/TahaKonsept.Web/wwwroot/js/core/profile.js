/**
 * TK.profile — kanal profili ekleme / düzenleme penceresi (üst bardaki ✏️ ve ➕).
 * Alanlar: kanal adı, video dili, biçim (dikey/yatay), hedef süre, kanal içeriği, hazır şablon.
 * Pencere dışına tıklanınca ya da kapatılınca değişiklik varsa kaydeder (POST /Profil/Ekle, PUT /Profil/Guncelle/{id}).
 * Kaydedince document üzerinde 'tk:profile-saved' tetikler. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    var LANGS = [['tr', 'Türkçe'], ['en', 'İngilizce'], ['de', 'Almanca'], ['es', 'İspanyolca'], ['fr', 'Fransızca'], ['ar', 'Arapça']];

    function formHtml() {
        var langs = $.map(LANGS, function (l) { return '<option value="' + l[0] + '">' + l[1] + '</option>'; }).join('');
        return $(
            '<form class="tk-profile-form" autocomplete="off" novalidate>' +
            '<div class="tk-field"><label for="pf-template">Hazır şablon</label>' +
            '<select id="pf-template" name="templateKey" class="tk-select"><option value="">Şablonsuz başla</option></select>' +
            '<span class="tk-hint">Şablon seçmek alanları doldurur; sonra dilediğiniz gibi değiştirebilirsiniz.</span></div>' +
            '<div class="tk-form-grid">' +
            '<div class="tk-field"><label for="pf-name">Kanal adı</label><input id="pf-name" name="name" class="tk-input" maxlength="120" required placeholder="Ör. Bizans Günlükleri" /></div>' +
            '<div class="tk-field"><label for="pf-lang">Video dili</label><select id="pf-lang" name="language" class="tk-select">' + langs + '</select></div>' +
            '<div class="tk-field"><label for="pf-format">Biçim</label><select id="pf-format" name="format" class="tk-select">' +
            '<option value="Vertical">Dikey (9:16 — Shorts/Reels)</option><option value="Horizontal">Yatay (16:9 — YouTube)</option></select></div>' +
            '<div class="tk-field"><label for="pf-duration">Hedef süre (sn)</label><input id="pf-duration" name="targetDurationSec" type="number" min="15" max="1800" step="5" class="tk-input" value="60" /></div>' +
            '</div>' +
            '<div class="tk-field"><label for="pf-content">Kanal içeriği</label>' +
            '<textarea id="pf-content" name="content" class="tk-textarea" rows="5" maxlength="4000" placeholder="Odak, tarih aralığı, kaçınılacak konular, hedef kitle… (konsept üretiminde sistem talimatına eklenir)"></textarea></div>' +
            '</form>');
    }

    function read($f) {
        return {
            name: $.trim($f.find('[name=name]').val()),
            language: $f.find('[name=language]').val(),
            format: $f.find('[name=format]').val(),
            targetDurationSec: TK.util.clampInt($f.find('[name=targetDurationSec]').val(), 15, 1800, 60),
            content: $.trim($f.find('[name=content]').val()),
            templateKey: $f.find('[name=templateKey]').val() || null
        };
    }

    function fill($f, p) {
        if (!p) { return; }
        $f.find('[name=name]').val(p.name || p.channelName || '');
        if (p.language) { $f.find('[name=language]').val(p.language); }
        if (p.format) { $f.find('[name=format]').val(p.format); }
        if (p.targetDurationSec) { $f.find('[name=targetDurationSec]').val(p.targetDurationSec); }
        $f.find('[name=content]').val(p.content || p.channelContent || '');
        if (p.templateKey) { $f.find('[name=templateKey]').val(p.templateKey); }
    }

    function loadTemplates($f) {
        var templates = [];
        TK.api.get('/Profil/Sablonlar', null, { silent: true }).done(function (data) {
            templates = TK.util.items(data);
            var $sel = $f.find('[name=templateKey]');
            $.each(templates, function (_, t) { $sel.append($('<option></option>').val(t.key || t.id).text(t.name || t.title)); });
        });
        $f.on('change', '[name=templateKey]', function () {
            var key = $(this).val();
            $.each(templates, function (_, t) {
                if (String(t.key || t.id) === key) {
                    var keepName = $.trim($f.find('[name=name]').val());
                    fill($f, $.extend({}, t, { templateKey: key, name: keepName || t.name }));
                }
            });
        });
    }

    function open(title, initial, save) {
        var $f = formHtml();
        loadTemplates($f);
        fill($f, initial);
        var snapshot = JSON.stringify(read($f));

        var $saveBtn = $('<button type="button" class="tk-btn tk-btn-primary"></button>').html(TK.util.icon('check') + ' Kaydet');
        var h = TK.modal.open({
            title: title,
            body: $f,
            footer: $saveBtn,
            hint: 'Dışarı tıklayınca da kaydedilir.',
            onClose: function () {
                var data = read($f);
                if (JSON.stringify(data) === snapshot) { return true; }
                if (!data.name) { TK.toast.warning('Kanal adı boş olamaz.'); $f.find('[name=name]').addClass('is-invalid').trigger('focus'); return false; }
                return save(data).done(function (res) {
                    TK.toast.success('Profil kaydedildi.');
                    $(document).trigger('tk:profile-saved', [res || data]);
                });
            }
        });
        $saveBtn.on('click', function () { h.close(); });
        $f.on('submit', function (e) { e.preventDefault(); h.close(); });
        return h;
    }

    TK.profile = {
        create: function () {
            return open('Yeni profil', { language: 'tr', format: 'Vertical', targetDurationSec: 60 }, function (data) {
                return TK.api.post('/Profil/Ekle', data);
            });
        },
        edit: function (id) {
            TK.api.get('/Profil/Getir/' + encodeURIComponent(id)).done(function (p) {
                open('Profili düzenle', p, function (data) {
                    return TK.api.put('/Profil/Guncelle/' + encodeURIComponent(id), $.extend({ id: id }, data));
                });
            });
        }
    };
})(window, window.jQuery);
