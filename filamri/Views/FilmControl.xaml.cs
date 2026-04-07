using filamri.Models;
using filamri.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace filamri.Views
{
    public partial class FilmControl : UserControl
    {
        private Film _film;
        private ApiService _apiService;

        public FilmControl(Film film)
        {
            InitializeComponent();
            _film = film;
            _apiService = new ApiService();
            DisplayFilm();
        }

        private void DisplayFilm()
        {
            TitleText.Text = _film.Name;
            DescriptionText.Text = _film.Description ?? "Описание отсутствует";
            YearText.Text = _film.Year?.ToString() ?? "";
            GenreText.Text = _film.GenresString ?? _film.Genre ?? "";
            RatingText.Text = _film.Rating.HasValue ? $"★ {_film.Rating:F1}" : "";
            CountryText.Text = _film.Country ?? "";
            LengthText.Text = _film.MovieLength.HasValue && _film.MovieLength > 0
                ? $"{_film.MovieLength} мин."
                : "";

            if (_film.Actors != null && _film.Actors.Count > 0)
            {
                var actorsToShow = _film.Actors.Count > 5
                    ? string.Join(", ", _film.Actors.GetRange(0, 5))
                    : string.Join(", ", _film.Actors);
                ActorsText.Text = $"Актеры: {actorsToShow}";
                if (_film.Actors.Count > 5)
                    ActorsText.Text += $"\nи еще {_film.Actors.Count - 5}";
            }
            else
            {
                ActorsText.Text = "";
            }

            if (!string.IsNullOrEmpty(_film.PosterUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_film.PosterUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    PosterImage.Source = bitmap;
                }
                catch
                {
                    PosterImage.Source = null;
                }
            }
        }

        private void CommentsButton_Click(object sender, RoutedEventArgs e)
        {
            var commentsWindow = new CommentsWindow(_film.Id, _film.Name);
            commentsWindow.Owner = Window.GetWindow(this);
            commentsWindow.ShowDialog();
        }
    }
}