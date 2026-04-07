using System;

namespace filamri.Models
{
    public static class AppData
    {
        public static string UserId { get; set; } = "";
        public static string UserName { get; set; } = "Пользователь";
        public static string? AvatarPath { get; set; }
        public static int TotalMoviesInCollections { get; set; } = 0;

        public static void Initialize()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                UserId = Guid.NewGuid().ToString();
                UserName = "User_" + UserId.Substring(0, 4);
            }

            // Загружаем из LocalStorage вместо Properties
            var userData = LocalStorage.Load();
            if (userData != null)
            {
                if (!string.IsNullOrEmpty(userData.UserName))
                    UserName = userData.UserName;
                if (!string.IsNullOrEmpty(userData.AvatarPath))
                    AvatarPath = userData.AvatarPath;
                TotalMoviesInCollections = userData.TotalMovies;
            }
        }

        public static void Save()
        {
            var userData = new UserData
            {
                UserId = UserId,
                UserName = UserName,
                AvatarPath = AvatarPath ?? "",
                TotalMovies = TotalMoviesInCollections
            };
            LocalStorage.Save(userData);
        }
    }
}