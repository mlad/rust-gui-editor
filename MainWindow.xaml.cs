using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace RustGuiEditor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            FontRoboto = new FontFamily(new Uri("pack://application:,,,/"), "fonts/#Roboto");
        }

        FontFamily FontRoboto;

        public enum EditModes
        {
            None,
            Move,
            Resize
        }

        public EditModes EditMode = EditModes.Move;
        FrameworkElement currentElement;
        int offsetX;
        int offsetY;


        BaseCuiElement ActiveElement => (BaseCuiElement)this.Resources["activeElement"];

        public const int canvasWidth = 1600;
        public const int canvasHeight = 900;

        private string _activeProject;
        public string ActiveProject { get => _activeProject; set { _activeProject = value; this.Title = $"CUI Editor - {value}"; } }


        /*
                     foreach(var i in Fonts.GetFontFamilies(new Uri("pack://application:,,,/fonts/#")))
            {
                Console.WriteLine(i);
            }
             
             */
        private void OnMoveGuiComponent(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var element = (FrameworkElement)sender;
            if (element != currentElement) return;

            var position = e.GetPosition(mainCanvas);
            var newX = (int)position.X - offsetX;
            var newY = (int)position.Y - offsetY;

            switch (EditMode)
            {
                case EditModes.Move:
                    {
                        if (newX < 0) newX = 0;
                        if (newX + element.Width > mainCanvas.Width) newX = (int)(mainCanvas.Width - element.Width);

                        if (newY < 0) newY = 0;
                        if (newY + element.Height > mainCanvas.Height) newY = (int)(mainCanvas.Height - element.Height);

                        if (Keyboard.IsKeyUp(Key.LeftCtrl)) Canvas.SetLeft(element, newX);
                        if (Keyboard.IsKeyUp(Key.LeftShift)) Canvas.SetTop(element, newY);

                        break;
                    }
                case EditModes.Resize:
                    {
                        if (newX < 5) newX = 5;
                        if (newY < 5) newY = 5;

                        if (Canvas.GetLeft(element) + newX > canvasWidth) newX = canvasWidth - (int) Canvas.GetLeft(element);
                        if (Canvas.GetTop(element) + newY > canvasHeight) newY = canvasHeight - (int) Canvas.GetTop(element);

                        if (element is Image || Keyboard.IsKeyDown(Key.LeftShift))
                        {
                            var coef = element.Height / element.Width;
                            element.Width = newX;
                            element.Height = newX * coef;
                        }
                        else
                        {
                            element.Width = newX;
                            element.Height = newY;
                        }

                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private void OnMoveGuiCompoentBegin(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var element = (FrameworkElement)sender;
            if (currentElement != null) return;

            var position = e.GetPosition(element);
            switch (EditMode)
            {
                case EditModes.Move:
                    break;
                case EditModes.Resize:
                    position = e.GetPosition(mainCanvas);
                    position.Offset(-element.Width, -element.Height);
                    break;
                default:
                    throw new NotImplementedException();
            }

            offsetX = (int)position.X;
            offsetY = (int)position.Y;

            element.CaptureMouse();
            currentElement = element;

            this.Resources["activeElement"] = element.Tag;
            this.Resources["activeFrameworkElement"] = element;
        }

        private void OnMoveGuiComponentEnd(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Released) return;

            var element = (UIElement)sender;
            if (element != currentElement) return;

            var activeElem = ActiveElement;

            switch (EditMode)
            {
                case EditModes.Move:
                    activeElem.OnChanged("X");
                    activeElem.OnChanged("Y");
                    break;
                case EditModes.Resize:
                    activeElem.OnChanged("Width");
                    activeElem.OnChanged("Height");
                    break;
                default:
                    throw new NotImplementedException();
            }

            element.ReleaseMouseCapture();
            currentElement = null;
        }

        void AddCanvasElement<T>(T element, ElementType type) where T : FrameworkElement
        {
            Canvas.SetLeft(element, 0);
            Canvas.SetTop(element, 0);

            if (typeof(T) == typeof(Button))
            {
                element.PreviewMouseDown += OnMoveGuiCompoentBegin;
                element.PreviewMouseMove += OnMoveGuiComponent;
                element.PreviewMouseUp += OnMoveGuiComponentEnd;
            }
            else
            {
                element.MouseDown += OnMoveGuiCompoentBegin;
                element.MouseMove += OnMoveGuiComponent;
                element.MouseUp += OnMoveGuiComponentEnd;
            }

            switch (type)
            {
                case ElementType.None:
                    break;
                case ElementType.Rectangle:
                    element.Tag = new CuiRectangle { Elem = element };
                    break;
                case ElementType.Image:
                    element.Tag = new CuiImage { Elem = element };
                    break;
                case ElementType.Button:
                    element.Tag = new CuiButton { Elem = element };
                    break;
                case ElementType.Label:
                    element.Tag = new CuiLabel { Elem = element };
                    break;
                case ElementType.Texture:
                    element.Tag = new CuiTexture { Elem = element };
                    break;
                default:
                    throw new NotImplementedException();
            }

            mainCanvas.Children.Add(element);
        }

        private void CreateRectangleButton(object sender, RoutedEventArgs e)
        {
            var rect = new Rectangle() { Fill = Brushes.Green, Width = 40, Height = 40 };
            AddCanvasElement(rect, ElementType.Rectangle);
        }

        private void CreateButtonButton(object sender, RoutedEventArgs e)
        {
            var btn = new Button()
            {
                Width = 40,
                Height = 40,
                Content = "Button",
                FontSize = 16,
                FontFamily = FontRoboto,
                Foreground = Brushes.White
            };
            AddCanvasElement(btn, ElementType.Button);
        }

        private void CreateImageButton(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = "Image (*.png;*.jpeg;*.bmp;*.jpg)|*.png;*.jpeg;*.bmp;*.jpg|All files (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() != true)
                return;

            BitmapImage bmp;
            try
            {
                bmp = new BitmapImage(new Uri(fileDialog.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"При загрузке изображения произошла ошибка:\n{ex.Message}",
                    "Ошибка создания", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var width = bmp.Width;
            var height = bmp.Height;

            var coef = Math.Ceiling(width / canvasWidth);
            if (coef > 1)
            {
                width /= coef;
                height /= coef;
            }

            coef = Math.Ceiling(height / canvasHeight);
            if (coef > 1)
            {
                width /= coef;
                height /= coef;
            }

            var img = new Image() { Width = width, Height = height, Source = bmp };
            AddCanvasElement(img, ElementType.Image);
        }

        private void CreateTextButton(object sender, RoutedEventArgs e)
        {
            var text = new TextBlock()
            {
                Width = 50,
                Height = 50,
                Text = "Text Block",
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                FontSize = 16,
                FontFamily = FontRoboto,
                Foreground = Brushes.White
            };
            AddCanvasElement(text, ElementType.Label);
        }

        private void CreateTextureButton(object sender, RoutedEventArgs e)
        {
            var spriteDialog = new SpriteLib();
            if (spriteDialog.ShowDialog() != true)
                return;

            BitmapImage bmp;
            try
            {
                bmp = new BitmapImage(SpriteLib.FromGameToUri(spriteDialog.SpritePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"При загрузке изображения произошла ошибка:\n{ex.Message}",
                    "Ошибка создания", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var width = bmp.Width;
            var height = bmp.Height;

            var coef = Math.Ceiling(width / canvasWidth);
            if (coef > 1)
            {
                width /= coef;
                height /= coef;
            }

            coef = Math.Ceiling(height / canvasHeight);
            if (coef > 1)
            {
                width /= coef;
                height /= coef;
            }

            var img = new Image() { Width = width, Height = height, Source = bmp };
            AddCanvasElement(img, ElementType.Texture);
        }

        private void SetMoveEditMode(object sender, RoutedEventArgs e)
        {
            EditMode = EditModes.Move;
            btnModeMove.IsEnabled = false;
            btnModeReize.IsEnabled = true;
        }

        private void SetResizeEditMode(object sender, RoutedEventArgs e)
        {
            EditMode = EditModes.Resize;
            btnModeMove.IsEnabled = true;
            btnModeReize.IsEnabled = false;
        }

        private void DeleteElementButton(object sender, RoutedEventArgs e) { }

        private void ButtonGcCollect(object sender, RoutedEventArgs e) => GC.Collect();

        private void CenterXButton(object sender, RoutedEventArgs e)
        {
            var element = ActiveElement;
            if (element == null) return;

            var coef = Keyboard.IsKeyDown(Key.LeftShift) ? 3 : 2;

            Canvas.SetLeft(element.Elem, (int)(mainCanvas.Width / coef - element.Width / 2));
            element.OnChanged("X");
        }

        private void CenterYButton(object sender, RoutedEventArgs e)
        {
            var element = ActiveElement;
            if (element == null) return;

            var coef = Keyboard.IsKeyDown(Key.LeftShift) ? 3 : 2;

            Canvas.SetTop(element.Elem, (int)(mainCanvas.Height / coef - element.Height / 2));
            element.OnChanged("Y");
        }

        private void RemoveButton(object sender, RoutedEventArgs e)
        {
            var element = ActiveElement;
            if (element == null) return;

            mainCanvas.Children.Remove(element.Elem);
        }

        private void CopyButton(object sender, RoutedEventArgs e)
        {
            var element = ActiveElement;
            if (element == null) return;

        }

        private void ExportButton(object sender, RoutedEventArgs e)
        {
            var st = new StringBuilder();

            st.Append("var elements = new CuiElementContainer\n{\n");

            // Основная планка
            st.Append("{\n");
            new CuiRectangle()
            {
                Elem = new Rectangle(),
                X = 0, Y = 0, Width=canvasWidth, Height=canvasHeight,
                Color = new SolidColorBrush(ProjectSettings.RootColor),
                EnableCursor=ProjectSettings.ShowCursor
            }.Export(st);
            st.AppendFormat(",\n\t\"{0}\", \"{1}\"", ProjectSettings.RootPlane, ProjectSettings.RootId);
            st.Append("\n},\n");

            // Элементы
            foreach (FrameworkElement i in mainCanvas.Children)
            {
                st.Append("{\n");

                (i.Tag as BaseCuiElement).Export(st);

                st.AppendFormat(",\n\t\"{0}\"", ProjectSettings.RootId);
                st.Append("\n},\n");
            }

            st.Append("};\n");

            File.WriteAllText("output.cs", st.ToString());

            Process.Start(@"C:\Program Files (x86)\Notepad++\notepad++.exe", "output.cs");
        }

        private void SaveProject(string filename)
        {
            var proj = new XElement("Project");
            XDocument doc = new XDocument(proj);

            var projSettings = new XElement("Settings");
            projSettings.Add(new XElement("RootId", ProjectSettings.RootId));
            projSettings.Add(new XElement("RootPlane", ProjectSettings.RootPlane));
            projSettings.Add(new XElement("RootColor", ProjectSettings.RootColor));
            projSettings.Add(new XElement("ShowCursor", ProjectSettings.ShowCursor));
            proj.Add(projSettings);

            var cuiList = new XElement("Elements");
            proj.Add(cuiList);

            foreach (FrameworkElement i in mainCanvas.Children)
            {
                var data = i.Tag as BaseCuiElement;

                var node = new XElement(data.Type.ToString());
                node.SetAttributeValue("X", data.X);
                node.SetAttributeValue("Y", data.Y);
                node.SetAttributeValue("W", data.Width);
                node.SetAttributeValue("H", data.Height);

                switch (data.Type)
                {
                    case ElementType.None:
                        throw new NotSupportedException();
                    case ElementType.Rectangle:
                        node.SetAttributeValue("Color", (data as CuiRectangle).Color);
                        node.SetAttributeValue("EnableCursor", (data as CuiRectangle).EnableCursor);
                        break;
                    case ElementType.Image:
                        node.SetAttributeValue("Url", (data as CuiImage).Url);
                        node.SetAttributeValue("Sprite", (data as CuiImage).Sprite);
                        break;
                    case ElementType.Button:
                        node.SetAttributeValue("TextSize", (data as CuiButton).TextSize);
                        node.SetAttributeValue("TextColor", (data as CuiButton).TextColor);
                        node.SetAttributeValue("Text", (data as CuiButton).Text);
                        node.SetAttributeValue("Color", (data as CuiButton).Color);
                        node.SetAttributeValue("Close", (data as CuiButton).Close);
                        node.SetAttributeValue("Action", (data as CuiButton).Action);
                        node.SetAttributeValue("Align", (data as CuiButton).Align);
                        break;
                    case ElementType.Label:
                        node.SetAttributeValue("TextSize", (data as CuiLabel).TextSize);
                        node.SetAttributeValue("Text", (data as CuiLabel).Text);
                        node.SetAttributeValue("Color", (data as CuiLabel).Color);
                        node.SetAttributeValue("Align", (data as CuiLabel).Align);
                        break;
                    case ElementType.Texture:
                        node.SetAttributeValue("Sprite", (data as CuiTexture).Sprite);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                cuiList.Add(node);
            }

            doc.Save(filename);
        }

        private void LoadProject(string filename)
        {
            CreateNewProject(null, null);

            var doc = XDocument.Load(filename);
            var proj = doc.Element("Project");

            var settings = proj.Element("Settings");
            ProjectSettings.RootId = settings.Element("RootId").Value;
            ProjectSettings.RootPlane = settings.Element("RootPlane").Value;
            ProjectSettings.RootColor = (Color)ColorConverter.ConvertFromString(settings.Element("RootColor").Value);
            ProjectSettings.ShowCursor = bool.Parse(settings.Element("ShowCursor").Value);

            foreach (var i in proj.Element("Elements").Elements())
            {
                var type = (ElementType)Enum.Parse(typeof(ElementType), i.Name.ToString());

                FrameworkElement element;
                switch (type)
                {
                    case ElementType.None:
                        throw new NotSupportedException();
                    case ElementType.Rectangle:
                        var rect = new Rectangle();
                        AddCanvasElement(rect, ElementType.Rectangle);
                        element = rect;

                        (rect.Tag as CuiRectangle).Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(i.Attribute("Color").Value));
                        (rect.Tag as CuiRectangle).EnableCursor = bool.Parse(i.Attribute("EnableCursor").Value);
                        break;
                    case ElementType.Image:
                        var img = new Image();
                        AddCanvasElement(img, ElementType.Image);
                        element = img;

                        (img.Tag as CuiImage).Url = i.Attribute("Url").Value;
                        (img.Tag as CuiImage).Sprite = i.Attribute("Sprite").Value;
                        break;
                    case ElementType.Button:
                        var btn = new Button() { FontFamily=FontRoboto };
                        AddCanvasElement(btn, ElementType.Button);
                        element = btn;

                        (btn.Tag as CuiButton).TextSize = int.Parse(i.Attribute("TextSize").Value);
                        (btn.Tag as CuiButton).TextColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(i.Attribute("TextColor").Value));
                        (btn.Tag as CuiButton).Text = i.Attribute("Text").Value;
                        (btn.Tag as CuiButton).Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(i.Attribute("Color").Value));
                        (btn.Tag as CuiButton).Close = i.Attribute("Close")?.Value ?? "";
                        (btn.Tag as CuiButton).Action = i.Attribute("Action")?.Value ?? "";
                        (btn.Tag as CuiButton).Align = (TextAnchor)Enum.Parse(typeof(TextAnchor), i.Attribute("Align").Value);
                        break;
                    case ElementType.Label:
                        var lbl = new TextBlock()
                        {
                            Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                            FontFamily = FontRoboto
                        };
                        AddCanvasElement(lbl, ElementType.Label);
                        element = lbl;

                        (lbl.Tag as CuiLabel).TextSize = int.Parse(i.Attribute("TextSize").Value);
                        (lbl.Tag as CuiLabel).Text = i.Attribute("Text").Value;
                        (lbl.Tag as CuiLabel).Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(i.Attribute("Color").Value));
                        (lbl.Tag as CuiLabel).Align = (TextAnchor)Enum.Parse(typeof(TextAnchor), i.Attribute("Align").Value);
                        break;
                    case ElementType.Texture:
                        var sImg = new Image();
                        AddCanvasElement(sImg, ElementType.Texture);
                        element = sImg;

                        (sImg.Tag as CuiTexture).Sprite = i.Attribute("Sprite").Value;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var cuiElem = (BaseCuiElement)element.Tag;
                cuiElem.X = int.Parse(i.Attribute("X").Value);
                cuiElem.Y = int.Parse(i.Attribute("Y").Value);
                cuiElem.Width = int.Parse(i.Attribute("W").Value);
                cuiElem.Height = int.Parse(i.Attribute("H").Value);
            }
        }


        private void SaveProject(object sender, RoutedEventArgs e)
        {
            if (ActiveProject == null)
            {
                var dialog = new SaveFileDialog()
                {
                    AddExtension = true,
                    Filter = "Project (*.xml)|*.xml|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == false)
                    return;

                ActiveProject = dialog.FileName;
            }

            try
            {
                SaveProject(ActiveProject);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Во время сохранения проекта произошла ошибка:\n{ex.Message}", 
                    "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);

                ActiveProject = null;
            }
        }

        private void LoadProject(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = "Project (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == false)
                return;

            try
            {
                LoadProject(dialog.FileName);
                ActiveProject = dialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Во время загрузки проекта произошла ошибка:\n{ex.Message}",
                    "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateNewProject(object sender, RoutedEventArgs e)
        {
            mainCanvas.Children.Clear();
            this.Resources["activeElement"] = null;
            ActiveProject = null;
            GC.Collect();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            new ProjectSettings
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }
            .ShowDialog();
        }

        private void OpenAbout(object sender, RoutedEventArgs e)
        {
            new About
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }
            .ShowDialog();
        }
    }

}
