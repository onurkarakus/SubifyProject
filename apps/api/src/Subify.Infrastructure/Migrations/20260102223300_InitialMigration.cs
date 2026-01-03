using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Subify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarketingOptIn = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRateSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BaseCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TargetCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "exchangerate-api. com"),
                    FetchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "TRY"),
                    Price = table.Column<float>(type: "real", nullable: true),
                    PriceBefore = table.Column<float>(type: "real", nullable: true),
                    BillingCycle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Monthly"),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Global"),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastVerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PageName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiSuggestionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "gpt-4o-mini"),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSuggestionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiSuggestionLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "revenuecat"),
                    SessionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntitlementCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Entitlement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProductId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Store = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsTrial = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    WillRenew = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntitlementCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntitlementCaches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false, defaultValue: "tr"),
                    ApplicationThemeColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Royal Purple"),
                    DarkTheme = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MainCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "TRY"),
                    MonthlyBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Plan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Free"),
                    PlanRenewsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PushTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCategories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PushEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DaysBeforeRenewal = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSettings_Profiles_Id",
                        column: x => x.Id,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "TRY"),
                    BillingCycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NextRenewalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastUsedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    Archived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SharedWithCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Subscriptions_UserCategories_UserCategoryId",
                        column: x => x.UserCategoryId,
                        principalTable: "UserCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Sent"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPaymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "TRY"),
                    PaymentDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Paid"),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderSessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRefunded = table.Column<bool>(type: "bit", nullable: false),
                    RefundedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPaymentRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubscriptionPaymentRecords_BillingSessions_BillingSessionId",
                        column: x => x.BillingSessionId,
                        principalTable: "BillingSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubscriptionPaymentRecords_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "CreatedAt", "DeletedAt", "Icon", "IsActive", "IsDefault", "Slug", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "#E50914", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9753), new TimeSpan(0, 0, 0, 0, 0)), null, "play-circle", true, false, "streaming", 1, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9754), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "#1DB954", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9758), new TimeSpan(0, 0, 0, 0, 0)), null, "music", true, false, "music", 2, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9758), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "#0078D4", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9761), new TimeSpan(0, 0, 0, 0, 0)), null, "briefcase", true, false, "productivity", 3, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9762), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "#9147FF", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9765), new TimeSpan(0, 0, 0, 0, 0)), null, "gamepad-2", true, false, "gaming", 4, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9765), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "#0066FF", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9768), new TimeSpan(0, 0, 0, 0, 0)), null, "cloud", true, false, "cloud-storage", 5, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9769), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "#FF6B00", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9772), new TimeSpan(0, 0, 0, 0, 0)), null, "newspaper", true, false, "news-magazines", 6, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9772), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "#FF2D55", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9775), new TimeSpan(0, 0, 0, 0, 0)), null, "heart-pulse", true, false, "fitness-health", 7, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9776), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "#5856D6", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9779), new TimeSpan(0, 0, 0, 0, 0)), null, "graduation-cap", true, false, "education", 8, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9779), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "#8E8E93", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9782), new TimeSpan(0, 0, 0, 0, 0)), null, "wrench", true, false, "utilities", 9, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9783), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("10000000-0000-0000-0000-00000000000a"), "#636366", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9786), new TimeSpan(0, 0, 0, 0, 0)), null, "folder", true, false, "other", 99, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 625, DateTimeKind.Unspecified).AddTicks(9786), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "EmailTemplates",
                columns: new[] { "Id", "Body", "CreatedAt", "LanguageCode", "Name", "Subject", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2121a1c3-f76b-4d07-8f00-e0622ad9a5c6"), "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\r\n  <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />\r\n  <title>Subify Email Doğrulama</title>\r\n  <style>\r\n    /* Genel Resetler */\r\n    body {\r\n      background-color: #f4f4f7;\r\n      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;\r\n      -webkit-font-smoothing: antialiased;\r\n      font-size: 14px;\r\n      line-height: 1.4;\r\n      margin: 0;\r\n      padding: 0;\r\n      -ms-text-size-adjust: 100%;\r\n      -webkit-text-size-adjust: 100%;\r\n    }\r\n\r\n    table {\r\n      border-collapse: separate;\r\n      mso-table-lspace: 0pt;\r\n      mso-table-rspace: 0pt;\r\n      width: 100%;\r\n    }\r\n\r\n    /* Ana Konteyner */\r\n    .container {\r\n      display: block;\r\n      margin: 0 auto !important;\r\n      max-width: 580px;\r\n      padding: 10px;\r\n      width: 580px;\r\n    }\r\n\r\n    /* İçerik Kutusu */\r\n    .content {\r\n      box-sizing: border-box;\r\n      display: block;\r\n      margin: 0 auto;\r\n      max-width: 580px;\r\n      padding: 10px;\r\n    }\r\n\r\n    /* Beyaz Kart Tasarımı */\r\n    .main {\r\n      background: #ffffff;\r\n      border-radius: 8px;\r\n      width: 100%;\r\n      box-shadow: 0 4px 10px rgba(0, 0, 0, 0.03);\r\n    }\r\n\r\n    .wrapper {\r\n      box-sizing: border-box;\r\n      padding: 40px;\r\n    }\r\n\r\n    /* Tipografi */\r\n    h1 {\r\n      font-size: 24px;\r\n      font-weight: 700;\r\n      margin: 0;\r\n      margin-bottom: 20px;\r\n      color: #1a1a1a;\r\n      text-align: center;\r\n    }\r\n\r\n    p {\r\n      font-size: 16px;\r\n      font-weight: normal;\r\n      margin: 0;\r\n      margin-bottom: 20px;\r\n      color: #555555;\r\n      line-height: 1.6;\r\n      text-align: center;\r\n    }\r\n\r\n    /* Buton Tasarımı - Subify için Mor Tonları */\r\n    .btn {\r\n      box-sizing: border-box;\r\n      width: 100%;\r\n      margin-bottom: 20px;\r\n    }\r\n\r\n    .btn > tbody > tr > td {\r\n      padding-bottom: 15px;\r\n    }\r\n\r\n    .btn table {\r\n      width: auto;\r\n    }\r\n\r\n    .btn table td {\r\n      background-color: #ffffff;\r\n      border-radius: 5px;\r\n      text-align: center;\r\n    }\r\n\r\n    .btn a {\r\n      background-color: #6366f1; /* Subify Ana Rengi (Örnek: İndigo) */\r\n      border: solid 1px #6366f1;\r\n      border-radius: 6px;\r\n      box-sizing: border-box;\r\n      color: #ffffff;\r\n      cursor: pointer;\r\n      display: inline-block;\r\n      font-size: 16px;\r\n      font-weight: bold;\r\n      margin: 0;\r\n      padding: 12px 25px;\r\n      text-decoration: none;\r\n      text-transform: capitalize;\r\n      transition: background-color 0.3s;\r\n    }\r\n\r\n    .btn a:hover {\r\n      background-color: #4f46e5 !important;\r\n      border-color: #4f46e5 !important;\r\n    }\r\n\r\n    /* Footer */\r\n    .footer {\r\n      clear: both;\r\n      margin-top: 10px;\r\n      text-align: center;\r\n      width: 100%;\r\n    }\r\n\r\n    .footer td,\r\n    .footer p,\r\n    .footer span,\r\n    .footer a {\r\n      color: #999999;\r\n      font-size: 12px;\r\n      text-align: center;\r\n    }\r\n    \r\n    .logo-container {\r\n        text-align: center;\r\n        padding-bottom: 20px;\r\n    }\r\n    \r\n    .icon-container {\r\n        text-align: center;\r\n        padding-bottom: 20px;\r\n    }\r\n\r\n    /* Mobil Uyumluluk */\r\n    @media only screen and (max-width: 620px) {\r\n      .main {\r\n        border-radius: 0;\r\n      }\r\n      .container {\r\n        width: 100% !important;\r\n        padding: 0 !important;\r\n      }\r\n      .wrapper {\r\n        padding: 20px !important;\r\n      }\r\n    }\r\n  </style>\r\n</head>\r\n<body>\r\n  <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"body\">\r\n    <tr>\r\n      <td>&nbsp;</td>\r\n      <td class=\"container\">\r\n        <div class=\"content\">\r\n\r\n          <table role=\"presentation\" class=\"main\">\r\n\r\n            <tr>\r\n              <td class=\"wrapper\">\r\n                <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\r\n                  \r\n                  <tr>\r\n                    <td class=\"logo-container\">\r\n                      <h2 style=\"color: #6366f1; margin:0; font-size: 28px; letter-spacing: -1px;\">Subify.</h2>\r\n                    </td>\r\n                  </tr>\r\n\r\n                  <tr>\r\n                    <td class=\"icon-container\">\r\n                        <img src=\"https://cdn-icons-png.flaticon.com/512/646/646094.png\" alt=\"Email Verify\" width=\"80\" style=\"opacity: 0.8;\">\r\n                    </td>\r\n                  </tr>\r\n\r\n                  <tr>\r\n                    <td>\r\n                      <h1>E-posta Adresini Doğrula</h1>\r\n                      <p>Selam! Subify'a hoş geldin. 🎉</p>\r\n                      <p>Hesabını güvenli bir şekilde oluşturabilmemiz ve aboneliklerini yönetmeye başlayabilmen için aşağıdaki butona tıklayarak e-posta adresini doğrulaman gerekiyor.</p>\r\n                      \r\n                      <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"btn btn-primary\">\r\n                        <tbody>\r\n                          <tr>\r\n                            <td align=\"center\">\r\n                              <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\r\n                                <tbody>\r\n                                  <tr>\r\n                                    <td> <a href=\"{{VERIFICATION_LINK}}\" target=\"_blank\">Hesabımı Doğrula</a> </td>\r\n                                  </tr>\r\n                                </tbody>\r\n                              </table>\r\n                            </td>\r\n                          </tr>\r\n                        </tbody>\r\n                      </table>\r\n                      \r\n                      <p style=\"font-size: 14px; margin-top: 20px;\">Eğer butona tıklayamıyorsan, aşağıdaki bağlantıyı tarayıcına kopyalayabilirsin:</p>\r\n                      <p style=\"font-size: 12px; color: #6366f1; word-break: break-all;\">{{VERIFICATION_LINK}}</p>\r\n                      \r\n                    </td>\r\n                  </tr>\r\n                </table>\r\n              </td>\r\n            </tr>\r\n            </table>\r\n          <div class=\"footer\">\r\n            <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\r\n              <tr>\r\n                <td class=\"content-block\">\r\n                  <span class=\"apple-link\">Subify Inc, İstanbul, Türkiye</span>\r\n                  <br> Bu işlemi sen yapmadıysan, bu e-postayı görmezden gelebilirsin.\r\n                </td>\r\n              </tr>\r\n            </table>\r\n          </div>\r\n          </div>\r\n      </td>\r\n      <td>&nbsp;</td>\r\n    </tr>\r\n  </table>\r\n</body>\r\n</html>", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "en", "VerifyEmail", "Verify Your Email", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a323b8af-d5fe-402f-a35b-2f1fff3a2aa2"), "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\r\n  <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />\r\n  <title>Subify Email Verification</title>\r\n  <style>\r\n    /* Genel Resetler */\r\n    body {\r\n      background-color: #f4f4f7;\r\n      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;\r\n      -webkit-font-smoothing: antialiased;\r\n      font-size: 14px;\r\n      line-height: 1.4;\r\n      margin: 0;\r\n      padding: 0;\r\n      -ms-text-size-adjust: 100%;\r\n      -webkit-text-size-adjust: 100%;\r\n    }\r\n\r\n    table {\r\n      border-collapse: separate;\r\n      mso-table-lspace: 0pt;\r\n      mso-table-rspace: 0pt;\r\n      width: 100%;\r\n    }\r\n\r\n    /* Ana Konteyner */\r\n    .container {\r\n      display: block;\r\n      margin: 0 auto !important;\r\n      max-width: 580px;\r\n      padding: 10px;\r\n      width: 580px;\r\n    }\r\n\r\n    /* İçerik Kutusu */\r\n    .content {\r\n      box-sizing: border-box;\r\n      display: block;\r\n      margin: 0 auto;\r\n      max-width: 580px;\r\n      padding: 10px;\r\n    }\r\n\r\n    /* Beyaz Kart Tasarımı */\r\n    .main {\r\n      background: #ffffff;\r\n      border-radius: 8px;\r\n      width: 100%;\r\n      box-shadow: 0 4px 10px rgba(0, 0, 0, 0.03);\r\n    }\r\n\r\n    .wrapper {\r\n      box-sizing: border-box;\r\n      padding: 40px;\r\n    }\r\n\r\n    /* Tipografi */\r\n    h1 {\r\n      font-size: 24px;\r\n      font-weight: 700;\r\n      margin: 0;\r\n      margin-bottom: 20px;\r\n      color: #1a1a1a;\r\n      text-align: center;\r\n    }\r\n\r\n    p {\r\n      font-size: 16px;\r\n      font-weight: normal;\r\n      margin: 0;\r\n      margin-bottom: 20px;\r\n      color: #555555;\r\n      line-height: 1.6;\r\n      text-align: center;\r\n    }\r\n\r\n    /* Buton Tasarımı - Subify için Mor Tonları */\r\n    .btn {\r\n      box-sizing: border-box;\r\n      width: 100%;\r\n      margin-bottom: 20px;\r\n    }\r\n\r\n    .btn > tbody > tr > td {\r\n      padding-bottom: 15px;\r\n    }\r\n\r\n    .btn table {\r\n      width: auto;\r\n    }\r\n\r\n    .btn table td {\r\n      background-color: #ffffff;\r\n      border-radius: 5px;\r\n      text-align: center;\r\n    }\r\n\r\n    .btn a {\r\n      background-color: #6366f1; /* Subify Brand Color */\r\n      border: solid 1px #6366f1;\r\n      border-radius: 6px;\r\n      box-sizing: border-box;\r\n      color: #ffffff;\r\n      cursor: pointer;\r\n      display: inline-block;\r\n      font-size: 16px;\r\n      font-weight: bold;\r\n      margin: 0;\r\n      padding: 12px 25px;\r\n      text-decoration: none;\r\n      text-transform: capitalize;\r\n      transition: background-color 0.3s;\r\n    }\r\n\r\n    .btn a:hover {\r\n      background-color: #4f46e5 !important;\r\n      border-color: #4f46e5 !important;\r\n    }\r\n\r\n    /* Footer */\r\n    .footer {\r\n      clear: both;\r\n      margin-top: 10px;\r\n      text-align: center;\r\n      width: 100%;\r\n    }\r\n\r\n    .footer td,\r\n    .footer p,\r\n    .footer span,\r\n    .footer a {\r\n      color: #999999;\r\n      font-size: 12px;\r\n      text-align: center;\r\n    }\r\n    \r\n    .logo-container {\r\n        text-align: center;\r\n        padding-bottom: 20px;\r\n    }\r\n    \r\n    .icon-container {\r\n        text-align: center;\r\n        padding-bottom: 20px;\r\n    }\r\n\r\n    /* Mobil Uyumluluk */\r\n    @media only screen and (max-width: 620px) {\r\n      .main {\r\n        border-radius: 0;\r\n      }\r\n      .container {\r\n        width: 100% !important;\r\n        padding: 0 !important;\r\n      }\r\n      .wrapper {\r\n        padding: 20px !important;\r\n      }\r\n    }\r\n  </style>\r\n</head>\r\n<body>\r\n  <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"body\">\r\n    <tr>\r\n      <td>&nbsp;</td>\r\n      <td class=\"container\">\r\n        <div class=\"content\">\r\n\r\n          <table role=\"presentation\" class=\"main\">\r\n\r\n            <tr>\r\n              <td class=\"wrapper\">\r\n                <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\r\n                  \r\n                  <tr>\r\n                    <td class=\"logo-container\">\r\n                      <h2 style=\"color: #6366f1; margin:0; font-size: 28px; letter-spacing: -1px;\">Subify.</h2>\r\n                    </td>\r\n                  </tr>\r\n\r\n                  <tr>\r\n                    <td class=\"icon-container\">\r\n                        <img src=\"https://cdn-icons-png.flaticon.com/512/646/646094.png\" alt=\"Verify Email\" width=\"80\" style=\"opacity: 0.8;\">\r\n                    </td>\r\n                  </tr>\r\n\r\n                  <tr>\r\n                    <td>\r\n                      <h1>Verify Your Email Address</h1>\r\n                      <p>Hi there! Welcome to Subify. 🎉</p>\r\n                      <p>To start managing your subscriptions securely, please verify your email address by clicking the button below.</p>\r\n                      \r\n                      <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"btn btn-primary\">\r\n                        <tbody>\r\n                          <tr>\r\n                            <td align=\"center\">\r\n                              <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\r\n                                <tbody>\r\n                                  <tr>\r\n                                    <td> <a href=\"{{VERIFICATION_LINK}}\" target=\"_blank\">Verify My Account</a> </td>\r\n                                  </tr>\r\n                                </tbody>\r\n                              </table>\r\n                            </td>\r\n                          </tr>\r\n                        </tbody>\r\n                      </table>\r\n                      \r\n                      <p style=\"font-size: 14px; margin-top: 20px;\">If the button doesn't work, copy and paste the link below into your browser:</p>\r\n                      <p style=\"font-size: 12px; color: #6366f1; word-break: break-all;\">{{VERIFICATION_LINK}}</p>\r\n                      \r\n                    </td>\r\n                  </tr>\r\n                </table>\r\n              </td>\r\n            </tr>\r\n            </table>\r\n          <div class=\"footer\">\r\n            <table role=\"presentation\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\r\n              <tr>\r\n                <td class=\"content-block\">\r\n                  <span class=\"apple-link\">Subify Inc, Istanbul, Turkey</span>\r\n                  <br> If you didn't create this account, you can safely ignore this email.\r\n                </td>\r\n              </tr>\r\n            </table>\r\n          </div>\r\n          </div>\r\n      </td>\r\n      <td>&nbsp;</td>\r\n    </tr>\r\n  </table>\r\n</body>\r\n</html>", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "tr", "VerifyEmail", "E-postanızı Doğrulayın", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "BillingCycle", "CreatedAt", "Currency", "DeletedAt", "IsActive", "LastVerifiedAt", "LogoUrl", "Name", "Price", "PriceBefore", "Region", "Slug", "SourceUrl", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "Monthly", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(398), new TimeSpan(0, 0, 0, 0, 0)), "TRY", null, true, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(394), new TimeSpan(0, 0, 0, 0, 0)), "https://example.com/logos/netflix.png", "Netflix", 299.99f, 349.99f, "TR", "netflix", "https://www.netflix.com/tr/", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(399), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "Monthly", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(405), new TimeSpan(0, 0, 0, 0, 0)), "TRY", null, true, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(403), new TimeSpan(0, 0, 0, 0, 0)), "https://example.com/logos/spotify.png", "Spotify", 299.99f, 349.99f, "TR", "spotify", "https://www.spotify.com/tr/", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(405), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "Monthly", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(410), new TimeSpan(0, 0, 0, 0, 0)), "TRY", null, true, new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(408), new TimeSpan(0, 0, 0, 0, 0)), "https://example.com/logos/adobe.png", "Adobe Creative Cloud", 499.99f, 599.99f, "TR", "adobe-creative-cloud", "https://www.adobe.com/tr/creativecloud.html", new DateTimeOffset(new DateTime(2026, 1, 2, 22, 32, 59, 626, DateTimeKind.Unspecified).AddTicks(410), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Resources",
                columns: new[] { "Id", "CreatedAt", "IsActive", "LanguageCode", "Name", "PageName", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "streaming", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Video Akış" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "music", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Müzik" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "productivity", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Üretkenlik" },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "gaming", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Oyun" },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "cloud-storage", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bulut Depolama" },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "news-magazines", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Haber & Dergi" },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "fitness-health", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Fitness & Sağlık" },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "education", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Eğitim" },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "utilities", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Araçlar" },
                    { new Guid("20000000-0000-0000-0000-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "other", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Diğer" },
                    { new Guid("20000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "streaming", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Streaming" },
                    { new Guid("20000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "music", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Music" },
                    { new Guid("20000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "productivity", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Productivity" },
                    { new Guid("20000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "gaming", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gaming" },
                    { new Guid("20000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "cloud-storage", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cloud Storage" },
                    { new Guid("20000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "news-magazines", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "News & Magazines" },
                    { new Guid("20000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "fitness-health", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Fitness & Health" },
                    { new Guid("20000000-0000-0000-0001-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "education", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Education" },
                    { new Guid("20000000-0000-0000-0001-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "utilities", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Utilities" },
                    { new Guid("20000000-0000-0000-0001-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "other", "Category", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Other" },
                    { new Guid("30000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Save", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kaydet" },
                    { new Guid("30000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Cancel", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "İptal" },
                    { new Guid("30000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Delete", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sil" },
                    { new Guid("30000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Edit", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Düzenle" },
                    { new Guid("30000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Loading", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yükleniyor.. ." },
                    { new Guid("30000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Add", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ekle" },
                    { new Guid("30000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Search", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ara" },
                    { new Guid("30000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Filter", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Filtrele" },
                    { new Guid("30000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Yes", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Evet" },
                    { new Guid("30000000-0000-0000-0000-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "No", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hayır" },
                    { new Guid("30000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Save", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Save" },
                    { new Guid("30000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Cancel", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cancel" },
                    { new Guid("30000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Delete", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Delete" },
                    { new Guid("30000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Edit", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Edit" },
                    { new Guid("30000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Loading", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Loading..." },
                    { new Guid("30000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Add", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Add" },
                    { new Guid("30000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Search", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Search" },
                    { new Guid("30000000-0000-0000-0001-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Filter", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Filter" },
                    { new Guid("30000000-0000-0000-0001-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Yes", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yes" },
                    { new Guid("30000000-0000-0000-0001-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "No", "Common", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "No" },
                    { new Guid("40000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "FreeLimitReached", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Free planda en fazla 3 abonelik ekleyebilirsin.  Daha fazlası için Premium'a geç." },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Unauthorized", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bu işlem için giriş yapmalısın." },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "NotFound", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aradığın içerik bulunamadı." },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "ValidationFailed", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Lütfen tüm alanları doğru doldur." },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "ServerError", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bir hata oluştu. Lütfen daha sonra tekrar dene." },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "RateLimitExceeded", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Çok fazla istek gönderdin. Lütfen biraz bekle." },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "PremiumRequired", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bu özellik Premium üyelik gerektirir." },
                    { new Guid("40000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "FreeLimitReached", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Free plan allows up to 3 subscriptions.  Upgrade to Premium for more." },
                    { new Guid("40000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Unauthorized", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "You must be logged in to perform this action." },
                    { new Guid("40000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "NotFound", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "The requested content was not found." },
                    { new Guid("40000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "ValidationFailed", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Please fill in all fields correctly." },
                    { new Guid("40000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "ServerError", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "An error occurred. Please try again later." },
                    { new Guid("40000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "RateLimitExceeded", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Too many requests.  Please wait a moment." },
                    { new Guid("40000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "PremiumRequired", "Error", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "This feature requires Premium membership." },
                    { new Guid("50000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Title", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Daha Akıllı Abonelik Yönetimi için Premium'a Geç" },
                    { new Guid("50000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "CTA", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Premium'a Geç" },
                    { new Guid("50000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Benefit1", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sınırsız abonelik ekleme" },
                    { new Guid("50000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Benefit2", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Detaylı kategori raporları" },
                    { new Guid("50000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Benefit3", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AI destekli tasarruf önerileri 🤖" },
                    { new Guid("50000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Benefit4", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Push bildirimleri" },
                    { new Guid("50000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Benefit5", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Öncelikli destek" },
                    { new Guid("50000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "PriceMonthly", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "49 TL / ay" },
                    { new Guid("50000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "PriceYearly", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "499 TL / yıl" },
                    { new Guid("50000000-0000-0000-0000-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "PriceLifetime", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "699 TL / ömür boyu" },
                    { new Guid("50000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Title", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Upgrade to Premium for Smarter Subscription Management" },
                    { new Guid("50000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "CTA", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Go Premium" },
                    { new Guid("50000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Benefit1", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Unlimited subscriptions" },
                    { new Guid("50000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Benefit2", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Detailed category reports" },
                    { new Guid("50000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Benefit3", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AI-powered savings tips 🤖" },
                    { new Guid("50000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Benefit4", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Push notifications" },
                    { new Guid("50000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Benefit5", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Priority support" },
                    { new Guid("50000000-0000-0000-0001-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "PriceMonthly", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "$4.99 / month" },
                    { new Guid("50000000-0000-0000-0001-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "PriceYearly", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "$49.99 / year" },
                    { new Guid("50000000-0000-0000-0001-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "PriceLifetime", "Paywall", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "$69.99 / lifetime" },
                    { new Guid("60000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "AddNew", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "+ Yeni Abonelik" },
                    { new Guid("60000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Name", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Abonelik Adı" },
                    { new Guid("60000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Price", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Fiyat" },
                    { new Guid("60000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Category", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kategori" },
                    { new Guid("60000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "BillingCycle", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ödeme Döngüsü" },
                    { new Guid("60000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Monthly", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aylık" },
                    { new Guid("60000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Yearly", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yıllık" },
                    { new Guid("60000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "NextRenewal", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sonraki Ödeme" },
                    { new Guid("60000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "LastUsed", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Son Kullanım" },
                    { new Guid("60000000-0000-0000-0000-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Notes", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Notlar" },
                    { new Guid("60000000-0000-0000-0000-00000000000b"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Archive", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Arşivle" },
                    { new Guid("60000000-0000-0000-0000-00000000000c"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "SharedWith", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Paylaşılan Kişi Sayısı" },
                    { new Guid("60000000-0000-0000-0000-00000000000d"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "YourShare", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Senin Payın" },
                    { new Guid("60000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "AddNew", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "+ New Subscription" },
                    { new Guid("60000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Name", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Subscription Name" },
                    { new Guid("60000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Price", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Price" },
                    { new Guid("60000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Category", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Category" },
                    { new Guid("60000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "BillingCycle", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Billing Cycle" },
                    { new Guid("60000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Monthly", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Monthly" },
                    { new Guid("60000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Yearly", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yearly" },
                    { new Guid("60000000-0000-0000-0001-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "NextRenewal", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Next Renewal" },
                    { new Guid("60000000-0000-0000-0001-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "LastUsed", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Last Used" },
                    { new Guid("60000000-0000-0000-0001-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Notes", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Notes" },
                    { new Guid("60000000-0000-0000-0001-00000000000b"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Archive", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Archive" },
                    { new Guid("60000000-0000-0000-0001-00000000000c"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "SharedWith", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Shared With" },
                    { new Guid("60000000-0000-0000-0001-00000000000d"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "YourShare", "Subscription", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Your Share" },
                    { new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Title", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Özet" },
                    { new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "MonthlyTotal", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aylık Toplam" },
                    { new Guid("70000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "YearlyTotal", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yıllık Toplam" },
                    { new Guid("70000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "ActiveSubscriptions", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aktif Abonelikler" },
                    { new Guid("70000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "UpcomingPayments", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yaklaşan Ödemeler" },
                    { new Guid("70000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "NoSubscriptions", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Henüz abonelik eklemedin." },
                    { new Guid("70000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "BudgetWarning", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aylık bütçeni aştın!" },
                    { new Guid("70000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Title", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Overview" },
                    { new Guid("70000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "MonthlyTotal", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Monthly Total" },
                    { new Guid("70000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "YearlyTotal", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yearly Total" },
                    { new Guid("70000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "ActiveSubscriptions", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Active Subscriptions" },
                    { new Guid("70000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "UpcomingPayments", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Upcoming Payments" },
                    { new Guid("70000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "NoSubscriptions", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "You haven't added any subscriptions yet." },
                    { new Guid("70000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "BudgetWarning", "Dashboard", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "You've exceeded your monthly budget!" },
                    { new Guid("80000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Title", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AI Önerileri" },
                    { new Guid("80000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "GetSuggestions", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AI'dan Analiz Al" },
                    { new Guid("80000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Analyzing", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aboneliklerin analiz ediliyor.. ." },
                    { new Guid("80000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "EstimatedSavings", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tahmini Tasarruf" },
                    { new Guid("80000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Tips", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Öneriler" },
                    { new Guid("80000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Summary", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Özet" },
                    { new Guid("80000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Title", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AI Suggestions" },
                    { new Guid("80000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "GetSuggestions", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Get AI Analysis" },
                    { new Guid("80000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Analyzing", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Analyzing your subscriptions..." },
                    { new Guid("80000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "EstimatedSavings", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Estimated Savings" },
                    { new Guid("80000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Tips", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tips" },
                    { new Guid("80000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Summary", "AI", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Summary" },
                    { new Guid("90000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "RenewalSubject", "Email", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Yaklaşan Ödeme Hatırlatması" },
                    { new Guid("90000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "RenewalBody", "Email", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "{SubscriptionName} aboneliğinin ödemesi {DaysRemaining} gün sonra ({RenewalDate})." },
                    { new Guid("90000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "RenewalGreeting", "Email", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Merhaba {FullName}," },
                    { new Guid("90000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "RenewalSubject", "Email", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Upcoming Payment Reminder" },
                    { new Guid("90000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "RenewalBody", "Email", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Your {SubscriptionName} subscription payment is due in {DaysRemaining} days ({RenewalDate})." },
                    { new Guid("90000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "RenewalGreeting", "Email", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hello {FullName}," },
                    { new Guid("a0000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Login", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Giriş Yap" },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Register", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kayıt Ol" },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Logout", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Çıkış Yap" },
                    { new Guid("a0000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Email", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "E-posta" },
                    { new Guid("a0000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Password", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Şifre" },
                    { new Guid("a0000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "ForgotPassword", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Şifremi Unuttum" },
                    { new Guid("a0000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "NoAccount", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hesabın yok mu?" },
                    { new Guid("a0000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "HaveAccount", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Zaten hesabın var mı? " },
                    { new Guid("a0000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Login", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Login" },
                    { new Guid("a0000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Register", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Register" },
                    { new Guid("a0000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Logout", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Logout" },
                    { new Guid("a0000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Email", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Email" },
                    { new Guid("a0000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Password", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Password" },
                    { new Guid("a0000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "ForgotPassword", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Forgot Password" },
                    { new Guid("a0000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "NoAccount", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Don't have an account?" },
                    { new Guid("a0000000-0000-0000-0001-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "HaveAccount", "Auth", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Already have an account?" },
                    { new Guid("b0000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Title", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ayarlar" },
                    { new Guid("b0000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Profile", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Profil" },
                    { new Guid("b0000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Notifications", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bildirimler" },
                    { new Guid("b0000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Theme", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tema" },
                    { new Guid("b0000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Language", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Dil" },
                    { new Guid("b0000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Currency", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Para Birimi" },
                    { new Guid("b0000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "Budget", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Aylık Bütçe" },
                    { new Guid("b0000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "DarkMode", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Karanlık Mod" },
                    { new Guid("b0000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "EmailNotifications", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "E-posta Bildirimleri" },
                    { new Guid("b0000000-0000-0000-0000-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "PushNotifications", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Push Bildirimleri" },
                    { new Guid("b0000000-0000-0000-0000-00000000000b"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "TR", "DaysBeforeReminder", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hatırlatma Günü" },
                    { new Guid("b0000000-0000-0000-0001-000000000001"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Title", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Settings" },
                    { new Guid("b0000000-0000-0000-0001-000000000002"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Profile", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Profile" },
                    { new Guid("b0000000-0000-0000-0001-000000000003"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Notifications", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Notifications" },
                    { new Guid("b0000000-0000-0000-0001-000000000004"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Theme", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Theme" },
                    { new Guid("b0000000-0000-0000-0001-000000000005"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Language", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Language" },
                    { new Guid("b0000000-0000-0000-0001-000000000006"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Currency", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Currency" },
                    { new Guid("b0000000-0000-0000-0001-000000000007"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "Budget", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Monthly Budget" },
                    { new Guid("b0000000-0000-0000-0001-000000000008"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "DarkMode", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Dark Mode" },
                    { new Guid("b0000000-0000-0000-0001-000000000009"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "EmailNotifications", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Email Notifications" },
                    { new Guid("b0000000-0000-0000-0001-00000000000a"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "PushNotifications", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Push Notifications" },
                    { new Guid("b0000000-0000-0000-0001-00000000000b"), new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "EN", "DaysBeforeReminder", "Settings", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Days Before Reminder" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId_CreatedAt",
                table: "ActivityLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestionLogs_UserId_CreatedAt",
                table: "AiSuggestionLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestionLogs_UserId_IsSuccess_CreatedAt",
                table: "AiSuggestionLogs",
                columns: new[] { "UserId", "IsSuccess", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DeletedAt_Active",
                table: "AspNetUsers",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsActive_Email",
                table: "AspNetUsers",
                columns: new[] { "IsActive", "Email" });

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillingSessions_Provider_SessionId_Unique",
                table: "BillingSessions",
                columns: new[] { "Provider", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingSessions_UserId_Status_CreatedAt",
                table: "BillingSessions",
                columns: new[] { "UserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive_SortOrder",
                table: "Categories",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug_Unique",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_LanguageCode",
                table: "EmailTemplates",
                columns: new[] { "Name", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntitlementCaches_UserId_Entitlement_Unique",
                table: "EntitlementCaches",
                columns: new[] { "UserId", "Entitlement" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EntitlementCaches_UserId_Status",
                table: "EntitlementCaches",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateSnapshots_BaseCurrency_TargetCurrency_FetchedAt",
                table: "ExchangeRateSnapshots",
                columns: new[] { "BaseCurrency", "TargetCurrency", "FetchedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_DuplicateCheck",
                table: "NotificationLogs",
                columns: new[] { "UserId", "SubscriptionId", "Type", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_SubscriptionId",
                table: "NotificationLogs",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_UserId_SentAt",
                table: "NotificationLogs",
                columns: new[] { "UserId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Plan",
                table: "Profiles",
                column: "Plan");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_IsActive",
                table: "Providers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Slug_Unique",
                table: "Providers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushTokens_Token_Platform_Unique",
                table: "PushTokens",
                columns: new[] { "Token", "Platform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushTokens_UserId_IsActive",
                table: "PushTokens",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_LanguageCode_IsActive_UpdatedAt",
                table: "Resources",
                columns: new[] { "LanguageCode", "IsActive", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_PageName_Name_LanguageCode_Unique",
                table: "Resources",
                columns: new[] { "PageName", "Name", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPaymentRecords_BillingSessionId",
                table: "SubscriptionPaymentRecords",
                column: "BillingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPaymentRecords_SubscriptionId_PaymentDate",
                table: "SubscriptionPaymentRecords",
                columns: new[] { "SubscriptionId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPaymentRecords_UserId_PaymentDate",
                table: "SubscriptionPaymentRecords",
                columns: new[] { "UserId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CategoryId",
                table: "Subscriptions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_DeletedAt_Active",
                table: "Subscriptions",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_NextRenewalDate_Active",
                table: "Subscriptions",
                columns: new[] { "NextRenewalDate", "Archived", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ProviderId",
                table: "Subscriptions",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserCategoryId",
                table: "Subscriptions",
                column: "UserCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Archived_NextRenewalDate",
                table: "Subscriptions",
                columns: new[] { "UserId", "Archived", "NextRenewalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_CategoryId",
                table: "Subscriptions",
                columns: new[] { "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategories_UserId_IsActive_SortOrder",
                table: "UserCategories",
                columns: new[] { "UserId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategories_UserId_Slug_Unique",
                table: "UserCategories",
                columns: new[] { "UserId", "Slug" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AiSuggestionLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "EntitlementCaches");

            migrationBuilder.DropTable(
                name: "ExchangeRateSnapshots");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "PushTokens");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "SubscriptionPaymentRecords");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "BillingSessions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "UserCategories");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
