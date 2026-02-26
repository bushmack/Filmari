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
            DescriptionText.Text = film.Description;
            YearText.Text = film.Year?.ToString() ?? "";
            GenreText.Text = film.Genre ?? "";
            RatingText.Text = film.Rating.HasValue ? $"★ {film.Rating:F1}" : "";
            CountryText.Text = film.Country ?? "";
            ActorsText.Text = film.Actors != null && film.Actors.Count > 0
                ? $"Актеры: {string.Join(", ", film.Actors.Take(3))}"
                : "";

            if (!string.IsNullOrEmpty(film.PosterUrl))
            {
                try
                {
                    PosterImage.Source = new BitmapImage(new Uri(film.PosterUrl));
                }
                catch { }
            }
        }
    }
}