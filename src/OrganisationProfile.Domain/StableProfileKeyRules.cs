namespace OrganisationProfile.Domain
{
    /// <summary>Internal validation rules shared by stable profile/template/domain keys.</summary>
    internal static class StableProfileKeyRules
    {
        /// <summary>Checks lowercase slug syntax without introducing regex/runtime dependencies.</summary>
        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > 64 ||
                value[0] < 'a' ||
                value[0] > 'z')
            {
                return false;
            }

            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];

                var valid =
                    (character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '-';

                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
