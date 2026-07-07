using System.Security;

using CommunityToolkit.Mvvm.ComponentModel;

using PasswordSecure.DomainModel;

namespace PasswordSecure.Presentation.ViewModels;

public class CreateMasterPasswordViewModel(AccessParams accessParams)
        : ObservableObject, IPasswordViewModel
{
    public SecureString? Password
    {
        get => accessParams.Password;
        set
        {
            SetProperty(
                accessParams.Password,
                value,
                accessParams,
                (accessParams, propertyValue)
                    => accessParams.Password = propertyValue);
        }
    }
}
