using System.Net.Mail;
using System.Net;
using WeatherApplicationProject.Helpers;
using Microsoft.Data.SqlClient;
using System.Reflection.Emit;

namespace WeatherApplicationProject.Background
{
	/// <summary>
	/// This is a background task that emails the users if the conditions
	/// are met for a delay. This email is sent at 1:00 every day the precepitation
	/// level meets the right conditions.
	/// </summary>
	public class WeatherHostedService : IHostedService, IDisposable
	{
		private int executionCount = 0;
		private readonly ILogger<WeatherHostedService> _logger;
		private readonly HttpClient _httpClient;
		private Timer? _timer = null;
		private static IConfiguration _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();

		public WeatherHostedService(ILogger<WeatherHostedService> logger, IHttpClientFactory httpClientFactory)
		{
			_logger = logger;
			_httpClient = httpClientFactory.CreateClient("weather.api");

		}

		public Task StartAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Timed Hosted Service running.");

			TimeSpan interval;
			TimeSpan firstInterval;
            if (_configuration.GetValue<bool>("UseEmailDelay"))
			{
				//note this is used for testing
				//no delay at start
				//runs the timer every 30 secconds
				interval = TimeSpan.FromSeconds(30);
				firstInterval = TimeSpan.Zero;
            }
			else
			{
				//note this is used for production
                //delays first running untill 1:00
                //runs the timer every 24 hours once synced
                interval = TimeSpan.FromHours(24);
				var nextRunTime = DateTime.Today.AddDays(1)
					.AddHours(_configuration.GetValue<int>("ChangeHour"))
					.AddMinutes(_configuration.GetValue<int>("ChangeMinute"));
				var curTime = DateTime.Now;
				firstInterval = nextRunTime.Subtract(curTime);
			}
            Action action = () =>
			{
				var t1 = Task.Delay(firstInterval);
				t1.Wait();
				_timer = new Timer(
					DoWork,
					null,
					TimeSpan.Zero,
					interval
				);
			};

			Task.Run(action);
			return Task.CompletedTask;
		}

		private async void DoWork(object? state)
		{
			var count = Interlocked.Increment(ref executionCount);

			string result = await WeatherHelper.checkWeather(weatherSendEmail, _httpClient, _configuration);

			_logger.LogInformation(
				"Timed Hosted Service is working. Count: {Count}", count);
		}

		private void weatherSendEmail(string result)
		{
            _logger.LogInformation("weatherSendEmail");
            string? connString = _configuration.GetConnectionString("DefaultConnection");
			string? smtpUser = _configuration.GetConnectionString("smtpUser");
			string? smtpPassword = _configuration.GetConnectionString("smtpPassword");
			if (!String.IsNullOrEmpty(smtpUser) && !String.IsNullOrEmpty(smtpPassword))
			{
				try
				{
					using (SqlConnection conn = new SqlConnection(connString))
					{

						List<string> emailList = new List<string>();

                        string query = @"SELECT u.Email
                                     FROM AspNetUsers u;
                                     ";

						SqlCommand cmd = new SqlCommand(query, conn);
						conn.Open();
						SqlDataReader dr = cmd.ExecuteReader();

						if (dr.HasRows)
						{
							while (dr.Read())
							{
                                emailList.Add(dr.GetString(0));

							}
						}
						else
						{
							Console.WriteLine("No data found.");
						}

						dr.Close();
						conn.Close();

						var smtpClient = new SmtpClient("smtp.gmail.com")
						{
							Port = 587,
							Credentials = new NetworkCredential(smtpUser, smtpPassword),
							EnableSsl = true,
						};
						for (int i = 0; i < emailList.Count;i++) {
							smtpClient.Send("noreply@weather.test", emailList[i], "Weather Test", result);
						}
					}
				}
                catch (Exception ex)
                {
                    //display error message
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
		}

		public Task StopAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Timed Hosted Service is stopping.");

			_timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}
	}
}
