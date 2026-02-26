using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace filamri
{
    public partial class FilmDetailWindow : Window
    {
        private readonly ApiService _apiService = new();
        private List<Film> _allFilms;
        private int _currentIndex;
        private bool _fromCollection; // Флаг, что окно открыто из подборки

        public FilmDetailWindow(Film film, List<Film> allFilms, bool fromCollection = false)
        {
            InitializeComponent();
            _allFilms = allFilms;
            _fromCollection = fromCollection;
            _currentIndex = _allFilms.FindIndex(f => f.Id == film.Id);
            if (_currentIndex == -1) _currentIndex = 0;

            ShowFilm(_allFilms[_currentIndex]);
        }

        private void ShowFilm(Film film)
        {
            TitleText.Text = film.Name;
            DescriptionText.Text = film.Description ?? "Описание отсутствует";
            YearText.Text = film.Year?.ToString() ?? "";
            GenreText.Text = film.Genre ?? "";
            RatingText.Text = film.Rating.HasValue ? $"★ {film.Rating:F1}" : "";
            CountryText.Text = film.Country ?? "";
            ActorsText.Text = film.Actors != null && film.Actors.Count > 0
                ? string.Join(", ", film.Actors)
                : "";

            NavigationText.Text = $"{_currentIndex + 1} из {_allFilms.Count}";

            if (!string.IsNullOrEmpty(film.PosterUrl))
            {
                try
                {
                    PosterImage.Source = new BitmapImage(new Uri(film.PosterUrl));
                }
                catch
                {
                    PosterImage.Source = null;
                }
            }

            // Если окно открыто из подборки - скрываем кнопку добавления
            if (_fromCollection)
            {
                AddToCollectionButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddToCollectionButton.Visibility = Visibility.Visible;
                AddToCollectionButton.Tag = film;
            }
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

        private async void AddToCollection_Click(object sender, RoutedEventArgs e)
        {
            var film = (sender as System.Windows.Controls.Button)?.Tag as Film;
            if (film == null) return;

            var selectWindow = new SelectCollectionWindow(film.Id);
            selectWindow.Owner = this;

            if (selectWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(selectWindow.SelectedCollectionName))
            {
                await _apiService.AddToCollectionAsync(film.Id, selectWindow.SelectedCollectionName);
                MessageBox.Show($"✅ Фильм добавлен в подборку \"{selectWindow.SelectedCollectionName}\"",
                    "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}