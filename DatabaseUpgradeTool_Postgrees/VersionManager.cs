using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace DatabaseUpgradeTool_Postgrees
{
    public class VersionManager
    {
        private readonly DbTools _dbTools;
        private readonly string _migrationsDirectory;
        

        public VersionManager(string connectionString, string migrationsDirectory = @"Migrations\")
        {
            _dbTools = new DbTools(connectionString);
            _migrationsDirectory = migrationsDirectory;
        }
        
        public async Task<Result<IEnumerable<string>>>ExecuteMigrations()
        {
            var output = new List<string>();
            var res = await GetCurrentVersion()
                .Bind(currentVersion => 
                {
                    output.Add("Current DB schema version is " + currentVersion);
                    return GetNewMigrations(currentVersion);
                })
                .Bind(migrations => 
                {
                    output.Add(migrations.Count + " migration(s) found");
                    int? duplicatedVersion = GetDuplicatedVersion(migrations);
                    return duplicatedVersion != null ? Result.Failure<IReadOnlyList<Migration>>("Non-unique migration found: " + duplicatedVersion) : Result.Success(migrations);
                })
                .Bind(async migrations =>
                {
                    var mirgationResults = new List<Result>();
                    foreach (var migration in migrations)
                    {
                         var mirgationRes=await _dbTools.ExecuteMigration(migration.GetContent()).Bind(() => UpdateVersion(migration.Version));
                         mirgationResults.Add(mirgationRes);
                         output.Add("Executed migration: " + migration.Name);
                    }
                    return Result.Combine(mirgationResults).Map(() => migrations);
                })
                .Bind(migrations =>
                {
                    if (!migrations.Any())
                    {
                        output.Add("No updates for the current schema version");
                    }
                    else
                    {
                        int newVersion = migrations.Last().Version;
                        output.Add("New DB schema version is " + newVersion);
                    }
                    return Result.Success();
                })
                .Map(() => output as IEnumerable<string>);
            
            return res;
        }


        private int? GetDuplicatedVersion(IReadOnlyList<Migration> migrations)
        {
            int duplicatedVersion = migrations
                .GroupBy(x => x.Version)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .FirstOrDefault();

            return duplicatedVersion == 0 ? (int?)null : duplicatedVersion;
        }


        private async Task<Result> UpdateVersion(int newVersion)
        {
            const string query = @"
                    UPDATE Settings SET Value = @Version WHERE Name = 'Version'
                    ";

           return await _dbTools.ExecuteAsync(query, new {Version = newVersion.ToString()});
        }


        private Result<IReadOnlyList<Migration>> GetNewMigrations(int currentVersion)
        {
            // 01_MyMigration.sql
            var regex = new Regex(@"^(\d)*_(.*)(sql)$");
            return new DirectoryInfo(_migrationsDirectory)
                .GetFiles()
                .Where(x => regex.IsMatch(x.Name))
                .Select(x => new Migration(x))
                .Where(x => x.Version > currentVersion)
                .OrderBy(x => x.Version)
                .ToList();
        }


        private async Task<Result<int>> GetCurrentVersion()
        {
            var res = await SettingsTableExists()
                .Bind(isExist => !isExist ? CreateSettingsTable().Map(() => 0) : GetCurrentVersionFromSettingsTable());
            return res;
        }


        private async Task<Result<int>> GetCurrentVersionFromSettingsTable()
        {
            const string query = @"
                    SELECT Value FROM Settings WHERE Name = 'Version'
                    ";
            var res = await _dbTools.QueryFirstAsync<int>(query);
            return res;
        }


        private async Task<Result> CreateSettingsTable()
        {
            const string scriptCreateTable = @"
                CREATE TABLE Settings
                (
                    Name varchar(50) NOT NULL PRIMARY KEY,
                    Value varchar(500) NOT NULL
                );
                ";
            const string scriptInit = @"
               INSERT INTO Settings (Name, Value)  VALUES ('Version', '0');
               ";
             const string query = scriptCreateTable + scriptInit;
             var res=await _dbTools.ExecuteAsync(query);
             return res;
        }


        private async Task<Result<bool>> SettingsTableExists()
        {
            const string query = @"
                        SELECT EXISTS
                        (
	                        SELECT true
	                        FROM pg_tables
	                        WHERE tablename = 'settings'
                        );";
            var res = await _dbTools.QueryFirstAsync<bool>(query);
            return res;
        }
    }
}
