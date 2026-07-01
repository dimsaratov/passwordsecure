using System;
using System.Runtime.InteropServices;
using System.Security;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using PasswordSecure.Infrastructure;

namespace PasswordSecure.Presentation.Controls
{
    public partial class RevealPasswordBox : UserControl
    {
        public static readonly StyledProperty<SecureString?> PasswordProperty =
            AvaloniaProperty.Register<RevealPasswordBox, SecureString?>(nameof(Password),
                defaultBindingMode: BindingMode.TwoWay);

        public SecureString? Password
        {
            get => GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<RevealPasswordBox, char>(nameof(PasswordChar), '*');

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public static readonly StyledProperty<string> PlaceholderTextProperty =
            AvaloniaProperty.Register<RevealPasswordBox, string>(nameof(PlaceholderText), "Enter password");

        public string PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public static readonly StyledProperty<bool> RevealProperty =
            AvaloniaProperty.Register<RevealPasswordBox, bool>(nameof(Reveal), defaultBindingMode: BindingMode.TwoWay);

        public bool Reveal
        {
            get => GetValue(RevealProperty);
            set => SetValue(RevealProperty, value);
        }

        private readonly DispatcherTimer _hideTimer;
        private bool _isUpdatingFromPassword;

        public RevealPasswordBox()
        {
            InitializeComponent();

            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                Reveal = false;
            };

            TextBox? textBox = this.FindControl<TextBox>("PART_TextBox");
            textBox?.TextChanged += OnTextBoxTextChanged;

            Button? btnReveal = this.FindControl<Button>("PART_RevealButton");
            btnReveal?.AddHandler(Button.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            btnReveal?.AddHandler(Button.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);

            UpdateTextBoxFromPassword();
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            UpdatePasswordChar(true);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            UpdatePasswordChar(false);
        }

        private void BtnReveal_IsCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.FindControl<ToggleButton>("PART_RevealButton") is ToggleButton btnReveal)
            {
                bool isRevealed = btnReveal.IsChecked ?? false;
                if (isRevealed)
                    _hideTimer.Start();
                else
                    _hideTimer.Stop();
                UpdatePasswordChar(isRevealed);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == RevealProperty)
            {
                bool newValue = e.GetNewValue<bool>();
                if (newValue)
                    _hideTimer.Start();
                else
                    _hideTimer.Stop();
            }
            else if (e.Property == PasswordProperty)
            {
                UpdateTextBoxFromPassword();
            }
        }

        private void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromPassword) return;

            if (sender is not TextBox textBox) return;

            string newText = textBox.Text ?? string.Empty;
            var secure = new SecureString();
            foreach (char c in newText)
                secure.AppendChar(c);
            secure.MakeReadOnly();

            if (!Password.SecureStringEquals(secure))
            {
                Password = secure;
            }
        }

        private void UpdateTextBoxFromPassword()
        {
            _isUpdatingFromPassword = true;
            try
            {
                TextBox? textBox = this.FindControl<TextBox>("PART_TextBox");
                if (textBox == null) return;

                string newText;
                if (Password == null || Password.Length == 0)
                {
                    newText = string.Empty;
                }
                else
                {
                    IntPtr ptr = Marshal.SecureStringToBSTR(Password);
                    try
                    {
                        newText = Marshal.PtrToStringBSTR(ptr);
                    }
                    finally
                    {
                        Marshal.ZeroFreeBSTR(ptr);
                    }
                }
                if (textBox.Text != newText)
                    textBox.Text = newText;
            }
            finally
            {
                _isUpdatingFromPassword = false;
            }
        }

        private void UpdatePasswordChar(bool isRevealed)
        {
            TextBox? textBox = this.FindControl<TextBox>("PART_TextBox");
            if (textBox == null) return;
            textBox.PasswordChar = isRevealed ? '\0' : PasswordChar;
        }

        public void FocusTextBox()
        {
            TextBox? textBox = this.FindControl<TextBox>("PART_TextBox");
            textBox?.Focus();
        }
    }
}