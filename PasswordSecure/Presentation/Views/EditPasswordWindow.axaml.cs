using System;
using System.Security;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using PasswordGenerator;

using PasswordSecure.Presentation.Controls.MessageBoxControl;
using PasswordSecure.Presentation.ViewModels;

using static PasswordGenerator.Extenders;


namespace PasswordSecure.Presentation.Views;

public partial class EditPasswordWindow : Window
{
    private const string Mm_Error = "Password Mismatch Error";
    private const string Mm_ErrorLong = "The password and the confirmed password do not match.";
    private const string PTS_Error = "Password Too Short Error";

    public EditPasswordWindow()
    {
        InitializeComponent();
        _isPasswordAccepted = false;
        AddHandler(KeyDownEvent, OnKeyPressing, RoutingStrategies.Tunnel);
    }

    public int MinimumPasswordLength { get; set; }

    private bool _isPasswordAccepted;

    private SecureString? _initialPassword;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _initialPassword = TextBoxPassword.Password;

        TextBoxPassword.Focus();
    }

    private SecureString? Password
    {
        get => TextBoxPassword.Password ?? _initialPassword;
        set => TextBoxPassword.Password = value;
    }

    private SecureString? ConfirmPassword
    {
        get => TextBoxConfirmPassword.Password ?? _initialPassword;
        set => TextBoxConfirmPassword.Password = value;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isPasswordAccepted)
        {
            Password = _initialPassword;
        }
    }

    private async void OnKeyPressing(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _isPasswordAccepted = await CanContinue();

            if (_isPasswordAccepted)
            {
                Close();
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _isPasswordAccepted = false;

            Close();

            e.Handled = true;
        }
    }

    private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
    {
        _isPasswordAccepted = false;

        Close();
    }

    private async void OnButtonGenerateClick(object? sender, RoutedEventArgs e)
    {
        if (await AppViewModel.GeneratePasswordAsync(this, true) is SecureString secure)
        {
            Password = secure;
            ConfirmPassword = secure;
        }
    }

    private async void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        _isPasswordAccepted = await CanContinue();

        if (_isPasswordAccepted)
        {
            Close();
        }
    }


    private async Task<bool> CanContinue()
    {
        if (IsPasswordTooShort)
        {
            await DisplayPasswordTooShortErrorMessage();

            return false;
        }

        if (!IsPasswordMismatch)
        {
            await DisplayPasswordMismatchErrorMessage();

            return false;
        }

        return true;
    }

    private bool IsPasswordTooShort
    {
        get => Password?.Length < MinimumPasswordLength;
    }

    private bool IsPasswordMismatch
    {
        get => Password is not null && !(Password?.Length < AppViewModel.GenSettings?.MinLength)
        && Password.SecureStringEquals(ConfirmPassword);
    }

    private async Task DisplayPasswordTooShortErrorMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            PTS_Error,
            TS_Error,
            MessageBoxType.Error,
            this);
    }

    private string TS_Error =>
        $"The password is too short.{Environment.NewLine}{Environment.NewLine}" +
        $"Minimum password length is {MinimumPasswordLength} characters.";



    private async Task DisplayPasswordMismatchErrorMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            Mm_Error,
            Mm_ErrorLong,
            MessageBoxType.Error,
            this);
    }
}
