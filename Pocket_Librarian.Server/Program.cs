// Backend configuration.
// Starts and configures ASP.NET Core and registers the service and repository.

using Pocket_Librarian.Server.Repositories;
using Pocket_Librarian.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<PipelineRepository>();
builder.Services.AddScoped<PipelineService>();
builder.Services.AddScoped<MemberProfileRepository>();
builder.Services.AddScoped<ReservationRepository>();
builder.Services.AddScoped<UserPreferencesRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<NotificationRepository>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

/////New Code/////

// Creates the BookDonations table when the application starts.
using (var scope = app.Services.CreateScope())
{
    var donationRepository =
        scope.ServiceProvider
            .GetRequiredService<DonationRepository>();

    await donationRepository.EnsureDonationsTableAsync();
}

/////New Code/////

using (var scope = app.Services.CreateScope())
{
    // Creates the UserPreferences table
    var preferencesRepository =
        scope.ServiceProvider
            .GetRequiredService<UserPreferencesRepository>();

    await preferencesRepository
        .EnsurePreferencesTableAsync();

    // Creates the Notifications table
    var notificationRepository =
        scope.ServiceProvider
            .GetRequiredService<NotificationRepository>();

    await notificationRepository
        .EnsureNotificationsTableAsync();

    // Creates the Messages table
    var messageRepository =
        scope.ServiceProvider
            .GetRequiredService<MessageRepository>();

    await messageRepository
        .EnsureMessagesTableAsync();
}

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    /////New Code/////
    
    app.UseHttpsRedirection();

    // Identifies the logged-in user.
    app.UseAuthentication();

    // Checks whether the user has permission.
    app.UseAuthorization();

    // Enables controller endpoints.
    app.MapControllers();

    /////New Code/////

    app.MapFallbackToFile("/index.html");

    app.Run();
}
