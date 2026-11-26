// Backend configuration.
// Starts and configures ASP.NET Core and registers the service and repository.

using Microsoft.AspNetCore.Authentication.Cookies;
using Pocket_Librarian.BLL.Interfaces;
using Pocket_Librarian.BLL.Services;
using Pocket_Librarian.DAL.Repositories;
using Pocket_Librarian.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSingleton<IPipelineRepository, PipelineRepository>();
builder.Services.AddScoped<PipelineService>();

builder.Services.AddSingleton<IAuthenticationRepository, AuthenticationRepository>();
builder.Services.AddScoped<AuthenticationService>();

builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<ReservationService>();

builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
builder.Services.AddScoped<MemberProfileService>();

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<NotificationService>();

builder.Services.AddScoped<IReturnsQueueRepository, ReturnsQueueRepository>();
builder.Services.AddScoped<ReturnsQueueService>();

builder.Services.AddScoped<IFulfillmentQueueRepository, FulfillmentQueueRepository>();
builder.Services.AddScoped<FulfillmentQueueService>();

// Registers the catalogue repository implementation and business service.
builder.Services.AddSingleton<ICatalogueRepository, CatalogueRepository>();
builder.Services.AddScoped<CatalogueService>();

builder.Services.AddScoped<IBookDonationRepository, BookDonationRepository>();
builder.Services.AddScoped<BookDonationService>();
builder.Services.AddScoped<IDonationRepository, DonationRepository>();
builder.Services.AddScoped<DonationService>();
builder.Services.AddScoped<IBookCopyRepository, BookCopyRepository>();
builder.Services.AddScoped<BookCopyService>();
builder.Services.AddScoped<CatalogueAdminService>();
builder.Services.AddSingleton<CoverImageStorage>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PocketLibrarian.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
