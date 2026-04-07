using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace filamri
{
    public partial class FilmDetailWindow : Window
    {
        private readonly ApiService _apiService;
        private List<Film> _allFilms;
        private int _currentIndex;
        private Collection _collection;

        public FilmDetailWindow(Film film, List<Film> allFilms, Collection collection)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _allFilms = allFilms;
            _collection = collection;
            _currentIndex = _allFilms.FindIndex(f => f.Id == film.Id);
            if (_currentIndex == -1) _currentIndex = 0;
            ShowFilm(_allFilms[_currentIndex]);
        }

        private void ShowFilm(Film film)
        {
            TitleText.Text = film.Name;

            if (!string.IsNullOrEmpty(film.Description))
                DescriptionText.Text = film.Description.Length > 250 ? film.Description[..250] + "..." : film.Description;
            else
                DescriptionText.Text = "Описание отсутствует";

            YearText.Text = film.Year?.ToString() ?? "";

            if (!string.IsNullOrEmpty(film.GenresString))
                GenreText.Text = film.GenresString;
            else if (film.AllGenres != null && film.AllGenres.Count > 0)
                GenreText.Text = string.Join(", ", film.AllGenres);
            else
                GenreText.Text = film.Genre ?? "";

            RatingText.Text = film.Rating.HasValue ? $"★ {film.Rating:F1}" : "";
            CountryText.Text = film.Country ?? "";

            if (film.MovieLength.HasValue && film.MovieLength > 0)
                LengthText.Text = $"Длительность: {film.MovieLength} мин.";
            else
                LengthText.Text = "";

            if (film.AgeRating.HasValue && film.AgeRating > 0)
                AgeRatingText.Text = $"Возрастной рейтинг: {film.AgeRating}+";
            else
                AgeRatingText.Text = "";

            if (film.Actors != null && film.Actors.Count > 0)
            {
                var actorsToShow = film.Actors.Take(5).ToList();
                ActorsText.Text = $"Актеры: {string.Join(", ", actorsToShow)}";
                if (film.Actors.Count > 5)
                    ActorsText.Text += $"\nи еще {film.Actors.Count - 5}";
            }
            else
                ActorsText.Text = "";

            NavigationText.Text = $"{_currentIndex + 1} из {_allFilms.Count}";

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
                catch
                {
                    PosterImage.Source = null;
                }
            }

            RemoveFromCollectionButton.Tag = film;
        }

        private void PreviousFilm_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                ShowFilm(_allFilms[_currentIndex]);
            }
        }

        private void NextFilm_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _allFilms.Count - 1)
            {
                _currentIndex++;
                ShowFilm(_allFilms[_currentIndex]);
            }
        }

        private void BackToList_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void RemoveFromCollection_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var film = button?.Tag as Film;
            if (film == null) return;

            var result = MessageBox.Show(
                $"Удалить фильм \"{film.Name}\" из подборки \"{_collection.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _apiService.RemoveFromCollection(_collection.Name, film.Id);
                    MessageBox.Show("✅ Фильм удален из подборки", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CommentsButton_Click(object sender, RoutedEventArgs e)
        {
            var currentFilm = _allFilms[_currentIndex];
            var commentsWindow = new CommentsWindow(currentFilm.Id, currentFilm.Name);
            commentsWindow.Owner = this;
            commentsWindow.ShowDialog();
        }

        // Кнопка "Смотреть вместе" - открывает MovieMatchWindow (совместный просмотр)
        private void WatchPartyButton_Click(object sender, RoutedEventArgs e)
        {
            var matchWindow = new MovieMatchWindow();
            matchWindow.Owner = this;
            matchWindow.ShowDialog();
        }
    }
}