﻿namespace DaGetV2.Api
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.Configuration;
    using System.IO;
    using Infrastructure.Data;

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerDaGetContext>
    {
        /// <summary>
        /// Create DB context
        /// </summary>
        /// <param name="args"></param>
        /// <returns>DB context</returns>
        public SqlServerDaGetContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var builder = new DbContextOptionsBuilder<SqlServerDaGetContext>();
            var connectionString = configuration.GetConnectionString("DaGetConnexionString");
            builder.UseSqlServer(connectionString, b => b.MigrationsAssembly("DaGetV2.Api"));
            return new SqlServerDaGetContext(builder.Options);
        }
    }
}