using System.Security;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

using static PasswordGenerator.Extenders;

namespace PasswordSecure.Presentation.Controls.PasswordBox
{
    public class PasswordField : TextBox
    {
        public static readonly StyledProperty<SecureString?> PasswordProperty =
            AvaloniaProperty.Register<RevealPasswordBox, SecureString?>(nameof(Password),
                defaultBindingMode: BindingMode.TwoWay);

        public SecureString? Password
        {
            get => GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public new string? Text { get; set; }

        private static new readonly StyledProperty<string> PlaceholderTextProperty =
            AvaloniaProperty.Register<PasswordField, string>(nameof(PlaceholderText), "Enter password");

        public new string PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            if ((base.Text ?? string.Empty).ToSecureString() is SecureString secure
                         && !Password.SecureStringEquals(secure))
            {
                Password = secure;
            }
        }
    }
}