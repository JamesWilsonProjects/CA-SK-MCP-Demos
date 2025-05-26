var builder = WebApplication.CreateBuilder(args);

// Explicitly load appsettings.Development.json
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);

// Register services
builder.Services.AddSingleton<KernelSetupService>();
builder.Services.AddControllers();

var app = builder.Build();

// Serve default files like index.html
app.UseDefaultFiles();
// Serve static files from wwwroot
app.UseStaticFiles();

// Map controller endpoints
app.MapControllers();

app.Run();