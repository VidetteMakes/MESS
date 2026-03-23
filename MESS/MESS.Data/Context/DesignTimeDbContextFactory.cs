using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MESS.Data.Context
{
    /// <summary>
    /// Used to create a design time DbContext to run migrations via shell
    /// </summary>
    public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        /// <summary>
        /// Creates the design time DbContext
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public ApplicationContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<ApplicationContextFactory>() // <-- This line adds user secrets
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            var connectionString = configuration.GetConnectionString("MESSConnection");

            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Optional but recommended
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
            
            optionsBuilder.UseSnakeCaseNamingConvention();

            return new ApplicationContext(optionsBuilder.Options);
        }

    }
}