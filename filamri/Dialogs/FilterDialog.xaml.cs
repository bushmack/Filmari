using System.Windows;

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

        public FilterDialog()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Genre = string.IsNullOrWhiteSpace(GenreTextBox.Text) ? null : GenreTextBox.Text;

            if (int.TryParse(YearFromTextBox.Text, out int yf)) YearFrom = yf;
            if (int.TryParse(YearToTextBox.Text, out int yt)) YearTo = yt;
            if (double.TryParse(RatingFromTextBox.Text, out double rf)) RatingFrom = rf;
            if (double.TryParse(RatingToTextBox.Text, out double rt)) RatingTo = rt;
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