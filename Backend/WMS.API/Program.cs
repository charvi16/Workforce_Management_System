using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using WMS.API.Middleware;
using WMS.Infrastructure.Data;
using WMS.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WmsFrontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "development-only-secret-key-change-before-production";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    await dbContext.Database.MigrateAsync();
    await EnsureDevelopmentClientSchemaAsync(dbContext);
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("WmsFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static Task EnsureDevelopmentClientSchemaAsync(WmsDbContext dbContext)
{
    if (!dbContext.Database.IsSqlServer())
    {
        return Task.CompletedTask;
    }

    return dbContext.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID('dbo.Clients', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Clients', 'CreatedOn') IS NULL
    BEGIN
        ALTER TABLE dbo.Clients
        ADD CreatedOn datetime2 NOT NULL CONSTRAINT DF_Clients_CreatedOn DEFAULT (GETDATE());
    END

    IF COL_LENGTH('dbo.Clients', 'UpdatedOn') IS NULL
    BEGIN
        ALTER TABLE dbo.Clients
        ADD UpdatedOn datetime2 NULL;
    END

    IF COL_LENGTH('dbo.Clients', 'Status') IS NOT NULL
    BEGIN
        UPDATE dbo.Clients SET Status = 1 WHERE Status IS NULL;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Clients')
              AND c.name = 'Status'
        )
        BEGIN
            ALTER TABLE dbo.Clients
            ADD CONSTRAINT DF_Clients_Status DEFAULT (1) FOR Status;
        END
    END

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('dbo.Clients')
          AND c.name = 'ClientPhoneNumber'
          AND t.name NOT IN ('varchar', 'nvarchar', 'char', 'nchar')
    )
    BEGIN
        ALTER TABLE dbo.Clients ALTER COLUMN ClientPhoneNumber varchar(15) NULL;
    END

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID('dbo.Clients')
          AND c.name = 'ClientLocation'
          AND c.max_length > 0
          AND c.max_length < 100
    )
    BEGIN
        ALTER TABLE dbo.Clients ALTER COLUMN ClientLocation varchar(100) NULL;
    END

    IF OBJECT_ID('dbo.Projects', 'U') IS NOT NULL
       AND COL_LENGTH('dbo.Projects', 'Status') IS NOT NULL
       AND EXISTS (
            SELECT 1
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID('dbo.Projects')
              AND c.name = 'Status'
              AND t.name NOT IN ('varchar', 'nvarchar', 'char', 'nchar')
       )
    BEGIN
        ALTER TABLE dbo.Projects ALTER COLUMN Status varchar(20) NULL;
        UPDATE dbo.Projects
        SET Status = CASE Status
            WHEN '1' THEN 'Active'
            WHEN '2' THEN 'Planned'
            WHEN '3' THEN 'Completed'
            ELSE COALESCE(NULLIF(Status, ''), 'Planned')
        END;
        ALTER TABLE dbo.Projects ALTER COLUMN Status varchar(20) NOT NULL;
    END

    IF OBJECT_ID('dbo.EmployeeProjectAllocations', 'U') IS NOT NULL
       AND COL_LENGTH('dbo.EmployeeProjectAllocations', 'Status') IS NOT NULL
    BEGIN
        UPDATE dbo.EmployeeProjectAllocations SET Status = 0 WHERE Status IS NULL;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.EmployeeProjectAllocations')
              AND c.name = 'Status'
        )
        BEGIN
            ALTER TABLE dbo.EmployeeProjectAllocations
            ADD CONSTRAINT DF_EmployeeProjectAllocations_Status DEFAULT (1) FOR Status;
        END
    END

    IF OBJECT_ID('dbo.Announcements', 'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.Announcements', 'UpdatedOn') IS NULL
        BEGIN
            ALTER TABLE dbo.Announcements ADD UpdatedOn datetime2 NULL;
        END

        IF COL_LENGTH('dbo.Announcements', 'TargetRole') IS NULL
        BEGIN
            ALTER TABLE dbo.Announcements ADD TargetRole varchar(20) NULL;
        END

        IF COL_LENGTH('dbo.Announcements', 'ExpiryDate') IS NULL
        BEGIN
            ALTER TABLE dbo.Announcements ADD ExpiryDate datetime2 NULL;
        END

        IF COL_LENGTH('dbo.Announcements', 'IsActive') IS NOT NULL
        BEGIN
            UPDATE dbo.Announcements SET IsActive = 1 WHERE IsActive IS NULL;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID('dbo.Announcements')
                  AND c.name = 'IsActive'
            )
            BEGIN
                ALTER TABLE dbo.Announcements
                ADD CONSTRAINT DF_Announcements_IsActive DEFAULT (1) FOR IsActive;
            END
        END
    END
END
""");
}
