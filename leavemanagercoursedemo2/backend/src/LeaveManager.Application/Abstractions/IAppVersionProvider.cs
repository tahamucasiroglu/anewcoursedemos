namespace LeaveManager.Application.Abstractions;

/// <summary>Port: the running application's version (implemented in Infrastructure).</summary>
public interface IAppVersionProvider
{
    string GetVersion();
}
