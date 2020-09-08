using System.Windows;
using System.Windows.Input;

namespace RustGuiEditor
{
    /// <summary>
    /// Логика взаимодействия для About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);

            var x = pos.X / 10 + pos.Y / 10;
            Logo.Margin = new Thickness(x);
        }
    }
}
