using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Services;
using CustomCharInfo.server.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Amazon.S3;
using Amazon.Runtime;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomCharInfo.server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddOpenApi();
            // Enable Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                // The full/internal doc is Development-only. Only "public" is ever generated in production.
                if (builder.Environment.IsDevelopment())
                {
                    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "UMC API", Version = "v1" });
                }
                options.SwaggerDoc("public", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "UMC Public API", Version = "v1" });
                options.DocInclusionPredicate((docName, apiDesc) =>
                    docName == "v1" || apiDesc.GroupName == "public");
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter JWT token. Bearer prefix is added automatically.",
                });
                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            // DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Cloudflare R2 (S3-compatible) client for image storage
            builder.Services.AddSingleton<IAmazonS3>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var accountId = config["R2:AccountId"];
                var accessKey = config["R2:AccessKeyId"];
                var secretKey = config["R2:SecretAccessKey"];

                var s3Config = new AmazonS3Config
                {
                    ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                    ForcePathStyle = true,
                };
                return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config);
            });
            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
                options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Email;
            });
            builder.Services.AddAuthorization();

            // Render terminates TLS at its own proxy in front of the app, so the raw connection IP is Render's proxy, not the client.
            // Trust X-Forwarded-For to recover the real client IP.
            // KnownNetworks/KnownProxies are cleared because Render's proxy IP isn't fixed/published.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddScoped<IpActivityService>();
            builder.Services.AddScoped<ActivityTrackingFilter>();

            // Downloads GitHub release assets that predate GitHub's own digests so the Repo Releases page can hash them.
            builder.Services.AddHttpClient<IReleaseAssetHasher, GitHubAssetHasher>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("UltimateMovesetCompatibility/1.0");
            });

            // Rate limiting for auth endpoints (login/register/refresh/reset-password) to slow
            // brute-force and credential-stuffing attempts. Partitioned per client IP.
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Public API rate limiting, partitioned per client IP.
                options.AddPolicy("public", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
                options.AddPolicy("public-heavy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            // Controllers
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ActivityTrackingFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });
            // CORS
            var corsOrigins = new List<string> { "http://localhost:5173", "https://lilylavender.github.io" };
            var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (configuredOrigins != null)
                corsOrigins.AddRange(configuredOrigins);

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(corsOrigins.ToArray())
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });

                // Public API surface: anonymous, any origin, GET/POST only, no credentials.
                // Applied per-action via [EnableCors("PublicApi")].
                // The default policy above stays in effect for every authenticated/write route.
                options.AddPolicy("PublicApi", policy =>
                {
                    policy.AllowAnyOrigin()
                          .WithMethods("GET", "POST")
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "UMC API");
                    options.SwaggerEndpoint("/swagger/public/swagger.json", "UMC Public API");
                });
                app.MapOpenApi();
            }
            else
            {
                // Only the public doc/UI is exposed outside Development.
                // The full internal doc stays dev-only
                // (AddSwaggerGen above never registers a "v1" doc outside Development,
                // so only /swagger/public/swagger.json can ever be generated here).
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/public/swagger.json", "UMC Public API");
                });
            }
            app.UseForwardedHeaders();
            app.UseCors();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers();

            app.Run();
        }
    }
}