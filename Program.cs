using Microsoft.EntityFrameworkCore;
using wt_lab7_7postgres_efcore_vasileva.Data;
using wt_lab7_7postgres_efcore_vasileva.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Получаем строку подключения
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- ОТЛАДКА: Выведем в логи Render то, что пришло ---
Console.WriteLine($"DEBUG: Строка подключения ДО конвертации = {connectionString}");

// Конвертируем формат postgresql:// в формат Npgsql (Host=...;Port=...)
if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("postgres"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);
    
    // Если порт не указан (равен -1), ставим стандартный 5432
    var finalPort = uri.Port == -1 ? 5432 : uri.Port;
    
    connectionString = $"Host={uri.Host};Port={finalPort};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

Console.WriteLine($"DEBUG: Строка подключения ПОСЛЕ конвертации = {connectionString}");

// Подключаем базу данных
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// 3. Автоматическое применение миграций
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// 4. Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// 5. Базовые эндпоинты
app.MapGet("/", () => "Minimal API работает!");
app.MapGet("/api/info", () => new { Application = "Web Technologies", Technology = "ASP.NET Core Minimal API", Version = "1.0" });

// --- 6. CRUD ---

app.MapGet("/api/products", async (string? search, AppDbContext db) =>
{
    var query = db.Products.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));
    }
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
    if (product.CreatedAt == default)
    {
        product.CreatedAt = DateTime.UtcNow;
    }
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

// LINQ 1: Фильтрация по цене (Price > 100)
app.MapGet("/api/products/filter", async (AppDbContext db) =>
{
    var products = await db.Products.Where(x => x.Price > 100).ToListAsync();
    return Results.Ok(products);
});

// LINQ 2: Сортировка по цене
app.MapGet("/api/products/sorted", async (AppDbContext db) =>
{
    var products = await db.Products.OrderBy(x => x.Price).ToListAsync();
    return Results.Ok(products);
});

// LINQ 3: Выборка с условием (IsAvailable == true) и сортировка по имени
app.MapGet("/api/products/available", async (AppDbContext db) =>
{
    var products = await db.Products.Where(x => x.IsAvailable).OrderBy(x => x.Name).ToListAsync();
    return Results.Ok(products);
});