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
using System.Windows.Controls;
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
        private string currentOpenImagePath;
        private List<string> _savedMaps = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            
            // This ensures that the window is always on top, doesn't always work but should be good enough
            this.Topmost = true;

            //load SettingsFile if exists
            SavedSettings settings = LoadSettings();
            //load saved opacity
            sliderMenu.Value = settings.visual_opacity * 100;
            //load last opened Map
            if (settings.currentMapPath != null)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(settings.currentMapPath, UriKind.RelativeOrAbsolute));
                TarkovMap.Source = bitmap;
            }
            //load saved transparency BG setting
            setting_transparency.IsChecked = settings.visual_transparency;
            //load Custom MapList if Maps were Saved
            _savedMaps.AddRange(settings.customMapList);
            foreach (string MapName in settings.customMapList) {
                System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
                item.Header = MapName;
                item.Click += this.ButtonClicked;
                List_CustomMaps.Items.Add(item);
            }

            EnableMapListIfNotEmpty();

            // Hooks to make the "M" key a keybind to toggle map
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;
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
            if (SaveSettings())
            {
                this.Close();
            }
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
    }
}
