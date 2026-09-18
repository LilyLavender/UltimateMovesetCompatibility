using Amazon.S3;
using Amazon.S3.Model;
using CustomCharInfo.server.Controllers;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Linq;
using Xunit;

namespace CustomCharInfo.server.Tests.Controllers
{
    public class ImageGarbageCollectionControllerTests : IDisposable
    {
        private const string PublicBaseUrl = "https://images.example.com";
        private const string Bucket = "test-bucket";

        private readonly TestDbContextFactory _db = new();
        private readonly Mock<IAmazonS3> _s3 = new();

        public ImageGarbageCollectionControllerTests()
        {
            SeedData.SeedLookups(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private ImageGarbageCollectionController CreateController(string? currentUserId = null)
        {
            var userManager = MockUserManagerFactory.Create(currentUserId, _db.Context.Users);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["R2:BucketName"] = Bucket,
                    ["R2:PublicBaseUrl"] = PublicBaseUrl,
                })
                .Build();

            var controller = new ImageGarbageCollectionController(_db.Context, userManager.Object, _s3.Object, config);
            controller.SetFakeUser();
            return controller;
        }

        private void SetupListObjects(params S3Object[] objects)
        {
            _s3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListObjectsV2Response
                {
                    S3Objects = objects.ToList(),
                    IsTruncated = false,
                });
        }

        private static S3Object Obj(string key, DateTime lastModified) => new()
        {
            Key = key,
            Size = 1234,
            LastModified = lastModified,
        };

        // Controller results are anonymous types, `internal` to the server assembly - `dynamic` member access fails across the assembly boundary, so read via reflection instead.
        private static T? Prop<T>(object item, string name)
        {
            var value = item.GetType().GetProperty(name)!.GetValue(item);
            return (T?)value;
        }

        [Fact]
        public async Task Scan_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController("user-1");

            var result = await controller.Scan();

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Scan_ReturnsAllImages_FlaggingInUseCorrectly()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);

            _db.Context.Movesets.Add(new Moveset
            {
                MovesetId = 1,
                ModdedCharName = "Test",
                VanillaCharInternalName = "mario",
                SlottedId = "slotone",
                ReleaseStateId = ReleaseStates.Released,
                ThumbhImageUrl = $"{PublicBaseUrl}/uploads/moveset-ui/referenced.png",
            });
            _db.Context.SaveChanges();

            SetupListObjects(
                Obj("uploads/moveset-ui/referenced.png", DateTime.UtcNow.AddDays(-10)),
                Obj("uploads/moveset-ui/recent-orphan.png", DateTime.UtcNow.AddHours(-1)),
                Obj("uploads/moveset-ui/old-orphan.png", DateTime.UtcNow.AddDays(-10))
            );

            var controller = CreateController("admin-1");

            var result = await controller.Scan();

            var ok = Assert.IsType<OkObjectResult>(result);
            var items = ((IEnumerable<object>)ok.Value!).ToList();
            Assert.Equal(3, items.Count);

            var referenced = items.Single(x => Prop<string>(x, "Key") == "uploads/moveset-ui/referenced.png");
            Assert.True(Prop<bool>(referenced, "InUse"));
            Assert.Equal($"{PublicBaseUrl}/uploads/moveset-ui/referenced.png", Prop<string>(referenced, "Url"));

            var recentOrphan = items.Single(x => Prop<string>(x, "Key") == "uploads/moveset-ui/recent-orphan.png");
            Assert.False(Prop<bool>(recentOrphan, "InUse"));

            var oldOrphan = items.Single(x => Prop<string>(x, "Key") == "uploads/moveset-ui/old-orphan.png");
            Assert.False(Prop<bool>(oldOrphan, "InUse"));
        }

        [Fact]
        public async Task Execute_NonAdmin_ReturnsForbid()
        {
            SeedData.AddUser(_db.Context, "user-1", userTypeId: UserTypes.User);
            var controller = CreateController("user-1");

            var result = await controller.Execute(new List<string> { "uploads/moveset-ui/old-orphan.png" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Execute_DeletesOnlyStillOrphanedKeys_SkipsNowReferencedKeys()
        {
            SeedData.AddUser(_db.Context, "admin-1", userTypeId: UserTypes.Admin);

            _db.Context.Movesets.Add(new Moveset
            {
                MovesetId = 1,
                ModdedCharName = "Test",
                VanillaCharInternalName = "mario",
                SlottedId = "slotone",
                ReleaseStateId = ReleaseStates.Released,
                ThumbhImageUrl = $"{PublicBaseUrl}/uploads/moveset-ui/now-referenced.png",
            });
            _db.Context.SaveChanges();

            // now-referenced.png was orphaned when the client scanned, but has since been attached to a moveset.
            // Execute must not delete it even though it's in the requested key list.
            SetupListObjects(
                Obj("uploads/moveset-ui/now-referenced.png", DateTime.UtcNow.AddDays(-10)),
                Obj("uploads/moveset-ui/old-orphan.png", DateTime.UtcNow.AddDays(-10))
            );

            _s3.Setup(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DeleteObjectsRequest req, CancellationToken _) => new DeleteObjectsResponse
                {
                    DeletedObjects = req.Objects.Select(o => new DeletedObject { Key = o.Key }).ToList(),
                    DeleteErrors = new List<DeleteError>(),
                });

            var controller = CreateController("admin-1");

            var result = await controller.Execute(new List<string>
            {
                "uploads/moveset-ui/now-referenced.png",
                "uploads/moveset-ui/old-orphan.png",
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = ok.Value!;
            var deleted = Prop<List<string>>(body, "deleted");
            var skipped = Prop<List<string>>(body, "skipped");

            Assert.Equal(new[] { "uploads/moveset-ui/old-orphan.png" }, deleted);
            Assert.Equal(new[] { "uploads/moveset-ui/now-referenced.png" }, skipped);

            _s3.Verify(s => s.DeleteObjectsAsync(
                It.Is<DeleteObjectsRequest>(r => r.Objects.Count == 1 && r.Objects[0].Key == "uploads/moveset-ui/old-orphan.png"),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
