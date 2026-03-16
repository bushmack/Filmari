using filamri.Models;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace filamri.Views
{
    public partial class FilmControl : UserControl
    {
        public FilmControl(Film film)
        {
            InitializeComponent();

            TitleText.Text = film.Name;

            // Полное описание (без обрезания)
            DescriptionText.Text = film.Description ?? "Описание отсутствует";

            YearText.Text = film.Year?.ToString() ?? "";

            // Все жанры полностью
            if (!string.IsNullOrEmpty(film.GenresString))
            {
                GenreText.Text = film.GenresString;
            }
            else if (film.AllGenres != null && film.AllGenres.Count > 0)
            {
                GenreText.Text = string.Join(", ", film.AllGenres);
            }
            else
            {
                GenreText.Text = film.Genre ?? "";
            }

            RatingText.Text = film.Rating.HasValue ? $"★ {film.Rating:F1}" : "";
            CountryText.Text = film.Country ?? "";

            // Все актеры (первые 10)
            if (film.Actors != null && film.Actors.Count > 0)
            {
                var actorsToShow = film.Actors.Take(10).ToList();
                ActorsText.Text = $"Актеры: {string.Join(", ", actorsToShow)}";
                if (film.Actors.Count > 10)
                    ActorsText.Text += $"\nи еще {film.Actors.Count - 10}";
            }
            else
            {
                ActorsText.Text = "";
            }

            LengthText.Text = film.MovieLength.HasValue && film.MovieLength > 0
                ? $"{film.MovieLength} мин."
                : "";

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
        }
    }
}