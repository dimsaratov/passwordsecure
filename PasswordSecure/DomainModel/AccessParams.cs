using System.Security;
using System.Text.Json.Serialization;

using PasswordSecure.Infrastructure.Services;

namespace PasswordSecure.DomainModel;

public record AccessParams : IPasswordEntry
{
    public bool IsNewContainer { get; set; }

    public string? FilePath { get; set; }

    [JsonConverter(typeof(SecureStringConverter))]
    public SecureString? Password { get; set; }

    public byte[]? Salt { get; set; }
}
