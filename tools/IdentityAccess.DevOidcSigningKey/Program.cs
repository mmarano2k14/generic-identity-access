using System.Security.Cryptography;
using System.Text;

namespace IdentityAccess.DevOidcSigningKey
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 2 || !int.TryParse(args[1], out var keySize) || keySize < 2048 || keySize > 16384)
            {
                Console.Error.WriteLine("Usage: IdentityAccess.DevOidcSigningKey <output-path> <key-size-bits>=2048..16384");
                return 2;
            }

            string outputPath;
            try
            {
                outputPath = Path.GetFullPath(args[0]);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                Console.Error.WriteLine("The signing-key output path is invalid.");
                return 3;
            }

            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var rsa = RSA.Create(keySize);
                if (rsa.KeySize < 2048)
                {
                    Console.Error.WriteLine("The generated RSA signing key is smaller than 2048 bits.");
                    return 4;
                }

                var pem = rsa.ExportPkcs8PrivateKeyPem();
                File.WriteAllText(outputPath, pem, new UTF8Encoding(false));
                Console.WriteLine(outputPath);
                return 0;
            }
            catch (Exception exception) when (exception is CryptographicException or IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Development OIDC signing-key generation failed.");
                return 5;
            }
        }
    }
}
