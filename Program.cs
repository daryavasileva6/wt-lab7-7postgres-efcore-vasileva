using Microsoft.EntityFrameworkCore;
using wt_lab7_7postgres_efcore_vasileva.Data;
using wt_lab7_7postgres_efcore_vasileva.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка подключения
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Disable";
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// Авто-миграции при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Minimal API работает!");
app.MapGet("/api/info", () => new { Application = "Web Technologies", Technology = "ASP.NET Core Minimal API", Version = "1.0" });

// --- CRUD через базу данных ---

app.MapGet("/api/products", async (string? search, AppDbContext db) =>
{
    var query = db.Products.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));
    
    return Results.Ok(await query.ToListAsync());
});

app.MapGet("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (product == null) return Results.NotFound();
    return Results.Ok(product);
});

app.MapPost("/api/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id}", async (int id, Product updatedProduct, AppDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (product == null) return Results.NotFound();

    product.Name = updatedProduct.Name;
    product.Category = updatedProduct.Category;
    product.Price = updatedProduct.Price;
    product.IsAvailable = updatedProduct.IsAvailable;

    await db.SaveChangesAsync();
    return Results.Ok(product);
});

app.MapDelete("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (product == null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");