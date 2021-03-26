using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Configuration;

namespace DatabaseUpgradeTool_Postgrees
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var config=GetConfig();
            var connectionString= config.GetConnectionString("DefaultConnection");
            if (connectionString == null)
            {
                Console.WriteLine("Please add a connection string with the 'Main' key");
                Console.ReadKey();
                return;
            }

            // Console.WriteLine("Press 1 to execute updates");
            // if (Console.ReadKey().KeyChar != '1')
            //     return;

            Console.WriteLine();
            Console.WriteLine();

            await new VersionManager(connectionString).ExecuteMigrations()
                .Finally(result =>
                {
                    if (result.IsFailure)
                    {
                        Console.WriteLine(result.Error);
                    }
                    else
                    {
                        foreach (var str in result.Value)
                        {
                            Console.WriteLine(str);
                        }
                    }
                    return result;
                });
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        
        private static IConfiguration GetConfig()
        {
            var path = JsonConfigLib.FindPath2ConfigFile("DatabaseUpgradeTool_Postgrees");
            var config = JsonConfigLib.GetConfiguration(path);
            return config;
        }
    }
}