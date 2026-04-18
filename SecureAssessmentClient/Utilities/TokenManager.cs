using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace SecureAssessmentClient.Utilities
{
    public static class TokenManager
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SecureAssessmentClient",
            "token.cfg"
        );

        public static void SaveToken(string token)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TokenFilePath));
            var encrypted = EncryptToken(token);
            File.WriteAllText(TokenFilePath, encrypted);
        }

        public static string GetToken()
        {
            if (!File.Exists(TokenFilePath))
                return null;

            var encrypted = File.ReadAllText(TokenFilePath);
            return DecryptToken(encrypted);
        }

        public static void ClearToken()
        {
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }

        private static string EncryptToken(string token)
        {
            var key = Encoding.UTF8.GetBytes("SecureAssessmentClientKey1234567");
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(Encoding.UTF8.GetBytes(token));
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private static string DecryptToken(string encryptedToken)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes("SecureAssessmentClientKey1234567");
                var buffer = Convert.FromBase64String(encryptedToken);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    var iv = new byte[aes.IV.Length];
                    Array.Copy(buffer, 0, iv, 0, iv.Length);
                    aes.IV = iv;
                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
