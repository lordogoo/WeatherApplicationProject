using Microsoft.AspNetCore.Mvc.RazorPages;
using WeatherApplicationProject.Controllers;
using WeatherApplicationProject.Helpers;

namespace WeatherApplicationProject.Views.Home
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private static IConfiguration _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("weather.api");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36");
        }

        public void OnGet()
        {
        }

        public async Task<string?> GetWeatherData()
        {

            return await WeatherHelper.checkWeather(null, _httpClient, _configuration);

        }
    }
}
