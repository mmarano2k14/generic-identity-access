using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Domain
{
    public sealed class OrganizationKeyTests
    {
        [Theory]
        [InlineData("urban-flower")]
        [InlineData("group1")]
        [InlineData("a")]
        public void Canonical_keys_are_accepted(string value)
        {
            Assert.Equal(value, new OrganizationKey(value).Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Urban-Flower")]
        [InlineData("1urban")]
        [InlineData("urban_flower")]
        public void Non_canonical_keys_are_rejected(string value)
        {
            Assert.ThrowsAny<ArgumentException>(() => new OrganizationKey(value));
        }
    }
}
