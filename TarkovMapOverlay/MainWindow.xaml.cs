using Gma.System.MouseKeyHook;
using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TarkovMapOverlay
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IKeyboardMouseEvents m_GlobalHook;
        private Point startPoint;
        private double opacity;
        private bool transparentBackground = false;
        private Keys minimizeKey = Keys.M;
        private bool toggleMinimizeKeybind = false;

        public MainWindow()
        {
            InitializeComponent();
            // This ensures that the window is always on top, doesn't always work but should be good enough
            this.Topmost = true;

            // Initial opacity
            sliderMenu.Value = 100;

            // Hooks to make the "M" key a keybind to toggle map
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;

        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            // This ensures that the image changes size when we resize the window
            TarkovMap.Height = this.Height;
            TarkovMap.Width = this.Width;
            base.OnRenderSizeChanged(sizeInfo);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (e.ChangedButton == MouseButton.Left)
            {
                startPoint = e.GetPosition(this);
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (sliderMenu.Value > 1.0 && sliderMenu.Value != opacity)
            {
                opacity = sliderMenu.Value;
                if (transparentBackground)
                {
                    this.Background = new SolidColorBrush(Colors.Black) {Opacity = 0};
                    this.Opacity = opacity * 0.01;
                    this.BorderBrush = new SolidColorBrush(Colors.Black) {Opacity = 0};
                }
                else
                {
                    this.Opacity = opacity * 0.01;
                    this.Background = new SolidColorBrush(Colors.Black) { Opacity = opacity * 0.01 };
                }
            }

            var currentPoint = e.GetPosition(this);

            // This makes the entire window draggable
            if (e.LeftButton == MouseButtonState.Pressed &&
                this.IsActive &&
                (Math.Abs(currentPoint.X - startPoint.X) >
                 SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(currentPoint.Y - startPoint.Y) >
                 SystemParameters.MinimumVerticalDragDistance))
            {
                this.DragMove();
            }
        }

        private void GlobalHookKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            // Change the current keybind for minimizing the map overlay
            if (toggleMinimizeKeybind)
            {
                minimizeKey = e.KeyCode;
                minimizeKeybindItem.Header = "Change " + e.KeyCode.ToString() + " Keybind for minimizing";
                toggleMinimizeKeybind = false;
            } 
            else if (e.KeyCode == minimizeKey) // Toggle minimizing of map with a keybing
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                    this.Topmost = true;
                }
                else
                {
                    this.WindowState = WindowState.Minimized;
                }
            }
        }

        private void Browse_OnClick(object sender, RoutedEventArgs e)
        { 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.Filter = "Image files|*.jpg;*.png;|All Files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
                string selectedFileName = dlg.FileName;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selectedFileName);
                bitmap.EndInit();
                TarkovMap.Source = bitmap;
            }
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Customs_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Customs.png", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Factory_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Factory.jpg", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Interchange_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Interchange.jpg", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Labs_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Labs.png", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Reserv_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Reserv.png", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Shoreline_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Shoreline.jpg", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Wooeds_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Woods.jpg", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void TransparentBackground_OnCheck(object sender, RoutedEventArgs e)
        {
            opacity = sliderMenu.Value;
            this.Background = new SolidColorBrush(Colors.Black) { Opacity = 0 };
            this.Opacity = opacity * 0.01;
            this.BorderBrush = new SolidColorBrush(Colors.Black) {Opacity = 0};
            transparentBackground = true;
        }

        private void TransparentBackground_OnUncheck(object sender, RoutedEventArgs e)
        {
            opacity = sliderMenu.Value;
            this.Background = new SolidColorBrush(Colors.Black) { Opacity = 1 };
            this.Opacity = opacity * 0.01;
            this.BorderBrush = new SolidColorBrush(Colors.Black) { Opacity = 1 };
            transparentBackground = false;
        }

        private void MinimizeKeybind_OnClick(object sender, RoutedEventArgs e)
        {
            minimizeKeybindItem.Header = "Press any key to set a keybind";
            toggleMinimizeKeybind = true;
        }
    }
}
