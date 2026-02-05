using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using OpenClaw.Windows.Models;

namespace OpenClaw.Windows.Services.Data
{
    public class ChatContextDb
    {
        private readonly string _dbPath;

        public ChatContextDb()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenClaw");
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, "messages.db");
        }

        public async Task InitializeAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = 
            @"
                CREATE TABLE IF NOT EXISTS Messages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Role TEXT NOT NULL,
                    Content TEXT,
                    ToolCallId TEXT,
                    Timestamp TEXT NOT NULL
                );
            ";
            await command.ExecuteNonQueryAsync();
        }

        public async Task SaveMessageAsync(string role, string content, string? toolCallId = null)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = 
            @"
                INSERT INTO Messages (Role, Content, ToolCallId, Timestamp)
                VALUES ($role, $content, $toolCallId, $timestamp);
            ";
            command.Parameters.AddWithValue("$role", role);
            command.Parameters.AddWithValue("$content", content ?? "");
            command.Parameters.AddWithValue("$toolCallId", (object?)toolCallId ?? DBNull.Value);
            command.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("O"));

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<ChatMessage>> GetRecentMessagesAsync(int count = 20)
        {
            var messages = new List<ChatMessage>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = 
            @"
                SELECT Role, Content FROM (
                    SELECT Role, Content, Timestamp FROM Messages
                    ORDER BY Timestamp DESC
                    LIMIT $count
                )
                ORDER BY Timestamp ASC;
            ";
            command.Parameters.AddWithValue("$count", count);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var role = reader.GetString(0);
                var content = reader.GetString(1);
                messages.Add(new ChatMessage(role, content));
            }

            return messages;
        }
    }
}
