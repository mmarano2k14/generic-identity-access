using System.Security.Cryptography;
using System.Text;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Computes a deterministic fingerprint over normalized application security-manifest semantics.</summary>
    public sealed class ApplicationSecurityManifestFingerprint
    {
        /// <summary>Computes the lowercase SHA-256 fingerprint.</summary>
        public string Compute(ApplicationSecurityManifest manifest)
        {
            ArgumentNullException.ThrowIfNull(manifest);

            var canonical = new StringBuilder();
            Append(canonical, "schema", manifest.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(canonical, "application", manifest.Application.Value);
            Append(canonical, "model", manifest.ModelVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Append(canonical, "project", manifest.RbacProject);

            foreach (var namespaceValue in manifest.RbacNamespaces)
            {
                Append(canonical, "namespace", namespaceValue);
            }

            foreach (var capability in manifest.Capabilities)
            {
                Append(canonical, "resource", capability.Key.Resource);
                Append(canonical, "feature", capability.Key.Feature);
                Append(canonical, "action", capability.Key.Action);
                Append(canonical, "display", capability.DisplayName);
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())))
                .ToLowerInvariant();
        }

        private static void Append(StringBuilder builder, string name, string value)
        {
            builder.Append(name.Length).Append(':').Append(name).Append('=')
                .Append(value.Length).Append(':').Append(value).Append('\n');
        }
    }
}
