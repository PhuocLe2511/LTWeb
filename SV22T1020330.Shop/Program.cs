using SV22T1020330.DataLayers.Interfaces;
using SV22T1020330.DataLayers.SqlServer;
using SV22T1020330.DataLayers.SQLServer;
using SV22T1020330.Models.Catalog;
using SV22T1020330.Models.Security;
using SV22T1020330.Shop;

var builder = WebApplication.CreateBuilder(args);

// ======================
// 1️⃣ Add Services
// ======================

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddHttpContextAccessor();

// ⭐ Session (bắt buộc cho Cart + Login)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ======================
// 2️⃣ Đăng ký Repository
// ======================

// Lấy connection string từ appsettings.json
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("ConnectionString is NULL!");
}

// Product
builder.Services.AddScoped<IProductRepository>(sp =>
    new ProductRepository(connectionString));

// Order
builder.Services.AddScoped<IOrderRepository>(sp =>
    new OrderRepository(connectionString));

// Customer / Account (Login)
builder.Services.AddScoped<IUserAccountRepository>(sp =>
    new CustomerAccountRepository(connectionString));

// Danh mục (dropdown Shop)
builder.Services.AddScoped<IGenericRepository<Category>>(sp =>
    new CategoryRepository(connectionString));

// Province cho dropdown
builder.Services.AddScoped<SV22T1020330.DataLayers.Interfaces.IDataDictionaryRepository<SV22T1020330.Models.DataDictionary.Province>>(sp =>
    new ProvinceRepository(connectionString));

// ======================
// 3️⃣ Build app
// ======================

var app = builder.Build();

ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    app.Configuration);

// ======================
// 4️⃣ Middleware
// ======================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();

app.UseRouting();

// ⭐ QUAN TRỌNG: phải đặt trước Authorization
app.UseSession();

app.UseAuthorization();

// ======================
// 5️⃣ Routing
// ======================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

// ======================
// 6️⃣ Run
// ======================

app.Run();