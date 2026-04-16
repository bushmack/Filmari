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
        private HttpClient _httpClient;
        private ClientWebSocket _webSocket;
        private UserData _userData;
        private string _roomId;
        private string _videoUrl;
        private bool _isHost;
        private Timer _syncTimer;
        private bool _isPlaying = false;
        private double _currentTime = 0;
        private bool _isVideoReady = false;
        private bool _isSyncing = false;

        public WatchPartyWindow(string videoUrl, bool isHost, string roomId)
        {
            InitializeComponent();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:8002");

            _videoUrl = videoUrl;
            _isHost = isHost;
            _roomId = roomId;
            _userData = LocalStorage.Load();

            RoomIdText.Text = $"ID комнаты: {roomId}";

            Log($"Инициализация, хост={isHost}");
            Log($"Видео URL: {_videoUrl}");

            if (_isHost)
            {
                RoomInfoText.Text = "🎬 Вы создатель комнаты";
                PartnerInfoText.Text = "⏳ Ожидание подключения партнера...";
            }
            else
            {
                RoomInfoText.Text = "🎬 Вы подключились к комнате";
                PartnerInfoText.Text = "⏳ Ожидание начала просмотра...";
            }

            Loaded += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await InitWebView();
            await ConnectWebSocket();
        }

        private async Task InitWebView()
        {
            try
            {
                Log("Инициализация WebView2...");
                await VideoWebView.EnsureCoreWebView2Async();

                // HTML5 video плеер для MP4
                string html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ margin: 0; padding: 0; background: #000; height: 100vh; }}
                        iframe {{ width: 100%; height: 100%; object-fit: contain; }}
                    </style>
                </head>
                <body>
                    <iframe 
                        src="+$"\"{ _videoUrl}\""+
                $@"     frameborder=\""0\""
                        allowfullscreen=\""1\"" 
                        style=\""background-color: #000\""
                        allow=\""autoplay; encrypted-media; fullscreen; picture-in-picture\"">
                    </iframe>
                </body>
                </html>";


                VideoWebView.NavigateToString(html);

                VideoWebView.CoreWebView2.DOMContentLoaded += async (s, e) =>
                {
                    Log("DOM загружен");
                    await Task.Delay(500);
                    await InjectJS();
                };

                VideoWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            }
            catch (Exception ex)
            {
                Log($"WebView ошибка: {ex.Message}");
            }
        }

        private async Task InjectJS()
        {
            string script = @"
                var video = document.getElementById('videoPlayer');
                if(video) {
                    window.chrome.webview.postMessage(JSON.stringify({type:'ready'}));
                    window.chrome.webview.postMessage(JSON.stringify({type:'videoFound'}));
                    
                    video.addEventListener('play', () => {
                        window.chrome.webview.postMessage(JSON.stringify({type:'play'}));
                    });
                    video.addEventListener('pause', () => {
                        window.chrome.webview.postMessage(JSON.stringify({type:'pause'}));
                    });
                    video.addEventListener('seeked', () => {
                        window.chrome.webview.postMessage(JSON.stringify({type:'seeked', time:video.currentTime}));
                    });
                    
                    setInterval(() => {
                        window.chrome.webview.postMessage(JSON.stringify({
                            type:'timeupdate',
                            time:video.currentTime,
                            playing:!video.paused
                        }));
                    }, 500);
                }
            ";

            await VideoWebView.CoreWebView2.ExecuteScriptAsync(script);
            Log("JS внедрен");
        }

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var msg = e.TryGetWebMessageAsString();

            try
            {
                var json = JsonDocument.Parse(msg);
                var type = json.RootElement.GetProperty("type").GetString();

                if (type == "ready")
                {
                    Log("Плеер готов");
                }
                else if (type == "videoFound")
                {
                    _isVideoReady = true;
                    Log("Видео найдено!");


                    if (!_isHost) _ = GetRoomState();
                }
                else if (type == "timeupdate" && !_isSyncing)
                {
                    _currentTime = json.RootElement.GetProperty("time").GetDouble();
                    _isPlaying = json.RootElement.GetProperty("playing").GetBoolean();
                    if (_isHost) _ = SendSync();
                }
                else if (type == "play" && _isHost && !_isSyncing)
                {
                    _isPlaying = true;
                    _ = SendSync();
                    Log("Play событие");
                }
                else if (type == "pause" && _isHost && !_isSyncing)
                {
                    _isPlaying = false;
                    _ = SendSync();
                    Log("Pause событие");
                }
                else if (type == "seeked" && _isHost && !_isSyncing)
                {
                    _currentTime = json.RootElement.GetProperty("time").GetDouble();
                    _ = SendSync();
                    Log($"Seeked to {_currentTime}");
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}");
            }
        }

        private async Task ConnectWebSocket()
        {
            try
            {
                var url = $"ws://localhost:8002/ws/{_roomId}/{_userData.UserId}";
                Log($"WebSocket: {url}");

                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(url), CancellationToken.None);

                Log("WebSocket подключен!");
                _ = Task.Run(ReceiveMessages);

                _syncTimer = new Timer(async _ =>
                {
                    if (_isHost && _isVideoReady && _webSocket?.State == WebSocketState.Open)
                        await SendSync();
                }, null, 2000, 2000);

                await GetRoomState();
            }
            catch (Exception ex)
            {
                Log($"WebSocket ошибка: {ex.Message}");
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
                        var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        var json = JsonDocument.Parse(msg);
                        var type = json.RootElement.GetProperty("type").GetString();

                        if (type == "init" || type == "sync")
                        {
                            var playing = json.RootElement.GetProperty("isPlaying").GetBoolean();
                            var time = json.RootElement.GetProperty("currentTime").GetDouble();
                            await SyncVideo(time, playing);
                        }
                        else if (type == "guest_joined")
                        {
                            var name = json.RootElement.GetProperty("guestName").GetString();
                            Dispatcher.Invoke(() => PartnerInfoText.Text = $"👥 С кем: {name}");
                            Log($"Гость подключился: {name}");
                        }
                        else if (type == "guest_left")
                        {
                            Dispatcher.Invoke(() => PartnerInfoText.Text = "⏳ Ожидание подключения партнера...");
                            Log("Гость отключился");
                        }
                        else if (type == "chat")
                        {
                            var m = json.RootElement.GetProperty("message");
                            AddMessage(m.GetProperty("userName").GetString(),
                                      m.GetProperty("text").GetString(),
                                      m.GetProperty("time").GetString());
                        }
                    }
                }
                catch { break; }
            }
        }

        private async Task GetRoomState()
        {
            try
            {
                var resp = await _httpClient.GetAsync($"/api/room/{_roomId}");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);
                    var playing = doc.RootElement.GetProperty("isPlaying").GetBoolean();
                    var time = doc.RootElement.GetProperty("currentTime").GetDouble();
                    await SyncVideo(time, playing);
                }
            }
            catch (Exception ex) { Log($"GetState ошибка: {ex.Message}"); }
        }

        private async Task SyncVideo(double time, bool playing)
        {
            if (!_isVideoReady) return;

            _isSyncing = true;

            var script = $@"
                var v = document.getElementById('videoPlayer');
                if(v) {{
                    if(Math.abs(v.currentTime - {time.ToString(System.Globalization.CultureInfo.InvariantCulture)}) > 0.5) {{
                        v.currentTime = {time.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                    }}
                    {(playing ? "v.play()" : "v.pause()")};
                }}
            ";

            try
            {
                await VideoWebView.CoreWebView2.ExecuteScriptAsync(script);
                Log($"Синхронизация: time={time:F1}, play={playing}");
            }
            catch (Exception ex)
            {
                Log($"Ошибка синхронизации: {ex.Message}");
            }

            _isSyncing = false;
        }

        private async Task SendSync()
        {
            if (_webSocket?.State != WebSocketState.Open) return;

            var msg = new { type = "sync", isPlaying = _isPlaying, currentTime = _currentTime };
            var json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void AddMessage(string userName, string text, string time)
        {
            Dispatcher.Invoke(() =>
            {
                var list = MessagesList.ItemsSource as System.Collections.ObjectModel.ObservableCollection<dynamic>;
                if (list == null)
                {
                    list = new System.Collections.ObjectModel.ObservableCollection<dynamic>();
                    MessagesList.ItemsSource = list;
                }
                list.Add(new { UserName = userName, Text = text, Time = time });
                MessagesScrollViewer.ScrollToBottom();
            });
        }

        private void Log(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[WatchParty] {msg}");
            });
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var text = MessageTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var msg = new { type = "chat", userId = _userData.UserId, userName = _userData.UserName, text = text };
            var json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                MessageTextBox.Clear();
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendButton_Click(sender, e);
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _syncTimer?.Dispose();
            if (_webSocket != null)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            Close();
        }
    }
}