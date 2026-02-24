using OneButtonApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace OneButtonApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public ApiService()
        {
            _httpClient.BaseAddress = new Uri("http://localhost:8002");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // 5 случайных фильмов
        public async Task<List<Film>> GetRandomMoviesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/random-movie");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Film>>(json, options);
                return result ?? new List<Film>();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при получении фильмов: " + ex.Message);
            }
        }

        // 5 случайных сериалов
        public async Task<List<Film>> GetRandomSeriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/random-series");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Film>>(json, options);
                return result ?? new List<Film>();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при получении сериалов: " + ex.Message);
            }
        }

        // Поиск по актерам
        public async Task<List<Film>> SearchByActorAsync(string actorName)
        {
            try
            {
                var url = $"/api/search/actor?name={HttpUtility.UrlEncode(actorName)}&count=5";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Film>>(json, options);
                return result ?? new List<Film>();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при поиске по актерам: " + ex.Message);
            }
        }

        // Поиск по названию
        public async Task<List<Film>> SearchByNameAsync(string query)
        {
            try
            {
                var url = $"/api/search/name?query={HttpUtility.UrlEncode(query)}&count=5";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Film>>(json, options);
                return result ?? new List<Film>();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при поиске по названию: " + ex.Message);
            }
        }

        // Поиск по фильтру
        public async Task<List<Film>> SearchByFilterAsync(
            string genre = null,
            int? yearFrom = null,
            int? yearTo = null,
            double? ratingFrom = null,
            double? ratingTo = null,
            string country = null)
        {
            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);

                if (!string.IsNullOrEmpty(genre))
                    query["genre"] = genre;
                if (yearFrom.HasValue)
                    query["year_from"] = yearFrom.ToString();
                if (yearTo.HasValue)
                    query["year_to"] = yearTo.ToString();
                if (ratingFrom.HasValue)
                    query["rating_from"] = ratingFrom.ToString();
                if (ratingTo.HasValue)
                    query["rating_to"] = ratingTo.ToString();
                if (!string.IsNullOrEmpty(country))
                    query["country"] = country;

                query["count"] = "5";

                var url = $"/api/search/filter?{query}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Film>>(json, options);
                return result ?? new List<Film>();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при поиске по фильтру: " + ex.Message);
            }
        }

        // Совместный поиск
        public async Task<List<Film>> CombinedSearchAsync(
            string query,
            string actor = null,
            string genre = null,
            int? year = null)
        {
            try
            {
                var url = $"/api/search/combined?query={HttpUtility.UrlEncode(query)}";

                if (!string.IsNullOrEmpty(actor))
                    url += $"&actor={HttpUtility.UrlEncode(actor)}";
                if (!string.IsNullOrEmpty(genre))
                    url += $"&genre={HttpUtility.UrlEncode(genre)}";
                if (year.HasValue)
                    url += $"&year={year}";

                url += "&count=5";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Film>>(json, options);
                return result ?? new List<Film>();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при совместном поиске: " + ex.Message);
            }
        }

        // Мои подборки
        public async Task<List<Collection>> GetCollectionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/collections");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<Collection>>(json, options);
                return result ?? new List<Collection>();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при получении подборок: " + ex.Message);
            }
        }

        // Добавить в подборку
        public async Task<bool> AddToCollectionAsync(int movieId, string collectionName)
        {
            try
            {
                var url = $"/api/collections/add?movie_id={movieId}&collection_name={HttpUtility.UrlEncode(collectionName)}";
                var response = await _httpClient.PostAsync(url, null);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при добавлении в подборку: " + ex.Message);
            }
        }
    }
}