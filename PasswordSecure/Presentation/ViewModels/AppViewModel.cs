using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using PasswordGenerator;

using PasswordSecure.Application;
using PasswordSecure.Application.Providers;
using PasswordSecure.Application.Services;
using PasswordSecure.DomainModel;
using PasswordSecure.DomainModel.CustomEventArgs;
using PasswordSecure.Presentation.Controls.MessageBoxControl;
using PasswordSecure.Presentation.Views;

namespace PasswordSecure.Presentation.ViewModels;

public class AppViewModel
{
    #region Static
    private static readonly DispatcherTimer _timer;
    private static TopLevel? topLevel;

    internal static GenerationSettings GenSettings => Settings.GenerationSettings;

    internal static int MinimumPasswordLength
    {
        get { return GenSettings.MinLength; }
    }

    internal static AppSettings Settings
    {
        get => field ?? new AppSettings();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    static AppViewModel()
    {
        EncryptedFileTypes = GetEncryptedFileTypes();
        _timer = new();
    }

    private static async void OnTimer_Tick(object? sender, System.EventArgs e)
    {
        _timer.Stop();
        await Dispatcher.UIThread
            .InvokeAsync(
                async () =>
                {
                    if (topLevel?.Clipboard is { } clipboard)
                    {
                        await clipboard.ClearAsync();
                    }
                });
    }

    public static async void Copy(string? secure)
    {
        if (secure == null)
            return;
        await Dispatcher.UIThread
            .InvokeAsync(
                async () =>
                {
                    if (topLevel?.Clipboard is { } clipboard)
                    {
                        await clipboard.SetTextAsync(secure);
                        _timer.Start();
                    }
                });
    }
    #endregion

    public AppViewModel(
        IDataAccessService dataAccessService,
        IAssemblyVersionProvider assemblyVersionProvider,
        IEncryptedDataFolderProvider encryptedDataFolderProvider,
        MainWindow mainWindow)
    {
        _dataAccessService = dataAccessService;
        _assemblyVersionProvider = assemblyVersionProvider;

        _mainWindow = mainWindow;
        topLevel = _mainWindow;

        _mainWindow.VisualStateChanged += OnVisualStateChanged;

        _mainWindow.NewMenuClicked += OnNewMenuClicked;
        _mainWindow.OpenMenuClicked += OnOpenMenuClicked;
        _mainWindow.SaveMenuClicked += OnSaveMenuClicked;
        _mainWindow.CloseMenuClicked += OnCloseMenuClicked;
        _mainWindow.ExitMenuClicked += OnExitMenuClicked;
        _mainWindow.WindowClosing += OnWindowClosing;
        _mainWindow.Opened += OnWindow_Opened;

        _mainWindow.HelpMenuClicked += OnHelpMenuClicked;

        _accessParams = new AccessParams();

        _encryptedDataFolderPath =
            encryptedDataFolderProvider.GetEncryptedDataFolderPath();
    }

    private void OnWindow_Opened(object? sender, EventArgs e) { RestoreState(); }

    internal async Task<bool> ReadSettings()
    {
        try
        {
            Settings = SettingsService.Load();
            _mainWindow.Width = Settings.WindowWidth;
            _mainWindow.Height = Settings.WindowHeight;
            _timer.Interval = TimeSpan.FromSeconds(Settings.TimeSafePassword);
            _timer.Tick += OnTimer_Tick;
            return true;
        }
        catch (Exception ex)
        {
            await MessageBoxManager.ShowDialogAsync(
                "Read settings error",
                ex.Message,
                MessageBoxType.Error,
                _mainWindow);
        }
        return false;
    }

    internal static async Task<SecureString?> GeneratePasswordAsync(Window owner, bool isFunction)
    {
        if (GenSettings is null)
        {
            return null;
        }
        PasswordGeneratorViewModel model = new(GenSettings);
        var passwordGeneratorView = new PasswordGeneratorView() { IsFunction = isFunction, DataContext = model };

        if (isFunction)
        {
            model.Generate();
        }

        SecureString? securePassword = await passwordGeneratorView.ShowDialog<SecureString?>(owner);

        if (securePassword is not null)
        {
            Settings.GenerationSettings = model.GetSettings();
            SettingsService.Save(Settings);
        }

        return securePassword;
    }

    private async void RestoreState()
    {
        if (System.IO.File.Exists(Settings.LastFile))
        {
            await LoadEncryptedContainer(Settings.LastFile);
        }
        _mainWindow.Width = Settings.WindowWidth;
        _mainWindow.Height = Settings.WindowHeight;
    }

    private static readonly IReadOnlyList<FilePickerFileType>
        EncryptedFileTypes;

    private readonly IDataAccessService _dataAccessService;
    private readonly IAssemblyVersionProvider _assemblyVersionProvider;

    internal readonly MainWindow _mainWindow;
    private readonly AccessParams _accessParams;

    private readonly string _encryptedDataFolderPath;

    private void OnVisualStateChanged(object? sender, EventArgs e) => _mainWindow.EnableControls();

    private async void OnNewMenuClicked(object? sender, AccountEntryCollectionEventArgs e)
    {
        bool shouldExitWithoutProcessing =
            await SuggestSaveChanges(e, MessageBoxType.WarningYesNoCancel);

        if (shouldExitWithoutProcessing)
        {
            return;
        }

        ResetData();

        try
        {
            await CreateEncryptedContainer();
        }
        catch (Exception ex)
        {
            ResetData();

            await DisplayErrorMessage(ex);
        }
        finally
        {
            _mainWindow.EnableControls();
        }
    }

    private async void OnOpenMenuClicked(object? sender, AccountEntryCollectionEventArgs e)
    {
        bool shouldExitWithoutProcessing = await SuggestSaveChanges(e, MessageBoxType.WarningYesNoCancel);

        if (shouldExitWithoutProcessing)
        {
            return;
        }

        ResetData();

        try
        {
            await Encrypted();
        }
        catch (Exception ex)
        {
            ResetData();

            await DisplayErrorMessage(ex);
        }
        finally
        {
            _mainWindow.EnableControls();
        }
    }

    private async void OnSaveMenuClicked(object? sender, AccountEntryCollectionEventArgs e)
    {
        try
        {
            await SaveEncryptedContainer(e.AccountEntryCollection);
        }
        catch (Exception ex)
        {
            await DisplayErrorMessage(ex);
        }
    }

    private async void OnCloseMenuClicked(object? sender, AccountEntryCollectionEventArgs e)
    {
        bool shouldExitWithoutProcessing =
            await SuggestSaveChanges(e, MessageBoxType.WarningYesNoCancel);

        if (shouldExitWithoutProcessing)
        {
            return;
        }

        ResetData();

        _mainWindow.EnableControls();
    }

    private async void OnExitMenuClicked(object? sender, AccountEntryCollectionEventArgs e)
    {
        bool shouldExitWithoutProcessing = await SuggestSaveChanges(e, MessageBoxType.WarningYesNoCancel);

        if (shouldExitWithoutProcessing)
        {
            return;
        }

        await _mainWindow.CloseWindow();
    }

    private async void OnWindowClosing(object? sender, AccountEntryCollectionEventArgs e)
    {
        await SuggestSaveChanges(e, MessageBoxType.WarningYesNo);
        await _mainWindow.CloseWindow();
    }

    private async void OnHelpMenuClicked(object? sender, EventArgs e) => await DisplayHelpMessage();

    private async Task<bool> SuggestSaveChanges(AccountEntryCollectionEventArgs e, MessageBoxType messageBoxType)
    {
        bool shouldExitWithoutProcessing = false;

        if (e.HasChanged)
        {
            MessageBoxResult buttonResult = await DisplayUnsavedChangesMessage(messageBoxType);

            if (buttonResult == MessageBoxResult.Yes)
            {
                await SaveEncryptedContainer(e.AccountEntryCollection);
            }
            else if (buttonResult == MessageBoxResult.Cancel)
            {
                shouldExitWithoutProcessing = true;
            }
        }

        return shouldExitWithoutProcessing;
    }

    private async Task CreateEncryptedContainer()
    {
        IStorageFolder? encryptedDataFolder = await GetEncryptedDataFolder();
        var encryptedFileCreateOptions = new FilePickerSaveOptions
        {
            DefaultExtension = ".encrypted",
            FileTypeChoices = EncryptedFileTypes,
            ShowOverwritePrompt = true,
            Title = "Select New Encrypted Container File",
            SuggestedStartLocation = encryptedDataFolder
        };

        IStorageFile? encryptedFile =
            await _mainWindow.StorageProvider.SaveFilePickerAsync(encryptedFileCreateOptions);

        if (encryptedFile is null || Settings is null)
        {
            return;
        }

        var createMasterPasswordWindow = new CreateMasterPasswordWindow();
        var createMasterPasswordViewModel = new CreateMasterPasswordViewModel(_accessParams);

        createMasterPasswordWindow.DataContext = createMasterPasswordViewModel;
        await createMasterPasswordWindow.ShowDialog(_mainWindow);

        if (_accessParams.Password is null)
        {
            return;
        }

        _accessParams.FilePath = encryptedFile.Path.LocalPath;
        _mainWindow.SetActiveFilePath(_accessParams.FilePath);
        Settings?.LastFile = _accessParams.FilePath;


        _accessParams.IsNewContainer = true;

        var accountEntryCollection = new AccountEntryCollection();
        await _dataAccessService.SaveAccountEntries(_accessParams, accountEntryCollection);

        _mainWindow.PopulateData(accountEntryCollection);
    }

    private async Task Encrypted()
    {
        IStorageFolder? encryptedDataFolder = await GetEncryptedDataFolder();
        var encryptedFileOpenOptions = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = EncryptedFileTypes,
            Title = "Select Encrypted Container File",
            SuggestedStartLocation = encryptedDataFolder
        };

        IReadOnlyList<IStorageFile> selectedEncryptedFiles =
            await _mainWindow.StorageProvider.OpenFilePickerAsync(encryptedFileOpenOptions);

        IStorageFile? encryptedFile = selectedEncryptedFiles.SingleOrDefault();

        if (encryptedFile is null)
        {
            return;
        }
        await LoadEncryptedContainer(encryptedFile.Path.LocalPath);
    }

    private async Task LoadEncryptedContainer(string? localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return;
        }

        var inputMasterPasswordWindow = new InputMasterPasswordWindow();
        var inputMasterPasswordViewModel =
            new InputMasterPasswordViewModel(_accessParams);

        inputMasterPasswordWindow.DataContext = inputMasterPasswordViewModel;
        await inputMasterPasswordWindow.ShowDialog(_mainWindow);

        if (_accessParams.Password is null)
        {
            return;
        }

        _accessParams.FilePath = localPath;
        _mainWindow.SetActiveFilePath(_accessParams.FilePath);
        Settings?.LastFile = _accessParams.FilePath;
        try
        {
            if (await _dataAccessService.ReadAccountEntries(_accessParams) is AccountEntryCollection accountEntryCollection)
            {
                _mainWindow.PopulateData(accountEntryCollection);
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.ShowDialogAsync("Decription error", ex.Message, MessageBoxType.Error, _mainWindow);
        }
    }

    private async Task SaveEncryptedContainer(AccountEntryCollection? accountEntryCollection)
    {
        if (_accessParams.FilePath is not null &&
            _accessParams.Password is not null &&
            accountEntryCollection is not null)
        {
            try
            {
                await _dataAccessService.SaveAccountEntries(_accessParams, accountEntryCollection);
                Settings?.WindowWidth = _mainWindow.Width;
                Settings?.WindowHeight = _mainWindow.Height;
                SettingsService.Save(Settings);
            }
            catch (Exception ex)
            {
                await MessageBoxManager.ShowDialogAsync("Save error", ex.Message, MessageBoxType.Error, _mainWindow);
            }
            _mainWindow.ResetHasChangedFlag();
        }
    }

    private async Task DisplayErrorMessage(Exception ex)
    { await MessageBoxManager.ShowDialogAsync("Error", ex.Message, MessageBoxType.Error, _mainWindow); }

    private async Task DisplayHelpMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            "About Password Secure",
            _assemblyVersionProvider.AssemblyVersionString,
            MessageBoxType.Info,
            _mainWindow);
    }

    private async Task<MessageBoxResult> DisplayUnsavedChangesMessage(MessageBoxType messageBoxType)
    {
        MessageBoxResult unsavedChangesMessageBoxResult =
            await MessageBoxManager.ShowDialogAsync(
            "Unsaved Changes",
            "There are unsaved changes. Would you like to save them?",
            messageBoxType,
            _mainWindow);

        return unsavedChangesMessageBoxResult;
    }

    private void ResetData()
    {
        _accessParams.Password = null;
        _accessParams.FilePath = null;

        _mainWindow.ClearData();
        Settings?.LastFile = string.Empty;
    }

    private async Task<IStorageFolder?> GetEncryptedDataFolder() => await _mainWindow.StorageProvider
        .TryGetFolderFromPathAsync(_encryptedDataFolderPath);

    private static List<FilePickerFileType> GetEncryptedFileTypes()
    {
        List<FilePickerFileType> encryptedFileTypes = [ new("Encrypted files (*.encrypted)")
        {
            Patterns = [ "*.encrypted" ]
        } ];
        return encryptedFileTypes;
    }
}

