using OneButtonApp.Models;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace OneButtonApp.Views
{
    public partial class FilmControl : UserControl
    {
        public FilmControl(Film film)
        {
            InitializeComponent();

            TitleText.Text = film.Name;
            DescriptionText.Text = film.Description;
            YearText.Text = film.Year.HasValue ? $"Год: {film.Year}" : "";
            GenreText.Text = !string.IsNullOrEmpty(film.Genre) ? $"Жанр: {film.Genre}" : "";
            RatingText.Text = film.Rating.HasValue ? $"Рейтинг: {film.Rating:F1}" : "";

            if (!string.IsNullOrEmpty(film.PosterUrl))
            {
                PosterImage.Source = new BitmapImage(new System.Uri(film.PosterUrl));
            }
        }
    }
}