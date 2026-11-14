using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using TahaKonsept.Agent.ViewModels.Concrete;

namespace TahaKonsept.Agent.Views;

/// <summary>
/// İlk kurulum penceresi (görünüm modeli: <see cref="SetupViewModel"/>). Kod tarafında yalnızca günlük panelini yeni satırda
/// en alta kaydırma vardır; tüm davranış görünüm modelindedir.
/// </summary>
public partial class SetupWindow : Window
{
    private SetupViewModel? _vm;

    /// <summary>Oluşturur.</summary>
    public SetupWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Attach(DataContext as SetupViewModel);
        Closed += (_, _) => Attach(null);
    }

    private void Attach(SetupViewModel? vm)
    {
        if (_vm is not null)
        {
            _vm.Logs.CollectionChanged -= OnLogsChanged;
        }

        _vm = vm;
        if (_vm is not null)
        {
            _vm.Logs.CollectionChanged += OnLogsChanged;
        }
    }

    private void OnLogsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() => LogScroll.ScrollToEnd(), DispatcherPriority.Background);
}
