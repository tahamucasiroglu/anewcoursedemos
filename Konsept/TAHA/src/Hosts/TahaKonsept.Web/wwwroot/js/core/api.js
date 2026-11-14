/**
 * TK.api — tarayıcı → MVC isteklerinin TEK kapısı (yalnızca jQuery $.ajax; başka HTTP API kullanılmaz).
 * TK.api.get(url, query, opts) / post(url, body, opts) / put(url, body, opts) / del(url, opts).
 * JSON gönderir/alır, antiforgery başlığını otomatik ekler, hatayı tek tip bildirimle gösterir
 * (opts.silent = true ise göstermez), 401'de giriş sayfasına yönlendirir (opts.allowAnonymous hariç). jqXHR promise döner.
 */
(function (window, $) {
    'use strict';
    var TK = window.TK = window.TK || {};

    function csrfToken() {
        return $('meta[name="csrf-token"]').attr('content') || '';
    }

    function csrfHeader() {
        return $('meta[name="csrf-header"]').attr('content') || 'X-CSRF-TOKEN';
    }

    /** ProblemDetails / doğrulama hatası / düz metin cevabından okunur mesaj çıkarır. */
    function errorMessage(xhr) {
        if (!xhr) { return 'Beklenmeyen bir hata oluştu.'; }
        if (xhr.status === 0) { return 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.'; }
        var body = xhr.responseJSON;
        if (!body && xhr.responseText) {
            try { body = JSON.parse(xhr.responseText); } catch (e) { body = null; }
        }
        if (body) {
            if (body.errors && typeof body.errors === 'object') {
                var lines = [];
                $.each(body.errors, function (_, v) { lines = lines.concat($.isArray(v) ? v : [v]); });
                if (lines.length) { return lines.join(' '); }
            }
            if (body.detail) { return body.detail; }
            if (body.message) { return body.message; }
            if (body.title) { return body.title; }
        }
        if (xhr.status === 403) { return 'Bu işlem için yetkiniz yok.'; }
        if (xhr.status === 404) { return 'İstenen kayıt bulunamadı.'; }
        if (xhr.status >= 500) { return 'Sunucu hatası (' + xhr.status + ').'; }
        return 'İstek başarısız (' + xhr.status + ').';
    }

    function request(method, url, data, opts) {
        opts = opts || {};
        var headers = {};
        headers[csrfHeader()] = csrfToken();
        headers['X-Requested-With'] = 'XMLHttpRequest';

        var settings = {
            url: url,
            method: method,
            headers: headers,
            dataType: 'json',
            cache: false,
            timeout: opts.timeout || 120000
        };

        if (method === 'GET') {
            settings.data = data || undefined;
        } else if (data !== undefined && data !== null) {
            settings.contentType = 'application/json; charset=utf-8';
            settings.data = JSON.stringify(data);
        }

        // Boş gövdeli başarılı cevaplar (204) jQuery'de "parsererror" olmasın diye dönüştürülür.
        var d = $.Deferred();
        var xhr = $.ajax(settings);
        xhr.done(function (body, status, jq) { d.resolve(body, status, jq); });
        xhr.fail(function (jq, textStatus, err) {
            if (textStatus === 'parsererror' && jq.status >= 200 && jq.status < 300) { d.resolve(null, 'success', jq); return; }
            d.reject(jq, textStatus, err);
            if (textStatus === 'abort') { return; }
            if (jq.status === 401 && !opts.allowAnonymous) {
                var back = encodeURIComponent(window.location.pathname + window.location.search);
                window.location.href = '/Account/Login?returnUrl=' + back;
                return;
            }
            if (!opts.silent && TK.toast) { TK.toast.error(errorMessage(jq)); }
        });
        var promise = d.promise();
        promise.abort = function () { xhr.abort(); return promise; };
        return promise;
    }

    TK.api = {
        get: function (url, query, opts) { return request('GET', url, query, opts); },
        post: function (url, body, opts) { return request('POST', url, body === undefined ? {} : body, opts); },
        put: function (url, body, opts) { return request('PUT', url, body === undefined ? {} : body, opts); },
        del: function (url, opts) { return request('DELETE', url, null, opts); },
        errorMessage: errorMessage
    };
})(window, window.jQuery);
