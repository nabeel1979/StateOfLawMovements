using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(3)
    )
);

// Authentication - Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "QanoonCoalition.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMovementService, MovementService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IJoinRequestService, JoinRequestService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISerialNumberService, SerialNumberService>();
builder.Services.AddScoped<SystemConstantService>();
builder.Services.AddScoped<MemberFilterOptionsService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Auto migration + seed admin on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();

        // seed admin إذا لم يكن موجوداً
        if (!db.Users.Any(u => u.Role == QanoonCoalition.Web.Models.UserRole.Admin))
        {
            db.Users.Add(new QanoonCoalition.Web.Models.User
            {
                FullName = "مدير النظام",
                Email = "admin@qanoon.iq",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2024"),
                Role = QanoonCoalition.Web.Models.UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        // الفئات المضافة لاحقاً تُبذر هنا لا في HasData، لأن المعرّفات الثابتة
        // قد تتعارض مع قيم أضافها المسؤول من الواجهة في قاعدة بيانات قائمة.
        foreach (var (category, values) in QanoonCoalition.Web.Models.SysConst.Defaults)
        {
            if (db.SystemConstants.Any(c => c.Category == category)) continue;

            var order = 1;
            foreach (var value in values)
            {
                db.SystemConstants.Add(new QanoonCoalition.Web.Models.SystemConstant
                {
                    Category = category,
                    Value = value,
                    DisplayOrder = order++,
                    IsActive = true
                });
            }
            db.SaveChanges();
        }

        // الأعضاء الذين قُبلوا قبل نقل الصورة من الطلب فقدوا صورهم، فنستعيدها من طلباتهم
        var missingPhotos = db.Members
            .Where(m => m.PhotoPath == null && m.JoinRequestId != null)
            .Join(db.JoinRequests.Where(r => r.PhotoPath != null),
                  m => m.JoinRequestId, r => r.Id,
                  (m, r) => new { Member = m, r.PhotoPath })
            .ToList();

        if (missingPhotos.Count > 0)
        {
            foreach (var row in missingPhotos)
                row.Member.PhotoPath = QanoonCoalition.Web.Services.JoinRequestService.CopyPhoto(row.PhotoPath);
            db.SaveChanges();
        }
    }
    catch { }   // التطبيق يعمل حتى لو كانت قاعدة البيانات غير متاحة مؤقتاً
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ─── حماية المسارات الإدارية ────────────────────────────────────────────────
// FormOnly=true  → وضع الاستمارة فقط (الفرونت)
// FormOnly=false → وضع الإدارة الكاملة (الباك)
var isFormOnly = string.Equals(
    app.Configuration["AppSettings:FormOnly"] ?? "",
    "true", StringComparison.OrdinalIgnoreCase);

// في وضع التطوير: الكشف بالـ Port بدلاً من الإعداد
if (!isFormOnly && app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Connection.LocalPort == 5249)
        {
            var path5249 = context.Request.Path.Value ?? "";
            var allowed5249 = path5249.StartsWith("/join", StringComparison.OrdinalIgnoreCase)
                           || path5249.StartsWith("/Public", StringComparison.OrdinalIgnoreCase)
                           || path5249.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                           || path5249.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                           || path5249.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                           || path5249.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
                           || path5249.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase)
                           || path5249 == "/";
            if (!allowed5249) { context.Response.StatusCode = 404; return; }
        }
        await next();
    });
}

// في وضع الفرونت (FormOnly): حجب كل المسارات الإدارية
if (isFormOnly)
{
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? "";
        var allowed = path.StartsWith("/join", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/Public", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase)
                   || path == "/";
        if (!allowed) { context.Response.StatusCode = 404; return; }
        await next();
    });
}

// Routes
app.MapControllerRoute(name: "join", pattern: "join/{token}", defaults: new { controller = "Public", action = "Join" });
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Development: بورتين — 5248 للإدارة، 5249 للاستمارة
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://0.0.0.0:5248");
    app.Urls.Add("http://0.0.0.0:5249");
}

app.Run();
