using Web_Dinosaurios.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Acceso a datos: fábrica de conexiones SQLite (singleton).
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();

var app = builder.Build();

// Base de datos lista (App_Data, esquema y semilla) antes de atender peticiones.
await DatabaseInitializer.InicializarAsync(app.Configuration);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
