using System;

namespace lrnWebApi.Models;

public class SentenceGenerationResponse
{
    public List<TranslationReponse> Sentences {get;set;} = new();
}
