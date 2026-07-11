using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. 設定 MySQL 連線字串（已換上你的真實密碼）
var connectionString = "server=localhost;port=3306;database=store_db;user=root;password=;charset=utf8mb4;";

// 2. 讓 Pomelo 自動偵測你的資料庫類型與版本（支援 MariaDB & MySQL）
var serverVersion = ServerVersion.AutoDetect(connectionString);

// 3. 註冊資料庫大總管，改用 MySQL/MariaDB 驅動
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// 設定 JSON 序列化為 camelCase 並支持 UTF-8
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// 4. 註冊 CORS 服務 (這是我們為了網頁新加的)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 5. 自動檢查並初始化 MySQL 資料庫與假資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // 如果 MySQL 裡沒有 store_db 資料庫或 Products 資料表，這行會自動幫你建好
    db.Database.EnsureCreated();

    // 如果資料表是空的，才塞入初始商品
    if (!db.Products.Any())
    {
        db.Products.Add(new Product { Name = "無線機械鍵盤", Price = 2490, IsAvailable = true });
        db.Products.Add(new Product { Name = "人體工學滑鼠", Price = 1890, IsAvailable = true });
        db.SaveChanges(); // 存入 MySQL
    }
}

// ==================== API 路由設定 (Endpoints) ====================

// 【GET】讀取全部商品
app.MapGet("/api/products", async (AppDbContext db) =>
{
    var allProducts = await db.Products.ToListAsync();
    return Results.Ok(allProducts);
});

// 【GET】透過 ID 讀取單一商品
app.MapGet("/api/product/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound(new { Message = $"找不到編號為 {id} 的商品" });
    return Results.Ok(product);
});

// 【POST】新增商品到 MySQL
app.MapGet("/api/product", () => "請使用 POST 方法來新增商品"); // 瀏覽器直接打開時的防呆提示

app.MapPost("/api/product", async (Product newProduct, AppDbContext db) =>
{
    // 資料防呆驗證
    if (string.IsNullOrEmpty(newProduct.Name) || newProduct.Price < 0)
    {
        return Results.BadRequest(new { Message = "商品名稱不能為空，且價格必須大於 0" });
    }

    // 加進大總管的追蹤清單
    db.Products.Add(newProduct);
    
    // 真正將資料寫入電腦硬碟的 MySQL 資料庫中
    await db.SaveChangesAsync();

    // 在終端機印出通知，方便在 VS Code 面板上觀察
    Console.WriteLine($"【系統通知】成功寫入 MySQL！商品：{newProduct.Name}");

    return Results.Created($"/api/product/{newProduct.Id}", newProduct);
});

app.Run();

// ==================== 資料結構與對應定義 ====================

// 定義資料表結構
public class Product
{
    public int Id { get; set; } // 在真實資料庫中，這欄會自動設定為 Auto Increment (自動遞增)
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public bool IsAvailable { get; set; }
}

// 定義資料庫大總管
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
}