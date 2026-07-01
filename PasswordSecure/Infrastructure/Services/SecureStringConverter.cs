using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PasswordSecure.Infrastructure.Services
{
    public class SecureStringConverter : JsonConverter<SecureString>
    {
        public override SecureString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return new SecureString();

            var secure = new SecureString();
            foreach (char c in value)
                secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }

        public override void Write(Utf8JsonWriter writer, SecureString value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            IntPtr ptr = Marshal.SecureStringToGlobalAllocUnicode(value);
            try
            {
                string? str = Marshal.PtrToStringUni(ptr);
                writer.WriteStringValue(str);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}