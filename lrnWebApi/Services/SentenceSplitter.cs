using System;

namespace lrnWebApi.Services;

public class SentenceSplitter
{
    public static string SentenceShuffer(string sentences)
    {
        string sentence = sentences;
        string[] words = sentence.ToLower().Replace(".", "").Split(' ');
        Random.Shared.Shuffle(words);
        string RandomSentence = String.Join(" ", words);
        return RandomSentence;
    }
}
