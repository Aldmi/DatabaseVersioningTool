using System;
using System.Threading.Tasks;
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

            var output = await new VersionManager(connectionString).ExecuteMigrations();
            foreach (string str in output.Value)
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
        }
    }
}