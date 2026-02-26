using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace filamri
{
    public partial class CollectionDetailWindow : Window
    {
        private readonly ApiService _apiService = new();
        private Collection _collection;
        private List<Film> _films = new();

        public CollectionDetailWindow(Collection collection)
        {
            InitializeComponent();
            _collection = collection;
            CollectionTitle.Text = $"📁 {collection.Name}";
            LoadMovies();
        }

        private async void LoadMovies()
        {
            try
            {
                _films.Clear();

                foreach (var movieId in _collection.Movies)
                {
                    var film = await _apiService.GetMovieById(movieId);
                    if (film != null)
                    {
                        _films.Add(film);
                    }
                }

                MoviesList.ItemsSource = _films;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильмов: {ex.Message}");
            }
        }

        private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            var film = border?.Tag as Film;

            if (film != null)
            {
                // Передаем ВСЕ фильмы из подборки для навигации
                var detailWindow = new FilmDetailWindow(film, _films, true);
                detailWindow.Owner = this;
                detailWindow.ShowDialog();
                LoadMovies(); // Перезагружаем после закрытия
            }
        }

        private async void RemoveFromCollection_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var film = button?.Tag as Film;

            if (film != null)
            {
                var result = MessageBox.Show(
                    $"Удалить фильм \"{film.Name}\" из подборки?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _apiService.RemoveFromCollection(_collection.Name, film.Id);
                    _collection.Movies.Remove(film.Id);
                    LoadMovies();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}