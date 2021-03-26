using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DatabaseUpgradeTool_Postgrees
{
    public static class JsonConfigLib
    {
        public static IConfigurationRoot GetConfiguration(string basePath, string fileName = "appsettings.json")
        {
            var builder = new ConfigurationBuilder();
            // установка пути к каталогу
            builder.SetBasePath(basePath);
            // получаем конфигурацию из файла appsettings.json
            builder.AddJsonFile(fileName);
            // создаем конфигурацию
            var config = builder.Build();
            return config;
        }
        
        
        public static string FindPath2ConfigFile(string rootFolderName)
        {
            var cd = Directory.GetCurrentDirectory();
            var rootFolderIndex=cd.IndexOf(rootFolderName, StringComparison.Ordinal) + rootFolderName.Length;
            var res = cd.Remove(rootFolderIndex);
            var newPAth = Path.Combine(res, "DatabaseUpgradeTool_Postgrees");
            return newPAth;
        }
    }
}