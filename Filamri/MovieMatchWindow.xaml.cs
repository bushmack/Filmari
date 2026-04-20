using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace filamri
{
    public partial class MovieMatchWindow : Window
    {
        private readonly HttpClient _httpClient = new();
        private UserData _userData;

        public MovieMatchWindow()
        {
            InitializeComponent();
            _httpClient.BaseAddress = new Uri("http://192.168.133.7:8002");
            _userData = LocalStorage.Load();
        }

        private void WatchTogether_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            WatchTogetherGrid.Visibility = Visibility.Visible;
            CreateWatchRoomPanel.Visibility = Visibility.Visible;
            JoinWatchRoomPanel.Visibility = Visibility.Collapsed;
        }

        private void SearchTogether_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            SearchTogetherGrid.Visibility = Visibility.Visible;
        }

        private void CreateWatchRoom_Click(object sender, RoutedEventArgs e)
        {
            CreateWatchRoomPanel.Visibility = Visibility.Visible;
            JoinWatchRoomPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowJoinWatchRoom_Click(object sender, RoutedEventArgs e)
        {
            CreateWatchRoomPanel.Visibility = Visibility.Collapsed;
            JoinWatchRoomPanel.Visibility = Visibility.Visible;
            WatchRoomIdTextBox.Text = "";
        }

        private void VideoUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CreateRoomFromUrlButton.IsEnabled = !string.IsNullOrWhiteSpace(VideoUrlTextBox.Text);
        }

        private string ExtractEmbedUrl(string url)
        {
            // YouTube
            if (url.Contains("youtube.com") || url.Contains("youtu.be"))
            {
                var regex = new Regex(@"(?:youtube\.com\/watch\?v=|youtu\.be\/)([a-zA-Z0-9_-]{11})");
                var match = regex.Match(url);
                if (match.Success)
                {
                    return $"https://www.youtube.com/embed/{match.Groups[1].Value}";
                }
            }
            return url;
        }

        private async void CreateRoomFromUrl_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = VideoUrlTextBox.Text.Trim();
            if (string.IsNullOrEmpty(videoUrl)) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Отправляем оригинальную ссылку, сервер сам сконвертирует
                var request = new { user_id = _userData.UserId, user_name = _userData.UserName, video_url = videoUrl };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/create-room", content);
                
                Mouse.OverrideCursor = null;

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var roomId = doc.RootElement.GetProperty("roomId").GetString();
                    var embedUrl = doc.RootElement.GetProperty("videoUrl").GetString();

                    MessageBox.Show($"✅ Комната создана!\n\nID комнаты: {roomId}\n\n" +
                                    $"Поделитесь этим ID с другом." +
                                    $"{roomId}, {embedUrl}",
                                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    var watchWindow = new WatchPartyWindow(embedUrl ?? videoUrl, true, roomId ?? "");
                    watchWindow.Owner = this;
                    watchWindow.ShowDialog();
                    Close();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка создания комнаты: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void WatchRoomIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            JoinWatchRoomButton.IsEnabled = !string.IsNullOrWhiteSpace(WatchRoomIdTextBox.Text);
        }

        private async void JoinWatchRoom_Click(object sender, RoutedEventArgs e)
        {
            string roomId = WatchRoomIdTextBox.Text.Trim();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                JoinWatchRoomButton.IsEnabled = false;

                var request = new { room_id = roomId, user_id = _userData.UserId, user_name = _userData.UserName };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/join-room", content);

                Mouse.OverrideCursor = null;
                JoinWatchRoomButton.IsEnabled = true;

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (result != null && result.ContainsKey("videoUrl"))
                    {
                        string videoUrl = result["videoUrl"].ToString();

                        MessageBox.Show($"✅ Вы подключились к комнате {roomId}!\n\nСейчас начнется совместный просмотр.",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        var watchWindow = new WatchPartyWindow("https://vkvideo.ru/video_ext.php?oid=-194145340&id=456240281&hash=50abc62ea9397f2f", false, roomId);
                        watchWindow.Owner = this;
                        watchWindow.ShowDialog();
                        Close();
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    MessageBox.Show("❌ Комната не найдена. Проверьте ID.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"❌ Ошибка подключения: {error}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                JoinWatchRoomButton.IsEnabled = true;
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToWatchMenu_Click(object sender, RoutedEventArgs e)
        {
            WatchTogetherGrid.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible;
            VideoUrlTextBox.Text = "";
        }

        private void BackToSearchMenu_Click(object sender, RoutedEventArgs e)
        {
            SearchTogetherGrid.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible;
        }
    }
}