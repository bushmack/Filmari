using filamri.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}