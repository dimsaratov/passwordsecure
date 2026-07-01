using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using PasswordSecure.Application;
using PasswordSecure.Application.Providers;
using PasswordSecure.Application.Services;
using PasswordSecure.DomainModel;
using PasswordSecure.DomainModel.CustomEventArgs;
using PasswordSecure.Presentation.Controls.MessageBoxControl;
using PasswordSecure.Presentation.ViewModels;
using PasswordSecure.Presentation.Views;

namespace PasswordSecure.Presentation;

public class MainPresenter
{
    static MainPresenter()
    {
        EncryptedFileTypes = GetEncryptedFileTypes();
    }

    private AppSettings? _settings;

    public MainPresenter(
        IDataAccessService dataAccessService,
        IAssemblyVersionProvider assemblyVersionProvider,
        IEncryptedDataFolderProvider encryptedDataFolderProvider,
        MainWindow mainWindow)
    {
        _dataAccessService = dataAccessService;
        _assemblyVersionProvider = assemblyVersionProvider;

        _mainWindow = mainWindow;

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

    private void OnWindow_Opened(object? sender, EventArgs e)
    {

        _settings = SettingsService.Load();
        RestoreState();
    }

    private async void RestoreState()
    {
        if (_settings is null)
        {
            return;
        }
        if (System.IO.File.Exists(_settings.LastFile))
        {
            await LoadEncryptedContainer(_settings.LastFile);
        }
        if (_settings.WindowWidth > 0)
        {
            _mainWindow.Width = _settings.WindowWidth;
        }
    }

    private static readonly IReadOnlyList<FilePickerFileType>
        EncryptedFileTypes;

    private const int MinimumMasterPasswordLength = 8;

    private readonly IDataAccessService _dataAccessService;
    private readonly IAssemblyVersionProvider _assemblyVersionProvider;

    private readonly MainWindow _mainWindow;
    private readonly AccessParams _accessParams;

    private readonly string _encryptedDataFolderPath;

    private void OnVisualStateChanged(object? sender, EventArgs e)
        => _mainWindow.EnableControls();

    private async void OnNewMenuClicked(
        object? sender, AccountEntryCollectionEventArgs e)
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

    private async void OnOpenMenuClicked(
        object? sender, AccountEntryCollectionEventArgs e)
    {
        bool shouldExitWithoutProcessing = await SuggestSaveChanges(
            e, MessageBoxType.WarningYesNoCancel);

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

    private async void OnSaveMenuClicked(
        object? sender, AccountEntryCollectionEventArgs e)
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

    private async void OnCloseMenuClicked(
        object? sender, AccountEntryCollectionEventArgs e)
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

    private async void OnExitMenuClicked(
        object? sender, AccountEntryCollectionEventArgs e)
    {
        bool shouldExitWithoutProcessing = await SuggestSaveChanges(
            e, MessageBoxType.WarningYesNoCancel);

        if (shouldExitWithoutProcessing)
        {
            return;
        }

        await _mainWindow.CloseWindow();
    }

    private async void OnWindowClosing(
        object? sender, AccountEntryCollectionEventArgs e)
    {
        await SuggestSaveChanges(e, MessageBoxType.WarningYesNo);
        await _mainWindow.CloseWindow();
    }

    private async void OnHelpMenuClicked(object? sender, EventArgs e)
        => await DisplayHelpMessage();

    private async Task<bool> SuggestSaveChanges(
        AccountEntryCollectionEventArgs e, MessageBoxType messageBoxType)
    {
        bool shouldExitWithoutProcessing = false;

        if (e.HasChanged)
        {
            MessageBoxResult buttonResult = await DisplayUnsavedChangesMessage(
                messageBoxType);

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
            await _mainWindow.StorageProvider.SaveFilePickerAsync(
                encryptedFileCreateOptions);

        if (encryptedFile is null)
        {
            return;
        }

        var createMasterPasswordWindow = new CreateMasterPasswordWindow
        {
            MinimumPasswordLength = MinimumMasterPasswordLength
        };
        var createMasterPasswordViewModel = new CreateMasterPasswordViewModel(
            _accessParams);

        createMasterPasswordWindow.DataContext = createMasterPasswordViewModel;
        await createMasterPasswordWindow.ShowDialog(_mainWindow);

        if (_accessParams.Password is null)
        {
            return;
        }

        _accessParams.FilePath = encryptedFile.Path.LocalPath;
        _mainWindow.SetActiveFilePath(_accessParams.FilePath);

        _accessParams.IsNewContainer = true;

        var accountEntryCollection = new AccountEntryCollection();
        await _dataAccessService.SaveAccountEntries(
            _accessParams, accountEntryCollection);

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
            await _mainWindow.StorageProvider.OpenFilePickerAsync(
                encryptedFileOpenOptions);

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
        _settings?.LastFile = _accessParams.FilePath;

        AccountEntryCollection accountEntryCollection =
            await _dataAccessService.ReadAccountEntries(_accessParams);

        _mainWindow.PopulateData(accountEntryCollection);
    }

    private async Task SaveEncryptedContainer(
        AccountEntryCollection? accountEntryCollection)
    {
        if (_accessParams.FilePath is not null &&
            _accessParams.Password is not null &&
            accountEntryCollection is not null)
        {
            await _dataAccessService.SaveAccountEntries(
                _accessParams, accountEntryCollection);

            _settings?.WindowWidth = _mainWindow.Width;
            SettingsService.Save(_settings);

            _mainWindow.ResetHasChangedFlag();
        }
    }

    private async Task DisplayErrorMessage(Exception ex)
    {
        await MessageBoxManager.ShowDialogAsync(
            "Error",
            ex.Message,
            MessageBoxType.Error,
            _mainWindow);
    }

    private async Task DisplayHelpMessage()
    {
        await MessageBoxManager.ShowDialogAsync(
            "About Password Secure",
            _assemblyVersionProvider.AssemblyVersionString,
            MessageBoxType.Info,
            _mainWindow);
    }

    private async Task<MessageBoxResult> DisplayUnsavedChangesMessage(
        MessageBoxType messageBoxType)
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
    }

    private async Task<IStorageFolder?> GetEncryptedDataFolder()
        => await _mainWindow.StorageProvider.TryGetFolderFromPathAsync(
            _encryptedDataFolderPath);

    private static List<FilePickerFileType> GetEncryptedFileTypes()
    {
        List<FilePickerFileType> encryptedFileTypes =
        [
            new("Encrypted files (*.encrypted)")
            {
                Patterns =
                [
                    "*.encrypted"
                ]
            }
        ];
        return encryptedFileTypes;
    }

    public static SecureString GetSecure(string pass)
    {
        if (string.IsNullOrEmpty(pass))
        {
            return new SecureString();
        }
        else
        {
            var secure = new SecureString();
            foreach (char c in pass)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }
    }
}

