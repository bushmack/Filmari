using filamri.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace filamri.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:8001");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // ========== СЛУЧАЙНЫЕ ФИЛЬМЫ ==========

        public async Task<List<Film>> GetRandomMoviesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/random-movie");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Film>>(json, _jsonOptions) ?? new List<Film>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Film>();
        }

        // ========== СЛУЧАЙНЫЕ СЕРИАЛЫ ==========

        public async Task<List<Film>> GetRandomSeriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/random-series");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Film>>(json, _jsonOptions) ?? new List<Film>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Film>();
        }

        // ========== ПОИСК ПО АКТЕРУ ==========

        public async Task<List<Film>> SearchByActorAsync(string actorName)
        {
            try
            {
                var url = $"/api/search/actor?name={HttpUtility.UrlEncode(actorName)}&limit=100";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Film>>(json, _jsonOptions) ?? new List<Film>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Film>();
        }

        // ========== ПОИСК ПО НАЗВАНИЮ ==========

        public async Task<List<Film>> SearchByNameAsync(string query)
        {
            try
            {
                var url = $"/api/search/name?query={HttpUtility.UrlEncode(query)}&limit=20";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Film>>(json, _jsonOptions) ?? new List<Film>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Film>();
        }





        // ========== СОВМЕСТНЫЙ ПРОСМОТР (WATCH PARTY) ==========

        public async Task<WatchRoom?> CreateWatchRoom(string userId, string userName, string videoPath)
        {
            try
            {
                var request = new { user_id = userId, user_name = userName, video_path = videoPath };
                var response = await _httpClient.PostAsJsonAsync("/api/watch/create-room", request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<WatchRoom>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания комнаты: {ex.Message}");
            }
            return null;
        }

        public async Task<WatchRoom?> JoinWatchRoom(string roomId, string userId, string userName)
        {
            try
            {
                var request = new { room_id = roomId, user_id = userId, user_name = userName };
                var response = await _httpClient.PostAsJsonAsync("/api/watch/join-room", request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<WatchRoom>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения: {ex.Message}");
            }
            return null;
        }

        public async Task<WatchRoom?> GetWatchRoomStatus(string roomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/watch/room-status/{roomId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<WatchRoom>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения статуса: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> SendWatchMessage(string roomId, string userId, string userName, string message)
        {
            try
            {
                var request = new { room_id = roomId, user_id = userId, user_name = userName, message };
                var response = await _httpClient.PostAsJsonAsync("/api/watch/send-message", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncWatchState(string roomId, double position, bool isPlaying)
        {
            try
            {
                var request = new { room_id = roomId, position, is_playing = isPlaying };
                var response = await _httpClient.PostAsJsonAsync("/api/watch/sync-state", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка синхронизации: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LeaveWatchRoom(string roomId, string userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/watch/leave-room/{roomId}/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка выхода: {ex.Message}");
                return false;
            }
        }






        // ========== ПОИСК ПО ФИЛЬТРУ ==========

        public async Task<List<Film>> SearchByFilterAsync(
            string? genre = null,
            int? yearFrom = null,
            int? yearTo = null,
            double? ratingFrom = null,
            double? ratingTo = null,
            string? country = null)
        {
            try
            {
                var query = HttpUtility.ParseQueryString(string.Empty);

                if (!string.IsNullOrEmpty(genre)) query["genre"] = genre;
                if (yearFrom.HasValue) query["year_from"] = yearFrom.ToString();
                if (yearTo.HasValue) query["year_to"] = yearTo.ToString();
                if (ratingFrom.HasValue) query["rating_from"] = ratingFrom.ToString();
                if (ratingTo.HasValue) query["rating_to"] = ratingTo.ToString();
                if (!string.IsNullOrEmpty(country)) query["country"] = country;

                query["limit"] = "50";

                var url = $"/api/search/filter?{query}";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Film>>(json, _jsonOptions) ?? new List<Film>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return new List<Film>();
        }

        // ========== ПОЛУЧЕНИЕ ФИЛЬМА ПО ID ==========

        public async Task<Film?> GetMovieById(int movieId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/movie/{movieId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Film>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return null;
        }

        // ========== ПОДБОРКИ ==========

        private static List<Collection> _collections = new List<Collection>();

        public async Task<List<Collection>> GetCollectionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/collections");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var collections = JsonSerializer.Deserialize<List<Collection>>(json, _jsonOptions);
                    if (collections != null)
                    {
                        _collections = collections;
                        return collections;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
            return _collections;
        }

        public async Task<bool> AddToCollectionAsync(int movieId, string collectionName)
        {
            try
            {
                var url = $"/api/collections/add?movie_id={movieId}&collection_name={HttpUtility.UrlEncode(collectionName)}";
                var response = await _httpClient.PostAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");

                var collection = _collections.Find(c => c.Name == collectionName);
                if (collection != null && !collection.Movies.Contains(movieId))
                {
                    collection.Movies.Add(movieId);
                }
                return true;
            }
        }

        public async Task<bool> CreateCollection(string collectionName)
        {
            try
            {
                var url = $"/api/collections/create?name={HttpUtility.UrlEncode(collectionName)}";
                var response = await _httpClient.PostAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");

                if (!_collections.Any(c => c.Name == collectionName))
                {
                    _collections.Add(new Collection { Name = collectionName, Movies = new List<int>() });
                }
                return true;
            }
        }

        public async Task<bool> RemoveFromCollection(string collectionName, int movieId)
        {
            try
            {
                var url = $"/api/collections/remove?name={HttpUtility.UrlEncode(collectionName)}&movie_id={movieId}";
                var response = await _httpClient.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");

                var collection = _collections.Find(c => c.Name == collectionName);
                if (collection != null)
                {
                    collection.Movies.Remove(movieId);
                }
                return true;
            }
        }

        // ========== СОВМЕСТНЫЙ ПОИСК (MATCH) ==========



        public async Task<dynamic> CreateMatchRoom(object request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/match/create-room", request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<dynamic>(json, _jsonOptions)!;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания комнаты: {ex.Message}");
            }

            return new { success = true, room_id = "TEST123", user_id = Guid.NewGuid().ToString() };
        }

        public async Task<dynamic> JoinMatchRoom(object request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/match/join-room", request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<dynamic>(json, _jsonOptions)!;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения к комнате: {ex.Message}");
            }

            return new { success = true, room_id = "TEST123" };
        }

        public async Task<dynamic> SelectGenres(string roomId, string userId, List<string> genres)
        {
            try
            {
                var request = new { room_id = roomId, user_id = userId, genres };
                var response = await _httpClient.PostAsJsonAsync("/api/match/select-genres", request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<dynamic>(json, _jsonOptions)!;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка выбора жанров: {ex.Message}");
            }

            return new { success = true };
        }

        public async Task<dynamic> SendSwipe(string roomId, string userId, int movieId, bool liked)
        {
            try
            {
                var request = new { room_id = roomId, user_id = userId, movie_id = movieId, liked };
                var response = await _httpClient.PostAsJsonAsync("/api/match/swipe", request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<dynamic>(json, _jsonOptions)!;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отправки свайпа: {ex.Message}");
            }

            return new { success = true, is_match_found = false };
        }

        public async Task<dynamic> GetRoomStatus(string roomId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/match/room-status/{roomId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<dynamic>(json, _jsonOptions)!;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения статуса комнаты: {ex.Message}");
            }

            return new { status = "waiting" };
        }

        public async Task LeaveRoom(string roomId, string userId)
        {
            try
            {
                await _httpClient.DeleteAsync($"/api/match/leave-room/{roomId}/{userId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка выхода из комнаты: {ex.Message}");
            }
        }

        // ========== КОММЕНТАРИИ ==========

        public async Task<List<Comment>?> GetCommentsAsync(int movieId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/comments/{movieId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                    if (data != null && data.TryGetValue("comments", out var commentsObj))
                    {
                        var commentsJson = ((JsonElement)commentsObj).GetRawText();
                        return JsonSerializer.Deserialize<List<Comment>>(commentsJson, _jsonOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки комментариев: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> AddCommentAsync(int movieId, string userName, string text, string userId)
        {
            try
            {
                var url = $"/api/comments/add?movie_id={movieId}&user_name={HttpUtility.UrlEncode(userName)}&text={HttpUtility.UrlEncode(text)}&user_id={userId}";
                var response = await _httpClient.PostAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления комментария: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCommentAsync(int movieId, int commentIndex, string userId)
        {
            try
            {
                var url = $"/api/comments/delete?movie_id={movieId}&comment_index={commentIndex}&user_id={userId}";
                var response = await _httpClient.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления комментария: {ex.Message}");
                return false;
            }
        }

        // ========== ЛИЧНЫЙ КАБИНЕТ ==========

        public async Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/user/profile?user_id={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
                    if (data != null && data.TryGetValue("profile", out var profileObj))
                    {
                        var profileJson = ((JsonElement)profileObj).GetRawText();
                        return JsonSerializer.Deserialize<UserProfile>(profileJson, _jsonOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки профиля: {ex.Message}");
            }
            return null;
        }

        public async Task<UserProgress?> GetUserProgressAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/user/progress?user_id={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserProgress>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки прогресса: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, string? userName, string? avatarUrl)
        {
            try
            {
                var parameters = new List<string>();
                parameters.Add($"user_id={userId}");
                if (!string.IsNullOrEmpty(userName))
                    parameters.Add($"user_name={HttpUtility.UrlEncode(userName)}");
                if (!string.IsNullOrEmpty(avatarUrl))
                    parameters.Add($"avatar_url={HttpUtility.UrlEncode(avatarUrl)}");
                
                var url = $"/api/user/update?{string.Join("&", parameters)}";
                var response = await _httpClient.PostAsync(url, null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления профиля: {ex.Message}");
                return false;
            }
        }
    }
}