using Microsoft.Web.WebView2.Core;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace filamri
{
    public partial class WatchPartyWindow : Window
    {
        private readonly HttpClient _httpClient = new();
        private ClientWebSocket? _webSocket;
        private Timer? _pingTimer;
        private UserData _userData;
        private string _roomId;
        private string _videoUrl;
        private bool _isHost;
        private bool _isSyncing;
        private bool _isPlaying = false;
        private double _currentTime = 0;

        public WatchPartyWindow(string videoUrl, bool isHost, string roomId)
        {
            InitializeComponent();
            _httpClient.BaseAddress = new Uri("http://localhost:8002");
            _videoUrl = videoUrl;
            _isHost = isHost;
            _roomId = roomId;
            _userData = LocalStorage.Load();

            RoomIdText.Text = $"ID комнаты: {roomId}";

            if (_isHost)
            {
                RoomInfoText.Text = "🎬 Вы создатель комнаты";
                PartnerInfoText.Text = "⏳ Ожидание подключения партнера...";
                LoadingText.Visibility = Visibility.Visible;
            }
            else
            {
                RoomInfoText.Text = "🎬 Вы подключились к комнате";
                PartnerInfoText.Text = "⏳ Ожидание начала просмотра...";
                LoadingText.Visibility = Visibility.Visible;
            }

            InitializeWebView();
            ConnectWebSocket();
        }

        private async void ConnectWebSocket()
        {
            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri($"ws://localhost:8002/ws/{_roomId}/{_userData.UserId}"), CancellationToken.None);

                if (_webSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine("WebSocket connected!");
                    _ = Task.Run(ReceiveMessages);

                    _pingTimer = new Timer(async _ =>
                    {
                        if (_webSocket?.State == WebSocketState.Open)
                        {
                            try
                            {
                                var pingMsg = JsonSerializer.Serialize(new { type = "ping" });
                                var bytes = Encoding.UTF8.GetBytes(pingMsg);
                                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                            catch { }
                        }
                    }, null, 30000, 30000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[8192];

            while (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received: {message}");

                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;
                        var type = root.GetProperty("type").GetString();

                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (type == "init")
                            {
                                var isPlaying = root.GetProperty("isPlaying").GetBoolean();
                                var currentTime = root.GetProperty("currentTime").GetDouble();
                                _isPlaying = isPlaying;
                                _currentTime = currentTime;
                                PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
                                Console.WriteLine($"Init: isPlaying={isPlaying}, time={currentTime}");
                            }
                            else if (type == "sync")
                            {
                                var isPlaying = root.GetProperty("isPlaying").GetBoolean();
                                var currentTime = root.GetProperty("currentTime").GetDouble();

                                if (!_isSyncing)
                                {
                                    _isPlaying = isPlaying;
                                    _currentTime = currentTime;
                                    PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
                                    Console.WriteLine($"Sync: isPlaying={isPlaying}, time={currentTime}");
                                }
                            }
                            else if (type == "guest_joined")
                            {
                                var guestName = root.GetProperty("guestName").GetString();
                                PartnerInfoText.Text = $"👥 С кем: {guestName}";
                                LoadingText.Visibility = Visibility.Collapsed;
                                PlayIcon.Visibility = Visibility.Collapsed;
                                Console.WriteLine($"Guest joined: {guestName}");
                            }
                            else if (type == "guest_left")
                            {
                                PartnerInfoText.Text = "⏳ Ожидание подключения партнера...";
                                LoadingText.Visibility = Visibility.Visible;
                                Console.WriteLine("Guest left");
                            }
                            else if (type == "chat")
                            {
                                var msg = root.GetProperty("message");
                                var userName = msg.GetProperty("userName").GetString();
                                var text = msg.GetProperty("text").GetString();
                                var time = msg.GetProperty("time").GetString();
                                AddMessageToChat(userName ?? "", text ?? "", time ?? "");
                            }
                            else if (type == "pong")
                            {
                                Console.WriteLine("Pong received");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Receive error: {ex.Message}");
                    break;
                }
            }
        }

        private void AddMessageToChat(string userName, string text, string time)
        {
            var messages = MessagesList.ItemsSource as System.Collections.ObjectModel.ObservableCollection<ChatMessageItem>;
            if (messages == null)
            {
                messages = new System.Collections.ObjectModel.ObservableCollection<ChatMessageItem>();
                MessagesList.ItemsSource = messages;
            }

            messages.Add(new ChatMessageItem
            {
                UserName = userName,
                Text = text,
                Time = time
            });

            MessagesScrollViewer.ScrollToBottom();
        }

        private async void InitializeWebView()
        {
            await VideoWebView.EnsureCoreWebView2Async();

            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ margin: 0; padding: 0; background: #000; height: 100vh; overflow: hidden; }}
                    iframe {{ width: 100%; height: 100%; border: none; }}
                </style>
            </head>
            <body>
                <iframe src='{_videoUrl}' allow='autoplay; fullscreen; picture-in-picture' 
                        allowfullscreen='true'></iframe>
            </body>
            </html>";

            VideoWebView.NavigateToString(html);
            LoadingText.Visibility = Visibility.Collapsed;
            PlayIcon.Visibility = Visibility.Collapsed;
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            _isSyncing = true;
            _isPlaying = !_isPlaying;
            PlayPauseButton.Content = _isPlaying ? "⏸" : "▶";

            await SendSync();
            _isSyncing = false;
        }

        private async Task SendSync()
        {
            if (_webSocket?.State != WebSocketState.Open) return;

            var syncMsg = new
            {
                type = "sync",
                isPlaying = _isPlaying,
                currentTime = _currentTime
            };

            var json = JsonSerializer.Serialize(syncMsg);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Sent sync: isPlaying={_isPlaying}, time={_currentTime}");
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

        private async Task SendMessage()
        {
            string text = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var chatMsg = new
            {
                type = "chat",
                userId = _userData.UserId,
                userName = _userData.UserName,
                text = text
            };

            var json = JsonSerializer.Serialize(chatMsg);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            MessageTextBox.Clear();
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _pingTimer?.Dispose();
            if (_webSocket != null)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                _webSocket.Dispose();
            }
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _pingTimer?.Dispose();
            base.OnClosed(e);
        }
    }

    public class ChatMessageItem
    {
        public string UserName { get; set; } = "";
        public string Text { get; set; } = "";
        public string Time { get; set; } = "";
    }
}