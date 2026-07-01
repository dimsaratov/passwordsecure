using System.Security;

using PasswordSecure.DomainModel;

namespace PasswordSecure.Application.Services;

public interface IDataEncryptionService
{
    Vault EncryptDataToVault(byte[] data, SecureString password);

    byte[] DecryptDataFromVault(Vault encryptedData, SecureString password);
}
