using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace PasswordSecure.Behaviors
{
    public class SpecialCharsOnlyBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject?.TextChanged += OnTextChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject?.TextChanged -= OnTextChanged;
        }

        private void OnTextChanged([AllowNull] object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || textBox.Text is null)
                return;

            string filteredText = new(
                [ .. textBox
                    .Text
                    .Where(PasswordGenerator.GenerationSettings.BaseChars.Contains)
                    .Distinct() ]);

            if (textBox.Text != filteredText)
            {
                textBox.Text = filteredText;
                textBox.CaretIndex = filteredText.Length;
            }
        }
    }
}