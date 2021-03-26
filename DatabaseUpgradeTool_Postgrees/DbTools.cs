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
        
        

        public void ExecuteNonQuery(string commandText, params SqlParameter[] parameters)
        {
            using (SqlConnection cnn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand(commandText, cnn)
                {
                    CommandType = CommandType.Text
                };
                foreach (SqlParameter parameter in parameters)
                {
                    cmd.Parameters.Add(parameter);
                }

                cnn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void ExecuteMigration4Npgsql(string commandText)
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
