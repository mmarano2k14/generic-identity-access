using IdentityAccess.Application.Administration;

namespace IdentityAccess.Tests
{
    public sealed class AdministrationPagingTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, AdministrationPaging.DefaultLimit)]
        [InlineData(100, AdministrationPaging.MaximumLimit)]
        public void Valid_windows_are_accepted(int offset, int limit)
        {
            Assert.True(AdministrationPaging.IsValid(offset, limit));
            AdministrationPaging.EnsureValid(offset, limit);
        }

        [Theory]
        [InlineData(-1, 50)]
        [InlineData(0, 0)]
        [InlineData(0, AdministrationPaging.MaximumLimit + 1)]
        public void Invalid_windows_are_rejected(int offset, int limit)
        {
            Assert.False(AdministrationPaging.IsValid(offset, limit));
            Assert.Throws<ArgumentOutOfRangeException>(() => AdministrationPaging.EnsureValid(offset, limit));
        }
    }
}
