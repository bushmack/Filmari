using filamri.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            _httpClient.BaseAddress = new Uri("http://192.168.133.7:8001");
            _userData = LocalStorage.Load();
        }

        // ========== ГЛАВНОЕ МЕНЮ ==========

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

        // ========== РЕЖИМ "СМОТРЕТЬ ВМЕСТЕ" ==========

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

            // VK Video
            if (url.Contains("vk.com/video") || url.Contains("vkvideo.ru"))
            {
                var regex = new Regex(@"(?:vk\.com\/video|vkvideo\.ru\/video)(-?\d+_\d+)");
                var match = regex.Match(url);
                if (match.Success)
                {
                    var parts = match.Groups[1].Value.Split('_');
                    if (parts.Length == 2)
                    {
                        return $"https://vk.com/video_ext.php?oid={parts[0]}&id={parts[1]}&hash=";
                    }
                }
            }

            // RuTube
            if (url.Contains("rutube.ru"))
            {
                var regex = new Regex(@"rutube\.ru\/video\/([a-f0-9]+)");
                var match = regex.Match(url);
                if (match.Success)
                {
                    return $"https://rutube.ru/play/embed/{match.Groups[1].Value}";
                }
            }

            // Яндекс Видео
            if (url.Contains("yandex.ru/video") || url.Contains("yandex.ru/efir"))
            {
                var regex = new Regex(@"yandex\.ru\/video\/preview\/(\d+)");
                var match = regex.Match(url);
                if (match.Success)
                {
                    return $"https://yandex.ru/video/preview/{match.Groups[1].Value}";
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

                string embedUrl = ExtractEmbedUrl(videoUrl);

                if (string.IsNullOrEmpty(embedUrl))
                {
                    MessageBox.Show("Не удалось распознать ссылку. Поддерживаются YouTube, VK, RuTube, Яндекс Видео.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var request = new { user_id = _userData.UserId, user_name = _userData.UserName, video_url = embedUrl };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/watch/create-room", content);

                Mouse.OverrideCursor = null;

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var room = JsonSerializer.Deserialize<WatchRoom>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (room != null)
                    {
                        MessageBox.Show($"✅ Комната создана!\n\nID комнаты: {room.RoomId}\n\n" +
                                        $"Поделитесь этим ID с другом.",
                                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        var watchWindow = new WatchPartyWindow(embedUrl, true, room.RoomId);
                        watchWindow.Owner = this;
                        watchWindow.ShowDialog();
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка создания комнаты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                var request = new { room_id = roomId, user_id = _userData.UserId, user_name = _userData.UserName };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/watch/join-room", content);

                Mouse.OverrideCursor = null;

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var room = JsonSerializer.Deserialize<WatchRoom>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (room != null && !string.IsNullOrEmpty(room.VideoUrl))
                    {
                        MessageBox.Show($"✅ Вы подключились к комнате {roomId}!\n\nСейчас начнется совместный просмотр.",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        var watchWindow = new WatchPartyWindow(room.VideoUrl, false, roomId);
                        watchWindow.Owner = this;
                        watchWindow.ShowDialog();
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("❌ Ошибка получения данных комнаты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    MessageBox.Show("❌ Комната не найдена. Проверьте ID.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("❌ Ошибка подключения к комнате", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToWatchMenu_Click(object sender, RoutedEventArgs e)
        {
            WatchTogetherGrid.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible;
        }

        // ========== РЕЖИМ "ИСКАТЬ ФИЛЬМ ВМЕСТЕ" (заглушка) ==========

        private void BackToSearchMenu_Click(object sender, RoutedEventArgs e)
        {
            SearchTogetherGrid.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible;
        }
    }
}