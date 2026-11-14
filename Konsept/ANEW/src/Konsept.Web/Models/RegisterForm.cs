namespace Konsept.Web.Models;

/// <summary>Kayıt formu (ad, e-posta, tek şifre alanı).</summary>
public sealed class RegisterForm : AuthForm
{
    /// <summary>Gets or sets ad.</summary>
    public string? Name { get; set; }

    /// <inheritdoc />
    public override IReadOnlyList<string> FieldOrder { get; } = ["Name", "Email", "Password"];
}
