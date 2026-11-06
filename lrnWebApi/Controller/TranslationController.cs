using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using lrnWebApi.Models;
using lrnWebApi.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using lrnWebApi.Database;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace lrnWebApi.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly UserApiKeyStore _apiKeyStore;

        public TranslationController(UserApiKeyStore apiKeyStore)
        {
            _apiKeyStore = apiKeyStore;
        }

        static SentenceDto? _result;

        [HttpPost("generate")] 
        public async Task<IActionResult> GenerateSentences([FromBody] TranslationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Topic))
            {
                return BadRequest("Prompt alanı boş bırakılamaz.");
            } 
            try
            {
                var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdValue, out var userId))
                {
                    return Unauthorized();
                }

                var apiKey = _apiKeyStore.GetApiKey(userId);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Conflict(new { error = "Önce Groq API anahtarınızı ayarlayın." });
                }

                await DatabaseService.generateSentences(request.Topic, request.Level, apiKey);
                var word = await WordPicker.PickWord(request.Topic, request.Level);
                if(word == null)
                {
                    Console.WriteLine("Seçili kelime null döndü");
                    return BadRequest("Kelime Boş");
                }
                return Ok(word);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Cümle üretimi başarısız: {ex}");
                return StatusCode(500, new { error = "Cümle oluşturulurken bir sunucu hatası oluştu." });
            }
        }

        [HttpPost("generateCheck")]
        public async Task<IActionResult> GetCheck([FromBody] AnswerCheckModel answer)
        {
            int isCorrect = CheckCorrect.CheckSentence(Convert.ToInt32(answer.id), answer.english);
            if(isCorrect == 1)
            {
                return Ok();
            }
            else
            {
                return BadRequest("yanlış, correct : " + isCorrect);
            }
        }
        [HttpGet("getR")]
        public async Task<IActionResult> GetResults()
        {
            return Ok(_result);
        }
    }
}
