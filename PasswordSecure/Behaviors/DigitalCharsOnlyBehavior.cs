using System.Linq;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace PasswordSecure.Behaviors
{
    public class DigitalCharsOnlyBehavior : Behavior<NumericUpDown>
    {
        private TextBox? textBox;
        private string decimalSeparator = ".";

        protected override void OnAttached()
        {
            base.OnAttached();
            decimalSeparator = AssociatedObject?.NumberFormat?
                                                .NumberDecimalSeparator is string ds ?
                                                 ds : ".";

            AssociatedObject?.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            textBox = AssociatedObject?.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(tb => tb.Name == "PART_TextBox" ||
                                 tb.Parent is NumericUpDown);

            AssociatedObject?.Loaded -= OnLoaded;
            textBox?.KeyDown += OnKeyDown;
            textBox?.TextChanged += OnTextChanged;
        }


        protected override void OnDetaching()
        {
            base.OnDetaching();
            textBox?.KeyDown -= OnKeyDown;
            textBox?.TextChanged -= OnTextChanged;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            Key key = e.Key;
            KeyModifiers modifiers = e.KeyModifiers;

            if (key is Key.Back or Key.Delete or Key.Left or Key.Right or
                Key.Up or Key.Down or Key.Home or Key.End or Key.Tab)
            {
                return;
            }

            if (modifiers == KeyModifiers.Control && (key is Key.C or Key.V or Key.X))
            {
                return;
            }

            if ((key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9))
            {
                return;
            }

            if (IsDecimalSeparatorKey(key))
            {
                string? currentText = textBox?.Text;
                if (!string.IsNullOrEmpty(currentText) && currentText.Contains(decimalSeparator))
                {
                    e.Handled = true;
                    return;
                }
                return;
            }
            e.Handled = true;
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || string.IsNullOrEmpty(textBox.Text))
                return;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                AssociatedObject?.Value = AssociatedObject?.Minimum;
                return;
            }

            if (decimal.TryParse(textBox.Text, out decimal value)
                && value > AssociatedObject?.Minimum
                && value < AssociatedObject?.Maximum)
            {
                textBox.Text = $"{value}";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }


        private bool IsDecimalSeparatorKey(Key key)
        {
            return decimalSeparator switch
            {
                "," => key is Key.OemComma,
                "." => key is Key.OemPeriod or Key.Decimal,
                _ => false
            };
        }
    }
}