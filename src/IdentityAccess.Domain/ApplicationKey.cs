namespace IdentityAccess.Domain;

/// <summary>An application identifier, not proof of trust or authorization.</summary>
public sealed record ApplicationKey
{
    public string Value { get; }

    public ApplicationKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 64 || value[0] is < 'a' or > 'z' ||
            value.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
            throw new ArgumentException("Application keys use 1 to 64 lowercase letters, digits or hyphens, starting with a letter.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
}
