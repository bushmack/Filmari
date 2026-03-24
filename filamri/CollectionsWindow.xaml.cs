using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace filamri
{
    public partial class CollectionsWindow : Window
    {
        private readonly ApiService _apiService = new ApiService();

        public CollectionsWindow()
        {
            InitializeComponent();
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
                var detailWindow = new CollectionDetailWindow(collection);
                detailWindow.Owner = this;
                detailWindow.ShowDialog();
                LoadCollections(); // Перезагружаем после закрытия
            }
        }

        // ЭТОТ МЕТОД БЫЛ ПРОПУЩЕН!
        private void OpenCollection_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var collection = button?.Tag as Collection;

            if (collection != null)
            {
                var detailWindow = new CollectionDetailWindow(collection);
                detailWindow.Owner = this;
                detailWindow.ShowDialog();
                LoadCollections();
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

                    MessageBox.Show($"Подборка \"{dialog.InputText}\" создана!",
                        "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
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