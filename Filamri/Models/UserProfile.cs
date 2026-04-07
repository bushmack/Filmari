namespace filamri.Models
{
    public class UserProfile
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public int TotalMoviesInCollections { get; set; }
        public double Progress { get; set; }
    }

    public class UserProgress
    {
        public int TotalMovies { get; set; }
        public double Progress { get; set; }
    }
}