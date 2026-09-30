namespace OrganisationProfile.Domain
{
    /// <summary>Stable technical identity of one OrganisationProfile.</summary>
    public readonly record struct OrganisationProfileId
    {
        /// <summary>Gets the underlying UUID.</summary>
        public Guid Value { get; }

        /// <summary>Initializes a non-empty OrganisationProfile identifier.</summary>
        public OrganisationProfileId(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(
                    "OrganisationProfileId cannot be empty.",
                    nameof(value));
            }

            Value = value;
        }

        /// <summary>Creates a new random OrganisationProfile identifier.</summary>
        public static OrganisationProfileId New() => new(Guid.NewGuid());

        /// <inheritdoc />
        public override string ToString() => Value.ToString("D");
    }
}
