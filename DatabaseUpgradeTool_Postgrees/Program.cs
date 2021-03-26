using System;
using System.Collections.Generic;
using DatabaseVersioningTool;
using System.Configuration;
using System.IO;
using Configuration;
using Configuration.Sdk;
using Microsoft.Extensions.Configuration;

namespace DatabaseUpgradeTool_Postgrees
{
    class Program
    {
        static void Main(string[] args)
        {
            //ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings["Main"];
            var config=GetConfig();
            var connectionString= config.GetConnectionString("DefaultConnection");
            if (connectionString == null)
            {
                Console.WriteLine("Please add a connection string with the 'Main' key");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Press 1 to execute updates");
            if (Console.ReadKey().KeyChar != '1')
                return;

            Console.WriteLine();
            Console.WriteLine();

            IReadOnlyList<string> output = new VersionManager(connectionString).ExecuteMigrations();
            foreach (string str in output)
            {
                Console.WriteLine(str);
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        
        private static IConfiguration GetConfig()
        {
            var path = JsonConfigLib.FindPath2ConfigFile("DatabaseUpgradeTool_Postgrees");
            var config = JsonConfigLib.GetConfiguration(path);
            return config;
            
            // var builder = new ConfigurationBuilder()
            //     .SetBasePath("")
            //     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //     .AddEnvironmentVariables();
            // return builder.Build();
        }
    }
}