using System.Text.Json.Serialization;
using MediCoreSupply.Api.Data;
using MediCoreSupply.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MediCoreSupplyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.

// Swagger is enabled in every environment: until the frontend exists, the Swagger UI
// is the public demo of the deployed API.
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MediCoreSupplyDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

// Read-only mode for the public demo: reads are allowed, changes are refused.
// On in appsettings.Production.json; override with the app setting Api__ReadOnly=false.
if (app.Configuration.GetValue<bool>("Api:ReadOnly"))
{
    app.Use(async (context, next) =>
    {
        var method = context.Request.Method;
        var isRead = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);

        if (context.Request.Path.StartsWithSegments("/api") && !isRead)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "This demo API is read-only.",
                detail: "Creating, updating and deleting data is disabled on the public deployment.")
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Send visitors of the site root to the Swagger UI instead of a 404.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
