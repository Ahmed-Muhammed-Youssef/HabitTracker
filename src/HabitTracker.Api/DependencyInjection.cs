using System.Text;
using FluentValidation;
using HabitTracker.Api.Database;
using HabitTracker.Api.DTOs.Habits;
using HabitTracker.Api.Entities;
using HabitTracker.Api.Middleware;
using HabitTracker.Api.Services;
using HabitTracker.Api.Services.Sorting;
using HabitTracker.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HabitTracker.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllers(options => options.ReturnHttpNotAcceptable = true)
            .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver())
            .AddXmlSerializerFormatters();

        builder.Services.Configure<MvcOptions>(options =>
        {
            var formatter = options.OutputFormatters
            .OfType<NewtonsoftJsonOutputFormatter>()
            .First();

            formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJson);
        });

        builder.Services.AddOpenApi();
        return builder;
    }

    public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            };
        });

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(
                    builder.Configuration.GetConnectionString("Database"),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application)
                ));

        builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
           options
               .UseNpgsql(
                   builder.Configuration.GetConnectionString("Database"),
                   npgsqlOptions => npgsqlOptions
                       .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Identity)
               ));

        return builder;
    }

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            .UseOtlpExporter();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        return builder;
    }

    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<HabitDto, Habit>>(_ => HabitMappings.SortMapping);
        builder.Services.AddTransient<SortMappingProvider>();
        builder.Services.AddTransient<DataShapingService>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<LinkService>();

        builder.Services.AddTransient<TokenProvider>();

        builder.Services.AddMemoryCache();

        builder.Services.AddScoped<UserContext>();

        builder.Services.AddScoped<GithubAccessTokenService>();
        builder.Services.AddTransient<GithubService>();

        builder.Services
            .AddHttpClient("github")
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                
                client.DefaultRequestHeaders
                .UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("HabitTracker", "1.0"));

                client.DefaultRequestHeaders
                .Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            });

        return builder;
    }

    public static WebApplicationBuilder AddAuthenticationServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

        builder.Services.Configure<JwtAuthOptions>(builder.Configuration.GetSection("Jwt"));

        JwtAuthOptions jwtAuthOptions = builder.Configuration.GetSection("Jwt").Get<JwtAuthOptions>()!;

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtAuthOptions.Issuer,
                    ValidAudience = jwtAuthOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthOptions.Key))
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
