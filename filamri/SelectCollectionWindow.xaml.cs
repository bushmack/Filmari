using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace filamri
{
    public partial class SelectCollectionWindow : Window
    {
        private readonly ApiService _apiService = new();
        public int SelectedMovieId { get; set; }
        public string? SelectedCollectionName { get; private set; }

        public SelectCollectionWindow(int movieId)
        {
            InitializeComponent();
            SelectedMovieId = movieId;
            LoadCollections();
        }

        private async void LoadCollections()
        {
            try
            {
                var collections = await _apiService.GetCollectionsAsync();
                CollectionsList.ItemsSource = collections;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подборок: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            var collection = border?.Tag as Collection;

            if (collection != null)
            {
                SelectedCollectionName = collection.Name;
                DialogResult = true;
                Close();
            }
        }

        private async void CreateNewCollection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.InputDialog("Введите название новой подборки:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    await _apiService.CreateCollection(dialog.InputText);
                    LoadCollections();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания подборки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}