using System.Diagnostics;
using WeatherApplicationProject.Models;

namespace WeatherApplicationProject.Helpers
{
	public class WeatherHelper
	{
		private static string dateFormat = "yyyy-MM-dd";
		public delegate void CallBack(string lParam);

		/// <summary>
		/// helper function that returns the five day average percipitation.
		/// </summary>
		/// <param name="dateList">The list of dates and percipitation values to search</param>
		/// <param name="date">The end date of our search</param>
		/// <returns>the average percipitation value</returns>
		private static float getFiveDayAverage(List<WeatherDates> dateList, DateTime date)
		{
			DateTime endDate = date;
			DateTime startDate = date.AddDays(-5);

			List<WeatherDates> fiveday = dateList.Where<WeatherDates>(r => r.datetime > startDate && r.datetime <= endDate).ToList();
			float average = fiveday.Average(r => r.precip ?? 0);
			return average;
		}

		/// <summary>
		/// This function looks at the percipitation of the last five days
		/// and compares it to the three last years similar interval of dates.
		/// </summary>
		/// <param name="onyes">call back function for when percipitation condition is met</param>
		/// <param name="_httpClient">httpclient for getting percipitatin data</param>
		/// <returns>string message to be given to the user</returns>
		public static async Task<string> checkWeather(CallBack callback, HttpClient _httpClient, IConfiguration _configuration)
		{
			DateTime currentDate = DateTime.Now;

			string startDate = "2019-05-01";
			string endDate = currentDate.AddDays(_configuration.GetValue<int>("ChangeDay")).ToString(dateFormat);

			string apiPath = $"/v1/archive?latitude=26.2416&longitude=-81.8071&start_date={startDate}&end_date={endDate}&timezone=GMT&daily=precipitation_sum";
			try
			{
				var sw = new Stopwatch();
				WeatherResult result = await _httpClient.GetFromJsonAsync<WeatherResult>(apiPath);
				sw.Stop();

				if (result == null)
				{
					return "Error: No data";
				}

				IEnumerable<Tuple<DateTime, float?>> pairs = result.daily.time.Zip(result.daily.precipitation_sum, (a, b) => Tuple.Create(a, b));
				List<WeatherDates> datePairs = pairs.Select(x => new WeatherDates { datetime = x.Item1, precip = x.Item2 }).ToList();

				int num = 4;
				float[] averageList = new float[num];
				DateTime workingDate = currentDate;
				string outputProof = "Current Percipitation\r\n";
				outputProof += workingDate.AddDays(-5).ToString(dateFormat) + " to " + workingDate.ToString(dateFormat) + " => Percipitation:" + averageList[0] + "\r\n";
				outputProof += "previous three years\r\n";
                workingDate = workingDate.AddYears(-1);

                for (int i = 1; i < num; i++)
				{
					averageList[i] = getFiveDayAverage(datePairs, workingDate);
					outputProof += workingDate.AddDays(-5).ToString(dateFormat) + " to " + workingDate.ToString(dateFormat) + " => Percipitation:" + averageList[i] + "\r\n";
					workingDate = workingDate.AddYears(-1);
				}

				if ((averageList[0] > averageList[1]) && (averageList[0] > averageList[2]) && (averageList[0] > averageList[1]))
				{
					outputProof += "Yes schedule delay can be claimed\r\n";
					outputProof += "Please make the claim sometime between the following dates:\r\n";
					outputProof += currentDate.ToString(dateFormat) + " " + currentDate.AddDays(2).ToString(dateFormat);

                    if (callback != null)
					{
                        callback(outputProof);
					}
				}
				else
				{
					outputProof += "No schedule delay";
					if (_configuration.GetValue<bool>("AlwaysCallback"))
					{
						if (callback != null) {
							callback(outputProof);
						}
                    }
				}

				return outputProof;
			}
			catch (Exception ex)
			{
				return "error";
			}
		}

		public class WeatherResult
		{
			public weatherDaily daily { get; init; }
		}

		public class weatherDaily
		{
			public List<DateTime> time { get; init; }
			public List<float?> precipitation_sum { get; init; }
		}
	}
}
