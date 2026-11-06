using System;

namespace lrnWebApi.Models;

public class TranslationRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Level {get;set;} = string.Empty;
}   

