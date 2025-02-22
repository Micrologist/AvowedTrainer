using AvowedTrainer.Input;
using AvowedTrainer.Logic;
using AvowedTrainer.UI;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;

namespace AvowedTrainer.UI
{
    public partial class MainWindow : Window
    {
        private readonly Logic.AvowedTrainer trainer;
        private GlobalKeyboardHook kbHook;
        private readonly float[] gameSpeeds = new float[4] { 1f, 2f, 4f, 0.5f };
        private bool shouldAcceptKeystrokes = true;
        private readonly Dictionary<string, Key> defaultKeybinds = new Dictionary<string, Key>()
        {
            { "god", Key.F1 },
            { "noclip", Key.F2 },
            { "store", Key.F6 },
            { "teleport", Key.F7 }
        };
        private Dictionary<string, Key> keybinds = new();
        private Dictionary<Key, Action> keybindActions;


        public MainWindow()
        {
            InitializeComponent();
            InitializeKeyboardHook();
            trainer = new Logic.AvowedTrainer();
            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = new TimeSpan(10 * 10000) };
            timer.Tick += UIUpdateTick;
            timer.Start();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This enables dragging the window from anywhere
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            trainer.ShouldAbort = true;
            kbHook.unhook();
            Application.Current.Shutdown();
        }

        public void SetKeybinds(Dictionary<string, Key> newKeybinds)
        {
            kbHook.HookedKeys.Clear();
            keybindActions.Clear();
            string keybindStore = "";

            foreach (KeyValuePair<string, Key> keybind in newKeybinds)
            {
                switch (keybind.Key)
                {
                    case "god":
                        keybindActions.Add(keybind.Value, () => godBtn_Click(null, null));
                        SetKeybindText(godBtn, keybind.Value);
                        keybindStore += "god,";
                        break;
                    case "noclip":
                        keybindActions.Add(keybind.Value, () => noclipBtn_Click(null, null));
                        SetKeybindText(noclipBtn, keybind.Value);
                        keybindStore += "noclip,";
                        break;
                    case "store":
                        keybindActions.Add(keybind.Value, () => saveBtn_Click(null, null));
                        SetKeybindText(saveBtn, keybind.Value);
                        keybindStore += "store,";
                        break;
                    case "teleport":
                        keybindActions.Add(keybind.Value, () => teleBtn_Click(null, null));
                        SetKeybindText(teleBtn, keybind.Value);
                        keybindStore += "teleport,";
                        break;
                    default:
                        break;
                }
                kbHook.HookedKeys.Add(keybind.Value);
                keybindStore += (int)keybind.Value + ",";
            }
            keybindStore = keybindStore.Substring(0, keybindStore.LastIndexOf(","));
            keybinds = newKeybinds;
            try
            {
                File.WriteAllText("AvowedTrainer_Keybinds.cfg", keybindStore);
            }
            catch (UnauthorizedAccessException)
            {
                _ = System.Windows.MessageBox.Show("Keybindings could not be saved.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void InitializeKeyboardHook()
        {
            kbHook = new GlobalKeyboardHook();
            kbHook.KeyDown += HandleKeyDown;
            keybindActions = new Dictionary<Key, Action>();
            string keybindStore = "";
            if (File.Exists("AvowedTrainer_Keybinds.cfg"))
            {
                try
                {
                    keybindStore = File.ReadAllText("AvowedTrainer_Keybinds.cfg");
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("Keybindings could not be read.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                string[] keybindArray = keybindStore.Split(',');

                if (keybindArray.Length == defaultKeybinds.Count * 2)
                {
                    Dictionary<string, Key> savedKeybinds = new Dictionary<string, Key>();
                    for (int i = 0; i < defaultKeybinds.Count * 2; i += 2)
                    {
                        savedKeybinds.Add(keybindArray[i], (Key)int.Parse(keybindArray[i + 1]));
                    }
                    SetKeybinds(savedKeybinds);
                    return;
                }
            }
            SetKeybinds(defaultKeybinds);
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (!shouldAcceptKeystrokes)
            {
                return;
            }
            if (keybindActions.ContainsKey(e.Key))
            {
                keybindActions[e.Key]();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            trainer.ShouldAbort = true;
            kbHook.unhook();
        }

        private void UIUpdateTick(object sender, EventArgs e)
        {
            positionBlock.Text =
                  (trainer.XPos / 100).ToString("0.00") + "\n"
                + (trainer.YPos / 100).ToString("0.00") + "\n"
                + (trainer.ZPos / 100).ToString("0.00");
            speedBlock.Text = trainer.Vel.ToString("0.00") + " m/s";
            SetLabel(trainer.ShouldGod, godLabel);
            SetLabel(trainer.ShouldNoclip, noclipLabel);
            SetLabel(trainer.ShouldAmmo, ammoLabel);
        }

        private void SetLabel(bool state, System.Windows.Controls.Label label)
        {
            label.Content = state ? "ON" : "OFF";
            label.Foreground = state ? Brushes.Green : Brushes.Red;
        }

        private void SetKeybindText(Button button, Key key)
        {
            string text = (string)button.Content;
            string keyName = key.ToString();
            if ((int)key >= 48 && (int)key <= 57)
            {
                keyName = keyName.Replace("D", "");
            }
            button.Content = Regex.Replace(text, "\\[.*\\]", "[" + keyName + "]");
        }

        private void teleBtn_Click(object sender, RoutedEventArgs e)
        {
            trainer.ShouldTeleport = true;
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            trainer.ShouldStore = true;
        }

        private void noclipBtn_Click(object sender, RoutedEventArgs e)
        {
            trainer.ShouldNoclip = !trainer.ShouldNoclip;
        }

        private void godBtn_Click(object sender, RoutedEventArgs e)
        {
            trainer.ShouldGod = !trainer.ShouldGod;
        }

        private void ammoBtn_Click(object sender, RoutedEventArgs e)
        {
            trainer.ShouldAmmo = !trainer.ShouldAmmo;
        }

        private void editKeybindBtn_Click(object sender, RoutedEventArgs e)
        {
            shouldAcceptKeystrokes = false;
            KeybindWindow kbWindow = new KeybindWindow(this, keybinds);
            _ = kbWindow.ShowDialog();
            shouldAcceptKeystrokes = true;
        }
    }
}
