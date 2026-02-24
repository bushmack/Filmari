namespace OneButtonApp.Models
{
    public class Film
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string PosterUrl { get; set; } = "";
        public int? Year { get; set; }
        public string? Genre { get; set; }
        public double? Rating { get; set; }
    }
}