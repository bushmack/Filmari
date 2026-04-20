using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.ObjectModel;
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
        private ClientWebSocket? _webSocket;
        private UserData _userData;
        private string _roomId;
        private string _videoUrl;
        private bool _isHost;
        private ObservableCollection<ChatMessageItem> _messages = new();
        private bool _isWebViewReady = false;
        private readonly object _videoLock = new();

        public WatchPartyWindow(string videoUrl, bool isHost, string roomId)
        {
            InitializeComponent();

            _videoUrl = videoUrl;
            _isHost = isHost;
            _roomId = roomId;
            _userData = LocalStorage.Load();

            MessagesList.ItemsSource = _messages;
            RoomIdText.Text = $"ID: {roomId}";

            if (_isHost)
            {
                RoomInfoText.Text = "🎬 Вы создатель";
                PartnerInfoText.Text = "⏳ Ожидание партнера...";
            }
            else
            {
                RoomInfoText.Text = "🎬 Вы подключились";
                PartnerInfoText.Text = "⏳ Ожидание начала...";
            }

            Loaded += async (s, e) => await InitializeWebView();
            _ = ConnectWebSocket();
        }

        private async Task InitializeWebView()
        {
            try
            {
                await VideoWebView.EnsureCoreWebView2Async();
                _isWebViewReady = true;

                if (!string.IsNullOrEmpty(_videoUrl))
                {
                    await LoadVideo(_videoUrl);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 init error: {ex.Message}");
            }
        }

        private async Task LoadVideo(string url)
        {
            if (!_isWebViewReady || VideoWebView.CoreWebView2 == null)
            {
                await Task.Delay(500);
                await LoadVideo(url);
                return;
            }

            lock (_videoLock)
            {
                _videoUrl = url;
            }

            System.Diagnostics.Debug.WriteLine($"🎬 Загрузка видео: {url}");

            string html = $@"
    <html>
    <head>
        <style>
            body {{ margin: 0; padding: 0; background: #000; }}
            iframe {{ width: 100%; height: 100vh; border: none; }}
        </style>
    </head>
    <body>
        <iframe src='{url}' 
                allow='autoplay; fullscreen; picture-in-picture' 
                allowfullscreen='true'>
        </iframe>
    </body>
    </html>";

            // ИСПРАВЛЕНО: вызываем у CoreWebView2
            VideoWebView.CoreWebView2.NavigateToString(html);
            await Task.CompletedTask;
        }

        private async Task ConnectWebSocket()
        {
            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri($"ws://192.168.133.7:8002/ws/{_roomId}/{_userData.UserId}"), CancellationToken.None);
                _ = Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebSocket error: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine($"📨 Получено: {message}");

                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;
                        var type = root.GetProperty("type").GetString();

                        await Dispatcher.InvokeAsync(async () =>
                        {
                            switch (type)
                            {
                                case "init":
                                    var newUrl = root.GetProperty("videoUrl").GetString();
                                    System.Diagnostics.Debug.WriteLine($"🎬 INIT videoUrl: {newUrl}");
                                    if (!string.IsNullOrEmpty(newUrl))
                                    {
                                        await LoadVideo(newUrl);
                                    }
                                    break;

                                case "guest_joined":
                                    PartnerInfoText.Text = $"👥 С кем: {root.GetProperty("guestName").GetString()}";
                                    break;

                                case "guest_left":
                                    PartnerInfoText.Text = "⏳ Ожидание партнера...";
                                    break;

                                case "chat":
                                    var msg = root.GetProperty("message");
                                    _messages.Add(new ChatMessageItem
                                    {
                                        UserName = msg.GetProperty("userName").GetString() ?? "",
                                        Text = msg.GetProperty("text").GetString() ?? "",
                                        Time = msg.GetProperty("time").GetString() ?? ""
                                    });
                                    MessagesScrollViewer.ScrollToBottom();
                                    break;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Receive error: {ex.Message}");
                    break;
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text) || _webSocket?.State != WebSocketState.Open) return;

            var json = JsonSerializer.Serialize(new
            {
                type = "chat",
                userId = _userData.UserId,
                userName = _userData.UserName,
                text = text
            });

            await _webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
            MessageTextBox.Clear();
        }

        private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendButton_Click(sender, e);
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_webSocket != null)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            Close();
        }
    }

    public class ChatMessageItem
    {
        public string UserName { get; set; } = "";
        public string Text { get; set; } = "";
        public string Time { get; set; } = "";
    }
}