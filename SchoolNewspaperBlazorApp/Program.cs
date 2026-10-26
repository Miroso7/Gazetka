using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using SchoolNewspaperBlazorApp.Components;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;
using SchoolNewspaperBlazorApp.Repository;
using SchoolNewspaperBlazorApp.Service;
using System.Security.Claims;

namespace SchoolNewspaperBlazorApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            //sql connect
            builder.Services.AddDbContextFactory<NewspaperDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("NewspaperConnectionString")));

            //Repositry
            builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
            builder.Services.AddScoped<IFileRepository, FileRepository>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
            builder.Services.AddScoped<ISchoolDataRepository, SchoolDataRepository>();
            builder.Services.AddScoped<ISubstitutionRepository, SubstitutionRepository>();


            //Services

            builder.Services.AddScoped<IArticleService, ArticleService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPermissionService, PermissionService>();
            builder.Services.AddScoped<ISchoolDataService, SchoolDataService>();
            builder.Services.AddScoped<ISubstitutionService, SubstitutionService>();
            builder.Services.AddScoped<NotificationService>();

            // sesje logowania: pozwalają unieważnić logowanie także w otwartych obwodach Blazora
            builder.Services.AddSingleton<ISessionRegistry, SessionRegistry>();
            builder.Services.AddScoped<AuthenticationStateProvider, SessionRevalidatingAuthenticationStateProvider>();

            builder.Services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.Cookie.Name = "SchoolNewspaper.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                    options.SlidingExpiration = true;
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        // cookie bez aktywnej sesji w rejestrze (wylogowane, wygasłe, sprzed restartu) jest odrzucane
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<ISessionRegistry>();
                        var sessionId = context.Principal?.FindFirst(SessionRegistry.SessionClaimType)?.Value;
                        if (!sessions.IsActive(sessionId))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }
                        sessions.Touch(sessionId);
                    };
                });
            builder.Services.AddAuthorization(options =>
            {
                // konto z wymuszoną zmianą hasła jest zalogowane, ale do czasu zmiany nie wejdzie na chronione strony
                options.AddPolicy(AuthConstants.FullAccessPolicy, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => !context.User.HasClaim(c => c.Type == AuthConstants.MustChangePasswordClaim)));
            });
            builder.Services.AddCascadingAuthenticationState();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");

                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider
                (@"C:\Users\Uczeń2026\source\repos\Gazetka\Images"),
                RequestPath = "/Images",
                OnPrepareResponse = ctx =>
                {
                    // wgrane pliki nigdy nie mogą działać jako dokument z własnym kodem (stare pliki HTML/SVG też)
                    ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    ctx.Context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
                }
            });

            // Uwierzytelnianie musi być przed UseAntiforgery i endpointami,
            // inaczej serwer nie odczyta cookie i użytkownik zawsze będzie "niezalogowany".
            app.UseAuthentication();
            app.UseAuthorization();

            // Konto z wymuszoną zmianą hasła widzi tylko stronę zmiany hasła (i wylogowanie).
            app.Use(async (context, next) =>
            {
                if (context.User.HasClaim(c => c.Type == AuthConstants.MustChangePasswordClaim)
                    && !IsAllowedBeforePasswordChange(context.Request.Path))
                {
                    if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                    {
                        context.Response.Redirect("/change-password");
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    }
                    return;
                }
                await next();
            });

            app.Use(async (context, next) =>
            {
                if (HttpMethods.IsGet(context.Request.Method)
                    && context.Request.Path.Equals("/login", StringComparison.OrdinalIgnoreCase)
                    && context.User.Identity?.IsAuthenticated == true)
                {
                    context.Response.Redirect("/adminPanel");
                    return;
                }
                await next();
            });
            app.UseAntiforgery();

            app.MapPost("/auth/login", async (
                HttpContext context,
                IAuthService authService,
                ISessionRegistry sessions,
                ILogger<Program> logger) =>
            {
                // żądanie musi pochodzić z formularza z naszej strony (token antiforgery)
                if (!await IsAntiforgeryValidAsync(context))
                {
                    return Results.Redirect("/login");
                }

                var form = await context.Request.ReadFormAsync();
                string email = form["Email"].ToString().Trim();
                string password = form["Password"].ToString();

                var login = await authService.LoginAsync(email, password);
                if (!login.Success)
                {
                    logger.LogWarning("Nieudane logowanie z adresu {RemoteIp}.", context.Connection.RemoteIpAddress);
                    return Results.Redirect("/login?error=1");
                }

                // stara sesja z tej przeglądarki (jeśli była) przestaje być ważna
                sessions.Revoke(context.User.FindFirst(SessionRegistry.SessionClaimType)?.Value);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(SessionRegistry.SessionClaimType, sessions.Create(email))
                };
                if (login.MustChangePassword)
                {
                    claims.Add(new Claim(AuthConstants.MustChangePasswordClaim, "1"));
                }
                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);

                // pierwsze logowanie: najpierw ustawienie własnego hasła
                if (login.MustChangePassword)
                {
                    return Results.Redirect("/change-password");
                }

                // parametr wyświetla w panelu komunikat „Zalogowano pomyślnie"
                return Results.Redirect("/adminPanel?zalogowano=1");
            });

            app.MapPost("/auth/change-password", async (
                HttpContext context,
                IAuthService authService,
                ISessionRegistry sessions,
                ILogger<Program> logger) =>
            {
                if (context.User.Identity?.IsAuthenticated != true)
                {
                    return Results.Redirect("/login");
                }

                if (!await IsAntiforgeryValidAsync(context))
                {
                    return Results.Redirect("/change-password");
                }

                var form = await context.Request.ReadFormAsync();
                string email = context.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

                var result = await authService.ChangePasswordAsync(
                    email,
                    form["CurrentPassword"].ToString(),
                    form["NewPassword"].ToString(),
                    form["ConfirmPassword"].ToString());
                if (result != ChangePasswordResult.Success)
                {
                    logger.LogWarning("Nieudana zmiana hasła ({Result}) z adresu {RemoteIp}.", result, context.Connection.RemoteIpAddress);
                    return Results.Redirect($"/change-password?error={result}");
                }

                // po zmianie hasła kończymy wszystkie dotychczasowe sesje konta (także na innych urządzeniach)
                // i logujemy tę przeglądarkę od nowa, już bez wymogu zmiany hasła
                sessions.Revoke(context.User.FindFirst(SessionRegistry.SessionClaimType)?.Value);
                sessions.RevokeAllForUser(email);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(SessionRegistry.SessionClaimType, sessions.Create(email))
                };
                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                // parametr wyświetla w panelu komunikat „Hasło zostało zmienione"
                return Results.Redirect("/adminPanel?haslo=1");
            });

            app.MapPost("/auth/logout", async (HttpContext context, ISessionRegistry sessions) =>
            {
                if (!await IsAntiforgeryValidAsync(context))
                {
                    return Results.Redirect("/");
                }

                // unieważnienie sesji wylogowuje też otwarte obwody Blazora, nie tylko to cookie
                sessions.Revoke(context.User.FindFirst(SessionRegistry.SessionClaimType)?.Value);
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }

        // Ścieżki dostępne dla konta, które musi jeszcze zmienić hasło (strona zmiany hasła, wylogowanie, zasoby Blazora).
        private static bool IsAllowedBeforePasswordChange(PathString path) =>
            path.Equals("/change-password", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/change-password", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth/logout", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/Error", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase);

        private static async Task<bool> IsAntiforgeryValidAsync(HttpContext context)
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            try
            {
                await antiforgery.ValidateRequestAsync(context);
                return true;
            }
            catch (AntiforgeryValidationException)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Odrzucono żądanie {Path} z adresu {RemoteIp}: nieprawidłowy token antiforgery.",
                    context.Request.Path, context.Connection.RemoteIpAddress);
                return false;
            }
        }
    }
}
