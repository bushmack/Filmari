using filamri.Models;
using filamri.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace filamri
{
    public partial class MovieMatchWindow : Window
    {
        private readonly ApiService _apiService = new();
        private UserData _userData;
        private string _selectedVideoPath = "";
        private List<GenreItem> _allGenres = new();
        private List<string> _selectedGenres = new();

        public MovieMatchWindow()
        {
            InitializeComponent();
            _userData = LocalStorage.Load();
            InitializeGenres();
        }

        private void InitializeGenres()
        {
            string[] genres = { "боевик", "вестерн", "военный", "детектив", "документальный",
                                "драма", "исторический", "комедия", "криминал", "мелодрама",
                                "мультфильм", "музыка", "приключения", "семейный", "спорт",
                                "триллер", "ужасы", "фантастика", "фэнтези" };

            foreach (var genre in genres)
            {
                _allGenres.Add(new GenreItem { Name = genre, IsSelected = false });
            }
            UpdateGenresList();
        }

        private void UpdateGenresList()
        {
            string search = GenreSearchBox?.Text ?? "";
            var filtered = string.IsNullOrEmpty(search)
                ? _allGenres
                : _allGenres.Where(g => g.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            GenresPanel.Children.Clear();

            foreach (var genre in filtered)
            {
                var border = new Border
                {
                    Background = genre.IsSelected ? (Brush)new BrushConverter().ConvertFrom("#6C5CE7") : (Brush)new BrushConverter().ConvertFrom("#E0E0E0"),
                    CornerRadius = new CornerRadius(20),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(5, 5, 5, 5),
                    Cursor = Cursors.Hand
                };

                var textBlock = new TextBlock
                {
                    Text = genre.Name,
                    Foreground = genre.IsSelected ? Brushes.White : (Brush)new BrushConverter().ConvertFrom("#2D3436"),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                };

                border.Child = textBlock;
                border.MouseLeftButtonUp += (s, e) => ToggleGenre(genre.Name);

                GenresPanel.Children.Add(border);
            }
        }

        private void ToggleGenre(string genreName)
        {
            var genre = _allGenres.FirstOrDefault(g => g.Name == genreName);
            if (genre != null)
            {
                genre.IsSelected = !genre.IsSelected;
                if (genre.IsSelected)
                    _selectedGenres.Add(genre.Name);
                else
                    _selectedGenres.Remove(genre.Name);
                UpdateGenresList();
            }
        }

        private void GenreSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGenresList();
        }

        private void WatchTogether_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            WatchTogetherGrid.Visibility = Visibility.Visible;
            JoinWatchRoomPanel.Visibility = Visibility.Collapsed;
        }

        private void SearchTogether_Click(object sender, RoutedEventArgs e)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            SearchTogetherGrid.Visibility = Visibility.Visible;
            CreateMatchRoomPanel.Visibility = Visibility.Collapsed;
            JoinMatchRoomPanel.Visibility = Visibility.Collapsed;
        }

        private void CreateWatchRoom_Click(object sender, RoutedEventArgs e)
        {
            SelectVideoFile();
        }

        private void ShowJoinWatchRoom_Click(object sender, RoutedEventArgs e)
        {
            JoinWatchRoomPanel.Visibility = Visibility.Visible;
            WatchRoomIdTextBox.Text = "";
        }

        private void SelectVideoFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите видеофайл",
                Filter = "Видео файлы (*.mp4;*.avi;*.mkv;*.mov;*.wmv)|*.mp4;*.avi;*.mkv;*.mov;*.wmv|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedVideoPath = openFileDialog.FileName;
                CreateRoomWithVideo();
            }
        }

        private async void CreateRoomWithVideo()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Filamri", "Videos");
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                string videoId = Guid.NewGuid().ToString();
                string videoPath = Path.Combine(appFolder, videoId + Path.GetExtension(_selectedVideoPath));
                File.Copy(_selectedVideoPath, videoPath, true);

                var room = await _apiService.CreateWatchRoom(_userData.UserId, _userData.UserName, videoPath);

                Mouse.OverrideCursor = null;

                if (room != null)
                {
                    MessageBox.Show($"✅ Комната создана!\nID: {room.RoomId}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    var watchWindow = new WatchPartyWindow(videoPath, true, room.RoomId);
                    watchWindow.Owner = this;
                    watchWindow.ShowDialog();
                    Close();
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
                var room = await _apiService.JoinWatchRoom(roomId, _userData.UserId, _userData.UserName);
                Mouse.OverrideCursor = null;

                if (room != null && !string.IsNullOrEmpty(room.VideoPath) && File.Exists(room.VideoPath))
                {
                    var watchWindow = new WatchPartyWindow(room.VideoPath, false, roomId);
                    watchWindow.Owner = this;
                    watchWindow.ShowDialog();
                    Close();
                }
                else
                {
                    MessageBox.Show("❌ Комната не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void CreateMatchRoom_Click(object sender, RoutedEventArgs e)
        {
            CreateMatchRoomPanel.Visibility = Visibility.Visible;
            JoinMatchRoomPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowJoinMatchRoom_Click(object sender, RoutedEventArgs e)
        {
            CreateMatchRoomPanel.Visibility = Visibility.Collapsed;
            JoinMatchRoomPanel.Visibility = Visibility.Visible;
            MatchRoomIdTextBox.Text = "";
        }

        private async void StartMatchSearch_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGenres.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один жанр", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var request = new
                {
                    user_id = _userData.UserId,
                    user_name = _userData.UserName,
                    genres = _selectedGenres
                };

                var room = await _apiService.CreateMatchRoom(request);

                Mouse.OverrideCursor = null;

                if (room != null)
                {
                    string roomId = room.roomId;
                    MessageBox.Show($"✅ Комната поиска создана!\nID: {roomId}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    var swipeWindow = new MatchSwipeWindow(roomId, _userData.UserId, true);
                    swipeWindow.Owner = this;
                    swipeWindow.ShowDialog();
                    Close();
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

        private void MatchRoomIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            JoinMatchRoomButton.IsEnabled = !string.IsNullOrWhiteSpace(MatchRoomIdTextBox.Text);
        }

        private async void JoinMatchRoom_Click(object sender, RoutedEventArgs e)
        {
            string roomId = MatchRoomIdTextBox.Text.Trim();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var request = new { room_id = roomId, user_id = _userData.UserId, user_name = _userData.UserName };
                var room = await _apiService.JoinMatchRoom(request);

                Mouse.OverrideCursor = null;

                if (room != null)
                {
                    var filterDialog = new Dialogs.FilterDialog();
                    if (filterDialog.ShowDialog() == true)
                    {
                        var genres = new List<string>();
                        if (!string.IsNullOrEmpty(filterDialog.Genre))
                            genres.Add(filterDialog.Genre);

                        await _apiService.SelectGenres(roomId, _userData.UserId, genres);

                        var swipeWindow = new MatchSwipeWindow(roomId, _userData.UserId, false);
                        swipeWindow.Owner = this;
                        swipeWindow.ShowDialog();
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show("❌ Комната не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToSearchMenu_Click(object sender, RoutedEventArgs e)
        {
            SearchTogetherGrid.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible;
            CreateMatchRoomPanel.Visibility = Visibility.Collapsed;
            JoinMatchRoomPanel.Visibility = Visibility.Collapsed;
        }
    }

    public class GenreItem
    {
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }
}