using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace filamri
{
    public partial class CollectionViewWindow : Window
    {
        private readonly ApiService _apiService = new();
        private Collection _collection;
        private List<Film> _films = new();

        public CollectionViewWindow(Collection collection)
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
                var allFilms = new List<Film>();
                foreach (var movieId in _collection.Movies)
                {
                    var film = await _apiService.GetMovieById(movieId);
                    if (film != null)
                    {
                        allFilms.Add(film);
                    }
                }
                _films = allFilms;
                MoviesList.ItemsSource = _films;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильмов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    try
                    {
                        await _apiService.RemoveFromCollection(_collection.Name, film.Id);
                        _collection.Movies.Remove(film.Id);
                        _films.Remove(film);
                        MoviesList.ItemsSource = null;
                        MoviesList.ItemsSource = _films;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}