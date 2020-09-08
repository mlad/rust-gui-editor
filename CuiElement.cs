using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace RustGuiEditor
{
    public class MultilineTextBoxEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
            textBox.AcceptsReturn = true;
            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(textBox, System.Windows.Controls.TextBox.TextProperty, _binding);
            return textBox;
        }
    }
    public enum ElementType : byte
    {
        None,
        Rectangle,
        Image,
        Button,
        Label,
        Texture
    }

    public enum TextAnchor : byte
    {
        UpperLeft,
        UpperCenter,
        UpperRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        LowerLeft,
        LowerCenter,
        LowerRight,
    }

    abstract class BaseCuiElement : INotifyPropertyChanged
    {
        public FrameworkElement Elem;

        public abstract ElementType Type { get; }

        [PropertyOrder(0)]
        public int X { get => (int)Canvas.GetLeft(Elem); set => Canvas.SetLeft(Elem, value); }

        [PropertyOrder(1)]
        public int Y { get => (int)Canvas.GetTop(Elem); set => Canvas.SetTop(Elem, value); }

        [PropertyOrder(2)]
        public double Width { get => (int)Elem.Width; set => Elem.Width = value; }

        [PropertyOrder(3)]
        public double Height { get => (int)Elem.Height; set => Elem.Height = value; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public abstract void Export(StringBuilder st);

        protected static string FormatColor(Brush brush)
        {
            var clr = (brush as SolidColorBrush).Color;
            return $"{clr.R / 255f:0.##} {clr.G / 255f:0.##} {clr.B / 255f:0.##} {clr.A / 255f:0.##}";
        }

        protected void ExportTransform(StringBuilder st)
        {
            var canvasWidth = MainWindow.canvasWidth;
            var canvasHeight = MainWindow.canvasHeight;

            float xMin, yMin, xMax, yMax;

            xMin = (float)X / canvasWidth;
            yMin = (canvasHeight - (Y + (float)Height)) / canvasHeight;

            xMax = (X + (float)Width) / canvasWidth;
            yMax = ((float)(canvasHeight - Y)) / canvasHeight;

            st.AppendFormat("\t\tRectTransform = {0}AnchorMin = \"{1:0.###} {2:0.###}\", AnchorMax = \"{3:0.###} {4:0.###}\"{5},\n", '{', xMin, yMin, xMax, yMax, '}');
        }
    }

    class CuiRectangle : BaseCuiElement
    {
        public override ElementType Type => ElementType.Rectangle;

        public Brush Color { get => (Elem as Rectangle).Fill; set => (Elem as Rectangle).Fill = value; }
        public bool EnableCursor { get; set; }

        public override void Export(StringBuilder st)
        {
            st.AppendLine("\tnew CuiPanel");
            st.AppendLine("\t{");
            st.Append("\t\tImage = new CuiImageComponent {Color = \"" + FormatColor(Color) + "\"},\n");
            ExportTransform(st);
            if (EnableCursor) st.AppendFormat("\t\tCursorEnabled = {0}\n", EnableCursor.ToString().ToLower());
            st.Append("\t}");
        }
    }

    class CuiImage : BaseCuiElement
    {
        public override ElementType Type => ElementType.Image;
        public string Url { get => (Elem as Image).Source.ToString(); set => (Elem as Image).Source = new BitmapImage(new Uri(value)); }
        public string Sprite { get; set; } = "assets/content/textures/generic/fulltransparent.tga";

        public override void Export(StringBuilder st)
        {
            st.AppendLine("\tnew CuiPanel");
            st.AppendLine("\t{");
            st.AppendLine("\t\tImage = null,");
            st.AppendLine("\t\tRawImage = new CuiRawImageComponent");
            st.AppendLine("\t\t{");
            st.AppendLine("\t\t\tColor = \"1 1 1 1\",");
            st.AppendFormat("\t\t\tUrl = \"{0}\",\n", Url);
            st.AppendFormat("\t\t\tSprite = \"{0}\"\n", Sprite);
            st.AppendLine("\t\t},");
            ExportTransform(st);
            st.Append("\t}");
        }
    }

    class CuiLabel : BaseCuiElement
    {
        public override ElementType Type => ElementType.Label;
        public Brush Color { get => (Elem as TextBlock).Foreground; set => (Elem as TextBlock).Foreground = value; }
        public int TextSize { get => (int)(Elem as TextBlock).FontSize; set => (Elem as TextBlock).FontSize = value; }

        private TextAnchor _align;
        public TextAnchor Align
        {
            get => _align;
            set
            {
                _align = value;
                switch (value)
                {
                    case TextAnchor.UpperLeft:
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.LowerLeft:
                        (Elem as TextBlock).TextAlignment = TextAlignment.Left;
                        break;

                    case TextAnchor.UpperCenter:
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        (Elem as TextBlock).TextAlignment = TextAlignment.Center;
                        break;

                    case TextAnchor.UpperRight:
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        (Elem as TextBlock).TextAlignment = TextAlignment.Right;
                        break;
                }
            }
        }

        [Editor(typeof(MultilineTextBoxEditor), typeof(MultilineTextBoxEditor))]
        public string Text { get => (Elem as TextBlock).Text; set => (Elem as TextBlock).Text = value; }

        public override void Export(StringBuilder st)
        {
            st.AppendLine("\tnew CuiLabel");
            st.AppendLine("\t{");
            st.AppendLine("\t\tText =");
            st.AppendLine("\t\t{");
            st.AppendFormat("\t\t\tText = \"{0}\",\n", Text.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""));
            st.AppendFormat("\t\t\tColor = \"{0}\",\n", FormatColor(Color));
            st.AppendFormat("\t\t\tFontSize = {0},\n", (int)(TextSize * 1.5f));
            st.AppendFormat("\t\t\tAlign = TextAnchor.{0},\n", Align);
            st.AppendLine("\t\t},");
            ExportTransform(st);
            st.Append("\t}");
        }
    }

    class CuiButton : BaseCuiElement
    {
        public override ElementType Type => ElementType.Button;
        public Brush Color { get => (Elem as Button).Background; set => (Elem as Button).Background = value; }
        public Brush TextColor { get => (Elem as Button).Foreground; set => (Elem as Button).Foreground = value; }
        public string Text { get => (string)(Elem as Button).Content; set => (Elem as Button).Content = value; }
        public int TextSize { get => (int)(Elem as Button).FontSize; set => (Elem as Button).FontSize = value; }
        public TextAnchor Align { get; set; } = TextAnchor.MiddleCenter;
        public string Action { get; set; }
        public string Close { get; set; }



        public override void Export(StringBuilder st)
        {
            st.Append("\tnew CuiButton\n\t{\n\t\tButton = { ");
            if (!string.IsNullOrWhiteSpace(Action)) st.AppendFormat("Command = \"{0}\", ", Action);
            if (!string.IsNullOrWhiteSpace(Close)) st.AppendFormat("Close = \"{0}\", ", Close);
            st.AppendFormat("Color = \"{0}\"", FormatColor(Color));
            st.AppendLine(" },");
            ExportTransform(st);
            st.AppendLine("\t\tText =");
            st.AppendLine("\t\t{");
            st.AppendFormat("\t\t\tText = \"{0}\",\n", Text.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""));
            st.AppendFormat("\t\t\tColor = \"{0}\",\n", FormatColor(TextColor));
            st.AppendFormat("\t\t\tFontSize = {0},\n", (int)(TextSize * 1.5f));
            st.AppendFormat("\t\t\tAlign = TextAnchor.{0}\n", Align);
            st.AppendLine("\t\t}");
            st.Append("\t}");
        }
    }

    class CuiTexture : BaseCuiElement
    {
        public override ElementType Type => ElementType.Texture;
        public string Sprite
        {
            get => new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, "Sprite"))
                .MakeRelativeUri(new Uri((Elem as Image).Source.ToString())).ToString().Substring(7);
            set => (Elem as Image).Source = new BitmapImage(SpriteLib.FromGameToUri(value));
        }

        public override void Export(StringBuilder st)
        {
            st.AppendLine("\tnew CuiPanel");
            st.AppendLine("\t{");
            st.AppendLine("\t\tImage = null,");
            st.AppendLine("\t\tRawImage = new CuiRawImageComponent");
            st.AppendLine("\t\t{");
            st.AppendLine("\t\t\tColor = \"1 1 1 1\",");
            st.AppendFormat("\t\t\tUrl = null,\n");
            st.AppendFormat("\t\t\tSprite = \"{0}\"\n", Sprite);
            st.AppendLine("\t\t},");
            ExportTransform(st);
            st.Append("\t}");
        }
    }
}
