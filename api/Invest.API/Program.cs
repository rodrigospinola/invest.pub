using Microsoft.EntityFrameworkCore;
using Invest.API.Extensions;
using Invest.API.Middleware;
using Invest.Application.Extensions;
using Invest.Infrastructure.Data;
using Invest.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configurações
builder.Configuration
    .AddEnvironmentVariables();

// Serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Invest API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Informe o token JWT"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationHandlers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var configOrigins = builder.Configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 
                            ?? Array.Empty<string>();
        
        var allowedOrigins = configOrigins.Concat(new[] { "http://localhost:5173", "http://127.0.0.1:5173" }).Distinct().ToArray();

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// O CORS deve ser um dos primeiros para garantir que pre-flights (OPTIONS) 
// e respostas de erro também recebam os headers.
app.UseCors();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Aplicar migrations automaticamente no startup (apenas desenvolvimento)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
