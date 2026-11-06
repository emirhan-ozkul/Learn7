using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace lrnWebApi.Services;

public class _groqSentence
{
    private const string GroqUrl =
        "https://api.groq.com/openai/v1/chat/completions";

    private const string Model =
        "openai/gpt-oss-120b";

    public static async Task<List<SentenceDto>> GroqSentence(string topic, string level, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Bu kullanıcı için API anahtarı tanımlanmamış.");
        }

        // ---------------------------------------------------------
        // GROQ REQUEST
        // ---------------------------------------------------------

        var payload = new
        {
            model = Model,

            temperature = 0.7,

            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are an English learning assistant. " +
                        "Always return the response according to the provided JSON schema."
                },

                new
                {
                    role = "user",
                    content =
                        "Generate exactly 10 short "+ level +"-level English sentences " +
                        "Pick activities from about " + topic +
                        "Do NOT include grammar explanations. " +
                        "Keep every sentence short and suitable for an " + level + " English learner." +
                        "For the Grammar field, do NOT provide an explanation." +
                        "Only provide a short grammar category such as: 'Simple Past - Regular'"+
                        "Do not write full sentences in the Grammar field."
                }
            },

            // -----------------------------------------------------
            // JSON SCHEMA
            // -----------------------------------------------------

            response_format = new
            {
                type = "json_schema",

                json_schema = new
                {
                    name = "translation_response",

                    strict = true,

                    schema = new
                    {
                        type = "object",

                        properties = new
                        {
                            sentences = new
                            {
                                type = "array",

                                items = new
                                {
                                    type = "object",

                                    properties = new
                                    {
                                        English = new
                                        {
                                            type = "string"
                                        },

                                        Turkish = new
                                        {
                                            type = "string"
                                        },
                                        Grammar = new
                                        {
                                            type = "string"
                                        },
                                        Topic = new
                                        {
                                            type = "string"
                                        },
                                        Level = new
                                        {
                                            type = "string"
                                        }
                                    },

                                    required = new[]
                                    {
                                        "English",
                                        "Turkish",
                                        "Grammar",
                                        "Topic",
                                        "Level"
                                    },

                                    additionalProperties = false
                                }
                            }
                        },

                        required = new[]
                        {
                            "sentences"
                        },

                        additionalProperties = false
                    }
                }
            }
        };

        // ---------------------------------------------------------
        // SERIALIZE REQUEST
        // ---------------------------------------------------------

        var serializerOptions = new JsonSerializerOptions
        {
            Encoder =
                System.Text.Encodings.Web.JavaScriptEncoder
                    .UnsafeRelaxedJsonEscaping
        };

        string requestJson =
            JsonSerializer.Serialize(payload, serializerOptions);

        // ---------------------------------------------------------
        // HTTP REQUEST
        // ---------------------------------------------------------

        using var client = new HttpClient();

        using var request =
            new HttpRequestMessage(HttpMethod.Post, GroqUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );

        request.Content =
            new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json"
            );

        // ---------------------------------------------------------
        // GROQ API CALL
        // ---------------------------------------------------------

        using HttpResponseMessage response =
            await client.SendAsync(request);

        string responseJson =
            await response.Content.ReadAsStringAsync();
            await SaveGroqLogAsync(responseJson);

        // Hata varsa Groq'un gerçek hata mesajını göster
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Groq API hatası ({(int)response.StatusCode}): {responseJson}"
            );
        }

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            throw new Exception("Groq boş bir response döndürdü.");
        }



        GroqResponse? groqResponse =
            JsonSerializer.Deserialize<GroqResponse>(
                responseJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

        if (groqResponse == null)
        {
            throw new Exception(
                "Groq response deserialize edilemedi."
            );
        }

        if (groqResponse.Choices == null ||
            groqResponse.Choices.Count == 0)
        {
            throw new Exception(
                "Groq response içerisinde choices bulunamadı."
            );
        }

        string content =
            groqResponse.Choices[0].Message?.Content
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception(
                "Groq response içerisindeki content boş."
            );
        }

        SentenceResponse? sentenceResponse =
            JsonSerializer.Deserialize<SentenceResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

        if (sentenceResponse == null)
        {
            throw new Exception(
                "Sentence response deserialize edilemedi."
            );
        }

        if (sentenceResponse.Sentences == null ||
            sentenceResponse.Sentences.Count == 0)
        {
            throw new Exception(
                "Groq herhangi bir cümle üretmedi."
            );
        }

        // ---------------------------------------------------------
        // SON KONTROL
        // ---------------------------------------------------------

        Console.WriteLine(
            $"[Groq] {sentenceResponse.Sentences.Count} cümle üretildi."
        );

        foreach (SentenceDto sentence in sentenceResponse.Sentences)
        {
            Console.WriteLine(
                $"[Sentence] {sentence.English} -> {sentence.Turkish}"
            );
        }
        List<SentenceDto> sentences = sentenceResponse.Sentences;
        await DbSaver.SaveAsyncData(sentences);
        return sentences;

    }
    private static async Task SaveGroqLogAsync(string responseJson)
    {
        try
        {
            string logDirectory =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Logs"
                );

            Directory.CreateDirectory(logDirectory);

            string fileName =
                $"groq_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";

            string filePath =
                Path.Combine(logDirectory, fileName);

            using JsonDocument document =
                JsonDocument.Parse(responseJson);

            string formattedJson =
                JsonSerializer.Serialize(
                    document,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder =
                            System.Text.Encodings.Web.JavaScriptEncoder
                                .UnsafeRelaxedJsonEscaping
                    }
                );

            await File.WriteAllTextAsync(
                filePath,
                formattedJson,
                Encoding.UTF8
            );

            Console.WriteLine(
                $"[Groq Log] Kaydedildi: {filePath}"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Groq Log] Kaydedilemedi: {ex.Message}"
            );
        }
    }
}


// ================================================================
// GROQ RESPONSE MODELS
// ================================================================

public class GroqResponse
{
    public List<GroqChoice> Choices { get; set; } = new();
}


public class GroqChoice
{
    public GroqMessage Message { get; set; } = new();
}


public class GroqMessage
{
    public string Content { get; set; } = string.Empty;
}


// ================================================================
// SENTENCE RESPONSE MODELS
// ================================================================

public class SentenceResponse
{
    public List<SentenceDto> Sentences { get; set; } = new();
}


public class SentenceDto
{
    public string English { get; set; } = string.Empty;

    public string Turkish { get; set; } = string.Empty;
    public string Grammar {get;set;} = string.Empty;
    public string Topic {get;set;} = string.Empty;
    public string Level { get; set; } = string.Empty;
}


