using System;
using System.Collections.Generic;

namespace filamri.Models
{
    public class MatchUser
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public List<string> SelectedGenres { get; set; } = new();
        public bool IsReady { get; set; }
    }

    public class MatchRoom
    {
        public string RoomId { get; set; } = "";
        public List<MatchUser> Users { get; set; } = new();
        public List<Film> CurrentMovies { get; set; } = new();
        public int CurrentMovieIndex { get; set; }
        public bool IsMatchFound { get; set; }
        public Film? MatchedFilm { get; set; }
        public string Status { get; set; } = "waiting";
    }
}