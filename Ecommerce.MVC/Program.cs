using Ecommerce.MVC.Config;
using Ecommerce.MVC.Hubs;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PgSqlConnection"))
);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.LogoutPath = "/Cliente/Logout";
        options.AccessDeniedPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;

        options.Cookie.Name = "Ecommerce.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToAccessDenied = context =>
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                var redirectUrl = $"/Home/Index?acessoNegado=true&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
                context.Response.Redirect(redirectUrl);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("Asaas", (sp, c) =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var baseUrl = config["Asaas:BaseUrl"];
    var apiKey = config["Asaas:ApiKey"];
    var userAgent = config["Asaas:UserAgent"];

    c.BaseAddress = new Uri(baseUrl!);

    c.DefaultRequestHeaders.Add("access_token", apiKey);
    c.DefaultRequestHeaders.Add("User-Agent", userAgent);
    c.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSignalR();

builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<ICarrinhoService, CarrinhoService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";

        context.HttpContext.Response.Headers.Remove("Retry-After");

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "N�o foi poss�vel autenticar. Verifique os dados e tente novamente."
        }, cancellationToken: token);
    };

    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var email = (httpContext.Request.Query["email"].ToString() ?? string.Empty).Trim().ToLowerInvariant();
        var key = $"{ip}:{email}";

        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: key,
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 8,                     
                TokensPerPeriod = 2,                
                ReplenishmentPeriod = TimeSpan.FromSeconds(15),
                AutoReplenishment = true,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

//var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";
//builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapHub<PagamentoHub>("/hubs/pagamento");

app.Run();
