using System.Windows.Input;

namespace Konsept.Agent;

/// <summary>
/// Tray menü öğeleri için en basit komut: her zaman çalıştırılabilir, verilen eylemi çağırır.
/// </summary>
internal sealed class DelegateCommand : ICommand
{
    private readonly Action _execute;

    /// <summary>Initializes a new instance of the <see cref="DelegateCommand"/> class.</summary>
    /// <param name="execute">Çalıştırılacak eylem.</param>
    public DelegateCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();
}
