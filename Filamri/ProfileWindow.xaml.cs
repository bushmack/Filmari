using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace filamri
{
    public partial class ProfileWindow : Window
    {
        private UserData _userData;
        private string _avatarPath = "";

        public ProfileWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            _userData = LocalStorage.Load();

            // Заполняем ID
            if (string.IsNullOrEmpty(_userData.UserId))
            {
                _userData.UserId = Guid.NewGuid().ToString();
                LocalStorage.Save(_userData);
            }
            UserIdText.Text = $"ID: {_userData.UserId}";

            // Заполняем имя
            NameText.Text = _userData.UserName;

            // Заполняем аватар
            _avatarPath = _userData.AvatarPath;
            if (!string.IsNullOrEmpty(_avatarPath) && File.Exists(_avatarPath))
            {
                LoadImage(_avatarPath);
            }

            // Прогресс
            int totalMovies = _userData.TotalMovies;
            double progress = totalMovies * 0.25;
            if (progress > 100) progress = 100;

            ProgressBar.Value = progress;
            ProgressText.Text = $"📊 Прогресс: {totalMovies} фильмов ({progress:F0}%)";
            TotalMoviesText.Text = $"Фильмов в подборках: {totalMovies}";

            int nextLevel = (int)(Math.Ceiling(progress / 25) * 4);
            NextLevelText.Text = $"До следующего уровня: {nextLevel} фильма";
        }

        private void LoadImage(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                AvatarImage.Source = bitmap;
            }
            catch
            {
                AvatarImage.Source = null;
            }
        }

        private void EditNameButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.InputDialog("Введите новое имя:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                _userData.UserName = dialog.InputText;
                NameText.Text = _userData.UserName;
                LocalStorage.Save(_userData);
                MessageBox.Show("Имя сохранено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ChangeAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите аватар",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string appFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Filamri",
                        "Avatars");

                    if (!Directory.Exists(appFolder))
                        Directory.CreateDirectory(appFolder);

                    string newPath = Path.Combine(appFolder, "avatar_" + _userData.UserId + Path.GetExtension(openFileDialog.FileName));
                    File.Copy(openFileDialog.FileName, newPath, true);

                    LoadImage(newPath);
                    _userData.AvatarPath = newPath;
                    LocalStorage.Save(_userData);

                    MessageBox.Show("Аватар сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}   