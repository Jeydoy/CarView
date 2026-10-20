using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CarView.Services
{
    public interface ICrmService
    {
        Task<bool> CreateLeadAsync(string email, string name, string phone, string carInterest);
    }

    public class HubSpotCrmService : ICrmService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = ""; 

        public HubSpotCrmService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            _apiKey = _config["HubspotApiKey"];
        }

        public async Task<bool> CreateLeadAsync(string email, string name, string phone, string carInterest)
        {
            var url = "https://api.hubapi.com/crm/v3/objects/contacts";
           // string hubspotKey = _config["HubspotApiKey"];

            var payload = new
            {
                properties = new
                {
                    email = email,
                    firstname = name,
                    phone = phone,
                    jobtitle = $"Цікавиться: {carInterest}" // Job Title
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey.Trim());

            var response = await _httpClient.PostAsync(url, jsonContent);
            return response.IsSuccessStatusCode;
        }
    }
}
