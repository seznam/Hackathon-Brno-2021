using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CommentApi.Sql
{
    public class MigrateSqlService : IHostedService
    {
        private readonly Func<NpgsqlConnection> _npgsqlConnectionFactory;

        public MigrateSqlService(Func<NpgsqlConnection> npgsqlConnectionFactory)
        {
            _npgsqlConnectionFactory = npgsqlConnectionFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var createDatabase = LoadFile("CommentApi.Sql.CreateDatabase.sql");


            var databse = new NpgsqlConnection("User ID=postgres;Password=myPassword;Host=localhost;Port=5432");
            await databse.OpenAsync();

            using (var cmd = new NpgsqlCommand(createDatabase, databse))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await databse.CloseAsync();
            await databse.DisposeAsync();

            await using var connection = _npgsqlConnectionFactory();
            await connection.OpenAsync();
            
            var createTable = LoadFile("CommentApi.Sql.CreateTable.sql");

            using (var cmd = new NpgsqlCommand(createTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static string LoadFile(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}