using System;
using System.Runtime.InteropServices;
using System.Security;

namespace PasswordSecure.Infrastructure
{
    public static class Extenders
    {
        public static bool SecureStringEquals(this SecureString? a, SecureString? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            IntPtr ptrA = Marshal.SecureStringToBSTR(a);
            IntPtr ptrB = Marshal.SecureStringToBSTR(b);
            try
            {
                string strA = Marshal.PtrToStringBSTR(ptrA);
                string strB = Marshal.PtrToStringBSTR(ptrB);
                return strA == strB;
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptrA);
                Marshal.ZeroFreeBSTR(ptrB);
            }
        }

        public static string ToPasswordString(this SecureString secure)
        {
            if (secure is null || secure.Length == 0)
            {
                throw new ArgumentException("Secure string is null");
            }
            IntPtr ptr = Marshal.SecureStringToBSTR(secure);
            try
            {
                return Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
        }

        public static SecureString ToSecureString(this string password)
        {
            var secure = new SecureString();
            foreach (char c in password)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }
    }
}
