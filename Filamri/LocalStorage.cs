using System.IO;
using System.Text.Json;

namespace filamri
{
    public static class LocalStorage
    {
        private static string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Filamri",
            "userdata.json");

        public static UserData Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<UserData>(json) ?? new UserData();
                }
            }
            catch { }
            return new UserData();
        }

        public static void Save(UserData data)
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }

    public class UserData
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "Пользователь";
        public string AvatarPath { get; set; } = "";
        public int TotalMovies { get; set; } = 0;
    }
}