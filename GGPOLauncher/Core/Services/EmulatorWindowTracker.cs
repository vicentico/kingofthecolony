using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace GGPOLauncher.Core.Services;

public sealed class EmulatorWindowTracker : IDisposable
{
    private readonly System.Diagnostics.Process _process;
    private readonly DispatcherTimer _timer;
    private Rect _lastBounds = Rect.Empty;
    private bool _disposed;

    public event Action<Rect>? BoundsChanged;
    public event Action? Exited;

    public EmulatorWindowTracker(System.Diagnostics.Process process)
    {
        _process = process;
        _process.EnableRaisingEvents = true;
        _process.Exited += Process_Exited;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(125)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_disposed)
            return;

        if (_process.HasExited)
        {
            RaiseExited();
            return;
        }

        _process.Refresh();
        var handle = _process.MainWindowHandle;
        if (handle == IntPtr.Zero)
            return;

        if (!GetWindowRect(handle, out var rect))
            return;

        var bounds = new Rect(rect.Left, rect.Top, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
        if (bounds == _lastBounds)
            return;

        _lastBounds = bounds;
        BoundsChanged?.Invoke(bounds);
    }

    private void Process_Exited(object? sender, EventArgs e)
    {
        RaiseExited();
    }

    private void RaiseExited()
    {
        if (_disposed)
            return;

        _timer.Stop();
        Exited?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
        _process.Exited -= Process_Exited;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}