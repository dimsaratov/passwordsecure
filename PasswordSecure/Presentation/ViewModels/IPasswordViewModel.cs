using System.Security;

namespace PasswordSecure.Presentation.ViewModels;

public interface IPasswordViewModel
{
    SecureString? Password { get; set; }
}
