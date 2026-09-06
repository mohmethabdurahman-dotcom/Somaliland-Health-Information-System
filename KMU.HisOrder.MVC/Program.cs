using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Controllers;
using KMU.HisOrder.MVC.Areas.HisOrder.Services;
using KMU.HisOrder.MVC.Areas.Radiology.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using NuGet.Packaging;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using KMU.HisOrder.MVC.Hubs;
using KMU.HisOrder.MVC.Areas.Radiology.Middleware;
using KMU.HisOrder.MVC.Areas.Radiology.Models;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;
using Npgsql;

//var builder = WebApplication.CreateBuilder(args);
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorPages();

var target = builder.Configuration.GetConnectionString("Target");
var connectionString = builder.Configuration.GetConnectionString("LocalServer");
switch (target)
{
    case "LocalServer":
        connectionString = builder.Configuration.GetConnectionString("LocalServer");
        break;
    case "HGHTestServer":
        connectionString = builder.Configuration.GetConnectionString("HGHTestServer");
        break;

    case "HGHProductionServer":
        connectionString = builder.Configuration.GetConnectionString("HGHProductionServer");
        break;


    case "BOLocalServer":
        connectionString = builder.Configuration.GetConnectionString("BOLocalServer");
        break;

    case "BOProductionServer":
        connectionString = builder.Configuration.GetConnectionString("BOProductionServer");
        break;


    case "Backup":
        connectionString = builder.Configuration.GetConnectionString("Backup");
        break;
    case "test":
        connectionString = builder.Configuration.GetConnectionString("test");
        break;
    case "MoHD":
        connectionString = builder.Configuration.GetConnectionString("MoHD");
        break;

    default:
        break;
}

var EncryptionType = builder.Configuration.GetConnectionString("EncryptionType");

switch (EncryptionType) 
{
    case "Base64":
        connectionString = Encoding.UTF8.GetString(Convert.FromBase64String(connectionString));
        break;


    case "AES256":
        //參考資料 [Day14] 資料使用安全(保護連接字串)上 - iT 邦幫忙::一起幫忙解決難題，拯救 IT 人的一天 https://ithelp.ithome.com.tw/articles/10187947
        var AES256_Key = builder.Configuration.GetSection("ConnectionStrings")["Key"];//加密金鑰(32 Byte)
        var AES256_IV = builder.Configuration.GetSection("ConnectionStrings")["IV"];//初始向量(Initial Vector, iv) 類似雜湊演算法中的加密鹽(16 Byte)

        connectionString = LoginController.AES256(connectionString, AES256_Key, AES256_IV, false);//Do AES256 Decryption
        break;


    default:
        break;
}

// Optional: plain Npgsql connection string (user secrets, appsettings.Development.json, or env).
// Env wins so CI/local shells can set: HIS_ORDER_PG_CONNECTION=Host=...;Password=...;
var connectionOverride = Environment.GetEnvironmentVariable("HIS_ORDER_PG_CONNECTION")
    ?? builder.Configuration.GetConnectionString("PostgreSql");
if (!string.IsNullOrWhiteSpace(connectionOverride))
    connectionString = connectionOverride.Trim();

connectionString = NormalizePostgreSqlConnectionString(connectionString);

builder.Services.AddDbContext<KMUContext>(options =>
    options.UseNpgsql(connectionString));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddScoped<KMU.HisOrder.MVC.Areas.BloodBank.Models.BloodBankService>();
builder.Services.AddScoped<KMU.HisOrder.MVC.Areas.BloodBank.Models.BloodBankIntegrationService>();

// Register Orthanc Worklist Service
builder.Services.AddScoped<OrthancWorklistService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IExamWorkflowService, ExamWorkflowService>();
builder.Services.Configure<AIAssistOptions>(builder.Configuration.GetSection(AIAssistOptions.SectionName));
builder.Services.PostConfigure<AIAssistOptions>(options =>
{
    var envKey = Environment.GetEnvironmentVariable("AIAssist__OpenAIApiKey")
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (!string.IsNullOrWhiteSpace(envKey))
    {
        options.OpenAIApiKey = envKey.Trim();
    }
});
builder.Services.AddScoped<IAIAssistService, AIAssistService>();
builder.Services.AddScoped<INcdAiContextService, NcdAiContextService>();
builder.Services.AddScoped<INonMedAiContextService, NonMedAiContextService>();
builder.Services.AddSingleton<PromptComposer>();
builder.Services.AddScoped<IClinicNcdAiAssistService, ClinicNcdAiAssistService>();
builder.Services.AddHttpClient("OrthancProxy", (sp, client) =>
    ConfigureOrthancHttpClient(client, sp.GetRequiredService<IConfiguration>()));
builder.Services.AddHttpClient("OrthancInternal", (sp, client) =>
    ConfigureOrthancHttpClient(client, sp.GetRequiredService<IConfiguration>()));
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(360);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

//builder.Services.ConfigureApplicationCookie(options => {

//    options.Cookie.Name = ".AspNetCore.Session";
//    options.ExpireTimeSpan = TimeSpan.FromSeconds(10);
//    options.LoginPath = new PathString("/Login/NotLogin");

//});

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.SameAsRequest;
    options.CheckConsentNeeded = _ => false;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option =>
{
    option.LoginPath = new PathString("/Login/NotLogin");
    option.AccessDeniedPath = new PathString("/Login/NotAuth");
    option.ExpireTimeSpan = TimeSpan.FromHours(6);
    option.SlidingExpiration = true;
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
    option.Cookie.SameSite = SameSiteMode.Lax;
    option.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            if (string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            if (string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    };
});

//全專案都適用登入驗證
//參考資料
//ASP.NET Core Web API 入門教學 - 使用 cookie 驗證但不使用 ASP.NET Core Identity（實作登入登出） | 凱哥寫程式's Blog | TalllKai
//https://blog.talllkai.com/ASPNETCore/2021/08/22/CookieAuthentication
builder.Services.AddMvc(options =>
{
    //如須例外排除不需要驗證，請加上[AllowAnonymous]
    options.Filters.Add(new AuthorizeFilter());
});
//全專案都適用登入驗證

//加入 SignalR 2023.01.03 add by elain
builder.Services.AddSignalR();


if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = (int)HttpStatusCode.PermanentRedirect;
        options.HttpsPort = 443;
    });
}

//builder.WebHost.UseSetting("https_port", "443");

//builder.WebHost.UseUrls("https://192.168.30.245:5588");
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<KMUContext>();
        KMU.HisOrder.MVC.Areas.BloodBank.Models.BloodBankProjectSeeder.EnsureBloodBankTables(db);
        KMU.HisOrder.MVC.Areas.BloodBank.Models.BloodBankProjectSeeder.EnsureBloodBankProjects(db);
        KMU.HisOrder.MVC.Areas.BloodBank.Models.BloodBankProjectSeeder.EnsureBloodBankAuths(db);
        KMU.HisOrder.MVC.Areas.BloodBank.Models.BloodBankProjectSeeder.EnsureBloodBankDonorSchema(db);
    }
    catch (Exception ex)
    {
        if (app.Environment.IsDevelopment())
            Console.WriteLine("Startup seed warning: " + ex.Message);
    }
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}


//2022.11.07 add by 1050325
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


var supportedCultures = new List<CultureInfo>()
            {
                new CultureInfo("zh"),
                new CultureInfo("en"),
            };

app.UseRequestLocalization(new RequestLocalizationOptions()
{

    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("zh"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
});


// In Development, skip HTTPS redirection so http://localhost:<port>/ works for every route
// (main app + e.g. /Radiology/RisCallings/Monitor). Otherwise HTTP requests get redirected to
// https://127.0.0.1/... with no port and the browser shows ERR_EMPTY_RESPONSE.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

//-----------------------
app.UseSession();
//順序要一樣
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
//-----------------------

app.UseMiddleware<OrthancProxyMiddleware>();

//app.Urls.AddRange(new List<string>() { "http://*:5000","https://localhost:9037/" });

//app.Urls.AddRange(new List<string>() { "http://*:5001", "https://*:443/" });


app.MapControllerRoute(
    name: "MyArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

//加入 Hub 2023.01.03 add by elain
app.MapHub<ChatHub>("/chatHub");

app.Run();

static string NormalizePostgreSqlConnectionString(string raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return raw;

    // Legacy HIS strings use "Server=" / "User ID="; Npgsql prefers Host/Username.
    var builder = new NpgsqlConnectionStringBuilder(raw)
    {
        SslMode = SslMode.Prefer
    };
    return builder.ConnectionString;
}

static void ConfigureOrthancHttpClient(HttpClient client, IConfiguration configuration)
{
    var baseUrl = configuration["OrthancSettings:ApiBaseUrl"]?.TrimEnd('/')
        ?? "http://localhost:8042";
    client.BaseAddress = new Uri(baseUrl + "/");
    client.Timeout = TimeSpan.FromMinutes(2);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var username = configuration["OrthancSettings:HttpUsername"]
        ?? Environment.GetEnvironmentVariable("ORTHANC_HTTP_USERNAME");
    var password = configuration["OrthancSettings:HttpPassword"]
        ?? Environment.GetEnvironmentVariable("ORTHANC_HTTP_PASSWORD");
    if (!string.IsNullOrWhiteSpace(username))
    {
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{username}:{password ?? string.Empty}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }
}

