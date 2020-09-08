using System;
using System.Windows;
using System.Windows.Media;

namespace RustGuiEditor
{
    /// <summary>
    /// Логика взаимодействия для ProjectSettings.xaml
    /// </summary>
    public partial class ProjectSettings : Window
    {
        public static string RootId { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
        public static string RootPlane { get; set; } = "Overlay";
        public static Color RootColor { get; set; } = Colors.Transparent;
        public static bool ShowCursor { get; set; } = true;

        public static string[] PlaneList { get; } = new[] { "Overall", "Overlay", "Hud.Menus", "Hud", "Hud.Under" };

        public ProjectSettings()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void CloseSettings(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
