using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Models;

namespace CustomCharInfo.server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Main data
        public DbSet<Moveset> Movesets { get; set; }
        public DbSet<Dependency> Dependencies { get; set; }
        public DbSet<MovesetDependency> MovesetDependencies { get; set; }
        public DbSet<Modder> Modders { get; set; }
        public DbSet<MovesetModder> MovesetModders { get; set; }
        public DbSet<MovesetEditor> MovesetEditors { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<MovesetArticle> MovesetArticles { get; set; }
        public DbSet<Hook> Hooks { get; set; }
        public DbSet<MovesetHook> MovesetHooks { get; set; }
        public DbSet<HookableStatus> HookableStatuses { get; set; }
        public DbSet<VanillaChar> VanillaChars { get; set; }
        public DbSet<ReleaseState> ReleaseStates { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BannerImage> BannerImages { get; set; }
        public DbSet<Plugin> Plugins { get; set; }
        public DbSet<PluginVersion> PluginVersions { get; set; }
        public DbSet<UnknownPluginHash> UnknownPluginHashes { get; set; }

        // Users
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserIpAddress> UserIpAddresses { get; set; }

        // Likes
        public DbSet<MovesetLike> MovesetLikes { get; set; }

        // Compatibility
        public DbSet<CompatibilityReport> CompatibilityReports { get; set; }

        // Admin
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<ItemType> ItemTypes { get; set; }
        public DbSet<AcceptanceState> AcceptanceStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUser");
            
            // Data
            modelBuilder.Entity<MovesetDependency>()
                .HasKey(md => new { md.MovesetId, md.DependencyId });

            modelBuilder.Entity<MovesetModder>()
                .HasKey(mm => new { mm.MovesetId, mm.ModderId });

            modelBuilder.Entity<MovesetEditor>()
                .HasKey(me => new { me.MovesetId, me.ModderId });

            modelBuilder.Entity<MovesetArticle>()
                .HasKey(ma => new { ma.MovesetId, ma.ArticleId });

            modelBuilder.Entity<MovesetHook>()
                .HasKey(mh => new { mh.MovesetId, mh.HookId });

            modelBuilder.Entity<MovesetLike>()
                .HasKey(ml => new { ml.MovesetId, ml.UserId });

            modelBuilder.Entity<Moveset>()
                .HasOne(m => m.VanillaChar)
                .WithMany()
                .HasForeignKey(m => m.VanillaCharInternalName);

            modelBuilder.Entity<Article>()
                .HasOne(a => a.VanillaChar)
                .WithMany()
                .HasForeignKey(a => a.VanillaCharInternalName);

            modelBuilder.Entity<Modder>()
                .HasOne(m => m.User)
                .WithOne(u => u.Modder)
                .HasForeignKey<Modder>(m => m.UserId)
                .IsRequired(false);

            modelBuilder.Entity<BlogPost>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId);

            // DB-level backstop for the client-side uniqueness pre-checks in HookForm.vue/SeriesForm.vue,
            // which are check-then-act and can't prevent a genuine race on their own.
            modelBuilder.Entity<Hook>()
                .HasIndex(h => h.Offset)
                .IsUnique();

            modelBuilder.Entity<Series>()
                .HasIndex(s => s.SeriesName)
                .IsUnique();

            // Action Logs
            modelBuilder.Entity<ActionLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId);

            modelBuilder.Entity<ActionLog>()
                .HasOne(al => al.ItemType)
                .WithMany(it => it.ActionLogs)
                .HasForeignKey(al => al.ItemTypeId);

            modelBuilder.Entity<ActionLog>()
                .HasOne(al => al.AcceptanceState)
                .WithMany(ass => ass.ActionLogs)
                .HasForeignKey(al => al.AcceptanceStateId);
            
            // Compatibility Reports
            modelBuilder.Entity<CompatibilityReport>()
                .HasOne<Moveset>()
                .WithMany()
                .HasForeignKey(cr => cr.MovesetId1)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompatibilityReport>()
                .HasOne<Moveset>()
                .WithMany()
                .HasForeignKey(cr => cr.MovesetId2)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompatibilityReport>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Backstop for the check-then-act dedup logic in CompatibilityController.SubmitReport
            modelBuilder.Entity<CompatibilityReport>()
                .HasIndex(cr => new { cr.MovesetId1, cr.MovesetId2, cr.UserId })
                .IsUnique();

            // Refresh tokens are looked up by Token on every refresh request
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.UserId);

            // Unique IPs per user, upserted on each hit rather than kept as full history.
            modelBuilder.Entity<UserIpAddress>()
                .HasIndex(uip => new { uip.UserId, uip.IpAddress })
                .IsUnique();

            modelBuilder.Entity<UserIpAddress>()
                .HasOne(uip => uip.User)
                .WithMany()
                .HasForeignKey(uip => uip.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ItemId is polymorphic (meaning depends on ItemTypeId), so it can't be a real FK,
            // but it's queried by value in every action-log lookup.
            modelBuilder.Entity<ActionLog>()
                .HasIndex(al => al.ItemId);

            // User Roles
            modelBuilder.Entity<UserType>()
                .HasMany(u => u.Users)
                .WithOne(u => u.UserType)
                .HasForeignKey(u => u.UserTypeId);

            // Plugins
            modelBuilder.Entity<Plugin>()
                .HasOne(p => p.Moveset)
                .WithMany()
                .HasForeignKey(p => p.MovesetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Plugin>()
                .HasOne(p => p.Dependency)
                .WithMany()
                .HasForeignKey(p => p.DependencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plugin>()
                .HasOne(p => p.OwnerModder)
                .WithMany()
                .HasForeignKey(p => p.OwnerModderId)
                .OnDelete(DeleteBehavior.Restrict);

            // A Plugin is attached to at most one of MovesetId/DependencyId (case 1 xor case 2);
            // neither set means "other" (case 3).
            modelBuilder.Entity<Plugin>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Plugin_SingleAttachment",
                    "(CASE WHEN \"MovesetId\" IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN \"DependencyId\" IS NOT NULL THEN 1 ELSE 0 END) <= 1"));

            modelBuilder.Entity<PluginVersion>()
                .HasOne(pv => pv.Plugin)
                .WithMany(p => p.PluginVersions)
                .HasForeignKey(pv => pv.PluginId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PluginVersion>()
                .HasOne(pv => pv.SubmittedByUser)
                .WithMany()
                .HasForeignKey(pv => pv.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Globally unique. One hash identifies exactly one build of one plugin,
            // and is the lookup key for the public identify endpoint.
            modelBuilder.Entity<PluginVersion>()
                .HasIndex(pv => pv.Hash)
                .IsUnique();

            modelBuilder.Entity<UnknownPluginHash>()
                .HasIndex(u => u.Hash)
                .IsUnique();

            // ItemTypeId 5, following the same numbering as the AddHookItemType migration (id 4).
            modelBuilder.Entity<ItemType>()
                .HasData(new ItemType { ItemTypeId = 5, ItemTypeName = "Plugin" });
        }
    }
}
