using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.Text.Json;

namespace filamri
{
    public partial class MatchSwipeWindow : Window
    {
        private readonly HttpClient _httpClient = new();
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
                    var response = await _httpClient.GetAsync($"http://localhost:8001/api/match/room-status/{_roomId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        await Dispatcher.InvokeAsync(() => UpdateUI(json));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Polling error: {ex.Message}");
                }
            }, null, 0, 2000);
        }

        private void UpdateUI(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string status = root.GetProperty("status").GetString() ?? "";

                if (status == "watching")
                {
                    if (_movies.Count == 0 && root.TryGetProperty("currentMovies", out var moviesProp))
                    {
                        _movies.Clear();
                        foreach (var movie in moviesProp.EnumerateArray())
                        {
                            var film = new Film();
                            if (movie.TryGetProperty("Id", out var idProp)) film.Id = idProp.GetInt32();
                            if (movie.TryGetProperty("Name", out var nameProp)) film.Name = nameProp.GetString() ?? "";
                            if (movie.TryGetProperty("Description", out var descProp)) film.Description = descProp.GetString() ?? "";
                            if (movie.TryGetProperty("PosterUrl", out var posterProp)) film.PosterUrl = posterProp.GetString() ?? "";
                            if (movie.TryGetProperty("Year", out var yearProp)) film.Year = yearProp.GetInt32();
                            if (movie.TryGetProperty("Genre", out var genreProp)) film.Genre = genreProp.GetString() ?? "";
                            if (movie.TryGetProperty("Rating", out var ratingProp)) film.Rating = ratingProp.GetDouble();
                            if (movie.TryGetProperty("GenresString", out var genresProp)) film.GenresString = genresProp.GetString() ?? "";
                            if (movie.TryGetProperty("Country", out var countryProp)) film.Country = countryProp.GetString() ?? "";
                            if (movie.TryGetProperty("MovieLength", out var lengthProp)) film.MovieLength = lengthProp.GetInt32();
                            if (movie.TryGetProperty("Actors", out var actorsProp))
                            {
                                film.Actors = new List<string>();
                                foreach (var actor in actorsProp.EnumerateArray())
                                {
                                    film.Actors.Add(actor.GetString() ?? "");
                                }
                            }
                            _movies.Add(film);
                        }

                        if (root.TryGetProperty("currentMovieIndex", out var indexProp))
                            _currentIndex = indexProp.GetInt32();

                        ShowCurrentMovie();
                    }

                    CounterText.Text = $"{_currentIndex + 1} из {_movies.Count}";
                    PartnerStatusText.Text = "⏳ Ожидание выбора партнера...";
                }
                else if (status == "matched" && !_isMatchFound)
                {
                    _isMatchFound = true;
                    _statusTimer?.Dispose();

                    if (root.TryGetProperty("matchedFilm", out var matchedProp))
                    {
                        var film = new Film();
                        if (matchedProp.TryGetProperty("Name", out var nameProp)) film.Name = nameProp.GetString() ?? "Фильм";
                        if (matchedProp.TryGetProperty("Description", out var descProp)) film.Description = descProp.GetString() ?? "";
                        ShowMatchScreen(film);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateUI error: {ex.Message}");
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
                var request = new
                {
                    room_id = _roomId,
                    user_id = _userId,
                    movie_id = currentMovie.Id,
                    liked = liked
                };

                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("http://localhost:8001/api/match/swipe", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    bool isMatchFound = false;
                    if (root.TryGetProperty("is_match_found", out var matchProp))
                        isMatchFound = matchProp.GetBoolean();

                    if (isMatchFound)
                    {
                        _isMatchFound = true;
                        _statusTimer?.Dispose();

                        if (root.TryGetProperty("matched_film", out var matchedProp))
                        {
                            var film = new Film();
                            if (matchedProp.TryGetProperty("Name", out var nameProp)) film.Name = nameProp.GetString() ?? "Фильм";
                            if (matchedProp.TryGetProperty("Description", out var descProp)) film.Description = descProp.GetString() ?? "";
                            ShowMatchScreen(film);
                        }
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
            try
            {
                await _httpClient.DeleteAsync($"http://localhost:8001/api/match/leave-room/{_roomId}/{_userId}");
            }
            catch { }
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _statusTimer?.Dispose();
            base.OnClosed(e);
        }
    }
}