using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace filamri
{
    public partial class CollectionDetailWindow : Window
    {
        private readonly ApiService _apiService = new ApiService();
        private Collection _collection;
        private List<Film> _films = new List<Film>();

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
                    if (film != null) _films.Add(film);
                }
                MoviesList.ItemsSource = _films;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильмов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            var film = border?.Tag as Film;
            if (film != null)
            {
                // Передаем коллекцию, а не bool
                var detailWindow = new FilmDetailWindow(film, _films, _collection);
                detailWindow.Owner = this;
                detailWindow.ShowDialog();
                LoadMovies();
            }
        }

        private async void RemoveFromCollection_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var film = button?.Tag as Film;
            if (film == null) return;

            var result = MessageBox.Show($"Удалить фильм \"{film.Name}\" из подборки?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _apiService.RemoveFromCollection(_collection.Name, film.Id);
                _collection.Movies.Remove(film.Id);
                _films.Remove(film);
                LoadMovies();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}