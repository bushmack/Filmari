using System.Collections.Generic;

namespace filamri.Models
{
    public class MatchRoomResponse
    {
        public string roomId { get; set; } = "";
        public string status { get; set; } = "";
        public List<MatchUserResponse> users { get; set; } = new();
        public List<Film> currentMovies { get; set; } = new();
        public int currentMovieIndex { get; set; }
        public bool isMatchFound { get; set; }
        public Film? matchedFilm { get; set; }
        public object? current_movie_swipes { get; set; }
    }

    public class MatchUserResponse
    {
        public string userId { get; set; } = "";
        public string userName { get; set; } = "";
        public bool isReady { get; set; }
    }
}