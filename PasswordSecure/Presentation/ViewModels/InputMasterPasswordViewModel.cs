using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using PasswordSecure.DomainModel;

namespace PasswordSecure.Presentation.ViewModels;

public partial class InputMasterPasswordViewModel : ObservableObject, IPasswordViewModel
{
    internal const string capsLockOn = "*Attention CapsLock is enabled";

    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int vKey);

    private const int VK_CAPITAL = 0x14;

    private readonly CancellationTokenSource _cts = new();

    public InputMasterPasswordViewModel(AccessParams accessParams)
    {
        _accessParams = accessParams;
        _cts = new CancellationTokenSource();
        StartMonitoring();
    }

    public SecureString? Password
    {
        get => _accessParams.Password;
        set
        {
            SetProperty(
                _accessParams.Password,
                value,
                _accessParams,
                (accessParams, propertyValue) => accessParams.Password = propertyValue);
        }
    }

    public string IsCapsLockOn
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(IsCapsLockOn));
            }
        }
    } = string.Empty;

    private readonly AccessParams _accessParams;

    private void UpdateCapsLock()
    {
        short keyState = GetKeyState(VK_CAPITAL);
        IsCapsLockOn = (keyState & 1) != 0 ? capsLockOn : string.Empty;
    }

    private async void StartMonitoring()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(100, _cts.Token);
                UpdateCapsLock();
            }
        }
        catch (TaskCanceledException) { }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}


