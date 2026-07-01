using System.Security;
using System.Text.Json.Serialization;

using PasswordSecure.Infrastructure.Services;

namespace PasswordSecure.DomainModel;

public record AccountEntry : IPasswordEntry
{
    public string Name { get; set; } = "New Entry";
    public string Url { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;

    [JsonConverter(typeof(SecureStringConverter))]
    public SecureString? Password { get; set; }

    public string Notes { get; set; } = string.Empty;
}
