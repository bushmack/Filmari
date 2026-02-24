using System.Windows;

namespace OneButtonApp
{
    public partial class FilterDialog : Window
    {
        public string Genre { get; private set; }
        public int? YearFrom { get; private set; }
        public int? YearTo { get; private set; }
        public double? RatingFrom { get; private set; }
        public double? RatingTo { get; private set; }
        public string Country { get; private set; }

        public FilterDialog()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем значения
            Genre = string.IsNullOrWhiteSpace(GenreTextBox.Text) ? null : GenreTextBox.Text;

            if (int.TryParse(YearFromTextBox.Text, out int yearFrom))
                YearFrom = yearFrom;

            if (int.TryParse(YearToTextBox.Text, out int yearTo))
                YearTo = yearTo;

            if (double.TryParse(RatingFromTextBox.Text, out double ratingFrom))
                RatingFrom = ratingFrom;

            if (double.TryParse(RatingToTextBox.Text, out double ratingTo))
                RatingTo = ratingTo;

            Country = string.IsNullOrWhiteSpace(CountryTextBox.Text) ? null : CountryTextBox.Text;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}