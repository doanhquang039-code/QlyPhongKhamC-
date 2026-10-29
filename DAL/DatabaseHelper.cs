using System;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;

namespace QlyPhongKham.DAL
{
    public static class DatabaseHelper
    {
        private static readonly string ConfigDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QlyPhongKham");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectoryPath, "dbconfig.txt");
        private static string _connectionString = string.Empty;

        public static string ServerIp { get; set; } = ".";
        public static string DatabaseName { get; set; } = "QL_PhongKham";
        public static string Username { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;

        static DatabaseHelper()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                foreach (string line in File.ReadAllLines(ConfigFilePath))
                {
                    string[] parts = line.Split('=', 2);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim().ToLowerInvariant();
                    string value = parts[1].Trim();
                    if (key == "server") ServerIp = value;
                    else if (key == "database") DatabaseName = value;
                    else if (key == "username") Username = value;
                    else if (key == "password") Password = value;
                }
            }

            _connectionString = BuildConnectionString(ServerIp, DatabaseName, Username, Password);
        }

        public static void SaveConfig(string server, string database, string username, string password)
        {
            string connectionString = BuildConnectionString(server, database, username, password);
            Directory.CreateDirectory(ConfigDirectoryPath);
            File.WriteAllLines(ConfigFilePath, new[]
            {
                $"server={server}",
                $"database={database}",
                $"username={username}",
                $"password={password}"
            });

            ServerIp = server;
            DatabaseName = database;
            Username = username;
            Password = password;
            _connectionString = connectionString;
        }

        private static string BuildConnectionString(string server, string database, string username, string password)
        {
            return new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                UserID = username,
                Password = password,
                TrustServerCertificate = true,
                ConnectTimeout = 5
            }.ConnectionString;
        }

        public static string ConnectionString => _connectionString;

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public static bool TestConnection(out string error)
            => TestConnection(ServerIp, DatabaseName, Username, Password, out error);

        public static bool TestServerConnection(string server, string username, string password, out string error)
        {
            error = string.Empty;
            try
            {
                using var connection = new SqlConnection(BuildConnectionString(server, "master", username, password));
                connection.Open();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TestConnection(string server, string database, string username, string password, out string error)
        {
            error = string.Empty;
            try
            {
                using var connection = new SqlConnection(BuildConnectionString(server, database, username, password));
                connection.Open();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            var dataTable = new DataTable();
            using var connection = GetConnection();
            using var command = new SqlCommand(query, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            return dataTable;
        }

        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(query, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);
            connection.Open();
            return command.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string query, SqlParameter[]? parameters = null)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(query, connection);
            if (parameters != null)
                command.Parameters.AddRange(parameters);
            connection.Open();
            return command.ExecuteScalar();
        }
    }
}
