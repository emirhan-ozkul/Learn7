using System;
using System.Threading.Tasks;
using lrnWebApi.Services;
using SQLitePCL;

namespace lrnWebApi.Database;

public class DatabaseService
{
    public static async Task generateSentences(string topic, string level, string apiKey)
    {
        int sentencesCount = DatabasePoolService.CountPool(topic, level);
        if (sentencesCount < 10)
        {
            Console.WriteLine("Db içerisi :" + sentencesCount);
            await _groqSentence.GroqSentence(topic, level, apiKey);
            Console.WriteLine("DBServis içerisindeki oluşturma çalıştırıldı...");

        }
        else
        {
            Console.WriteLine(sentencesCount.ToString() + " tane kelime var, üretim yapılmayacak.");
        }
    }
}
