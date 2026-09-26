namespace IdentityAccess.Application.Administration
{
    /// <summary>Defines bounded search rules for server-side administration entity lookups.</summary>
    public static class AdministrationSearch
    {
        public const int MinimumLength = 3;
        public const int MaximumLength = 128;
        public const int MaximumResults = 20;

        /// <summary>Normalizes an optional search term and rejects undersized or oversized lookup terms.</summary>
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var trimmed = value.Trim();
            if (trimmed.Length < MinimumLength || trimmed.Length > MaximumLength)
                throw new ArgumentException(
                    $"Search terms must contain between {MinimumLength} and {MaximumLength} characters.",
                    nameof(value));

            return trimmed;
        }

        /// <summary>Restricts searched list operations to the autocomplete result bound.</summary>
        public static int Limit(string? search, int requestedLimit) =>
            search is null ? requestedLimit : Math.Min(requestedLimit, MaximumResults);
    }
}
