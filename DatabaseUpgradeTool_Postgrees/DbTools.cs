using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dapper;
using Npgsql;

namespace DatabaseUpgradeTool_Postgrees
{
    public class DbTools
    {
        private readonly string _connectionString;
        public DbTools(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        
        public async Task<Result> ExecuteAsync(string script)
        {
            try
            {
                await using var cn = new NpgsqlConnection(_connectionString);
                cn.Open();
                await cn.ExecuteAsync(script);
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure(e.Message);
            }
        }
        
        public async Task<Result> ExecuteAsync(string script, object param)
        {
            try
            {
                await using var cn = new NpgsqlConnection(_connectionString);
                cn.Open();
                await cn.ExecuteAsync(script, param);
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Failure(e.Message);
            }
        }
        
        public async Task<Result<T>> QueryFirstAsync<T>(string script)
        {
            try
            {
                await using var cn = new NpgsqlConnection(_connectionString);
                cn.Open();
                var res= await cn.QueryFirstAsync<T>(script);
                return res;
            }
            catch (Exception e)
            {
                return Result.Failure<T>(e.Message);
            }
        }
        
        //TODO: Изучить транзакции, возможно запись в Setting делать тут же, в пределах транзакции
        public void ExecuteMigration(string commandText)
        {
            Regex regex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string[] subCommands = regex.Split(commandText);

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            NpgsqlTransaction transaction = connection.BeginTransaction();
            using (var cmd = connection.CreateCommand())
            {
                cmd.Connection = connection;
                cmd.Transaction = transaction;

                foreach (string command in subCommands)
                {
                    if (command.Length <= 0)
                        continue;

                    cmd.CommandText = command;
                    cmd.CommandType = CommandType.Text;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            try
            {
                transaction.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
