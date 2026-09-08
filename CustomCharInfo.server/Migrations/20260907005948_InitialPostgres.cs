using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcceptanceStates",
                columns: table => new
                {
                    AcceptanceStateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AcceptanceStateName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceStates", x => x.AcceptanceStateId);
                });

            migrationBuilder.CreateTable(
                name: "Dependencies",
                columns: table => new
                {
                    DependencyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DownloadLink = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencies", x => x.DependencyId);
                });

            migrationBuilder.CreateTable(
                name: "HookableStatus",
                columns: table => new
                {
                    HookableStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HookableStatus", x => x.HookableStatusId);
                });

            migrationBuilder.CreateTable(
                name: "ItemTypes",
                columns: table => new
                {
                    ItemTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemTypeName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypes", x => x.ItemTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseStates",
                columns: table => new
                {
                    ReleaseStateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReleaseStateName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseStates", x => x.ReleaseStateId);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    SeriesId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeriesName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SeriesIconUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.SeriesId);
                });

            migrationBuilder.CreateTable(
                name: "UserType",
                columns: table => new
                {
                    UserTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserTypeName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserType", x => x.UserTypeId);
                });

            migrationBuilder.CreateTable(
                name: "VanillaChars",
                columns: table => new
                {
                    VanillaCharInternalName = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VanillaChars", x => x.VanillaCharInternalName);
                });

            migrationBuilder.CreateTable(
                name: "Hooks",
                columns: table => new
                {
                    HookId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Offset = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    HookableStatusId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hooks", x => x.HookId);
                    table.ForeignKey(
                        name: "FK_Hooks_HookableStatus_HookableStatusId",
                        column: x => x.HookableStatusId,
                        principalTable: "HookableStatus",
                        principalColumn: "HookableStatusId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ModderId = table.Column<int>(type: "integer", nullable: true),
                    UserTypeId = table.Column<int>(type: "integer", nullable: false),
                    Problematic = table.Column<bool>(type: "boolean", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUser_UserType_UserTypeId",
                        column: x => x.UserTypeId,
                        principalTable: "UserType",
                        principalColumn: "UserTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    ArticleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VanillaCharInternalName = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    ArticleName = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.ArticleId);
                    table.ForeignKey(
                        name: "FK_Articles_VanillaChars_VanillaCharInternalName",
                        column: x => x.VanillaCharInternalName,
                        principalTable: "VanillaChars",
                        principalColumn: "VanillaCharInternalName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Movesets",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModdedCharName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VanillaCharInternalName = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SeriesId = table.Column<int>(type: "integer", nullable: true),
                    SlottedId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReplacementId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SlotsStart = table.Column<int>(type: "integer", nullable: true),
                    SlotsEnd = table.Column<int>(type: "integer", nullable: true),
                    ReleaseStateId = table.Column<int>(type: "integer", nullable: true),
                    HasGlobalOpff = table.Column<bool>(type: "boolean", nullable: true),
                    HasCharacterOpff = table.Column<bool>(type: "boolean", nullable: true),
                    HasAgentInit = table.Column<bool>(type: "boolean", nullable: true),
                    HasGlobalOnLinePre = table.Column<bool>(type: "boolean", nullable: true),
                    HasGlobalOnLineEnd = table.Column<bool>(type: "boolean", nullable: true),
                    ModPageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    GamebananaWipId = table.Column<int>(type: "integer", nullable: true),
                    BackgroundColor = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    ModsWikiLink = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModpackName = table.Column<string>(type: "text", nullable: true),
                    SourceCode = table.Column<string>(type: "text", nullable: true),
                    AdminPick = table.Column<bool>(type: "boolean", nullable: true),
                    PrivateMoveset = table.Column<bool>(type: "boolean", nullable: true),
                    PrivateModder = table.Column<bool>(type: "boolean", nullable: true),
                    IsJokeMoveset = table.Column<bool>(type: "boolean", nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ThumbhImageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MovesetHeroImageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movesets", x => x.MovesetId);
                    table.ForeignKey(
                        name: "FK_Movesets_ReleaseStates_ReleaseStateId",
                        column: x => x.ReleaseStateId,
                        principalTable: "ReleaseStates",
                        principalColumn: "ReleaseStateId");
                    table.ForeignKey(
                        name: "FK_Movesets_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "SeriesId");
                    table.ForeignKey(
                        name: "FK_Movesets_VanillaChars_VanillaCharInternalName",
                        column: x => x.VanillaCharInternalName,
                        principalTable: "VanillaChars",
                        principalColumn: "VanillaCharInternalName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    ActionLogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ItemTypeId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    AcceptanceStateId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    Diff = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionLogs", x => x.ActionLogId);
                    table.ForeignKey(
                        name: "FK_ActionLogs_AcceptanceStates_AcceptanceStateId",
                        column: x => x.AcceptanceStateId,
                        principalTable: "AcceptanceStates",
                        principalColumn: "AcceptanceStateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionLogs_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionLogs_ItemTypes_ItemTypeId",
                        column: x => x.ItemTypeId,
                        principalTable: "ItemTypes",
                        principalColumn: "ItemTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlogPost",
                columns: table => new
                {
                    BlogPostId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    BlogTitle = table.Column<string>(type: "text", nullable: false),
                    BlogText = table.Column<string>(type: "text", nullable: false),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BlogImageUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPost", x => x.BlogPostId);
                    table.ForeignKey(
                        name: "FK_BlogPost_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modders",
                columns: table => new
                {
                    ModderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    GamebananaId = table.Column<int>(type: "integer", nullable: true),
                    DiscordUsername = table.Column<string>(type: "text", nullable: true),
                    PfpUrl = table.Column<string>(type: "text", nullable: true),
                    TwitterUsername = table.Column<string>(type: "text", nullable: true),
                    BlueskyHandle = table.Column<string>(type: "text", nullable: true),
                    GithubUsername = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modders", x => x.ModderId);
                    table.ForeignKey(
                        name: "FK_Modders_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompatibilityReports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MovesetId1 = table.Column<int>(type: "integer", nullable: false),
                    MovesetId2 = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    IsCompatible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompatibilityReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_CompatibilityReports_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompatibilityReports_Movesets_MovesetId1",
                        column: x => x.MovesetId1,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompatibilityReports_Movesets_MovesetId2",
                        column: x => x.MovesetId2,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovesetArticles",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    ArticleId = table.Column<int>(type: "integer", nullable: false),
                    ModdedName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetArticles", x => new { x.MovesetId, x.ArticleId });
                    table.ForeignKey(
                        name: "FK_MovesetArticles_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ArticleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovesetArticles_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovesetDependencies",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    DependencyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetDependencies", x => new { x.MovesetId, x.DependencyId });
                    table.ForeignKey(
                        name: "FK_MovesetDependencies_Dependencies_DependencyId",
                        column: x => x.DependencyId,
                        principalTable: "Dependencies",
                        principalColumn: "DependencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovesetDependencies_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovesetHooks",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    HookId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetHooks", x => new { x.MovesetId, x.HookId });
                    table.ForeignKey(
                        name: "FK_MovesetHooks_Hooks_HookId",
                        column: x => x.HookId,
                        principalTable: "Hooks",
                        principalColumn: "HookId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovesetHooks_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovesetLikes",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetLikes", x => new { x.MovesetId, x.UserId });
                    table.ForeignKey(
                        name: "FK_MovesetLikes_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovesetLikes_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovesetModders",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    ModderId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetModders", x => new { x.MovesetId, x.ModderId });
                    table.ForeignKey(
                        name: "FK_MovesetModders_Modders_ModderId",
                        column: x => x.ModderId,
                        principalTable: "Modders",
                        principalColumn: "ModderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovesetModders_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_AcceptanceStateId",
                table: "ActionLogs",
                column: "AcceptanceStateId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ItemTypeId",
                table: "ActionLogs",
                column: "ItemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId",
                table: "ActionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_UserTypeId",
                table: "ApplicationUser",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_VanillaCharInternalName",
                table: "Articles",
                column: "VanillaCharInternalName");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPost_UserId",
                table: "BlogPost",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityReports_MovesetId1",
                table: "CompatibilityReports",
                column: "MovesetId1");

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityReports_MovesetId2",
                table: "CompatibilityReports",
                column: "MovesetId2");

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityReports_UserId",
                table: "CompatibilityReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hooks_HookableStatusId",
                table: "Hooks",
                column: "HookableStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Modders_UserId",
                table: "Modders",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovesetArticles_ArticleId",
                table: "MovesetArticles",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_MovesetDependencies_DependencyId",
                table: "MovesetDependencies",
                column: "DependencyId");

            migrationBuilder.CreateIndex(
                name: "IX_MovesetHooks_HookId",
                table: "MovesetHooks",
                column: "HookId");

            migrationBuilder.CreateIndex(
                name: "IX_MovesetLikes_UserId",
                table: "MovesetLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MovesetModders_ModderId",
                table: "MovesetModders",
                column: "ModderId");

            migrationBuilder.CreateIndex(
                name: "IX_Movesets_ReleaseStateId",
                table: "Movesets",
                column: "ReleaseStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Movesets_SeriesId",
                table: "Movesets",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Movesets_VanillaCharInternalName",
                table: "Movesets",
                column: "VanillaCharInternalName");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "BlogPost");

            migrationBuilder.DropTable(
                name: "CompatibilityReports");

            migrationBuilder.DropTable(
                name: "MovesetArticles");

            migrationBuilder.DropTable(
                name: "MovesetDependencies");

            migrationBuilder.DropTable(
                name: "MovesetHooks");

            migrationBuilder.DropTable(
                name: "MovesetLikes");

            migrationBuilder.DropTable(
                name: "MovesetModders");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "AcceptanceStates");

            migrationBuilder.DropTable(
                name: "ItemTypes");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Dependencies");

            migrationBuilder.DropTable(
                name: "Hooks");

            migrationBuilder.DropTable(
                name: "Modders");

            migrationBuilder.DropTable(
                name: "Movesets");

            migrationBuilder.DropTable(
                name: "HookableStatus");

            migrationBuilder.DropTable(
                name: "ApplicationUser");

            migrationBuilder.DropTable(
                name: "ReleaseStates");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "VanillaChars");

            migrationBuilder.DropTable(
                name: "UserType");
        }
    }
}
