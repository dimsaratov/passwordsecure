using System.Security;

namespace PasswordSecure.DomainModel;

public interface IPasswordEntry
{
    SecureString? Password { get; set; }
}
