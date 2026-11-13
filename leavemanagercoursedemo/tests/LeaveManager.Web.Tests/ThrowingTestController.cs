using Microsoft.AspNetCore.Mvc;

namespace LeaveManager.Web.Tests;

/// <summary>Lives only in the test assembly: lets tests trigger an unexpected server error.</summary>
public sealed class ThrowingTestController : Controller
{
    public const string Path = "/__test/throw";
    public const string SecretMessage = "SECRET-TEST-EXCEPTION-7f3a";

    [Route(Path)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1822:Mark members as static", Justification = "MVC actions must be instance methods.")]
    public IActionResult Throw() => throw new InvalidOperationException(SecretMessage);
}
