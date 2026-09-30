using CustomCharInfo.server.Helpers;
using CustomCharInfo.server.Models;
using Xunit;

namespace CustomCharInfo.server.Tests.Helpers
{
    public class RequesterExtensionsTests
    {
        [Theory]
        [InlineData(UserTypes.User, false, false, false)]
        [InlineData(UserTypes.Modder, true, false, false)]
        [InlineData(UserTypes.Admin, true, true, false)]
        [InlineData(UserTypes.SuperAdmin, true, true, true)]
        public void RoleHelpers_TreatSuperAdminAsAdmin(int userTypeId, bool isModder, bool isAdmin, bool isSuperAdmin)
        {
            var user = new ApplicationUser { Id = "u", UserTypeId = userTypeId };
            Assert.Equal(isModder, user.IsModder());
            Assert.Equal(isAdmin, user.IsAdmin());
            Assert.Equal(isSuperAdmin, user.IsSuperAdmin());

            var summary = new RequesterSummary("u", userTypeId, null);
            Assert.Equal(isAdmin, summary.IsAdmin);
            Assert.Equal(isSuperAdmin, summary.IsSuperAdmin);
        }

        [Fact]
        public void RoleHelpers_NullUser_IsNothing()
        {
            ApplicationUser? user = null;
            Assert.False(user.IsModder());
            Assert.False(user.IsAdmin());
            Assert.False(user.IsSuperAdmin());
        }
    }
}
