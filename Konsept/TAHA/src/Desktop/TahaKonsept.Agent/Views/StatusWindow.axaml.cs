using Avalonia.Controls;
using TahaKonsept.Agent.ViewModels.Concrete;

namespace TahaKonsept.Agent.Views;

/// <summary>Durum penceresi (görünüm modeli: <see cref="StatusViewModel"/>); kod tarafında davranış yoktur.</summary>
public partial class StatusWindow : Window
{
    /// <summary>Oluşturur.</summary>
    public StatusWindow() => InitializeComponent();
}
