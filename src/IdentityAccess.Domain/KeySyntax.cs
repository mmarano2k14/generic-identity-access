namespace IdentityAccess.Domain
{
    /// <summary>
    /// Provides shared validation and normalization mechanics for canonical lowercase
    /// identifier segments while leaving semantic meaning to the owning value object.
    /// </summary>
    internal static class KeySyntax
    {
        /// <summary>
        /// Validates a canonical lowercase slug without changing the supplied value.
        /// </summary>
        public static string RequireCanonicalLowercaseSlug(
            string value,
            string parameterName,
            int maximumLength,
            bool allowUnderscore,
            string errorMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            if (!IsLowercaseSlug(value, maximumLength, allowUnderscore))
            {
                throw new ArgumentException(errorMessage, parameterName);
            }

            return value;
        }

        /// <summary>
        /// Trims and lowercases a slug before applying the canonical lowercase grammar.
        /// </summary>
        public static string NormalizeLowercaseSlug(
            string value,
            string parameterName,
            int maximumLength,
            bool allowUnderscore,
            string errorMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            var normalized = value.Trim().ToLowerInvariant();
            if (!IsLowercaseSlug(normalized, maximumLength, allowUnderscore))
            {
                throw new ArgumentException(errorMessage, parameterName);
            }

            return normalized;
        }

        /// <summary>
        /// Trims and lowercases a slug, allowing a whole-segment wildcard when requested.
        /// </summary>
        public static string NormalizeLowercaseSlugOrWildcard(
            string value,
            string parameterName,
            int maximumLength,
            string errorMessage)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            var normalized = value.Trim().ToLowerInvariant();
            if (normalized == "*")
            {
                return normalized;
            }

            if (!IsLowercaseSlug(normalized, maximumLength, allowUnderscore: false))
            {
                throw new ArgumentException(errorMessage, parameterName);
            }

            return normalized;
        }

        private static bool IsLowercaseSlug(string value, int maximumLength, bool allowUnderscore)
        {
            if (value.Length == 0 ||
                value.Length > maximumLength ||
                value[0] is < 'a' or > 'z')
            {
                return false;
            }

            foreach (var character in value)
            {
                var valid = character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-';
                if (!valid && !(allowUnderscore && character == '_'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
