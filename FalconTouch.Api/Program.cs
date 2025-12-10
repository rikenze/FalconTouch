using FalconTouch.Api.Hubs;
using FalconTouch.Application.Games;
using FalconTouch.Application.Payments;
using FalconTouch.Domain.Repositories;
using FalconTouch.Infrastructure.Data;
using FalconTouch.Infrastructure.Games;
using FalconTouch.Infrastructure.Payments;
using FalconTouch.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// DbContext (PostgreSQL)
builder.Services.AddDbContext<FalconTouchDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("Postgres")));

// Redis (StackExchange.Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = config.GetConnectionString("Redis");
    options.InstanceName = "falcontouch:";
});

// SignalR
builder.Services.AddSignalR();

// JWT
var jwtKey = config["Jwt:Key"]!;
var jwtIssuer = config["Jwt:Issuer"]!;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = signingKey
        };

        // Permitir JWT via WebSockets (SignalR)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/game"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Swagger com JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FalconTouch API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// CORS para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend",
        p => p
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// aplica migrations com retry
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FalconTouchDbContext>();

    const int maxRetries = 5;
    var retries = 0;

    while (true)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (NpgsqlException ex)
        {
            retries++;
            if (retries >= maxRetries)
                throw;

            Console.WriteLine($"[MIGRATION] Postgres ainda não respondeu. Tentativa {retries}/{maxRetries}. Erro: {ex.Message}");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
        catch (SocketException ex)
        {
            retries++;
            if (retries >= maxRetries)
                throw;

            Console.WriteLine($"[MIGRATION] Falha de socket ao conectar no Postgres. Tentativa {retries}/{maxRetries}. Erro: {ex.Message}");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
}

if (config.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.Run();
