using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace filamri
{
    public partial class MatchSwipeWindow : Window
    {
        private readonly ApiService _apiService = new();
        private Timer? _statusTimer;
        private string _roomId;
        private string _userId;
        private bool _isHost;
        private List<Film> _movies = new();
        private int _currentIndex = 0;
        private bool _isMatchFound = false;

        public MatchSwipeWindow(string roomId, string userId, bool isHost)
        {
            InitializeComponent();
            _roomId = roomId;
            _userId = userId;
            _isHost = isHost;

            RoomInfoText.Text = $"🎬 Комната: {_roomId}";
            PartnerStatusText.Text = "⏳ Ожидание партнера...";

            StartPolling();
        }

        private void StartPolling()
        {
            _statusTimer = new Timer(async _ =>
            {
                try
                {
                    var status = await _apiService.GetRoomStatus(_roomId);
                    await Dispatcher.InvokeAsync(() => UpdateUI(status));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Polling error: {ex.Message}");
                }
            }, null, 0, 2000);
        }

        private void UpdateUI(MatchRoomResponse? status)
        {
            if (status == null) return;

            if (status.status == "watching")
            {
                if (_movies.Count == 0 && status.currentMovies != null && status.currentMovies.Count > 0)
                {
                    _movies = status.currentMovies;
                    _currentIndex = status.currentMovieIndex;
                    ShowCurrentMovie();
                }

                CounterText.Text = $"{_currentIndex + 1} из {_movies.Count}";
                PartnerStatusText.Text = "⏳ Ожидание выбора партнера...";
            }
            else if (status.status == "matched" && !_isMatchFound && status.matchedFilm != null)
            {
                _isMatchFound = true;
                _statusTimer?.Dispose();
                ShowMatchScreen(status.matchedFilm);
            }
        }

        private void ShowCurrentMovie()
        {
            if (_movies.Count == 0 || _currentIndex >= _movies.Count) return;

            var film = _movies[_currentIndex];

            TitleText.Text = film.Name;
            DescriptionText.Text = string.IsNullOrEmpty(film.Description) ? "Описание отсутствует" : film.Description;
            YearText.Text = film.Year?.ToString() ?? "";
            GenreText.Text = film.GenresString ?? film.Genre ?? "";
            RatingText.Text = film.Rating.HasValue ? $"★ {film.Rating:F1}" : "";
            CountryText.Text = film.Country ?? "";
            LengthText.Text = film.MovieLength.HasValue ? $"{film.MovieLength} мин." : "";

            if (film.Actors != null && film.Actors.Count > 0)
            {
                var actorsToShow = film.Actors.Count > 5 ? film.Actors.GetRange(0, 5) : film.Actors;
                ActorsText.Text = string.Join(", ", actorsToShow);
                if (film.Actors.Count > 5)
                    ActorsText.Text += $"\nи еще {film.Actors.Count - 5}";
            }
            else
            {
                ActorsText.Text = "";
            }

            if (!string.IsNullOrEmpty(film.PosterUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(film.PosterUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PosterImage.Source = bitmap;
                }
                catch { PosterImage.Source = null; }
            }
        }

        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            await SendSwipe(true);
        }

        private async void DislikeButton_Click(object sender, RoutedEventArgs e)
        {
            await SendSwipe(false);
        }

        private async System.Threading.Tasks.Task SendSwipe(bool liked)
        {
            if (_movies.Count == 0 || _currentIndex >= _movies.Count) return;

            var currentMovie = _movies[_currentIndex];

            try
            {
                var result = await _apiService.SendSwipe(_roomId, _userId, currentMovie.Id, liked);

                if (result != null && result.isMatchFound && result.matchedFilm != null)
                {
                    _isMatchFound = true;
                    _statusTimer?.Dispose();
                    ShowMatchScreen(result.matchedFilm);
                }
                else
                {
                    if (_currentIndex + 1 < _movies.Count)
                    {
                        _currentIndex++;
                        ShowCurrentMovie();
                        CounterText.Text = $"{_currentIndex + 1} из {_movies.Count}";
                    }
                    PartnerStatusText.Text = "⏳ Ожидание выбора партнера...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                ShowCurrentMovie();
                CounterText.Text = $"{_currentIndex + 1} из {_movies.Count}";
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _movies.Count - 1)
            {
                _currentIndex++;
                ShowCurrentMovie();
                CounterText.Text = $"{_currentIndex + 1} из {_movies.Count}";
            }
        }

        private void ShowMatchScreen(Film film)
        {
            MessageBox.Show(
                $"🎉 МЭТЧ! 🎉\n\nВы оба хотите посмотреть:\n\n{film.Name}\n\n{film.Description}",
                "Мэтч!",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _statusTimer?.Dispose();
            try { await _apiService.LeaveRoom(_roomId, _userId); } catch { }
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _statusTimer?.Dispose();
            base.OnClosed(e);
        }
    }
}