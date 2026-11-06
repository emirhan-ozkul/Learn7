using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using lrnWebApi.Models;
using lrnWebApi.SettingCS;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
namespace lrnWebApi.Services;

public class DbSaver
{
    public static async Task SaveAsyncData(List<SentenceDto> responseList)
    {
        using (var conn = new SqliteConnection(DatabaseSettings.ConnectionString))
        {
            conn.Open();
            string insertCommand = "INSERT INTO sentences (english, Renglish, turkish, grammar, topic, level) VALUES (@eng, @Reng, @tr, @grammar, @topic, @level)";
            using (var sqlinsertCommand = new SqliteCommand(insertCommand, conn))
            {
                // Parametreleri döngüden önce bir kez tanımlıyoruz
                try
                {
                    sqlinsertCommand.Parameters.Add("@eng", SqliteType.Text);
                    sqlinsertCommand.Parameters.Add("@Reng", SqliteType.Text);
                    sqlinsertCommand.Parameters.Add("@tr", SqliteType.Text);
                    sqlinsertCommand.Parameters.Add("@grammar", SqliteType.Text);
                    sqlinsertCommand.Parameters.Add("@topic", SqliteType.Text);
                    sqlinsertCommand.Parameters.Add("@level", SqliteType.Text);

                    foreach(var sentence in responseList)
                    {
                        // Döngü içinde değerleri güncelliyoruz (AddWithValue yerine Value ataması)
                        sqlinsertCommand.Parameters["@eng"].Value = sentence.English ?? (object)DBNull.Value;
                        sqlinsertCommand.Parameters["@Reng"].Value = SentenceSplitter.SentenceShuffer(sentence.English!) ?? (object)DBNull.Value;
                        sqlinsertCommand.Parameters["@tr"].Value = sentence.Turkish ?? (object)DBNull.Value;
                        sqlinsertCommand.Parameters["@grammar"].Value = sentence.Grammar ?? (object)DBNull.Value;
                        sqlinsertCommand.Parameters["@topic"].Value = sentence.Topic ?? (object)DBNull.Value;
                        sqlinsertCommand.Parameters["@level"].Value = sentence.Level ?? (object)DBNull.Value;

                        sqlinsertCommand.ExecuteNonQuery();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Hata :  " + ex);
                }
            }
        }
    }
    public static async Task SaveUserData(string username, string email, string password)
    {
        using (var conn = new SqliteConnection(DatabaseSettings.ConnectionString))
        {
            conn.Open();
            long userID;
            var hasher = new PasswordHasher<User>();
            string hash = hasher.HashPassword(new User(), password);

            string insertCommand = "INSERT INTO users (username, email, passwordhash, RoleId) VALUES (@username, @email, @passwordhash, @roleid);";
            using (var command = new SqliteCommand(insertCommand, conn))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@passwordhash", hash);
                command.Parameters.AddWithValue("@roleid", 2);
                command.ExecuteNonQuery();

                command.CommandText = "SELECT last_insert_rowid();";

                userID = (long)command.ExecuteScalar()!;
                string USC = @"INSERT INTO UserStats (UserId, XP, Level, RankId, UpdatedAt) VALUES (@userid, @XP, @level, @rankid, @updatedat)";
                using (var userStatsCommand = new SqliteCommand(USC, conn))
                {
                    userStatsCommand.Parameters.AddWithValue("@userid", userID);
                    userStatsCommand.Parameters.AddWithValue("XP", 0);
                    userStatsCommand.Parameters.AddWithValue("@level", 0);
                    userStatsCommand.Parameters.AddWithValue("@rankid", 1);
                    userStatsCommand.Parameters.AddWithValue("@updatedat", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    userStatsCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
