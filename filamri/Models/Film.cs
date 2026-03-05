using System.Collections.Generic;

namespace filamri.Models
{
    public class Film
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string PosterUrl { get; set; } = "";
        public int? Year { get; set; }
        public string? Genre { get; set; } // Первый жанр
        public List<string>? AllGenres { get; set; } // ВСЕ жанры
        public string? GenresString { get; set; } // Все жанры строкой
        public double? Rating { get; set; }
        public string Type { get; set; } = "movie";
        public List<string> Actors { get; set; } = new();
        public string Country { get; set; } = "";
        public int? MovieLength { get; set; }
        public int? AgeRating { get; set; }
        public bool HasPoster { get; set; }
        public bool HasDescription { get; set; }
    }
}