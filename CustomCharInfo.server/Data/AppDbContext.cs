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
        public DbSet<Article> Articles { get; set; }
        public DbSet<MovesetArticle> MovesetArticles { get; set; }
        public DbSet<Hook> Hooks { get; set; }
        public DbSet<MovesetHook> MovesetHooks { get; set; }
        public DbSet<HookableStatus> HookableStatuses { get; set; }
        public DbSet<VanillaChar> VanillaChars { get; set; }
        public DbSet<ReleaseState> ReleaseStates { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }

        // Users
        public DbSet<ApplicationUser> Users { get; set; }

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

            modelBuilder.Entity<MovesetArticle>()
                .HasKey(ma => new { ma.MovesetId, ma.ArticleId });

            modelBuilder.Entity<MovesetHook>()
                .HasKey(mh => new { mh.MovesetId, mh.HookId });

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
            
            // User Roles
            modelBuilder.Entity<UserType>()
                .HasMany(u => u.Users)
                .WithOne(u => u.UserType)
                .HasForeignKey(u => u.UserTypeId);
        }
    }
}
