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
        private TextBox? _textBox;
        private string _decimalSeparator = ".";

        protected override void OnAttached()
        {
            base.OnAttached();
            _decimalSeparator = AssociatedObject?.NumberFormat?
                .NumberDecimalSeparator is string ds
                ? ds
                : ".";

            AssociatedObject?.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _textBox = AssociatedObject?.GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(tb => tb.Name == "PART_TextBox" || tb.Parent is NumericUpDown);

            AssociatedObject!.Loaded -= OnLoaded;

            if (_textBox != null)
            {
                _textBox.KeyDown += OnKeyDown;
                _textBox.TextChanged += OnTextChanged;
                _textBox.TextInput += OnTextInput;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (_textBox != null)
            {
                _textBox.KeyDown -= OnKeyDown;
                _textBox.TextChanged -= OnTextChanged;
                _textBox.TextInput -= OnTextInput;
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            Key key = e.Key;
            KeyModifiers modifiers = e.KeyModifiers;

            if (key is Key.Back or Key.Delete or Key.Left or Key.Right or
                Key.Up or Key.Down or Key.Home or Key.End or Key.Tab or Key.Enter)
            {
                return;
            }

            // Разрешаем Ctrl+C (копирование)
            if (modifiers == KeyModifiers.Control && key is Key.C)
            {
                return;
            }

            // Блокируем Ctrl+V (вставка), Ctrl+X (вырезание)
            if (modifiers == KeyModifiers.Control && (key is Key.V or Key.X))
            {
                e.Handled = true;
                return;
            }

            if ((key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9))
            {
                return;
            }

            if (IsDecimalSeparatorKey(key))
            {
                string? currentText = _textBox?.Text;
                if (!string.IsNullOrEmpty(currentText) && currentText.Contains(_decimalSeparator))
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
            if (sender is not TextBox textBox || AssociatedObject == null)
                return;

            if (!string.IsNullOrEmpty(textBox.Text))
            {
                string cleanText = new(
                    [.. textBox.Text.Where(c => char.IsDigit(c) || c.ToString() == _decimalSeparator)]);

                if (cleanText != textBox.Text)
                {
                    textBox.Text = cleanText;
                    textBox.CaretIndex = textBox.Text.Length;
                    return;
                }

                if (cleanText.Count(c => c.ToString() == _decimalSeparator) > 1)
                {
                    int firstSeparatorIndex = cleanText.IndexOf(_decimalSeparator);
                    string beforeSeparator = cleanText[..firstSeparatorIndex];
                    string afterSeparator = cleanText[(firstSeparatorIndex + 1)..]
                        .Replace(_decimalSeparator, string.Empty);
                    cleanText = beforeSeparator + _decimalSeparator + afterSeparator;

                    textBox.Text = cleanText;
                    textBox.CaretIndex = textBox.Text.Length;
                    return;
                }
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                AssociatedObject?.Value = AssociatedObject?.Minimum;
                string text = (AssociatedObject?.Minimum ?? 0).ToString(FormatString) ?? "0";
                textBox.Text = text;
                textBox.CaretIndex = text.Length;
                return;
            }

            if (decimal.TryParse(textBox.Text, out decimal value))
            {
                decimal min = AssociatedObject?.Minimum ?? 0;
                decimal max = AssociatedObject?.Maximum ?? 100;

                if (value >= min && value <= max)
                {
                    AssociatedObject?.Value = value;
                }
                else if (value < min)
                {
                    AssociatedObject?.Value = min;
                    textBox.Text = min.ToString(FormatString);
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (value > max)
                {
                    AssociatedObject?.Value = max;
                    textBox.Text = max.ToString(FormatString);
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        private void OnTextInput(object? sender, TextInputEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
                return;

            bool isInputValid = e.Text.All(c => char.IsDigit(c) || c.ToString() == _decimalSeparator);

            if (!isInputValid)
            {
                e.Handled = true;
                return;
            }

            if (e.Text.Contains(_decimalSeparator))
            {
                string? currentText = _textBox?.Text;
                if (!string.IsNullOrEmpty(currentText) && currentText.Contains(_decimalSeparator))
                {
                    e.Handled = true;
                }
            }
        }

        private bool IsDecimalSeparatorKey(Key key)
        {
            return _decimalSeparator switch
            {
                "," => key is Key.OemComma,
                "." => key is Key.OemPeriod or Key.Decimal,
                _ => false
            };
        }

        private string FormatString => AssociatedObject?.FormatString ?? "F0";
    }
}