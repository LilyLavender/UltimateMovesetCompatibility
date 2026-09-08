using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class AccountControllerTests : IDisposable
    {
        private readonly TestDbContextFactory _db = new();

        public AccountControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private static IConfiguration FakeConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "test-signing-key-that-is-long-enough-1234567890" })
                .Build();

        private AccountController CreateController(Mock<UserManager<ApplicationUser>> userManager, string? currentUserId = null)
        {
            var signInManager = MockSignInManagerFactory.Create(userManager.Object);
            var controller = new AccountController(_db.Context, userManager.Object, signInManager, FakeConfig());
            if (currentUserId != null)
                controller.SetFakeUser(currentUserId);
            else
                controller.SetFakeUser();
            return controller;
        }

        [Fact]
        public async Task Register_Success_ReturnsOk()
        {
            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var controller = CreateController(userManager);

            var result = await controller.Register(new RegisterDto { Email = "new@user.com", Password = "Password1!" });

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Register_Failure_ReturnsBadRequestWithErrors()
        {
            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var controller = CreateController(userManager);

            var result = await controller.Register(new RegisterDto { Email = "new@user.com", Password = "weak" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_UnknownEmail_ReturnsUnauthorized()
        {
            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var controller = CreateController(userManager);

            var result = await controller.Login(new LoginDto { Email = "nobody@x.com", Password = "wrong" });

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            user.Email = "user@x.com";
            _db.Context.SaveChanges();

            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager.Setup(m => m.FindByEmailAsync("user@x.com")).ReturnsAsync(user);
            userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var controller = CreateController(userManager);

            var result = await controller.Login(new LoginDto { Email = "user@x.com", Password = "wrong" });

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_CorrectCredentials_ReturnsTokenAndPersistsRefreshToken()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            user.Email = "user@x.com";
            _db.Context.SaveChanges();

            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager.Setup(m => m.FindByEmailAsync("user@x.com")).ReturnsAsync(user);
            userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);

            var controller = CreateController(userManager);

            var result = await controller.Login(new LoginDto { Email = "user@x.com", Password = "correct" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Single(_db.Context.RefreshTokens);
        }

        [Fact]
        public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            user.Email = "user@x.com";
            _db.Context.RefreshTokens.Add(new RefreshToken
            {
                Token = "expired-token",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-100),
                ExpiresAt = DateTime.UtcNow.AddDays(-10),
                Revoked = false
            });
            _db.Context.SaveChanges();

            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            var controller = CreateController(userManager);

            var result = await controller.Refresh(new RefreshTokenRequestDto { RefreshToken = "expired-token" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ValidToken_RotatesAndRevokesOld()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            user.Email = "user@x.com";
            _db.Context.RefreshTokens.Add(new RefreshToken
            {
                Token = "valid-token",
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(10),
                Revoked = false
            });
            _db.Context.SaveChanges();

            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            var controller = CreateController(userManager);

            var result = await controller.Refresh(new RefreshTokenRequestDto { RefreshToken = "valid-token" });

            Assert.IsType<OkObjectResult>(result);
            var oldToken = _db.Context.RefreshTokens.Single(t => t.Token == "valid-token");
            Assert.True(oldToken.Revoked);
            Assert.Equal(2, _db.Context.RefreshTokens.Count());
        }

        [Fact]
        public async Task GetCurrentUser_NoAuthenticatedUser_ReturnsUnauthorized()
        {
            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

            var controller = CreateController(userManager);

            var result = await controller.GetCurrentUser();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetCurrentUser_LinkedModder_IncludesModderId()
        {
            var user = SeedData.AddUser(_db.Context, "user-1", userTypeId: 2);
            SeedData.AddModder(_db.Context, 5, user.Id, "SomeModder");

            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = CreateController(userManager);

            var result = await controller.GetCurrentUser();

            var ok = Assert.IsType<OkObjectResult>(result);
            var modderId = (int?)ok.Value!.GetType().GetProperty("ModderId")!.GetValue(ok.Value);
            Assert.Equal(5, modderId);
        }

        [Fact]
        public async Task EditUsername_EmptyUsername_ReturnsBadRequest()
        {
            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            var controller = CreateController(userManager);

            var result = await controller.EditUsername(new EditUsernameDto { NewUsername = "  " });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GeneratePasswordReset_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: 1);
            var userManager = MockUserManagerFactory.Create("user-1", _db.Context.Users);

            var controller = CreateController(userManager, "user-1");

            var result = await controller.GeneratePasswordReset(new ForgotPasswordDto { UserId = "user-1" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GeneratePasswordReset_Admin_TargetNotFound_ReturnsNotFound()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: 3);
            var userManager = MockUserManagerFactory.Create("admin-1", _db.Context.Users);
            userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

            var controller = CreateController(userManager, "admin-1");

            var result = await controller.GeneratePasswordReset(new ForgotPasswordDto { UserId = "missing" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ResetPassword_InvalidUser_ReturnsBadRequest()
        {
            var userManager = MockUserManagerFactory.Create(users: _db.Context.Users);
            userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var controller = CreateController(userManager);

            var result = await controller.ResetPassword(new ResetPasswordDto { UserId = "nobody", Token = "t", NewPassword = "NewPass1!" });

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
