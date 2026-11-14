/**
 * TK.account — giriş (/Account/Login) ve ilk kurulum (/Kurulum) formları. Formlar jQuery Ajax ile JSON gönderir;
 * başarılı cevap { redirectUrl } ile yönlendirir, hatayı form içindeki uyarı kutusunda gösterir. Tek modül (IIFE).
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    function showError($box, message) {
        $box.prop('hidden', !message).find('span').text(message || '');
    }

    function submit($form, $box, payload) {
        var $btn = $form.find('button[type=submit]');
        showError($box, null);
        return TK.util.busy($btn, TK.api.post($form.data('url'), payload, { silent: true, allowAnonymous: true }))
            .done(function (res) { window.location.href = (res && res.redirectUrl) || '/'; })
            .fail(function (xhr) { showError($box, TK.api.errorMessage(xhr)); });
    }

    function initLogin() {
        var $form = $('#tk-login-form');
        if (!$form.length) { return; }
        var $box = $('#tk-login-error');
        $form.on('submit', function (e) {
            e.preventDefault();
            var userName = $.trim($form.find('[name=userName]').val());
            var password = $form.find('[name=password]').val();
            if (!userName || !password) { showError($box, 'Kullanıcı adı ve parola gerekli.'); return; }
            submit($form, $box, {
                userName: userName,
                password: password,
                rememberMe: $form.find('[name=rememberMe]').is(':checked'),
                returnUrl: $form.find('[name=returnUrl]').val() || null
            });
        });
    }

    function initSetup() {
        var $form = $('#tk-setup-form');
        if (!$form.length) { return; }
        var $box = $('#tk-setup-error');
        $form.on('submit', function (e) {
            e.preventDefault();
            var userName = $.trim($form.find('[name=userName]').val());
            var password = $form.find('[name=password]').val();
            var confirm = $form.find('[name=passwordConfirm]').val();
            if (userName.length < 3) { showError($box, 'Kullanıcı adı en az 3 karakter olmalı.'); return; }
            if (password.length < 8) { showError($box, 'Parola en az 8 karakter olmalı.'); return; }
            if (password !== confirm) { showError($box, 'Parolalar eşleşmiyor.'); return; }
            submit($form, $box, { userName: userName, password: password, passwordConfirm: confirm });
        });
    }

    TK.account = { initLogin: initLogin, initSetup: initSetup };
    $(function () { initLogin(); initSetup(); });
})(window, window.jQuery);
