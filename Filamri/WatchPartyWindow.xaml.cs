using filamri.Models;
using filamri.Services;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace filamri
{
    public partial class WatchPartyWindow : Window
    {
        private readonly ApiService _apiService = new();
        private Timer? _syncTimer;
        private UserData _userData;
        private WatchRoom? _currentRoom;
        private bool _isHost;
        private bool _isSyncing;
        private string _videoPath;
        private bool _isSeeking;

        public WatchPartyWindow(string videoPath, bool isHost, string roomId)
        {
            InitializeComponent();
            _videoPath = videoPath;
            _isHost = isHost;
            _userData = LocalStorage.Load();

            RoomIdText.Text = $"ID комнаты: {roomId}";

            if (_isHost)
            {
                RoomInfoText.Text = "🎬 Вы создатель комнаты";
                PartnerInfoText.Text = "⏳ Ожидание подключения партнера...";
                LoadingText.Visibility = Visibility.Visible;
                PlayIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                RoomInfoText.Text = "🎬 Вы подключились к комнате";
                PartnerInfoText.Text = "⏳ Ожидание начала просмотра...";
                LoadingText.Visibility = Visibility.Visible;
                PlayIcon.Visibility = Visibility.Visible;
            }

            LoadVideo();
            StartPolling();
        }

        private void LoadVideo()
        {
            try
            {
                VideoPlayer.Source = new Uri(_videoPath);
                VideoPlayer.MediaOpened += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PositionSlider.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    });
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видео: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartPolling()
        {
            _syncTimer = new Timer(async _ =>
            {
                if (_currentRoom == null) return;
                try
                {
                    var status = await _apiService.GetWatchRoomStatus(_currentRoom.RoomId);
                    if (status != null)
                    {
                        await Dispatcher.InvokeAsync(() => UpdateUI(status));
                    }
                }
                catch { }
            }, null, 0, 500);
        }

        private void UpdateUI(WatchRoom room)
        {
            _currentRoom = room;

            if (!_isSyncing && !_isSeeking && room.CurrentPosition > 0)
            {
                if (Math.Abs(VideoPlayer.Position.TotalSeconds - room.CurrentPosition) > 1)
                {
                    VideoPlayer.Position = TimeSpan.FromSeconds(room.CurrentPosition);
                    PositionSlider.Value = room.CurrentPosition;
                }
            }

            if (room.IsPlaying && VideoPlayer.LoadedBehavior != MediaState.Play)
            {
                VideoPlayer.Play();
                PlayIcon.Visibility = Visibility.Collapsed;
                PlayPauseButton.Content = "⏸";
            }
            else if (!room.IsPlaying && VideoPlayer.LoadedBehavior != MediaState.Pause && VideoPlayer.LoadedBehavior != MediaState.Stop)
            {
                VideoPlayer.Pause();
                PlayIcon.Visibility = Visibility.Visible;
                PlayPauseButton.Content = "▶";
            }

            if (!string.IsNullOrEmpty(room.GuestId))
            {
                PartnerInfoText.Text = $"👥 С кем: {room.GuestName}";
                LoadingText.Visibility = Visibility.Collapsed;
            }

            MessagesList.ItemsSource = null;
            MessagesList.ItemsSource = room.Messages;
            MessagesScrollViewer.ScrollToBottom();
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            PositionSlider.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRoom == null) return;

            bool newState = !_currentRoom.IsPlaying;
            _isSyncing = true;

            if (newState)
                VideoPlayer.Play();
            else
                VideoPlayer.Pause();

            await _apiService.SyncWatchState(_currentRoom.RoomId, VideoPlayer.Position.TotalSeconds, newState);
            _isSyncing = false;
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRoom == null) return;

            _isSyncing = true;
            VideoPlayer.Stop();
            VideoPlayer.Position = TimeSpan.Zero;
            PositionSlider.Value = 0;

            await _apiService.SyncWatchState(_currentRoom.RoomId, 0, false);
            _isSyncing = false;
        }

        private void PositionSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSeeking = true;
        }

        private async void PositionSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentRoom == null) return;

            double newPosition = PositionSlider.Value;
            VideoPlayer.Position = TimeSpan.FromSeconds(newPosition);

            _isSyncing = true;
            await _apiService.SyncWatchState(_currentRoom.RoomId, newPosition, _currentRoom.IsPlaying);
            _isSyncing = false;
            _isSeeking = false;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendMessage();
            }
        }

        private async System.Threading.Tasks.Task SendMessage()
        {
            string text = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text) || _currentRoom == null) return;

            await _apiService.SendWatchMessage(
                _currentRoom.RoomId,
                _userData.UserId,
                _userData.UserName,
                text);

            MessageTextBox.Clear();
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _syncTimer?.Dispose();
            if (_currentRoom != null)
            {
                await _apiService.LeaveWatchRoom(_currentRoom.RoomId, _userData.UserId);
            }
            VideoPlayer.Stop();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _syncTimer?.Dispose();
            VideoPlayer.Stop();
            base.OnClosed(e);
        }
    }
}