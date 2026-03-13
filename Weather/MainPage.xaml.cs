using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Reflection.Metadata;
using DotNetEnv;

namespace Weather
{
    public partial class MainPage : ContentPage
    {
        dynamic ? forecastData;
        private string ApiKey;
        public ObservableCollection<string> CityList { get; set; } = new ObservableCollection<string>();
        string connectionString = @"Server=.\SQLEXPRESS;Database=Weather;Integrated Security=True;TrustServerCertificate=True";

        public MainPage()
        {
            InitializeComponent();
            Env.Load();
            ApiKey = Env.GetString("OPENWEATHER_API_KEY");
            LoadCitiesFromSql();
            
        }

        private async void OnCitySelected(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            if (picker.SelectedIndex != -1)
            {
                string selectedCityName = (string)picker.ItemsSource[picker.SelectedIndex];
                await GetWeatherAsync(selectedCityName);
            }
        }

        private async Task GetWeatherAsync(string cityName)
        {
          
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://api.openweathermap.org/data/2.5/forecast?q={cityName}&appid={ApiKey}&units=metric&lang=tr";
                    var response = await client.GetStringAsync(url);
                    forecastData = JsonConvert.DeserializeObject<dynamic>(response);


                    var current = forecastData.list[0];
                    double sicaklik = (double)current.main.temp;
                    string durum = (string)current.weather[0].main;
                    string iconCode = (string)current.weather[0].icon;
                    string mainWeather = (string)current.weather[0].main;

                    TempLabel.Text = $"{Math.Round((double)current.main.temp)}°C";
                    SuggestionLabel.Text = $"{cityName} için şu an hava {current.weather[0].description}. {GetTavsiye(sicaklik, durum)}";
                    string iconName = GetIconPath((string)current.weather[0].main, (string)current.weather[0].icon);
                    WeatherIconImage.Source = iconName;

                    UpdateHourlyList();
                }
            }
            catch (Exception )
            {
                await DisplayAlert("Hata", "Hava durumu alınamadı.", "Tamam");
            }
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            UpdateUI(e.NewDate);
        }

        private void UpdateUI(DateTime selectedDate)
        {
            if (forecastData == null) return;

            bool found = false;
            foreach (var item in forecastData.list)
            {
                DateTime itemDate = DateTime.Parse(item.dt_txt.ToString());

                
                if (itemDate.Date == selectedDate.Date && itemDate.Hour >= 12)
                {
                    TempLabel.Text = $"{Math.Round((double)item.main.temp)}°C";
                    SuggestionLabel.Text = $"{selectedDate:dd MMMM} tarihinde hava {item.weather[0].description}.";
                    WeatherIconImage.Source = GetIconPath((string)item.weather[0].main, (string)item.weather[0].icon);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                SuggestionLabel.Text = "Seçilen tarih için detaylı tahmin bulunamadı.";
            }
        }

        private void UpdateHourlyList()
        {
            if (forecastData == null) return;
            HourlyForecastList.Children.Clear();

            for (int i = 0; i < 13; i++)
            {
                var forecast = forecastData.list[i];
                var dateTime = DateTime.Parse(forecast.dt_txt.ToString());

                var card = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 28 },
                    Background = Color.FromArgb("#30FFFFFF"),
                    WidthRequest = 90,
                    HeightRequest = 130,
                    StrokeThickness = 0,
                    Margin = new Thickness(5),
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Spacing = 5,
                        Children = {
                            new Label { Text = dateTime.ToString("HH:mm"), HorizontalOptions = LayoutOptions.Center, TextColor = Colors.White, FontSize = 12 },

                        new Image {
                            Source = GetIconPath((string)forecast.weather[0].main, (string)forecast.weather[0].icon),
                            WidthRequest = 45,
                            HeightRequest = 45,
                            Aspect = Aspect.AspectFit
                        },
                            new Label { Text = $"{Math.Round((double)forecast.main.temp)}°", HorizontalOptions = LayoutOptions.Center, FontSize = 18, TextColor = Colors.White, FontAttributes = FontAttributes.Bold }
                        }
                    }
                };
                HourlyForecastList.Children.Add(card);
            }
        }

        private string GetIconPath(string mainWeather, string iconCode)
        {
            System.Diagnostics.Debug.WriteLine($"Gelen Hava Durumu: {mainWeather}");
            string condition = mainWeather.ToLower();
            bool isNight = iconCode.EndsWith("n");

            // Gece ise ve hava açıksa direkt ayı göster
            if (isNight && condition == "clear")
            {
                return "half_moon.png";
            }

            return condition switch
            {

                "clear" => "sun.png",
                "clouds" => "clouds.png",
                "rain" => "heavy_rain.png", 
                "drizzle" => "heavy_rain.png",
                "thunderstorm" => "heavy_rain.png",
                "snow" => "snowflake.png",
                "clear_night" => "half_moon.png",
                "mist" => "cloud.png",
                _ => isNight ? "half_moon.png" : "sun.png"
            };
        }

        
        private string GetTavsiye(double temp, string condition)
        {
            condition = condition.ToLower();
            if (condition.Contains("rain") || condition.Contains("drizzle") || condition.Contains("thunderstorm"))
                return "Şemsiyeni yanına almayı unutma, gökyüzü bugün biraz ıslak! ☔";

            
            if (condition.Contains("snow"))
                return "Lapa lapa kar var! En kalın montunu giy ve sıcak bir kahve kap. ❄️☕";

            
            if (temp < 0) return "Dışarısı buz kesiyor! Atkı, bere, eldiven ne varsa kuşan. 🧣";
            if (temp < 10) return "Hava oldukça soğuk. Sıkı giyin, rüzgar çarpmasın. 🧥";
            if (temp < 18) return "Hafif serin bir hava var. Üzerine bir ceket veya hırka almalısın. 🧥";
            if (temp < 25) return "Hava tam kararında! Rahat bir şeyler giyip dışarı çıkmanın tam vakti. 😊";
            if (temp < 32) return "Güneş parlıyor! İnce kıyafetler seç ve güneş gözlüğünü unutma. 😎";

            return "Hava çok sıcak! Gölgede kalmaya çalış ve bol bol su iç. ☀️🥤";
        }
        

        private void LoadCitiesFromSql()
        {
            try
            {
                
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT CityName FROM City ORDER BY CityName ASC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            CityList.Clear();
                            while (reader.Read()) CityList.Add(reader["CityName"].ToString());
                        }
                    }
                }
                CityPicker.ItemsSource = CityList;

                
                if (CityList.Count > 0) CityPicker.SelectedIndex = 0;
            }
            catch (Exception ) { DisplayAlert("Hata", "Veritabanı bağlantısı kurulamadı.", "Tamam"); }
        }
    }
}