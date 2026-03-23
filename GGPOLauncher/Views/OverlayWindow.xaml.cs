using System.Windows;
using GGPOLauncher.Core.Services;

namespace GGPOLauncher.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow(OverlayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void UpdateBounds(Rect emulatorBounds)
    {
        Left = emulatorBounds.Left;
        Top = emulatorBounds.Top;
        Width = emulatorBounds.Width;
        Height = emulatorBounds.Height;
    }

    public void ShowOverlay()
    {
        if (!IsVisible)
            Show();
    }

    public void HideOverlay()
    {
        if (IsVisible)
            Hide();
    }
}