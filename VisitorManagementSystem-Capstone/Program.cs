using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using VisitorManagementSystem_Capstone.Data;
using VisitorManagementSystem_Capstone.Services;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<VisitorManagementSystemDatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")).EnableSensitiveDataLogging());

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(48);
    options.Cookie.IsEssential = true;
});
// Add response compression services
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

// Register EmailService for dependency injection
builder.Services.AddTransient<EmailService>();
// Add this to your services configuration
builder.Services.AddScoped<LocalImageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24; // 24 hours
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});


// Then in Configure method:
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseSession();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

//Starter Page Route - set to FacilityRegistrationPicker in HomeController
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=SymfoniHome}/{id?}")
    .WithStaticAssets();

app.Run();