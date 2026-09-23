namespace IdentityAccess.Application.Administration
{
    /// <summary>Defines bounded pagination rules for administration collection reads.</summary>
    public static class AdministrationPaging
    {
        /// <summary>Default number of records returned by one administration list request.</summary>
        public const int DefaultLimit = 50;

        /// <summary>Maximum number of records returned by one administration list request.</summary>
        public const int MaximumLimit = 200;

        /// <summary>Returns whether the requested offset and limit are valid.</summary>
        public static bool IsValid(int offset, int limit) => offset >= 0 && limit >= 1 && limit <= MaximumLimit;

        /// <summary>Rejects invalid list-window parameters before storage access.</summary>
        public static void EnsureValid(int offset, int limit)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (limit < 1 || limit > MaximumLimit) throw new ArgumentOutOfRangeException(nameof(limit));
        }
    }
}
