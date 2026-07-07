using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using PasswordGenerator;

namespace PasswordSecure.Presentation.ViewModels;

public partial class PasswordGeneratorViewModel : ObservableObject, IDisposable
{
    private readonly GenerationSettings settings;
    private bool disposed;

    public PasswordGeneratorViewModel(GenerationSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Подписываемся на ошибки валидации
        this.settings.ErrorsChanged += OnSettingsErrorsChanged;

        // Подписываемся на изменения свойств
        this.settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Пробрасываем изменения свойств из settings в ViewModel
        switch (e.PropertyName)
        {
            case nameof(settings.MinLength):
                OnPropertyChanged(nameof(MinLength));
                break;
            case nameof(settings.MaxLength):
                OnPropertyChanged(nameof(MaxLength));
                break;
            case nameof(settings.UseUppercase):
                OnPropertyChanged(nameof(UseUppercase));
                break;
            case nameof(settings.UseLowercase):
                OnPropertyChanged(nameof(UseLowercase));
                break;
            case nameof(settings.UseDigits):
                OnPropertyChanged(nameof(UseDigits));
                break;
            case nameof(settings.UseSymbols):
                OnPropertyChanged(nameof(UseSymbols));
                break;
            case nameof(settings.Symbols):
                OnPropertyChanged(nameof(Symbols));
                UpdateMessageSpecialChars();
                break;
            case nameof(settings.SpecialChars):
                OnPropertyChanged(nameof(SpecialChars));
                break;
        }
    }

    private void OnSettingsErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        // Обновляем сообщение об ошибке при изменении ошибок валидации
        UpdateMessageSpecialChars();
    }

    private void UpdateMessageSpecialChars()
    {
        // Получаем ошибки для свойства Symbols
        IEnumerable errors = settings.GetErrors(nameof(settings.Symbols));
        List<string> errorList = errors?.Cast<string>().ToList() ?? [];

        if (errorList.Count > 0)
        {
            MessageSpecialChars = $"*{string.Join("; ", errorList)}";
        }
        else
        {
            MessageSpecialChars = string.Empty;
        }
    }

    public string Password
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = string.Empty;

    public bool UseUppercase { get => settings.UseUppercase; set => settings.UseUppercase = value; }

    public bool UseLowercase { get => settings.UseLowercase; set => settings.UseLowercase = value; }

    public bool UseDigits { get => settings.UseDigits; set => settings.UseDigits = value; }

    public bool UseSymbols { get => settings.UseSymbols; set => settings.UseSymbols = value; }

    public int MinLength { get => settings.MinLength; set => settings.MinLength = value; }

    public int MaxLength { get => settings.MaxLength; set => settings.MaxLength = value; }

    public string Symbols { get => settings.Symbols; set => settings.Symbols = value; }

    public char[] SpecialChars { get => settings.SpecialChars; set => settings.SpecialChars = value; }

    private string _messageSpecialChars = string.Empty;

    public string MessageSpecialChars
    {
        get => _messageSpecialChars;
        private set
        {
            if (_messageSpecialChars != value)
            {
                _messageSpecialChars = value;
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    private void Generate()
    {
        if (settings.IsCorrect)
        {
            Password = Generator.Generate(settings);
        }
    }

    [RelayCommand]
    private void ResetSymbols()
    {
        settings.DefaultSpecialChars();
        // MessageSpecialChars обновится через события ErrorsChanged
    }

    public SecureString GetNewPassword() => Generator.GenerateAsSecure(settings);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed && disposing)
        {
            settings.ErrorsChanged -= OnSettingsErrorsChanged;
            settings.PropertyChanged -= OnSettingsPropertyChanged;
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}