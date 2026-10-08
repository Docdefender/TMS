using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TMS.Data;

// Schema tooling must not run Program's startup seed or connect to the production database.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = config.GetConnectionString("DefaultConnection");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            string.IsNullOrWhiteSpace(connectionString)
                ? "Server=(localdb)\\mssqllocaldb;Database=DizgeDesign;Trusted_Connection=True"
                : connectionString).Options);
    }
}
