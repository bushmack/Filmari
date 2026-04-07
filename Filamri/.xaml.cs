// Временное окно
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows;

public class MovieMatchSwipeWindow : Window
{
    public MovieMatchSwipeWindow(string roomId, string userId, bool isHost)
    {
        Title = "Поиск фильмов";
        Width = 800;
        Height = 600;
        Content = new TextBlock { Text = "Здесь будет поиск фильмов со свайпами", FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    }
}