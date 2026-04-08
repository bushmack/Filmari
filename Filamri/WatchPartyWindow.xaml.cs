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
        private bool _isVideoReady = false;
        private CoreWebView2Environment? _webViewEnvironment;

        public WatchPartyWindow(string videoUrl, bool isHost, string roomId)
        {
            InitializeComponent();

            // Для HTTP запросов используем порт 8002
            _httpClient.BaseAddress = new Uri("http://192.168.133.7:8002");
            _httpClient.Timeout = TimeSpan.FromSeconds(5);

            _videoUrl = videoUrl;
            _isHost = isHost;
            _roomId = roomId;
            _userData = LocalStorage.Load();

            RoomIdText.Text = $"ID комнаты: {roomId}";

            AddDebugMessage($"=== Инициализация ===");
            AddDebugMessage($"Комната: {roomId}, isHost={isHost}");
            AddDebugMessage($"Видео URL: {videoUrl}");
            AddDebugMessage($"UserID: {_userData.UserId}");

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

            Loaded += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await InitializeWebView();
            await ConnectWebSocket();
        }

        private async Task InitializeWebView()
        {
            try
            {
                AddDebugMessage("Инициализация WebView2...");

                string userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Filamri",
                    "WebView2",
                    _roomId);

                _webViewEnvironment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: userDataFolder,
                    options: new CoreWebView2EnvironmentOptions
                    {
                        AllowSingleSignOnUsingOSPrimaryAccount = false,
                        Language = "ru-RU"
                    });

                await VideoWebView.EnsureCoreWebView2Async(_webViewEnvironment);

                AddDebugMessage("WebView2 инициализирован успешно");

                VideoWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                string html = GetVideoHtml();
                VideoWebView.NavigateToString(html);

                VideoWebView.CoreWebView2.DOMContentLoaded += async (sender, args) =>
                {
                    AddDebugMessage("DOM загружен, внедряем JavaScript...");
                    await InjectJavaScript();
                };

                VideoWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                VideoWebView.CoreWebView2.NavigationCompleted += (sender, args) =>
                {
                    if (!args.IsSuccess)
                    {
                        AddDebugMessage($"Ошибка навигации: {args.WebErrorStatus}");
                        LoadingText.Text = "❌ Ошибка загрузки видео";
                    }
                };
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Ошибка инициализации WebView2: {ex.Message}");
                MessageBox.Show($"Ошибка инициализации видео: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetVideoHtml()
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ margin: 0; padding: 0; background: #000; height: 100vh; overflow: hidden; }}
                    iframe {{ width: 100%; height: 100%; border: none; }}
                </style>
            </head>
            <body>
                <iframe id='videoFrame' src='{_videoUrl}' allow='autoplay; fullscreen; picture-in-picture' 
                        allowfullscreen='true'></iframe>
            </body>
            </html>";
        }

        private async Task InjectJavaScript()
        {
            string script = @"
                function getVideoElement() {
                    try {
                        const iframe = document.getElementById('videoFrame');
                        if (iframe && iframe.contentDocument) {
                            const video = iframe.contentDocument.querySelector('video');
                            if (video) return video;
                        }
                        const video = document.querySelector('video');
                        if (video) return video;
                    } catch(e) {
                        console.log('Error finding video: ' + e.message);
                    }
                    return null;
                }

                window.chrome.webview.postMessage(JSON.stringify({ type: 'ready' }));

                let lastTime = 0;
                let lastPlaying = false;
                
                setInterval(() => {
                    try {
                        const video = getVideoElement();
                        if (video) {
                            const currentTime = video.currentTime;
                            const isPlaying = !video.paused;
                            
                            if (Math.abs(currentTime - lastTime) > 0.1 || isPlaying !== lastPlaying) {
                                lastTime = currentTime;
                                lastPlaying = isPlaying;
                                window.chrome.webview.postMessage(JSON.stringify({
                                    type: 'timeupdate',
                                    currentTime: currentTime,
                                    isPlaying: isPlaying
                                }));
                            }
                        }
                    } catch(e) {
                        console.log('Error: ' + e.message);
                    }
                }, 500);
            ";

            try
            {
                await VideoWebView.CoreWebView2.ExecuteScriptAsync(script);
                _isVideoReady = true;
                AddDebugMessage("JavaScript внедрен успешно");

                await Dispatcher.InvokeAsync(() =>
                {
                    LoadingText.Visibility = Visibility.Collapsed;
                    PlayIcon.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Ошибка внедрения JavaScript: {ex.Message}");
            }
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(message)) return;

                AddDebugMessage($"Получено сообщение из WebView: {message}");

                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                if (type == "ready")
                {
                    _isVideoReady = true;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        LoadingText.Visibility = Visibility.Collapsed;
                        PlayIcon.Visibility = Visibility.Collapsed;
                    });
                    AddDebugMessage("Видео готово к воспроизведению");

                    // Если мы гость, запрашиваем текущее состояние
                    if (!_isHost)
                    {
                        await GetRoomState();
                    }
                }
                else if (type == "timeupdate")
                {
                    var currentTime = root.GetProperty("currentTime").GetDouble();
                    var isPlaying = root.GetProperty("isPlaying").GetBoolean();

                    _currentTime = currentTime;
                    _isPlaying = isPlaying;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
                    });

                    if (_isHost && !_isSyncing && _webSocket?.State == WebSocketState.Open)
                    {
                        await SendSync();
                    }
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        private async Task ConnectWebSocket()
        {
            try
            {
                AddDebugMessage($"Подключение к WebSocket...");

                string wsUrl = $"ws://192.168.133.7:8002/ws/{_roomId}/{_userData.UserId}";
                AddDebugMessage($"URL: {wsUrl}");

                _webSocket = new ClientWebSocket();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _webSocket.ConnectAsync(new Uri(wsUrl), cts.Token);

                if (_webSocket.State == WebSocketState.Open)
                {
                    AddDebugMessage("✅ WebSocket подключен успешно!");
                    _ = Task.Run(ReceiveMessages);

                    _pingTimer = new Timer(async _ =>
                    {
                        if (_webSocket?.State == WebSocketState.Open)
                        {
                            try
                            {
                                var pingMsg = JsonSerializer.Serialize(new { type = "ping" });
                                var bytes = Encoding.UTF8.GetBytes(pingMsg);
                                await _webSocket.SendAsync(
                                    new ArraySegment<byte>(bytes),
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);
                            }
                            catch { }
                        }
                    }, null, 30000, 30000);

                    // Запрашиваем состояние комнаты
                    await GetRoomState();
                }
                else
                {
                    AddDebugMessage($"❌ WebSocket не открыт. Состояние: {_webSocket.State}");
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"❌ Ошибка WebSocket: {ex.Message}");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"Не удалось подключиться к серверу совместного просмотра.\n\n" +
                        $"Убедитесь, что сервер на порту 8002 запущен.\n\n" +
                        $"Ошибка: {ex.Message}",
                        "Ошибка подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    PartnerInfoText.Text = "❌ Ошибка подключения к серверу";
                });
            }
        }

        private async Task GetRoomState()
        {
            try
            {
                AddDebugMessage($"Запрос состояния комнаты {_roomId}...");
                var response = await _httpClient.GetAsync($"/api/room/{_roomId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    AddDebugMessage($"Состояние комнаты: {json}");

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var isPlaying = root.GetProperty("isPlaying").GetBoolean();
                    var currentTime = root.GetProperty("currentTime").GetDouble();

                    _isPlaying = isPlaying;
                    _currentTime = currentTime;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
                    });

                    await SyncVideoPosition(currentTime, isPlaying);
                    AddDebugMessage($"Установлено состояние: isPlaying={isPlaying}, time={currentTime}");
                }
                else
                {
                    AddDebugMessage($"Ошибка получения состояния: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Ошибка получения состояния: {ex.Message}");
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
                        AddDebugMessage($"Получено сообщение: {message}");

                        using var doc = JsonDocument.Parse(message);
                        var root = doc.RootElement;
                        var type = root.GetProperty("type").GetString();

                        await Dispatcher.InvokeAsync(async () =>
                        {
                            if (type == "init")
                            {
                                var isPlaying = root.GetProperty("isPlaying").GetBoolean();
                                var currentTime = root.GetProperty("currentTime").GetDouble();
                                _isPlaying = isPlaying;
                                _currentTime = currentTime;
                                PlayPauseButton.Content = isPlaying ? "⏸" : "▶";

                                await SyncVideoPosition(currentTime, isPlaying);
                                AddDebugMessage($"Init: isPlaying={isPlaying}, time={currentTime}");
                            }
                            else if (type == "sync")
                            {
                                var isPlaying = root.GetProperty("isPlaying").GetBoolean();
                                var currentTime = root.GetProperty("currentTime").GetDouble();

                                if (!_isSyncing)
                                {
                                    _isSyncing = true;
                                    _isPlaying = isPlaying;
                                    _currentTime = currentTime;
                                    PlayPauseButton.Content = isPlaying ? "⏸" : "▶";

                                    await SyncVideoPosition(currentTime, isPlaying);

                                    _isSyncing = false;
                                    AddDebugMessage($"Sync: isPlaying={isPlaying}, time={currentTime}");
                                }
                            }
                            else if (type == "guest_joined")
                            {
                                var guestName = root.GetProperty("guestName").GetString();
                                PartnerInfoText.Text = $"👥 С кем: {guestName}";
                                LoadingText.Visibility = Visibility.Collapsed;
                                PlayIcon.Visibility = Visibility.Collapsed;

                                if (_isHost && _isVideoReady)
                                {
                                    await SendSync();
                                }
                                AddDebugMessage($"Гость подключился: {guestName}");
                            }
                            else if (type == "guest_left")
                            {
                                PartnerInfoText.Text = "⏳ Ожидание подключения партнера...";
                                LoadingText.Visibility = Visibility.Visible;
                                AddDebugMessage("Гость отключился");
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
                                AddDebugMessage("Pong получен");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Ошибка получения сообщения: {ex.Message}");
                    break;
                }
            }
        }

        private async Task SyncVideoPosition(double position, bool isPlaying)
        {
            if (!_isVideoReady) return;

            string script = $@"
                (function() {{
                    try {{
                        const iframe = document.getElementById('videoFrame');
                        let video = null;
                        if (iframe && iframe.contentDocument) {{
                            video = iframe.contentDocument.querySelector('video');
                        }}
                        if (!video) {{
                            video = document.querySelector('video');
                        }}
                        if (video) {{
                            video.currentTime = {position.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                            {(isPlaying ? "video.play()" : "video.pause()")};
                        }}
                    }} catch(e) {{
                        console.log('Sync error: ' + e.message);
                    }}
                }})();
            ";

            try
            {
                await VideoWebView.CoreWebView2.ExecuteScriptAsync(script);
                AddDebugMessage($"Синхронизировано: position={position}, isPlaying={isPlaying}");
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Ошибка синхронизации видео: {ex.Message}");
            }
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
            AddDebugMessage($"Отправлен sync: isPlaying={_isPlaying}, time={_currentTime}");
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isVideoReady)
            {
                AddDebugMessage("Видео еще не готово");
                return;
            }

            if (_webSocket?.State != WebSocketState.Open)
            {
                AddDebugMessage("WebSocket не открыт, переподключаемся...");
                await ConnectWebSocket();
                if (_webSocket?.State != WebSocketState.Open)
                {
                    AddDebugMessage("Не удалось переподключиться!");
                    MessageBox.Show("Нет соединения с сервером. Проверьте подключение.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            _isSyncing = true;
            _isPlaying = !_isPlaying;
            PlayPauseButton.Content = _isPlaying ? "⏸" : "▶";

            string script = _isPlaying ?
                @"(function() { try { const iframe = document.getElementById('videoFrame'); let video = null; if (iframe && iframe.contentDocument) { video = iframe.contentDocument.querySelector('video'); } if (!video) { video = document.querySelector('video'); } if (video) video.play(); } catch(e) { console.log(e); } })();" :
                @"(function() { try { const iframe = document.getElementById('videoFrame'); let video = null; if (iframe && iframe.contentDocument) { video = iframe.contentDocument.querySelector('video'); } if (!video) { video = document.querySelector('video'); } if (video) video.pause(); } catch(e) { console.log(e); } })();";

            try
            {
                await VideoWebView.CoreWebView2.ExecuteScriptAsync(script);
                await SendSync();
                AddDebugMessage($"Play/Pause: isPlaying={_isPlaying}");
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Ошибка Play/Pause: {ex.Message}");
            }

            _isSyncing = false;
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
            AddDebugMessage($"Сообщение в чат от {userName}: {text}");
        }

        private void AddDebugMessage(string message)
        {
            Console.WriteLine($"[WatchParty] {DateTime.Now:HH:mm:ss} - {message}");

            Dispatcher.InvokeAsync(() =>
            {
                DebugConsole.Text = $"{DateTime.Now:HH:mm:ss} - {message}\n{DebugConsole.Text}";
                if (DebugConsole.Text.Length > 5000)
                    DebugConsole.Text = DebugConsole.Text.Substring(0, 5000);
            });
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

            if (_webSocket?.State != WebSocketState.Open)
            {
                AddDebugMessage("WebSocket не открыт, сообщение не отправлено");
                MessageBox.Show("Нет соединения с сервером.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
            AddDebugMessage($"Отправлено сообщение: {text}");
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _pingTimer?.Dispose();
            if (_webSocket != null)
            {
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                    _webSocket.Dispose();
                }
                catch { }
            }
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _pingTimer?.Dispose();
            _webSocket?.Dispose();
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