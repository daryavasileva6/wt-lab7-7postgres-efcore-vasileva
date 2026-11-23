using Microsoft.EntityFrameworkCore;
using wt_lab7_7postgres_efcore_vasileva.Data;
using wt_lab7_7postgres_efcore_vasileva.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Подключение к базе данных
// Код автоматически возьмет строку подключения из переменной окружения ConnectionStrings__DefaultConnection (на Render)
// или из appsettings.json (локально)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// 3. Автоматическое применение миграций при запуске (создаст таблицы в облаке)
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

// --- 6. CRUD через Entity Framework Core ---

// GET: Список всех товаров + поиск по имени (Блок 15)
app.MapGet("/api/products", async (string? search, AppDbContext db) =>
{
    var query = db.Products.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));
    }
    return Results.Ok(await query.ToListAsync());
});

// GET: Получить товар по ID
app.MapGet("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (product == null) return Results.NotFound();
    return Results.Ok(product);
});

// POST: Создать товар (Блок 16)
app.MapPost("/api/products", async (Product product, AppDbContext db) =>
{
    // Принудительно ставим текущую дату, если она не передана
    if (product.CreatedAt == default)
    {
        product.CreatedAt = DateTime.UtcNow;
    }

    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

// PUT: Обновить товар (Блок 17)
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

// DELETE: Удалить товар (Блок 18)
app.MapDelete("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
    if (product == null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// 7. Запуск для Render (порт из переменной окружения)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");