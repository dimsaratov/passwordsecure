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
    public EditPasswordWindow()
    {
        InitializeComponent();
        _isPasswordAccepted = false;
        _settings = new();
        AddHandler(KeyDownEvent, OnKeyPressing, RoutingStrategies.Tunnel);

    }
    public EditPasswordWindow(GenerationSettings g_settings) : this()
    {

        _settings = g_settings;
    }

    public int MinimumPasswordLength { get; set; }

    private bool _isPasswordAccepted;

    private SecureString? _initialPassword;

    private readonly GenerationSettings _settings;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _initialPassword = TextBoxPassword.Password;

        TextBoxPassword.Focus();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isPasswordAccepted)
        {
            TextBoxPassword.Password = _initialPassword;
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
        var passwordGeneratorView = new PasswordGeneratorView();
        {
            DataContext = new PasswordGeneratorViewModel(_settings);
        }
        await passwordGeneratorView.ShowDialog(this);
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
        get
        {
            bool isPasswordTooShort = false;

            if (TextBoxPassword.Password is not null)
            {
                isPasswordTooShort =
                    TextBoxPassword.Password.Length < MinimumPasswordLength;
            }

            return isPasswordTooShort;
        }
    }

    private async Task DisplayPasswordTooShortErrorMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            "Password Too Short Error",
            $"The password is too short.{Environment.NewLine}{Environment.NewLine}Minimum password length is {MinimumPasswordLength} characters.",
            MessageBoxType.Error,
            this);
    }

    private bool IsPasswordMismatch
    {
        get
        {
            bool isPasswordMismatch = false;

            if (TextBoxPassword.Password?.Length == 0)
            {
                TextBoxPassword.Password = null;
            }

            if (TextBoxConfirmPassword.Password?.Length == 0)
            {
                TextBoxConfirmPassword.Password = null;
            }

            if (TextBoxPassword.Password is not null ||
                TextBoxConfirmPassword.Password is not null)
            {
                isPasswordMismatch =
                    TextBoxPassword.Password.SecureStringEquals(TextBoxConfirmPassword.Password);
            }

            return isPasswordMismatch;
        }
    }

    private async Task DisplayPasswordMismatchErrorMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            "Password Mismatch Error",
            "The password and the confirmed password do not match.",
            MessageBoxType.Error,
            this);
    }
}
