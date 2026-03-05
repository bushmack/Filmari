using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace filamri
{
    public partial class MovieMatchWindow : Window
    {
        private readonly ApiService _apiService = new();
        private string _userId = Guid.NewGuid().ToString();
        private string _roomId = "";
        private Timer? _statusTimer;
        private Film? _currentMovie;
        private List<CheckableItem> _allGenres = new();
        private List<string> _selectedGenres = new();

        public MovieMatchWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            InitializeGenres();
        }

        private void InitializeGenres()
        {
            _allGenres = new List<CheckableItem>
            {
                new("боевик"),
                new("вестерн"),
                new("военный"),
                new("детектив"),
                new("документальный"),
                new("драма"),
                new("исторический"),
                new("комедия"),
                new("криминал"),
                new("мелодрама"),
                new("мультфильм"),
                new("музыка"),
                new("приключения"),
                new("семейный"),
                new("спорт"),
                new("триллер"),
                new("ужасы"),
                new("фантастика"),
                new("фэнтези")
            };
            UpdateGenresList();
        }

        private void UpdateGenresList()
        {
            string search = GenreSearchBox?.Text ?? "";
            var filtered = string.IsNullOrEmpty(search)
                ? _allGenres
                : _allGenres.Where(g => g.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            GenresListBox.ItemsSource = filtered;
        }

        private void GenreSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGenresList();
        }

        // СОЗДАНИЕ КОМНАТЫ
        private async void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var request = new
                {
                    user_id = _userId,
                    user_name = $"User_{_userId.Substring(0, 4)}"
                };

                var result = await _apiService.CreateMatchRoom(request);
                _roomId = result.room_id;

                YourIdText.Text = $"Ваш ID: {_userId} | Комната: {_roomId}";
                MessageBox.Show($"✅ Комната создана!\nID комнаты: {_roomId}\n\nПоделитесь этим ID с другом, чтобы он мог присоединиться.",
                    "Комната создана", MessageBoxButton.OK, MessageBoxImage.Information);

                MainMenuGrid.Visibility = Visibility.Collapsed;
                GenreSelectionGrid.Visibility = Visibility.Visible;
                RoomInfoText.Text = $"Комната: {_roomId} | Выбор жанров";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания комнаты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ПРОВЕРКА ВВОДА ID КОМНАТЫ
        private void RoomIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            JoinRoomButton.IsEnabled = !string.IsNullOrWhiteSpace(RoomIdTextBox.Text);
        }

        // ПРИСОЕДИНЕНИЕ К КОМНАТЕ
        private async void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            string roomId = RoomIdTextBox.Text.Trim();

            try
            {
                var request = new
                {
                    room_id = roomId,
                    user_id = _userId,
                    user_name = $"User_{_userId.Substring(0, 4)}"
                };

                var result = await _apiService.JoinMatchRoom(request);

                _roomId = roomId;
                MainMenuGrid.Visibility = Visibility.Collapsed;
                GenreSelectionGrid.Visibility = Visibility.Visible;
                RoomInfoText.Text = $"Комната: {_roomId} | Выбор жанров";
                PartnerStatusText.Text = "Ожидание подтверждения партнера...";

                StartStatusPolling();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Неправильный ID комнаты или комната не существует.\n\nОшибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ПОДТВЕРЖДЕНИЕ ЖАНРОВ
        private async void ConfirmGenres_Click(object sender, RoutedEventArgs e)
        {
            _selectedGenres = _allGenres.Where(g => g.IsSelected).Select(g => g.Name).ToList();

            if (_selectedGenres.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один жанр", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _apiService.SelectGenres(_roomId, _userId, _selectedGenres);
                PartnerStatusText.Text = "Жанры выбраны. Ожидание подтверждения партнера...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelGenres_Click(object sender, RoutedEventArgs e)
        {
            GenreSelectionGrid.Visibility = Visibility.Collapsed;
            MainMenuGrid.Visibility = Visibility.Visible;
        }

        // ПЕРИОДИЧЕСКАЯ ПРОВЕРКА СТАТУСА
        private void StartStatusPolling()
        {
            _statusTimer = new Timer(async _ =>
            {
                try
                {
                    var status = await _apiService.GetRoomStatus(_roomId);
                    await Dispatcher.InvokeAsync(() => UpdateUI(status));
                }
                catch { }
            }, null, 0, 2000);
        }

        private void UpdateUI(dynamic status)
        {
            string statusStr = status.status.ToString();

            switch (statusStr)
            {
                case "selecting_genres":
                    // Уже на экране выбора жанров
                    break;

                case "watching":
                    if (SwipeGrid.Visibility != Visibility.Visible)
                    {
                        GenreSelectionGrid.Visibility = Visibility.Collapsed;
                        SwipeGrid.Visibility = Visibility.Visible;

                        if (status.current_movie != null)
                        {
                            ShowCurrentMovie(status.current_movie);
                        }
                    }

                    SwipeRoomInfoText.Text = $"Комната: {_roomId}";

                    int swipedCount = status.current_movie_swipes?.Count ?? 0;
                    PartnerSwipeIndicator.Text = $"Партнер: {(swipedCount > 0 ? "сделал выбор" : "выбирает...")}";

                    if (status.current_movie != null && (_currentMovie?.Id != (int)status.current_movie.Id))
                    {
                        ShowCurrentMovie(status.current_movie);
                    }
                    break;

                case "matched":
                    ShowMatchScreen(status.matched_film);
                    break;
            }
        }

        private void ShowCurrentMovie(dynamic movie)
        {
            _currentMovie = new Film
            {
                Id = movie.Id,
                Name = movie.Name,
                Description = movie.Description,
                PosterUrl = movie.PosterUrl,
                Year = movie.Year,
                Genre = movie.Genre,
                Rating = movie.Rating,
                AllGenres = movie.AllGenres?.ToObject<List<string>>() ?? new List<string>()
            };

            SwipeTitleText.Text = _currentMovie.Name;
            SwipeDescriptionText.Text = _currentMovie.Description;
            SwipeYearText.Text = _currentMovie.Year?.ToString() ?? "";
            SwipeGenreText.Text = _currentMovie.GenresString ?? _currentMovie.Genre ?? "";
            SwipeRatingText.Text = _currentMovie.Rating.HasValue ? $"★ {_currentMovie.Rating:F1}" : "";

            if (!string.IsNullOrEmpty(_currentMovie.PosterUrl))
            {
                try
                {
                    SwipePosterImage.Source = new BitmapImage(new Uri(_currentMovie.PosterUrl));
                }
                catch { }
            }
        }

        // СВАЙП ВПРАВО (НРАВИТСЯ)
        private async void SwipeRight_Click(object sender, RoutedEventArgs e)
        {
            await SendSwipe(true);
        }

        // СВАЙП ВЛЕВО (НЕ НРАВИТСЯ)
        private async void SwipeLeft_Click(object sender, RoutedEventArgs e)
        {
            await SendSwipe(false);
        }

        private async Task SendSwipe(bool liked)
        {
            if (_currentMovie == null) return;

            try
            {
                var result = await _apiService.SendSwipe(_roomId, _userId, _currentMovie.Id, liked);

                if (result.is_match_found)
                {
                    ShowMatchScreen(result.matched_film);
                }
                else
                {
                    SwipeStatusText.Text = $"Фильм {(liked ? "понравился ❤️" : "не понравился 💔")}";
                    PartnerSwipeIndicator.Text = "Ожидание выбора партнера...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMatchScreen(dynamic movie)
        {
            SwipeGrid.Visibility = Visibility.Collapsed;
            MatchGrid.Visibility = Visibility.Visible;

            MatchTitleText.Text = movie.Name;
            MatchYearText.Text = movie.Year?.ToString() ?? "";
            MatchDescriptionText.Text = movie.Description;

            _statusTimer?.Dispose();
        }

        private void StartWatching_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            _statusTimer?.Dispose();
            if (!string.IsNullOrEmpty(_roomId))
            {
                await _apiService.LeaveRoom(_roomId, _userId);
            }
        }

        private void BackToMain_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class CheckableItem
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }

        public CheckableItem(string name)
        {
            Name = name;
            IsSelected = false;
        }
    }
}