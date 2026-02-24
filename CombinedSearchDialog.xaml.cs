using System.Windows;

namespace OneButtonApp
{
    public partial class CombinedSearchDialog : Window
    {
        public string Query { get; private set; }
        public string Actor { get; private set; }
        public string Genre { get; private set; }
        public int? Year { get; private set; }

        public CombinedSearchDialog()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            QueryTextBox.Focus();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем обязательное поле
            if (string.IsNullOrWhiteSpace(QueryTextBox.Text))
            {
                MessageBox.Show("Введите название фильма!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Query = QueryTextBox.Text;
            Actor = string.IsNullOrWhiteSpace(ActorTextBox.Text) ? null : ActorTextBox.Text;
            Genre = string.IsNullOrWhiteSpace(GenreTextBox.Text) ? null : GenreTextBox.Text;

            if (int.TryParse(YearTextBox.Text, out int year))
                Year = year;

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