using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace filamri.Dialogs
{
    public partial class FilterDialog : Window
    {
        public string? Genre { get; private set; }
        public int? YearFrom { get; private set; }
        public int? YearTo { get; private set; }
        public double? RatingFrom { get; private set; }
        public double? RatingTo { get; private set; }
        public string? Country { get; private set; }

        private List<CheckableItem> _allGenres = new();
        private List<CheckableItem> _allCountries = new();

        public FilterDialog()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            InitializeData();
        }

        private void InitializeData()
        {
            // Инициализация списка жанров
            _allGenres = new List<CheckableItem>
            {
                new("боевик"),
                new("вестерн"),
                new("военный"),
                new("детектив"),
                new("документальный"),
                new("драма"),
                new("исторический"),
                new("комедия"),
                new("криминал"),
                new("мелодрама"),
                new("мультфильм"),
                new("музыка"),
                new("приключения"),
                new("семейный"),
                new("спорт"),
                new("триллер"),
                new("ужасы"),
                new("фантастика"),
                new("фэнтези")
            };
            UpdateGenresList();

            // Инициализация списка стран
            _allCountries = new List<CheckableItem>
            {
                new("Австралия"),
                new("Австрия"),
                new("Аргентина"),
                new("Бельгия"),
                new("Бразилия"),
                new("Великобритания"),
                new("Венгрия"),
                new("Германия"),
                new("Гонконг"),
                new("Дания"),
                new("Индия"),
                new("Ирландия"),
                new("Испания"),
                new("Италия"),
                new("Канада"),
                new("Китай"),
                new("Мексика"),
                new("Нидерланды"),
                new("Новая Зеландия"),
                new("Норвегия"),
                new("Польша"),
                new("Россия"),
                new("СССР"),
                new("США"),
                new("Турция"),
                new("Украина"),
                new("Финляндия"),
                new("Франция"),
                new("Чехия"),
                new("Швейцария"),
                new("Швеция"),
                new("Южная Корея"),
                new("Япония")
            };
            UpdateCountriesList();

            // Устанавливаем значения по умолчанию
            YearFromTextBox.Text = "1990";
            YearToTextBox.Text = DateTime.Now.Year.ToString();
            RatingFromComboBox.Text = "5.0";
            RatingToComboBox.Text = "10.0";
        }

        private void UpdateGenresList()
        {
            string search = GenreSearchBox?.Text ?? "";
            var filtered = string.IsNullOrEmpty(search)
                ? _allGenres
                : _allGenres.Where(g => g.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            GenresListBox.ItemsSource = filtered;
        }

        private void UpdateCountriesList()
        {
            string search = CountrySearchBox?.Text ?? "";
            var filtered = string.IsNullOrEmpty(search)
                ? _allCountries
                : _allCountries.Where(c => c.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            CountriesListBox.ItemsSource = filtered;
        }

        private void GenreSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGenresList();
        }

        private void CountrySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCountriesList();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void YearFromUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(YearFromTextBox.Text, out int year))
                YearFromTextBox.Text = (year + 1).ToString();
        }

        private void YearFromDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(YearFromTextBox.Text, out int year) && year > 1900)
                YearFromTextBox.Text = (year - 1).ToString();
        }

        private void YearToUp_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(YearToTextBox.Text, out int year))
                YearToTextBox.Text = (year + 1).ToString();
        }

        private void YearToDown_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(YearToTextBox.Text, out int year) && year > 1900)
                YearToTextBox.Text = (year - 1).ToString();
        }

        private void RatingOption_Checked(object sender, RoutedEventArgs e)
        {
            if (RatingAnyRadio.IsChecked == true)
            {
                RatingFromComboBox.Text = "0";
                RatingToComboBox.Text = "10";
                RatingFromComboBox.IsEnabled = true;
                RatingToComboBox.IsEnabled = true;
            }
            else if (RatingHighRadio.IsChecked == true)
            {
                RatingFromComboBox.Text = "7";
                RatingToComboBox.Text = "10";
                RatingFromComboBox.IsEnabled = false;
                RatingToComboBox.IsEnabled = false;
            }
            else if (RatingMediumRadio.IsChecked == true)
            {
                RatingFromComboBox.Text = "5";
                RatingToComboBox.Text = "7";
                RatingFromComboBox.IsEnabled = false;
                RatingToComboBox.IsEnabled = false;
            }
            else if (RatingLowRadio.IsChecked == true)
            {
                RatingFromComboBox.Text = "0";
                RatingToComboBox.Text = "5";
                RatingFromComboBox.IsEnabled = false;
                RatingToComboBox.IsEnabled = false;
            }
        }

        // ========== МЕТОДЫ ЗАГРУЗКИ ==========

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GenresPanel.Visibility = Visibility.Collapsed;
            CountriesPanel.Visibility = Visibility.Collapsed;
            RatingPanel.Visibility = Visibility.Collapsed;
        }

        // ========== МЕТОДЫ ДЛЯ ЖАНРОВ ==========

        private void ShowGenresButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGenresButton.Visibility = Visibility.Collapsed;
            GenresPanel.Visibility = Visibility.Visible;
            GenreSearchBox.Focus();
        }

        private void GenreRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio?.DataContext is CheckableItem selectedItem)
            {
                foreach (var genre in _allGenres.Where(g => g != selectedItem))
                {
                    genre.IsSelected = false;
                }
            }
        }

        private void SelectGenre_Click(object sender, RoutedEventArgs e)
        {
            var selectedGenre = _allGenres.FirstOrDefault(g => g.IsSelected);
            if (selectedGenre != null)
            {
                SelectedGenreText.Text = selectedGenre.Name;
                SelectedGenreBorder.Visibility = Visibility.Visible;
                Genre = selectedGenre.Name;
            }

            GenresPanel.Visibility = Visibility.Collapsed;
            ShowGenresButton.Visibility = Visibility.Visible;
        }

        private void CancelGenre_Click(object sender, RoutedEventArgs e)
        {
            GenresPanel.Visibility = Visibility.Collapsed;
            ShowGenresButton.Visibility = Visibility.Visible;

            foreach (var genre in _allGenres)
                genre.IsSelected = false;
            UpdateGenresList();
            GenreSearchBox.Text = "";
        }

        private void ClearGenre_Click(object sender, RoutedEventArgs e)
        {
            SelectedGenreBorder.Visibility = Visibility.Collapsed;
            Genre = null;

            foreach (var genre in _allGenres)
                genre.IsSelected = false;
        }

        // ========== МЕТОДЫ ДЛЯ СТРАН ==========

        private void ShowCountriesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCountriesButton.Visibility = Visibility.Collapsed;
            CountriesPanel.Visibility = Visibility.Visible;
            CountrySearchBox.Focus();
        }

        private void CountryRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio?.DataContext is CheckableItem selectedItem)
            {
                foreach (var country in _allCountries.Where(c => c != selectedItem))
                {
                    country.IsSelected = false;
                }
            }
        }

        private void SelectCountry_Click(object sender, RoutedEventArgs e)
        {
            var selectedCountry = _allCountries.FirstOrDefault(c => c.IsSelected);
            if (selectedCountry != null)
            {
                SelectedCountryText.Text = selectedCountry.Name;
                SelectedCountryBorder.Visibility = Visibility.Visible;
                Country = selectedCountry.Name;
            }

            CountriesPanel.Visibility = Visibility.Collapsed;
            ShowCountriesButton.Visibility = Visibility.Visible;
        }

        private void CancelCountry_Click(object sender, RoutedEventArgs e)
        {
            CountriesPanel.Visibility = Visibility.Collapsed;
            ShowCountriesButton.Visibility = Visibility.Visible;

            foreach (var country in _allCountries)
                country.IsSelected = false;
            UpdateCountriesList();
            CountrySearchBox.Text = "";
        }

        private void ClearCountry_Click(object sender, RoutedEventArgs e)
        {
            SelectedCountryBorder.Visibility = Visibility.Collapsed;
            Country = null;

            foreach (var country in _allCountries)
                country.IsSelected = false;
        }

        // ========== МЕТОДЫ ДЛЯ РЕЙТИНГА ==========

        private void ShowRatingButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRatingButton.Visibility = Visibility.Collapsed;
            RatingPanel.Visibility = Visibility.Visible;
        }

        private void SelectRating_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(RatingFromComboBox.Text, out double from) &&
                double.TryParse(RatingToComboBox.Text, out double to))
            {
                RatingFrom = from;
                RatingTo = to;
                SelectedRatingText.Text = $"от {from:F1} до {to:F1}";
                SelectedRatingBorder.Visibility = Visibility.Visible;
            }

            RatingPanel.Visibility = Visibility.Collapsed;
            ShowRatingButton.Visibility = Visibility.Visible;
        }

        private void CancelRating_Click(object sender, RoutedEventArgs e)
        {
            RatingPanel.Visibility = Visibility.Collapsed;
            ShowRatingButton.Visibility = Visibility.Visible;
        }

        private void ClearRating_Click(object sender, RoutedEventArgs e)
        {
            SelectedRatingBorder.Visibility = Visibility.Collapsed;
            RatingFrom = null;
            RatingTo = null;
            RatingAnyRadio.IsChecked = true;
            RatingFromComboBox.Text = "5.0";
            RatingToComboBox.Text = "10.0";
        }

        // ========== МЕТОДЫ ДЛЯ КНОПОК ==========

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем жанры
            foreach (var genre in _allGenres)
                genre.IsSelected = false;
            SelectedGenreBorder.Visibility = Visibility.Collapsed;
            Genre = null;
            UpdateGenresList();
            GenreSearchBox.Text = "";

            // Сбрасываем страны
            foreach (var country in _allCountries)
                country.IsSelected = false;
            SelectedCountryBorder.Visibility = Visibility.Collapsed;
            Country = null;
            UpdateCountriesList();
            CountrySearchBox.Text = "";

            // Сбрасываем годы
            YearFromTextBox.Text = "1990";
            YearToTextBox.Text = DateTime.Now.Year.ToString();

            // Сбрасываем рейтинг
            SelectedRatingBorder.Visibility = Visibility.Collapsed;
            RatingFrom = null;
            RatingTo = null;
            RatingAnyRadio.IsChecked = true;
            RatingFromComboBox.Text = "5.0";
            RatingToComboBox.Text = "10.0";
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(YearFromTextBox.Text, out int yf)) YearFrom = yf;
            if (int.TryParse(YearToTextBox.Text, out int yt)) YearTo = yt;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CheckableItem
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }

        public CheckableItem(string name)
        {
            Name = name;
            IsSelected = false;
        }
    }
}