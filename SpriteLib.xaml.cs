using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RustGuiEditor
{
    /// <summary>
    /// Логика взаимодействия для SpriteLib.xaml
    /// </summary>
    public partial class SpriteLib : Window
    {
        private List<string> sprites = new List<string>();
        public string SpritePath { get; private set; }

        public static string FromGameToLocal(string gamePath)
        {
            var path = "Sprite\\" + gamePath.Substring(0, gamePath.Length - 4).Replace('/', '\\') + ".png";
            return path;
        }

        public static Uri FromGameToUri(string gamePath)
        {
            return new Uri(Path.Combine(Environment.CurrentDirectory, FromGameToLocal(gamePath)));
        }

        public static string FromLocalToGame(string localPath)
        {
            var path = localPath.Substring(7, localPath.Length - 7 - 4).Replace('\\', '/') + ".tga";
            return path;
        }

        public SpriteLib()
        {
            InitializeComponent();

            foreach (var i in Directory.EnumerateFiles("Sprite", "*.*", SearchOption.AllDirectories))
            {
                sprites.Add(i);
            }

            RefreshList();
        }

        void RefreshList()
        {
            spriteList.Items.Clear();

            var searchText = spriteSearch.Text;
            if (searchText.Length == 0)
            {
                foreach (var i in sprites)
                    spriteList.Items.Add(i);
            }
            else
            {
                foreach (var i in sprites)
                    if (i.Contains(searchText))
                        spriteList.Items.Add(i);
            }
        }

        private void SpriteList_Selected(object sender, RoutedEventArgs e)
        {
            if (spriteList.SelectedIndex == -1)
            {
                spritePreview.Source = null;
                return;
            }

            var path = (string)spriteList.SelectedValue;
            spritePreview.Source = new BitmapImage(new Uri(Path.Combine(Environment.CurrentDirectory, path)));
        }

        private void SpriteSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            RefreshList();
        }

        private void SetPreviewFill(object sender, RoutedEventArgs e)
        {
            Resources["previewStretch"] = Stretch.Fill;
        }

        private void SetPreviewNone(object sender, RoutedEventArgs e)
        {
            Resources["previewStretch"] = Stretch.None;
        }

        private void SaveButton(object sender, RoutedEventArgs e)
        {
            if (spriteList.SelectedIndex == -1)
            {
                MessageBox.Show("Ничего не выбрано", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SpritePath = FromLocalToGame((string)spriteList.SelectedValue);
            DialogResult = true;
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            SpritePath = null;
            DialogResult = false;
        }

        private void CopyButton(object sender, RoutedEventArgs e)
        {
            if (spriteList.SelectedIndex == -1)
            {
                MessageBox.Show("Ничего не выбрано", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var path = (string)spriteList.SelectedValue;
            path = path.Substring(7, path.Length - 7 - 4).Replace('\\', '/') + ".tga";

            Clipboard.SetText(path);
            MessageBox.Show("Скопировано: " + path, "Копировать", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
