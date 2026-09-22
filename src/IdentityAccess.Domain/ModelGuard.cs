

namespace IdentityAccess.Domain
{

    internal static class ModelGuard
    {
        public static Guid Identifier(Guid value, string parameterName)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("An identifier cannot be empty.", parameterName);
            return value;
        }

        public static string DisplayName(string value, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            var normalized = value.Trim();
            if (normalized.Length > 200 || normalized.Any(char.IsControl))
                throw new ArgumentException("A display name must contain 1 to 200 printable characters.", parameterName);
            return normalized;
        }

        public static T DefinedEnum<T>(T value, string parameterName) where T : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(parameterName, "The status is not recognized.");
            return value;
        }
    }
}
