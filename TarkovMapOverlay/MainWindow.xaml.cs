using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

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
        private Point origin; //var for zoom and pan
        private Point start; //var for zoom and pan
        private bool transparentBackground = false;
        private Keys minimizeKey = Keys.M;
        private MouseButtons minimizeButton = MouseButtons.Right;
        private bool toggleMinimizeKeybind = false;
        private bool toggleMinimizeMousebutton = false;
        private bool canMinimizeWithMouse = false;
        private string currentOpenImagePath;
        private List<string> _savedMaps = new List<string>();
        private Image image;


        public MainWindow()
        {
            InitializeComponent();

            // This ensures that the window is always on top, doesn't always work but should be good enough
            this.Topmost = true;
            
            //load SettingsFile if exists
            SavedSettings settings = LoadSettings();
            //load keybinds
            minimizeKey = settings.minimizeKey;
            minimizeButton = settings.minimizeMousebutton;
            minimizeKeybindItem.Header = "_Change " + minimizeKey.ToString() + " Keybind for minimizing";
            minimizeMouseButtonItem.IsEnabled = settings.minimizeWithMouseButton;
            minimizeWithMouseButtonItem.IsChecked = settings.minimizeWithMouseButton;
            canMinimizeWithMouse = settings.minimizeWithMouseButton;
            minimizeMouseButtonItem.Header = "_Change " + minimizeButton.ToString() + " Mousebutton for minimizing";
            //load saved opacity
            sliderMenu.Value = settings.visual_opacity * 100;
            //load saved windowState
            this.Top = settings.windowTop;
            this.Left = settings.windowLeft;
            this.Height = settings.windowHeight;
            this.Width = settings.windowWidth;
            MoveIntoView();
            //load last opened Map if file exists
            if (settings.currentMapPath != null)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(settings.currentMapPath, uriKind: UriKind.RelativeOrAbsolute));
                    TarkovMap.Source = bitmap;
                }
                catch (Exception e) {
                    Window win = new Window();
                    string error = "There was an error opening your last opened file. It will be removed from the selection";
                    System.Windows.Forms.MessageBox.Show(error,"",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.Error.WriteLine(e.Message);
                }
            }
            //load saved transparency BG setting
            setting_transparency.IsChecked = settings.visual_transparency;
            //load Custom MapList if Maps were Saved
            _savedMaps.AddRange(settings.customMapList);
            foreach (string MapName in settings.customMapList) {
                if (!File.Exists(MapName)) {
                    continue;
                }
                System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
                item.Header = MapName;
                item.Click += this.ButtonClicked;
                List_CustomMaps.Items.Add(item);
            }
            EnableMapListIfNotEmpty();

            //hooking MouseEvents to Imagecontainer for zoom and pan
            image = TarkovMap;
            WPFWindow.MouseWheel += MainWindow_MouseWheel;
            
            var prop = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
            prop.AddValueChanged(image, SourceChangedHandler);
            
            image.MouseLeftButtonDown += image_MouseLeftButtonDown;
            image.MouseLeftButtonUp += image_MouseLeftButtonUp;
            image.MouseMove += image_MouseMove;

            // Hooks to make the "M" key a keybind to toggle map
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;
            
            //Hooking to MouseEvents
            Hook.GlobalEvents().MouseDownExt += (sender, e) =>
            {
                if (e.Button != minimizeButton && !toggleMinimizeMousebutton)
                {
                    return;
                }
                    ToogleVisibilityWithMouseButtons(sender, e);   //toggle visibility if MouseButton is pressed 
            };
        }

        private void ToogleVisibilityWithMouseButtons(object sender, MouseEventExtArgs e)
        {
            if (!canMinimizeWithMouse)
            {
                return;
            }

            if (toggleMinimizeMousebutton)
            {
                if (e.Button == MouseButtons.Left) {
                    minimizeMouseButtonItem.Header = "Change MouseButton for minimizing";
                    return;
                }
                minimizeButton = e.Button;
                minimizeMouseButtonItem.Header = "Change " + minimizeButton.ToString() + " Mousebutton for minimizing";
                toggleMinimizeMousebutton = false;
            }
            else
            {
                if (this.Visibility == Visibility.Collapsed)
                {
                    this.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Visibility = Visibility.Collapsed;
                }
                this.Topmost = true;
                this.Focus();
            }  
        }

         void ButtonClicked(object sender, RoutedEventArgs e) {
            System.Windows.Controls.MenuItem item = (System.Windows.Controls.MenuItem)sender;
            string selectedFileName = item.Header.ToString();
            currentOpenImagePath = selectedFileName;
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(selectedFileName);
            bitmap.EndInit();
            TarkovMap.Source = bitmap;
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
                if (image.IsMouseCaptured)
                {
                    return;
                }
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
                if (this.Visibility == Visibility.Collapsed)
                {
                    this.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Visibility = Visibility.Collapsed;
                }
                this.Topmost = true;
                this.Focus();
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
                currentOpenImagePath = selectedFileName;
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selectedFileName);
                bitmap.EndInit();
                TarkovMap.Source = bitmap;
                Save_CustomMap.IsEnabled = true;
            }
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            SaveSettings();
            base.OnClosing(e);
        }

        private void SaveMap_OnClick(object sender, RoutedEventArgs e) 
        {
            if (TarkovMap.Source == null) 
            {
                return;
            }

            System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
            item.Header = currentOpenImagePath;
            List_CustomMaps.Items.Add(item);
            item.Click += this.ButtonClicked;

            if (currentOpenImagePath != null)
            {
                _savedMaps.Add(currentOpenImagePath);
            }
           
            EnableMapListIfNotEmpty();
        }

        private void minimizeWithMouseButtonItem_Checked(object sender, RoutedEventArgs e)
        {
            minimizeMouseButtonItem.IsEnabled = true;
            canMinimizeWithMouse = true;
        }

        private void minimizeWithMouseButtonItem_Unchecked(object sender, RoutedEventArgs e)
        {
            minimizeMouseButtonItem.IsEnabled = false;
            canMinimizeWithMouse = false;
        }

        private void Customs_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Customs.png", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Customs2_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Customs2.jpeg", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Customs_Dorms_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Customs_Dorms.jpeg", UriKind.Absolute));
            TarkovMap.Source = bitmap;
        }

        private void Customs_Stashes_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Customs_Stashes.jpeg", UriKind.Absolute));
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

        private void Woods_Stashes_OnClick(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage(new Uri(@"pack://application:,,,/Resources/Woods_Stashes.jpeg", UriKind.Absolute));
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
        private void MinimizeMousebutton_OnClick(object sender, RoutedEventArgs e)
        {
            minimizeMouseButtonItem.Header = "Press any Mousebutton except the left one";
            toggleMinimizeMousebutton = true;
        }

        private void EnableMapListIfNotEmpty()
        {
            if (List_CustomMaps.Items.Count != 0)
            {
                List_CustomMaps.IsEnabled = true;
            }
        }

        private bool SaveSettings() 
        {
            if (!Directory.Exists("Settings"))
            {
                Directory.CreateDirectory("Settings");
            }

            SavedSettings settings = new SavedSettings();
            settings.windowTop = this.Top;
            settings.windowLeft = this.Left;
            settings.windowHeight = this.Height;
            settings.windowWidth = this.Width;

            settings.visual_opacity = this.Opacity;
            settings.visual_transparency = transparentBackground;

            if (TarkovMap.Source != null)
            {
                settings.currentMapPath = TarkovMap.Source.ToString();
            }
            else {
                settings.currentMapPath = null;
            }

            settings.customMapList.AddRange(_savedMaps);
            settings.minimizeWithMouseButton = minimizeWithMouseButtonItem.IsChecked;
            settings.minimizeKey = minimizeKey;
            settings.minimizeMousebutton = minimizeButton;

            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = new FileStream("Settings/settings.sav", FileMode.Create);
            bf.Serialize(fs, settings);
            fs.Close();
            return true;
        }

        private SavedSettings LoadSettings()
        {
            if (File.Exists("Settings/settings.sav")) {
                FileStream fs = new FileStream("Settings/settings.sav", FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();

                SavedSettings settings = (SavedSettings)bf.Deserialize(fs);

                return settings;
            }

            return new SavedSettings();
        }

        public void MoveIntoView() //important if saved WindowPosition is out of screenbounds ( if saved on 2.screen for example )
        {
            double _windowTop = this.Top;
            double _windowHeight = this.Height;
            double _windowLeft = this.Left;
            double _windowWidth = this.Width;
            
            if (_windowTop + _windowHeight / 2 > SystemParameters.VirtualScreenHeight)
            {
                _windowTop = SystemParameters.VirtualScreenHeight -_windowHeight;
            }

            if (_windowLeft + _windowWidth / 2 > SystemParameters.VirtualScreenWidth)
            {
                _windowLeft = SystemParameters.VirtualScreenWidth - _windowWidth;
            }

            if (_windowTop < 0)
            {
                _windowTop = 0;
            }

            if (_windowLeft < 0)
            {
                _windowLeft = 0;
            }

            this.Top = _windowTop;
            this.Height = _windowHeight;
            this.Left = _windowLeft;
            this.Width = _windowWidth;
        }

        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            image.ReleaseMouseCapture();
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!image.IsMouseCaptured) {
                return;
            }
            
            Point p = e.MouseDevice.GetPosition(border);

            Matrix m = image.RenderTransform.Value;
            m.OffsetX = origin.X + (p.X - start.X);
            m.OffsetY = origin.Y + (p.Y - start.Y);

            image.RenderTransform = new MatrixTransform(m);
        }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (image.IsMouseCaptured) return;
            image.CaptureMouse();

            start = e.GetPosition(border);
            origin.X = image.RenderTransform.Value.OffsetX;
            origin.Y = image.RenderTransform.Value.OffsetY;
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(image);

            Matrix m = image.RenderTransform.Value;
            if (e.Delta > 0)
                m.ScaleAtPrepend(1.1, 1.1, p.X, p.Y);
            else
                m.ScaleAtPrepend(1 / 1.1, 1 / 1.1, p.X, p.Y);

            image.RenderTransform = new MatrixTransform(m);
        }

        void SourceChangedHandler(object sender, EventArgs e)
        {
            image.RenderTransform = new MatrixTransform();
        }
    }
}
