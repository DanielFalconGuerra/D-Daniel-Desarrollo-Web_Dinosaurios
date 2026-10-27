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

// Cliente HTTP tipado de Wikipedia (descripciones del catálogo importado).
builder.Services.AddHttpClient<WikipediaCliente>(cliente =>
{
    cliente.Timeout = TimeSpan.FromSeconds(60);
});

// Importador del catálogo PBDB (lo usa "dotnet run -- importar-pbdb").
builder.Services.AddTransient<ImportadorPbdb>();

var app = builder.Build();

// Base de datos lista (App_Data, esquema y semilla) antes de atender peticiones.
await DatabaseInitializer.InicializarAsync(app.Configuration);

// Modo importación: "dotnet run -- importar-pbdb".
// Inicializa la base (ya hecho arriba), ejecuta la importación y termina sin
// levantar la web. El código de salida indica si la importación tuvo éxito.
if (args.Contains("importar-pbdb", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var importador = scope.ServiceProvider.GetRequiredService<ImportadorPbdb>();

    try
    {
        await importador.ImportarAsync();
        Environment.ExitCode = 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Error durante la importación PBDB:");
        Console.Error.WriteLine(ex);
        Environment.ExitCode = 1;
    }

    return;
}

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
