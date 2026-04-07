using filamri.Models;
using filamri.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace filamri
{
    public partial class CommentsWindow : Window
    {
        private readonly ApiService _apiService = new();
        private readonly int _movieId;
        private readonly string _movieName;
        private List<Comment> _comments = new();
        private UserData _userData;

        public CommentsWindow(int movieId, string movieName)
        {
            InitializeComponent();
            _movieId = movieId;
            _movieName = movieName;

            // Загружаем данные пользователя
            _userData = LocalStorage.Load();

            MovieTitleText.Text = $"💬 {_movieName}";
            LoadComments();
            CommentTextBox.TextChanged += (s, e) =>
                AddCommentButton.IsEnabled = !string.IsNullOrWhiteSpace(CommentTextBox.Text);
        }

        private async void LoadComments()
        {
            try
            {
                var commentsList = await _apiService.GetCommentsAsync(_movieId);
                if (commentsList != null)
                {
                    _comments = commentsList;
                    CommentsList.ItemsSource = _comments;
                    EmptyStateText.Visibility = _comments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    CommentsList.ItemsSource = null;
                    EmptyStateText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки комментариев: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddCommentButton_Click(object sender, RoutedEventArgs e)
        {
            string text = CommentTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                // Используем актуальное имя из сохраненных данных
                string userName = _userData.UserName;

                bool success = await _apiService.AddCommentAsync(_movieId, userName, text, _userData.UserId);
                if (success)
                {
                    CommentTextBox.Clear();
                    LoadComments(); // обновляем список
                    MessageBox.Show("✅ Комментарий добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Не удалось добавить комментарий", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}