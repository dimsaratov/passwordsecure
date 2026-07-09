using System;
using System.Diagnostics;
using System.Security;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using PasswordGenerator;

using PasswordSecure.Presentation.Controls;
using PasswordSecure.Presentation.Controls.MessageBoxControl;
using PasswordSecure.Presentation.ViewModels;

using static PasswordGenerator.Extenders;


namespace PasswordSecure.Presentation.Views;

public partial class EditPasswordWindow : Window
{
    private const string Mm_Error = "Password Mismatch Error";
    private const string Mm_ErrorLong = "The password and the confirmed password do not match.";
    private const string PTS_Error = "Password Too Short Error";
    private bool _isPasswordAccepted;

    private SecureString? _initialPassword;

    public static readonly StyledProperty<string?> IsMessageMismatchProperty =
                    AvaloniaProperty.Register<EditPasswordWindow, string?>(nameof(IsMessageMismatch));

    public EditPasswordWindow()
    {
        InitializeComponent();
        _isPasswordAccepted = false;
        AddHandler(KeyDownEvent, OnKeyPressing, RoutingStrategies.Tunnel);
        TextBoxPassword.PropertyChanged += OnPasswordPropertyChanged;
        TextBoxConfirmPassword.PropertyChanged += OnPasswordPropertyChanged;
    }

    private void OnPasswordPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is RevealPasswordBox && e.Property.Name == "Password")
        {
            ValidatePassword();
        }
    }


    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _initialPassword = TextBoxPassword.Password?.Copy();
        TextBoxConfirmPassword.Password = TextBoxPassword.Password?.Copy();
        TextBoxPassword.Focus();
        IsMessageMismatch = "Без ошибок";
    }

    public string? IsMessageMismatch
    {
        get => GetValue(IsMessageMismatchProperty);
        set => SetValue(IsMessageMismatchProperty, value);
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
        if (await AppViewModel.GeneratePasswordAsync(this, true) is SecureString secure)
        {
            TextBoxPassword.Password = secure;
            TextBoxConfirmPassword.Password = secure.Copy();
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

    private void ValidatePassword()
    {
        var sb = new StringBuilder();
        PasswordValidationResult? result = null;
        if (AppViewModel.GenSettings is not null)
        {
            SecureString? pass = TextBoxPassword.Password;
            SecureString? c_pass = TextBoxConfirmPassword.Password;
            SecureString? oldPass = _initialPassword;

            Debug.WriteLine($"Current pass: {pass?.ToUnSecureString()}");
            Debug.WriteLine($"Confirm pass: {c_pass?.ToUnSecureString()}");
            Debug.WriteLine($"Initial pass: {oldPass?.ToUnSecureString()}");

            result = PasswordValidator.ValidatePassword(pass,
                                                        AppViewModel.GenSettings,
                                                        oldPass);
        }

        if (result?.IsValid == false)
        {
            foreach (string err in result.Errors)
            {
                sb.AppendLine(err);
            }
        }

        if (IsPasswordMismatch)
        {
            sb.AppendLine("The passwords do not match each other.");
        }
        IsMessageMismatch = sb.ToString();
    }


    private bool IsPasswordTooShort
    {
        get => TextBoxPassword.Password?.Length < AppViewModel.MinimumPasswordLength;
    }

    private bool IsPasswordMismatch
    {
        get => !TextBoxPassword.Password.SecureStringEquals(TextBoxConfirmPassword.Password);
    }

    private async Task DisplayPasswordTooShortErrorMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            PTS_Error,
            TS_Error,
            MessageBoxType.Error,
            this);
    }

    private static string TS_Error =>
        $"The password is too short.{Environment.NewLine}" +
        $"Minimum password length is {AppViewModel.MinimumPasswordLength} characters.";



    private async Task DisplayPasswordMismatchErrorMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            Mm_Error,
            Mm_ErrorLong,
            MessageBoxType.Error,
            this);
    }
}
