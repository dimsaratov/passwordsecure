using PasswordGenerator;

namespace PasswordSecure.Presentation.Views;

public class CreateMasterPasswordWindow : EditPasswordWindow
{
    public CreateMasterPasswordWindow(GenerationSettings settings) : base(settings)
    {
        Title = "Create Master Password";
        TextBlockPassword.Text = "Master password";
        TextBlockConfirmPassword.Text = "Confirm master password";
    }
}
