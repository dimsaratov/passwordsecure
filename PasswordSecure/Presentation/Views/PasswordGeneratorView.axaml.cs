using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;

using PasswordGenerator;

using PasswordSecure.Presentation.ViewModels;
namespace PasswordSecure.Presentation.Views
{

    public partial class PasswordGeneratorView : Window
    {
        private readonly DispatcherTimer _timer = new()
        {
            Interval = TimeSpan.FromSeconds(30)
        };

        public PasswordGeneratorView()
        {
            InitializeComponent();
            _timer.Tick += OnTimer_Tick;
        }

        public bool IsFunction
        {
            get => OkButton.IsVisible;
            set => OkButton.IsVisible = value;
        }

        private async void OnTimer_Tick(object? sender, System.EventArgs e)
        {
            _timer.Stop();
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard is { } clipboard)
                {
                    await clipboard.ClearAsync();
                }
            });
        }

        private async void OnCopyButtonClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PasswordGeneratorViewModel viewModel)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel?.Clipboard is { } clipboard)
                    {
                        await clipboard.SetTextAsync(viewModel.Password);
                        _timer.Start();
                    }
                });
            }
        }

        private void OnCheckedSpecChars(object sender, RoutedEventArgs e)
        {
            SetEnableSpecialChars(true);
        }
        private void OnUncheckedSpecChars(object sender, RoutedEventArgs e)
        {
            SetEnableSpecialChars(false);
        }

        private void SetEnableSpecialChars(bool enable)
        {
            SpecialChars.IsEnabled = enable;
            ResetSpecCharsButtons.IsEnabled = enable;
        }

        protected override void OnOpened(EventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            IFocusManager? focusManager = topLevel?.FocusManager;
            focusManager?.Focus(null);
            ButtonGenerate.Focus();
        }

        private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PasswordGeneratorViewModel viewModel)
            {
                viewModel.Password = null;
            }
            Close(null);
        }

        private void OnOkButtonClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PasswordGeneratorViewModel viewModel)
            {
                Close(viewModel.Password?.ToSecureString());
            }
            Close(null);
        }
    }
}