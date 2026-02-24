using OneButtonApp.Models;
using OneButtonApp.Services;
using OneButtonApp.Views;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace OneButtonApp
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService = new();
        private List<Film> _movies = new();
        private int _currentIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        // 5 случайных фильмов
        private async void LoadRandomMovies_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _movies = await _apiService.GetRandomMoviesAsync();
                _currentIndex = 0;
                DisplayCurrentFilm();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 5 случайных сериалов
        private async void LoadRandomSereals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _movies = await _apiService.GetRandomSeriesAsync();
                _currentIndex = 0;
                DisplayCurrentFilm();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Мои подборки
        private async void MyCollections_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var collections = await _apiService.GetCollectionsAsync();
                if (collections.Count == 0)
                {
                    MessageBox.Show("У вас пока нет подборок", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Здесь можно открыть окно с подборками
                    string message = "Ваши подборки:\n";
                    foreach (var collection in collections)
                    {
                        message += $"\n• {collection.Name} ({collection.Movies.Count} фильмов)";
                    }
                    MessageBox.Show(message, "Мои подборки",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Поиск по названию
        private async void NameSearch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Введите название фильма:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    _movies = await _apiService.SearchByNameAsync(dialog.InputText);
                    _currentIndex = 0;
                    DisplayCurrentFilm();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Поиск по фильтру
        private async void FilterSearch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FilterDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _movies = await _apiService.SearchByFilterAsync(
                        genre: dialog.Genre,
                        yearFrom: dialog.YearFrom,
                        yearTo: dialog.YearTo,
                        ratingFrom: dialog.RatingFrom,
                        ratingTo: dialog.RatingTo,
                        country: dialog.Country
                    );
                    _currentIndex = 0;
                    DisplayCurrentFilm();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Совместный поиск
        private async void CombinedSearch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CombinedSearchDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _movies = await _apiService.CombinedSearchAsync(
                        query: dialog.Query,
                        actor: dialog.Actor,
                        genre: dialog.Genre,
                        year: dialog.Year
                    );
                    _currentIndex = 0;
                    DisplayCurrentFilm();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Поиск по актерам
        private async void SerchAkter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Введите имя актера:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    _movies = await _apiService.SearchByActorAsync(dialog.InputText);
                    _currentIndex = 0;
                    DisplayCurrentFilm();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisplayCurrentFilm()
        {
            FilmContainer.Children.Clear();

            if (_movies.Count == 0)
            {
                FilmContainer.Children.Add(new TextBlock
                {
                    Text = "Ничего не найдено.",
                    Margin = new Thickness(10),
                    FontSize = 16
                });
                return;
            }

            var film = _movies[_currentIndex];
            var filmControl = new FilmControl(film);

            var stack = new StackPanel { Orientation = Orientation.Vertical };
            stack.Children.Add(filmControl);

            var navPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var prevBtn = new Button { Content = "<", Width = 30, Height = 30, FontSize = 14 };
            prevBtn.Click += (s, e) => NavigateTo(-1);
            navPanel.Children.Add(prevBtn);

            var indexLabel = new Label
            {
                Content = $"{_currentIndex + 1} из {_movies.Count}",
                Padding = new Thickness(10, 5, 10, 5),
                FontSize = 14
            };
            navPanel.Children.Add(indexLabel);

            var nextBtn = new Button { Content = ">", Width = 30, Height = 30, FontSize = 14 };
            nextBtn.Click += (s, e) => NavigateTo(1);
            navPanel.Children.Add(nextBtn);

            // Кнопка "Добавить в подборку"
            var addToCollectionBtn = new Button
            {
                Content = "➕ В подборку",
                Width = 100,
                Height = 30,
                Margin = new Thickness(10, 0, 0, 0),
                FontSize = 12,
                ToolTip = "Добавить в мою подборку"
            };
            addToCollectionBtn.Click += async (s, e) =>
            {
                var collectionDialog = new InputDialog("Введите название подборки:");
                if (collectionDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(collectionDialog.InputText))
                {
                    try
                    {
                        await _apiService.AddToCollectionAsync(film.Id, collectionDialog.InputText);
                        MessageBox.Show($"Фильм добавлен в подборку \"{collectionDialog.InputText}\"!",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
            navPanel.Children.Add(addToCollectionBtn);

            stack.Children.Add(navPanel);
            FilmContainer.Children.Add(stack);
        }

        private void NavigateTo(int direction)
        {
            if (_movies.Count == 0) return;

            _currentIndex += direction;
            if (_currentIndex < 0)
                _currentIndex = _movies.Count - 1;
            else if (_currentIndex >= _movies.Count)
                _currentIndex = 0;

            DisplayCurrentFilm();
        }
    }
}