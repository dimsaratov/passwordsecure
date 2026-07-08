namespace PasswordSecure.Presentation.Views;

public class CreateMasterPasswordWindow : EditPasswordWindow
{
    public CreateMasterPasswordWindow() : base()
    {
        Title = "Create Master Password";
        TextBlockPassword.Text = "Master password";
        TextBlockConfirmPassword.Text = "Confirm master password";
    }
}
