using filamri.Dialogs;
using filamri.Models;
using filamri.Services;
using filamri.Views;
using filamri.Pages;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace filamri
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService = new();
        private List<Film> _movies = new();
        private int _currentIndex = 0;
        private StackPanel? FilmContainer { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ShowFilmContainer();
        }

        private void ShowFilmContainer()
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            scrollViewer.Content = stackPanel;
            FilmContainer = stackPanel;
            MainContent.Content = scrollViewer;
        }

        private async void LoadRandomMovies_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _movies = await _apiService.GetRandomMoviesAsync();
                _currentIndex = 0;
                ShowFilmContainer();
                DisplayCurrentFilm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadRandomSeries_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _movies = await _apiService.GetRandomSeriesAsync();
                _currentIndex = 0;
                ShowFilmContainer();
                DisplayCurrentFilm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NameSearch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Введите название фильма:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    _movies = await _apiService.SearchByNameAsync(dialog.InputText);
                    _currentIndex = 0;
                    ShowFilmContainer();
                    DisplayCurrentFilm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ActorSearch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Введите имя актера:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    _movies = await _apiService.SearchByActorAsync(dialog.InputText);
                    _currentIndex = 0;
                    ShowFilmContainer();
                    DisplayCurrentFilm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void FilterSearch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FilterDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ShowFilmContainer();

                    var loadingText = new TextBlock
                    {
                        Text = "⚡ Поиск по фильтру...",
                        FontSize = 18,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(50)
                    };
                    FilmContainer?.Children.Add(loadingText);

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
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CombinedSearch_Click(object sender, RoutedEventArgs e)
        {
            var matchWindow = new MovieMatchWindow();
            matchWindow.Owner = this;
            matchWindow.ShowDialog();
        }

        private void MyCollections_Click(object sender, RoutedEventArgs e)
        {
            var collectionsWindow = new CollectionsWindow();
            collectionsWindow.Owner = this;
            collectionsWindow.ShowDialog();
        }

        private void DisplayCurrentFilm()
        {
            if (FilmContainer == null) return;

            FilmContainer.Children.Clear();

            if (_movies.Count == 0)
            {
                FilmContainer.Children.Add(new TextBlock
                {
                    Text = "😕 Ничего не найдено",
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(50)
                });
                return;
            }

            var film = _movies[_currentIndex];
            var filmControl = new FilmControl(film);
            filmControl.MouseLeftButtonUp += (s, e) => OpenFilmDetail(film);

            var stack = new StackPanel { Orientation = Orientation.Vertical };
            stack.Children.Add(filmControl);

            var navPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 10)
            };

            var prevBtn = new Button
            {
                Content = "◀",
                Width = 40,
                Height = 40,
                FontSize = 18,
                Margin = new Thickness(5),
                Cursor = Cursors.Hand
            };
            prevBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0078D7"));
            prevBtn.Foreground = Brushes.White;
            prevBtn.Click += (s, args) => NavigateTo(-1);
            navPanel.Children.Add(prevBtn);

            var indexLabel = new Label
            {
                Content = $"{_currentIndex + 1} из {_movies.Count}",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };
            navPanel.Children.Add(indexLabel);

            var nextBtn = new Button
            {
                Content = "▶",
                Width = 40,
                Height = 40,
                FontSize = 18,
                Margin = new Thickness(5),
                Cursor = Cursors.Hand
            };
            nextBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0078D7"));
            nextBtn.Foreground = Brushes.White;
            nextBtn.Click += (s, args) => NavigateTo(1);
            navPanel.Children.Add(nextBtn);

            var addBtn = new Button
            {
                Content = "📥 В подборку",
                Width = 120,
                Height = 40,
                Margin = new Thickness(20, 0, 0, 0),
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            addBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF107C10"));
            addBtn.Foreground = Brushes.White;

            addBtn.Click += async (s, args) =>
            {
                var selectWindow = new SelectCollectionWindow(film.Id);
                selectWindow.Owner = this;

                if (selectWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(selectWindow.SelectedCollectionName))
                {
                    try
                    {
                        await _apiService.AddToCollectionAsync(film.Id, selectWindow.SelectedCollectionName);
                        MessageBox.Show($"✅ Фильм добавлен в подборку \"{selectWindow.SelectedCollectionName}\"",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
            navPanel.Children.Add(addBtn);

            stack.Children.Add(navPanel);
            FilmContainer.Children.Add(stack);
        }

        private void OpenFilmDetail(Film film)
        {
            // Из главного окна всегда fromCollection = false
            var detailWindow = new FilmDetailWindow(film, _movies, false);
            detailWindow.Owner = this;
            detailWindow.ShowDialog();

            _currentIndex = _movies.FindIndex(f => f.Id == film.Id);
            if (_currentIndex >= 0)
            {
                DisplayCurrentFilm();
            }
        }

        private void NavigateTo(int direction)
        {
            if (_movies.Count == 0) return;

            _currentIndex += direction;
            if (_currentIndex < 0) _currentIndex = _movies.Count - 1;
            else if (_currentIndex >= _movies.Count) _currentIndex = 0;

            DisplayCurrentFilm();
        }
    }
}