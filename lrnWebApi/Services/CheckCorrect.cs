using System;
using lrnWebApi.Database;
using lrnWebApi.SettingCS;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace lrnWebApi.Services;

public class CheckCorrect
{
    public static int CheckSentence(int id, string sentences)
    {
        using (var conn = new SqliteConnection(DatabaseSettings.ConnectionString))
        {
            conn.Open();
            string checkQuery = @"SELECT COUNT(*) FROM sentences Where Id = @id AND english = @sentence";
            using(var checkCom = new SqliteCommand(checkQuery, conn))
            {
                checkCom.Parameters.AddWithValue("@id", id);
                checkCom.Parameters.AddWithValue("@sentence", sentences);
                int chif = Convert.ToInt32(checkCom.ExecuteScalar());
                if(chif == 0)
                {
                    Console.WriteLine("Yanlış");
                    return 0;
                }
                else
                {
                    Console.WriteLine("Doğru");
                    string updateQury = "UPDATE sentences SET IsUsed = 1 WHERE Id = @id";
                    using(var updateCommand = new SqliteCommand(updateQury, conn))
                    {
                        updateCommand.Parameters.AddWithValue("@id", id);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                return chif;
            }
        }
    }
}
