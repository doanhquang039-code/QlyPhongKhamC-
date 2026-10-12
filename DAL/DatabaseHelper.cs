using System;
using System.IO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace QlyPhongKham.DAL
{
    public static class DatabaseHelper
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.txt");
        private static string _connectionString = string.Empty;

        public static string ServerIp { get; set; } = "127.0.0.1";
        public static string DatabaseName { get; set; } = "doanhbtlc#";
        public static string Username { get; set; } = "sa";
        public static string Password { get; set; } = "123456789";

        static DatabaseHelper()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    var lines = File.ReadAllLines(ConfigFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim().ToLower();
                            var val = parts[1].Trim();
                            if (key == "server") ServerIp = val;
                            else if (key == "database") DatabaseName = val;
                            else if (key == "username") Username = val;
                            else if (key == "password") Password = val;
                        }
                    }
                }
                catch { }
            }
            BuildConnectionString();
        }

        public static void SaveConfig(string server, string database, string username, string password)
        {
            ServerIp = server;
            DatabaseName = database;
            Username = username;
            Password = password;
            BuildConnectionString();

            try
            {
                File.WriteAllLines(ConfigFilePath, new[]
                {
                    $"server={ServerIp}",
                    $"database={DatabaseName}",
                    $"username={Username}",
                    $"password={Password}"
                });
            }
            catch { }
        }

        private static void BuildConnectionString()
        {
            _connectionString = $"Server={ServerIp};Database={DatabaseName};User Id={Username};Password={Password};TrustServerCertificate=True;Connect Timeout=5;";
        }

        public static string ConnectionString => _connectionString;

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public static bool TestConnection(out string error)
        {
            error = string.Empty;
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            var dt = new DataTable();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(query, conn);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(query, conn);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string query, SqlParameter[]? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new SqlCommand(query, conn);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteScalar();
        }
    }
}
