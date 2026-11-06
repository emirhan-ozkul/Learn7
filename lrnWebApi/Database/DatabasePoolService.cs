using System;
using lrnWebApi.Services;
using lrnWebApi.SettingCS;
using Microsoft.Data.Sqlite;

namespace lrnWebApi.Database;

public class DatabasePoolService
{
    public static int CountPool(string topic, string level)
    {
        int count = 0;
        using(var conn = new SqliteConnection(DatabaseSettings.ConnectionString))
        {
            conn.Open();
            string commandCount = "SELECT COUNT(*) FROM sentences WHERE Topic = @topic AND Level = @level AND IsUsed = 0";
            using(var commandCountPool = new SqliteCommand(commandCount, conn))
            {
                commandCountPool.Parameters.AddWithValue("@topic",topic);
                commandCountPool.Parameters.AddWithValue("@level", level);
                int countSentences = Convert.ToInt32(commandCountPool.ExecuteScalar());
                count = countSentences;
            }
        }
        return count;
    }
}
