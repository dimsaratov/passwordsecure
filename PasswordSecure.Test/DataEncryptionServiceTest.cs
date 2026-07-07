using System.Security.Cryptography;

using PasswordSecure.Application.Extensions;
using PasswordSecure.DomainModel;
using PasswordSecure.Infrastructure.Services;

using Xunit;

using static PasswordGenerator.Extenders;

namespace PasswordSecure.Test;

public class DataEncryptionServiceTest
{
    public DataEncryptionServiceTest()
    {
        _dataEncryptionService = new DataEncryptionService();
    }

    [Fact]
    public void EncryptDecrypt_ShortMatchingPassword_ReturnsInitialData()
    {
        // Arrange
        const string serializedDataReference = "plain text";
        byte[] dataReference = serializedDataReference.ToByteArray();

        const string password = "password";
        var secure = password.ToSecureString();
        // Act
        Vault vault = _dataEncryptionService.EncryptDataToVault(
            dataReference, secure);
        byte[] data = _dataEncryptionService.DecryptDataFromVault(vault, secure);
        string serializedData = data.ToText();

        // Assert
        Assert.Equal(serializedDataReference, serializedData);
    }

    [Fact]
    public void EncryptDecrypt_LongMatchingPassword_ReturnsInitialData()
    {
        // Arrange
        const string serializedDataReference = "plain text";
        byte[] dataReference = serializedDataReference.ToByteArray();

        const string password = "password_password_password_password";
        var secure = password.ToSecureString();

        // Act
        Vault vault = _dataEncryptionService.EncryptDataToVault(
            dataReference, secure);
        byte[] data = _dataEncryptionService.DecryptDataFromVault(vault, secure);
        string serializedData = data.ToText();

        // Assert
        Assert.Equal(serializedDataReference, serializedData);
    }

    [Fact]
    public void EncryptDecrypt_ShortNotMatchingPassword_ThrowsCryptographicException()
    {
        // Arrange
        const string serializedDataReference = "plain text";
        byte[] dataReference = serializedDataReference.ToByteArray();

        const string encryptionPassword = "encryption password";
        const string decryptionPassword = "decryption password";

        // Act and Assert
        Vault vault = _dataEncryptionService.EncryptDataToVault(
            dataReference, encryptionPassword.ToSecureString());

        Assert.Throws<CryptographicException>(
            () => _dataEncryptionService.DecryptDataFromVault(
                vault, decryptionPassword.ToSecureString())
        );
    }

    [Fact]
    public void EncryptDecrypt_LongNotMatchingPassword_ThrowsCryptographicException()
    {
        // Arrange
        const string serializedDataReference = "plain text";
        byte[] dataReference = serializedDataReference.ToByteArray();

        const string encryptionPassword =
            "encryption password_password_password_password";
        const string decryptionPassword =
            "decryption password_password_password_password";
        // Act and Assert
        Vault vault = _dataEncryptionService.EncryptDataToVault(
            dataReference, encryptionPassword.ToSecureString());

        Assert.Throws<CryptographicException>(
            () => _dataEncryptionService.DecryptDataFromVault(
                vault, decryptionPassword.ToSecureString())
        );
    }

    private readonly DataEncryptionService _dataEncryptionService;
}
