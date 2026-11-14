using System.Net;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Konsept.Web.Tests.Support;

/// <summary>
/// Default-deny Web'de oturum açmış istemci (plan 0002, Risk 8): gerçek <c>GET /giris</c> → CSRF belirteci →
/// <c>POST /giris</c> akışıyla giriş yapar; oturum cookie'si istemcide kalır. Yönlendirmeler izlenmez (testler
/// <c>Location</c>'ı doğrulayabilsin).
/// </summary>
public static class SignedInClient
{
    /// <summary>Test e-postası.</summary>
    public const string Email = "test@konsept.local";

    /// <summary>Test şifresi.</summary>
    public const string Password = "Sifre1234";

    /// <summary>Yönlendirme izlemeyen, cookie tutan anonim istemci açar.</summary>
    /// <param name="factory">Web fabrikası.</param>
    /// <returns>İstemci.</returns>
    public static HttpClient CreateAnonymous(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    /// <summary>Giriş yapmış istemci açar.</summary>
    /// <param name="factory">Web fabrikası.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İstemci.</returns>
    public static async Task<HttpClient> CreateAsync(WebApplicationFactory<Program> factory, CancellationToken cancellationToken)
    {
        var client = CreateAnonymous(factory);
        using var response = await PostFormAsync(client, "/giris", new Dictionary<string, string> { ["Email"] = Email, ["Password"] = Password }, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            throw new InvalidOperationException($"Giriş başarısız: {(int)response.StatusCode}");
        }

        return client;
    }

    /// <summary>Formun sayfasını açıp CSRF belirteciyle formu gönderir.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="path">Form sayfası ve gönderim adresi (<c>/giris</c>, <c>/kayit</c>).</param>
    /// <param name="fields">Form alanları.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <param name="query">Gönderim adresine eklenecek sorgu (ör. <c>?returnUrl=…</c>; dönüş adresi forma da yazılır).</param>
    /// <returns>Cevap.</returns>
    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string path,
        IDictionary<string, string> fields,
        CancellationToken cancellationToken,
        string query = "")
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(fields);
        var html = await client.GetStringAsync(new Uri(path + query, UriKind.Relative), cancellationToken).ConfigureAwait(false);
        var document = await new HtmlParser().ParseDocumentAsync(html, cancellationToken).ConfigureAwait(false);
        var form = new Dictionary<string, string>(fields, StringComparer.Ordinal);
        foreach (var hidden in document.QuerySelectorAll("form[data-auth-form] input[type=hidden]"))
        {
            var name = hidden.GetAttribute("name");
            if (name is not null && !form.ContainsKey(name))
            {
                form[name] = hidden.GetAttribute("value") ?? string.Empty;
            }
        }

        using var content = new FormUrlEncodedContent(form);
        return await client.PostAsync(new Uri(path, UriKind.Relative), content, cancellationToken).ConfigureAwait(false);
    }
}
