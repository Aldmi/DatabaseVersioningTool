using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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


        public IReadOnlyList<string> ExecuteMigrations()
        {
            var output = new List<string>();
            int currentVersion = GetCurrentVersion();
            output.Add("Current DB schema version is " + currentVersion);

            IReadOnlyList<Migration> migrations = GetNewMigrations(currentVersion);
            output.Add(migrations.Count + " migration(s) found");

            int? duplicatedVersion = GetDuplicatedVersion(migrations);
            if (duplicatedVersion != null)
            {
                output.Add("Non-unique migration found: " + duplicatedVersion);
                return output;
            }

            foreach (Migration migration in migrations)
            {
                _dbTools.ExecuteMigration4Npgsql(migration.GetContent());
                UpdateVersion(migration.Version);
                output.Add("Executed migration: " + migration.Name);
            }

            if (!migrations.Any())
            {
                output.Add("No updates for the current schema version");
            }
            else
            {
                int newVersion = migrations.Last().Version;
                output.Add("New DB schema version is " + newVersion);
            }

            return output;
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


        private async void UpdateVersion(int newVersion)
        {
            const string query = @"
                    UPDATE Settings SET Value = @Version WHERE Name = 'Version'
                    ";

           await _dbTools.ExecuteAsync(query, new {Version = newVersion.ToString()});
            //_dbTools.ExecuteNonQuery(query, new SqlParameter("Version", newVersion.ToString()));
        }


        private IReadOnlyList<Migration> GetNewMigrations(int currentVersion)
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


        private int GetCurrentVersion()
        {
            if (!SettingsTableExists())
            {
                CreateSettingsTable();
                return 0;
            }

            return GetCurrentVersionFromSettingsTable();
        }


        private int GetCurrentVersionFromSettingsTable()
        {
            const string query = @"
                    SELECT Value FROM Settings WHERE Name = 'Version'
                    ";
            var res = _dbTools.QueryFirstAsync<string>(query).GetAwaiter().GetResult();
            return int.Parse(res.Value);
        }


        private void CreateSettingsTable()
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
             var res=_dbTools.ExecuteAsync(query).GetAwaiter().GetResult();;
        }


        private bool SettingsTableExists()
        {
            string query = @"
                        SELECT EXISTS
                        (
	                        SELECT true
	                        FROM pg_tables
	                        WHERE tablename = 'settings'
                        );";
            var res = _dbTools.QueryFirstAsync<bool>(query).GetAwaiter().GetResult();
            return res.Value;
        }
    }
}
