using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace filamri.Pages
{
    public partial class MovieMatchPage : Page
    {
        private readonly ApiService _apiService = new();
        private string _userId = Guid.NewGuid().ToString();
        private string _userName = "";
        private string _roomId = "";
        private List<string> _selectedGenres = new();
        private Timer? _statusTimer;
        private Film? _currentMovie;

        public Action? ReturnToMain { get; set; }

        public MovieMatchPage()
        {
            InitializeComponent();
        }

        private async void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            _userName = HostNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(_userName))
            {
                MessageBox.Show("Введите имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var genres = HostGenresListBox.SelectedItems
                .Cast<ListBoxItem>()
                .Select(x => x.Content.ToString()!)
                .ToList();

            if (genres.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один жанр", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var request = new { user_id = _userId, user_name = _userName, genres };
                var result = await _apiService.CreateMatchRoom(request);

                _roomId = result.room_id;
                _selectedGenres = genres;

                SetupGrid.Visibility = Visibility.Collapsed;
                WaitingGrid.Visibility = Visibility.Visible;
                WaitingRoomInfo.Text = $"ID комнаты: {_roomId}\nОжидание подключения...";

                StartStatusPolling();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            _userName = JoinerNameTextBox.Text;
            _roomId = RoomIdTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(_userName) || string.IsNullOrWhiteSpace(_roomId))
            {
                MessageBox.Show("Введите имя и ID комнаты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var genres = JoinerGenresListBox.SelectedItems
                .Cast<ListBoxItem>()
                .Select(x => x.Content.ToString()!)
                .ToList();

            if (genres.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один жанр", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var request = new { room_id = _roomId, user_id = _userId, user_name = _userName, genres };
                var result = await _apiService.JoinMatchRoom(request);

                SetupGrid.Visibility = Visibility.Collapsed;
                WaitingGrid.Visibility = Visibility.Visible;
                WaitingRoomInfo.Text = $"Подключение к комнате {_roomId}...\nОжидание начала...";

                StartStatusPolling();
            }
            catch (Exception ex)
            {
                JoinErrorText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private async void ConfirmGenres_Click(object sender, RoutedEventArgs e)
        {
            var genres = FinalGenresListBox.SelectedItems
                .Cast<ListBoxItem>()
                .Select(x => x.Content.ToString()!)
                .ToList();

            if (genres.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один жанр", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _apiService.SelectGenres(_roomId, _userId, genres);
                PartnerStatusText.Text = "Ожидание подтверждения партнера...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SwipeLeft_Click(object sender, RoutedEventArgs e) => await SendSwipe(false);
        private async void SwipeRight_Click(object sender, RoutedEventArgs e) => await SendSwipe(true);

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
                    UpdateSwipeUI(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                    if (GenresGrid.Visibility != Visibility.Visible)
                    {
                        WaitingGrid.Visibility = Visibility.Collapsed;
                        GenresGrid.Visibility = Visibility.Visible;
                    }
                    break;

                case "watching":
                    if (SwipeGrid.Visibility != Visibility.Visible)
                    {
                        GenresGrid.Visibility = Visibility.Collapsed;
                        SwipeGrid.Visibility = Visibility.Visible;

                        if (status.current_movie != null)
                        {
                            ShowCurrentMovie(status.current_movie);
                        }
                    }

                    RoomInfoText.Text = $"Комната: {_roomId} | Фильм {status.current_movie_index + 1} из {status.total_movies}";

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
                Rating = movie.Rating
            };

            SwipeTitleText.Text = _currentMovie.Name;
            SwipeDescriptionText.Text = _currentMovie.Description;
            SwipeYearText.Text = _currentMovie.Year?.ToString() ?? "";

            if (!string.IsNullOrEmpty(_currentMovie.PosterUrl))
            {
                try
                {
                    SwipePosterImage.Source = new BitmapImage(new Uri(_currentMovie.PosterUrl));
                }
                catch { }
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

        private void UpdateSwipeUI(dynamic result)
        {
            if (result.current_movie != null)
            {
                ShowCurrentMovie(result.current_movie);
            }

            RoomInfoText.Text = $"Комната: {_roomId} | Фильм {result.current_movie_index + 1} из {result.total_movies}";

            int swipedCount = result.current_movie_swipes?.Count ?? 0;
            PartnerSwipeIndicator.Text = $"Партнер: {(swipedCount > 0 ? "сделал выбор" : "выбирает...")}";
        }

        private async void CancelWaiting_Click(object sender, RoutedEventArgs e)
        {
            _statusTimer?.Dispose();
            await _apiService.LeaveRoom(_roomId, _userId);
            ReturnToMain?.Invoke();
        }

        private void StartWatching_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMain?.Invoke();
        }
    }
}