// Backend configuration.
// Starts and configures ASP.NET Core and registers the service and repository.

using Pocket_Librarian.Server.Repositories;
using Pocket_Librarian.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<PipelineRepository>();
builder.Services.AddScoped<PipelineService>();

/////// NEW CODE ///////
builder.Services.AddScoped<MemberProfileRepository>(); 
builder.Services.AddScoped<ReservationRepository>(); 
builder.Services.AddScoped<UserPreferencesRepository>(); 
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<NotificationRepository>(); 
/////// NEW CODE ///////


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

/////////////NEW CODE/////////////

// Creates database tables when the application starts
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
/////////////NEW CODE/////////////

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
