using Microsoft.EntityFrameworkCore;
using ElectronicInventoryManagementSystem.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. CẤU HÌNH SESSION (BỘ NHỚ TẠM CHO BACKEND)
builder.Services.AddDistributedMemoryCache(); // Cần thiết để chạy Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Đăng nhập xong, 60 phút sau mới hết hạn
    options.Cookie.HttpOnly = true; // Bảo mật Cookie
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None; // Cho phép gửi Cookie chéo cổng (localhost:3000 -> 7033)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Bắt buộc chạy trên HTTPS
});

// 3. CẤU HÌNH CORS (BẮT TAY VỚI FRONTEND)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Đích danh cổng React của Cúc
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // <--- CHỐT HẠ: Cho phép trình duyệt tự động gửi Cookie/Session ID
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. CẤU HÌNH PIPELINE (THỨ TỰ CHẠY)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();