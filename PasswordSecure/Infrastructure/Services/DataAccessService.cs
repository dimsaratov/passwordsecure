using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using PasswordSecure.Application.Exceptions;
using PasswordSecure.Application.Extensions;
using PasswordSecure.Application.Providers;
using PasswordSecure.Application.Services;
using PasswordSecure.DomainModel;

namespace PasswordSecure.Infrastructure.Services;

public class DataAccessService : IDataAccessService
{
    public DataAccessService(
        IFileAccessProvider fileAccessProvider,
        IDataSerializationService dataSerializationService,
        IDataEncryptionService dataEncryptionService,
        IBackupService backupService)
    {
        _fileAccessProvider = fileAccessProvider;

        _dataSerializationService = dataSerializationService;
        _dataEncryptionService = dataEncryptionService;
        _backupService = backupService;
    }

    public async Task<AccountEntryCollection> ReadAccountEntries(
        AccessParams accessParams)
    {
        try
        {
            byte[] vaultAsBinary = _fileAccessProvider.ReadData(
                accessParams.FilePath!);
            string vaultAsText = Encoding.GetString(vaultAsBinary);

            Vault vault = _dataSerializationService.DeserializeVault(vaultAsText);
            accessParams.Salt = vault.Header.Salt;

            byte[] data = _dataEncryptionService.DecryptDataFromVault(
                vault, accessParams.Password!);
            string serializedData = data.ToText();

            AccountEntryCollection accountEntries =
                _dataSerializationService.DeserializeAccountEntryCollection(
                    serializedData);

            accessParams.IsNewContainer = false;

            return await Task.FromResult(accountEntries);
        }
        catch (CryptographicException ex_c)
        {
            throw new DataAccessException(ex_c.Message, ex_c);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(
                FileReadError(accessParams.FilePath!), ex);
        }
    }

    public async Task SaveAccountEntries(
        AccessParams accessParams,
        AccountEntryCollection accountEntryCollection)
    {
        try
        {
            if (!accessParams.IsNewContainer)
            {
                _backupService.BackupFile(accessParams.FilePath!);
            }

            string serializedData =
                _dataSerializationService.SerializeAccountEntryCollection(
                    accountEntryCollection);
            byte[] data = serializedData.ToByteArray();

            Vault vault = _dataEncryptionService.EncryptDataToVault(
                data, accessParams.Password!);

            string vaultAsText = _dataSerializationService.SerializeVault(vault);
            byte[] vaultAsBinary = Encoding.GetBytes(vaultAsText);

            _fileAccessProvider.SaveData(accessParams.FilePath!, vaultAsBinary);

            accessParams.IsNewContainer = false;

            await Task.CompletedTask;
        }
        catch (CryptographicException ex_c)
        {
            throw new DataAccessException(ex_c.Message, ex_c);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(
                FileSaveError(accessParams.FilePath!), ex);
        }
    }

    private static readonly Encoding Encoding = Encoding.UTF8;

    private readonly IFileAccessProvider _fileAccessProvider;

    private readonly IDataSerializationService _dataSerializationService;
    private readonly IDataEncryptionService _dataEncryptionService;
    private readonly IBackupService _backupService;

    private static string FileReadError(string filePath)
        => $@"Could not read data from file ""{filePath}"".";
    private static string FileSaveError(string filePath)
        => $@"Could not save data to file ""{filePath}"".";
}
