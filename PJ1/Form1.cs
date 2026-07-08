using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ElectronicPartsFinder
{
    public partial class Form1 : Form
    {
        private readonly HttpClient _httpClient;
        private List<CryptoResult> _lastCryptoResults;

        public Form1()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            label3.Text = "Готов к работе!";
            _lastCryptoResults = new List<CryptoResult>();

            this.Text = "Информационный сервис";
            label1.Text = "Информационный сервис";
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string query = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Введите IP-адрес или название криптовалюты!", "Ошибка!",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            button1.Enabled = false;
            button1.Text = "Запрос...";
            listBox1.Items.Clear();
            textBox2.Clear();
            _lastCryptoResults.Clear();
            label3.Text = "Выполняется запрос...";

            try
            {
                if (IsIPAddress(query))
                {
                    var geoData = await GetGeoLocationAsync(query);
                    if (geoData != null && geoData.Status == "success")
                    {
                        listBox1.Items.Add($"IP: {geoData.Ip}");
                        listBox1.Items.Add($"Страна: {geoData.Country}");
                        listBox1.Items.Add($"Город: {geoData.City}");
                        listBox1.Items.Add($"Регион: {geoData.Region}");
                        listBox1.Items.Add($"Координаты: {geoData.Lat}, {geoData.Lon}");
                        listBox1.Items.Add($"Провайдер: {geoData.Isp}");
                        listBox1.Items.Add($"Организация: {geoData.Org}");
                        label3.Text = "Геолокация получена";
                    }
                    else
                    {
                        label3.Text = "Не удалось определить геолокацию";
                        MessageBox.Show($"Не удалось получить данные по IP: {query}\n\nПопробуйте другой IP или криптовалюту.",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    var cryptoResults = await GetCryptoPricesAsync(query);
                    if (cryptoResults.Count > 0)
                    {
                        _lastCryptoResults = cryptoResults;
                        foreach (var crypto in cryptoResults)
                        {
                            string displayText = $"{crypto.Symbol}: ${crypto.Price:F2} USD";
                            listBox1.Items.Add(displayText);
                        }
                        label3.Text = $"Найдено {cryptoResults.Count} криптовалют";
                    }
                    else
                    {
                        label3.Text = "Криптовалюта не найдена";
                        MessageBox.Show($"Криптовалюта '{query}' не найдена.\n\nПопробуйте: btc, eth, sol, doge, xrp",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка сети: {ex.Message}\n\nПроверьте подключение к интернету.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label3.Text = "Ошибка сети";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                label3.Text = "Ошибка при запросе";
            }
            
        }

        private bool IsIPAddress(string text)
        {
            return Regex.IsMatch(text, @"^\d+\.\d+\.\d+\.\d+$");
        }

        private async Task<GeoLocation> GetGeoLocationAsync(string ip)
        {
            try
            {
           
                string url = $"http://ip-api.com/json/{ip}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                var result = new GeoLocation();

             
                var statusMatch = Regex.Match(jsonResponse, @"""status""\s*:\s*""([^""]*)""");
                if (statusMatch.Success) result.Status = statusMatch.Groups[1].Value;

                var ipMatch = Regex.Match(jsonResponse, @"""query""\s*:\s*""([^""]*)""");
                if (ipMatch.Success) result.Ip = ipMatch.Groups[1].Value;

                var countryMatch = Regex.Match(jsonResponse, @"""country""\s*:\s*""([^""]*)""");
                if (countryMatch.Success) result.Country = countryMatch.Groups[1].Value;

                var cityMatch = Regex.Match(jsonResponse, @"""city""\s*:\s*""([^""]*)""");
                if (cityMatch.Success) result.City = cityMatch.Groups[1].Value;

                var regionMatch = Regex.Match(jsonResponse, @"""regionName""\s*:\s*""([^""]*)""");
                if (regionMatch.Success) result.Region = regionMatch.Groups[1].Value;

                var latMatch = Regex.Match(jsonResponse, @"""lat""\s*:\s*([\d.]+)");
                if (latMatch.Success) result.Lat = latMatch.Groups[1].Value;

                var lonMatch = Regex.Match(jsonResponse, @"""lon""\s*:\s*([\d.]+)");
                if (lonMatch.Success) result.Lon = lonMatch.Groups[1].Value;

                var ispMatch = Regex.Match(jsonResponse, @"""isp""\s*:\s*""([^""]*)""");
                if (ispMatch.Success) result.Isp = ispMatch.Groups[1].Value;

                var orgMatch = Regex.Match(jsonResponse, @"""org""\s*:\s*""([^""]*)""");
                if (orgMatch.Success) result.Org = orgMatch.Groups[1].Value;

                return result;
            }
            catch
            {
                return new GeoLocation { Status = "error", Ip = ip };
            }
        }

        private async Task<List<CryptoResult>> GetCryptoPricesAsync(string inputSymbol)
        {
            var results = new List<CryptoResult>();

            var cryptoMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "btc", "bitcoin" },
                { "eth", "ethereum" },
                { "sol", "solana" },
                { "doge", "dogecoin" },
                { "bnb", "binancecoin" },
                { "xrp", "ripple" },
                { "ada", "cardano" },
                { "dot", "polkadot" },
                { "link", "chainlink" },
                { "matic", "polygon" },
                { "etc", "ethereum-classic" },
                { "ltc", "litecoin" },
                { "avax", "avalanche-2" },
                { "uni", "uniswap" },
                { "atom", "cosmos" },
            };

            if (cryptoMap.TryGetValue(inputSymbol.ToLower(), out string coinId))
            {
                string url = $"https://api.coingecko.com/api/v3/simple/price?ids={coinId}&vs_currencies=usd";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Парсим цену
                    string pattern = @"""usd""\s*:\s*([\d.]+)";
                    var match = Regex.Match(jsonResponse, pattern);

                    if (match.Success)
                    {
                        double price = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                        results.Add(new CryptoResult
                        {
                            Symbol = inputSymbol.ToUpper(),
                            Price = price
                        });
                    }
                }
            }

            return results;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0 && _lastCryptoResults.Count > listBox1.SelectedIndex)
            {
                var crypto = _lastCryptoResults[listBox1.SelectedIndex];
                textBox2.Clear();
                textBox2.AppendText($"=== {crypto.Symbol} ===\r\n\r\n");
                textBox2.AppendText($"Цена: ${crypto.Price:F2} USD\r\n");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            textBox2.Clear();
            listBox1.Items.Clear();
            _lastCryptoResults.Clear();
            label3.Text = "Готов к работе";
            textBox1.Focus();
        }
    }

    public class GeoLocation
    {
        public string Status { get; set; } = "N/A";
        public string Ip { get; set; } = "N/A";
        public string Country { get; set; } = "N/A";
        public string City { get; set; } = "N/A";
        public string Region { get; set; } = "N/A";
        public string Lat { get; set; } = "N/A";
        public string Lon { get; set; } = "N/A";
        public string Isp { get; set; } = "N/A";
        public string Org { get; set; } = "N/A";
    }

    public class CryptoResult
    {
        public string Symbol { get; set; } = "";
        public double Price { get; set; }
    }
}
