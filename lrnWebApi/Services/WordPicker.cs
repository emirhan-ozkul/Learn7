using System;
using System.Data.SqlTypes;
using System.Text;
using lrnWebApi.SettingCS;
using Microsoft.Data.Sqlite;

namespace lrnWebApi.Services;

public class WordPicker
{
    public static async Task<WordDTO?> PickWord(string topic, string level)
    {
        string pickqr = "SELECT Id, renglish, turkish FROM sentences WHERE IsUsed = 0 AND topic = @topic AND level = @level ORDER BY RANDOM() LIMIT 1";

        using var conn = new SqliteConnection(DatabaseSettings.ConnectionString);
        await conn.OpenAsync();

        using var pick = new SqliteCommand(pickqr, conn);
        pick.Parameters.AddWithValue("@topic", topic);
        pick.Parameters.AddWithValue("@level", level);

        using var reader = await pick.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            Console.WriteLine("reader boş");
            return null;
        }

        var question = new WordDTO
        {
            id = reader.GetInt32(0),
            renglish = reader.GetString(1),
            turkish = reader.GetString(2)
        };

        return question;
    }

    public class WordDTO
    {
        public int id { get; set; } = 0;
        public string renglish { get; set; } = string.Empty;
        public string turkish { get; set; } = string.Empty;
    }
}

