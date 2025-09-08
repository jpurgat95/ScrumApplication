using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScrumApplication.Data;
using System.IO;

public class TestDependencyInjectionFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public TestDependencyInjectionFixture()
    {
        var services = new ServiceCollection();

        // Wczytaj konfigurację z appsettings.json (przykład)
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<ScrumDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
