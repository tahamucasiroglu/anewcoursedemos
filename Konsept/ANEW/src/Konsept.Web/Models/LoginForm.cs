namespace Konsept.Web.Models;

/// <summary>Giriş formu (e-posta, şifre).</summary>
public sealed class LoginForm : AuthForm
{
    /// <inheritdoc />
    public override IReadOnlyList<string> FieldOrder { get; } = ["Email", "Password"];
}
