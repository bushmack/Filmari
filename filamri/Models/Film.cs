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
        public string? Genre { get; set; }
        public List<string>? AllGenres { get; set; }
        public string? GenresString { get; set; }
        public double? Rating { get; set; }
        public string Type { get; set; } = "movie";
        public List<string> Actors { get; set; } = new();
        public string Country { get; set; } = "";
        public int? MovieLength { get; set; }
        public int? AgeRating { get; set; }
        public bool HasPoster { get; set; }
        public bool HasDescription { get; set; }

        // Для сериалов - длительность одной серии
        public int? SeriesLength { get; set; }

        // Отформатированная строка с длительностью
        public string EpisodeLength
        {
            get
            {
                if (Type == "tv-series" && SeriesLength.HasValue && SeriesLength > 0)
                    return $"⏱️ Серия: {SeriesLength} мин.";
                if (MovieLength.HasValue && MovieLength > 0)
                    return $"⏱️ Длительность: {MovieLength} мин.";
                return "";
            }
        }
    }
}