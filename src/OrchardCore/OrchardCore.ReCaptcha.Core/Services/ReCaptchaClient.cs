using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OrchardCore.ReCaptcha.Configuration;

namespace OrchardCore.ReCaptcha.Services
{
    public class ReCaptchaClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public ReCaptchaClient(HttpClient httpClient, IOptions<ReCaptchaSettings> optionsAccessor, ILogger<ReCaptchaClient> logger)
        {
            var options = optionsAccessor.Value;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(options.ReCaptchaApiUri);
            _logger = logger;
        }

        /// <summary>
        /// Verifies the supplied token with ReCaptcha Api
        /// </summary>
        /// <param name="responseToken">Token received from the ReCaptcha UI</param>
        /// <param name="secretKey">Key entered by user in the secrets</param>
        /// <returns>A boolean indicating if the token is valid</returns>
        public async Task<bool> VerifyAsync(string responseToken, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(responseToken))
            {
                return false;
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", responseToken }
            });
            try
            {
                using var response = await _httpClient.PostAsync("siteverify", content);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseModel = JObject.Parse(responseJson);

                return responseModel["success"].Value<bool>();
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Could not contact Google to verify captcha");
            }

            return false;
        }
    }
}
