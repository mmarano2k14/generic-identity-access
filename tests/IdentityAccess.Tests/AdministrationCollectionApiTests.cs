using IdentityAccess.Api.Controllers;
using IdentityAccess.Application.Administration;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Tests
{
    public sealed class AdministrationCollectionApiTests
    {
        [Fact]
        public void Core_administration_controllers_expose_bounded_collection_reads()
        {
            AssertListAction<UsersController>();
            AssertListAction<TenantsController>();
            AssertListAction<GroupsController>();
            AssertListAction<PoliciesController>();
            Assert.Equal(50, AdministrationPaging.DefaultLimit);
            Assert.Equal(200, AdministrationPaging.MaximumLimit);
        }

        private static void AssertListAction<TController>()
        {
            var method = typeof(TController).GetMethod("List");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).SingleOrDefault());
        }
    }
}
