using Web_Dinosaurios.Data;
using Web_Dinosaurios.Repositories;
using Web_Dinosaurios.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Acceso a datos: fábrica de conexiones SQLite (singleton).
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();

// Repositorios (una instancia por petición).
builder.Services.AddScoped<IDinosaurioRepository, DinosaurioRepository>();

// Cliente HTTP tipado de la Paleobiology Database (importación del catálogo).
builder.Services.AddHttpClient<PbdbCliente>(cliente =>
{
    cliente.Timeout = TimeSpan.FromSeconds(120);
});

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
    pattern: "{controller=Dinosaurios}/{action=Index}/{id?}");

app.Run();
