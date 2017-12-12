using Microsoft.Win32;
using PROBot;
using PROProtocol;
using PROShine.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.ObjectModel;
using PROBot.Modules;
using FontAwesome.WPF;
using System.Windows.Documents;
using MahApps.Metro.Controls;
using System.Linq;

namespace PROShine
{
    public partial class MainWindow : Window
    {
        public BotClient Bot { get; private set; }

        public TeamView Team { get; private set; }
        public InventoryView Inventory { get; private set; }
        public ChatView Chat { get; private set; }
        public PlayersView Players { get; private set; }
        public MapView Map { get; private set; }
        public TradeView Trade { get; private set; }
        public List<Pokedex> items = new List<Pokedex>();

        private struct TabView
        {
            public UserControl View;
            public ContentControl Content;
            public ToggleButton Button;
        }
        private List<TabView> _views = new List<TabView>();

        public FileLogger FileLog { get; private set; }

        DateTime _refreshPlayers;
        int _refreshPlayersDelay;
        DateTime _lastQueueBreakPointTime;
        int? _lastQueueBreakPoint;

        private int _queuePosition;

        private ObservableCollection<OptionSlider> _sliderOptions;
        private ObservableCollection<TextOption> _textOptions;
        private static List<string> PokeLinks;
        private static List<string> PokeName;
        public MainWindow()
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
#endif
            Thread.CurrentThread.Name = "UI Thread";

            Bot = new BotClient();
            Bot.StateChanged += Bot_StateChanged;
            Bot.ClientChanged += Bot_ClientChanged;
            Bot.AutoReconnector.StateChanged += Bot_AutoReconnectorStateChanged;
            Bot.StaffAvoider.StateChanged += Bot_StaffAvoiderStateChanged;
            Bot.PokemonEvolver.StateChanged += Bot_PokemonEvolverStateChanged;
            Bot.IsTrainerBattlesActive.StateChanged += Bot_BattleTrainersStateChanged;
            Bot.ConnectionOpened += Bot_ConnectionOpened;
            Bot.ConnectionClosed += Bot_ConnectionClosed;
            Bot.MessageLogged += Bot_LogMessage;
            Bot.CMessageLogged += Bot_ColorMessageLogged;
            Bot.SliderCreated += Bot_SliderCreated;
            Bot.SliderRemoved += Bot_SliderRemoved;
            Bot.TextboxCreated += Bot_TextboxCreated;
            Bot.TextboxRemoved += Bot_TextboxRemoved;

            InitializeComponent();
            AutoReconnectSwitch.IsChecked = Bot.AutoReconnector.IsEnabled;
            AvoidStaffSwitch.IsChecked = Bot.StaffAvoider.IsEnabled;
            AutoEvolveSwitch.IsChecked = Bot.PokemonEvolver.IsEnabled;

            App.InitializeVersion();

            Team = new TeamView(Bot);
            Inventory = new InventoryView(Bot);
            Chat = new ChatView(Bot);
            Players = new PlayersView(Bot);
            Map = new MapView(Bot);
            Trade = new TradeView(Bot);

            FileLog = new FileLogger();

            _refreshPlayers = DateTime.UtcNow;
            _refreshPlayersDelay = 5000;

            AddView(Team, TeamContent, TeamButton, true);
            AddView(Inventory, InventoryContent, InventoryButton);
            AddView(Chat, ChatContent, ChatButton);
            AddView(Players, PlayersContent, PlayersButton);
            AddView(Map, MapContent, MapButton);
            AddView(Trade, TradeContent, TradeButton);

            SetTitle(null);

            LogMessage("Running " + App.Name + " by " + App.Author + ", version " + App.Version);

            Task.Run(() => UpdateClients());

            OptionSliders.ItemsSource = _sliderOptions = new ObservableCollection<OptionSlider>();
            TextOptions.ItemsSource = _textOptions = new ObservableCollection<TextOption>();
            TextBlock Name = new TextBlock();
            Name.Foreground = Brushes.OrangeRed;
            Name.Text = "Not Loaded.";
            SpawnList.Children.Add(Name);
            TextBlock time = new TextBlock();
            time.Foreground = Brushes.OrangeRed;
            time.Text = "??";
            Time.Children.Add(time);
            TextBlock money1 = new TextBlock();
            money1.Foreground = Brushes.OrangeRed;
            money1.Text = "?";
            toolTipMoney.Children.Add(money1);
            TextBlock money2 = new TextBlock();
            money2.Foreground = Brushes.OrangeRed;
            money2.Text = "?";
            toolTipMoney2.Children.Add(money2);

            PokeLinks = new List<string>();
            PokeName = new List<string>();
        }

        private void Bot_ColorMessageLogged(string message, Brush color)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message, color);
            });
        }

        public void Bot_SliderRemoved(OptionSlider option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (_sliderOptions.Count == 1 && _textOptions.Count == 0)
                {
                    OptionsButton.Content = "Show Options";
                    OptionsButton.Visibility = Visibility.Collapsed;
                    OptionSliders.Visibility = Visibility.Collapsed;
                    TextOptions.Visibility = Visibility.Collapsed;
                }

                _sliderOptions.Remove(option);
                OptionSliders.Items.Refresh();
            });
        }

        public void Bot_TextboxRemoved(TextOption option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (_textOptions.Count == 1 && _sliderOptions.Count == 0)
                {
                    OptionsButton.Content = "Show Options";
                    OptionsButton.Visibility = Visibility.Collapsed;
                    OptionSliders.Visibility = Visibility.Collapsed;
                    TextOptions.Visibility = Visibility.Collapsed;
                }

                _textOptions.Remove(option);
                TextOptions.Items.Refresh();
            });
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            if (OptionSliders.Visibility == Visibility.Collapsed)
            {
                OptionsButton.Content = "Hide Options";
                OptionSliders.Visibility = Visibility.Visible;
                TextOptions.Visibility = Visibility.Visible;
            }
            else
            {
                OptionsButton.Content = "Show Options";
                OptionSliders.Visibility = Visibility.Collapsed;
                TextOptions.Visibility = Visibility.Collapsed;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // On pressing enter, take focus from the textbox and give it to the selected button in _views
            // This is necessary to update the content of the TextOption
            if (e.Key == Key.Enter || e.Key == Key.Return)
                foreach (TabView view in _views)
                    if (view.Button.IsChecked.Value)
                        Keyboard.Focus(view.Button);
        }

        public void Bot_TextboxCreated(TextOption option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                OptionsButton.Visibility = Visibility.Visible;
                _textOptions.Add(option);
                TextOptions.Items.Refresh();
            });
        }

        public void Bot_SliderCreated(OptionSlider option)
        {
            Dispatcher.InvokeAsync(delegate
            {
                OptionsButton.Visibility = Visibility.Visible;
                _sliderOptions.Add(option);
                OptionSliders.Items.Refresh();
            });
        }

        private void AddView(UserControl view, ContentControl content, ToggleButton button, bool visible = false)
        {
            _views.Add(new TabView
            {
                View = view,
                Content = content,
                Button = button
            });
            content.Content = view;
            if (visible)
            {
                content.Visibility = Visibility.Visible;
                button.IsChecked = true;
            }
            else
            {
                content.Visibility = Visibility.Collapsed;
            }
            button.Click += ViewButton_Click;
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (TabView view in _views)
            {
                if (view.Button == sender)
                {
                    view.Content.Visibility = Visibility.Visible;
                    view.Button.IsChecked = true;
                    _refreshPlayersDelay = view.View == Players ? 200 : 5000;
                }
                else
                {
                    view.Content.Visibility = Visibility.Collapsed;
                    view.Button.IsChecked = false;
                }
            }
        }

        private void SetTitle(string username)
        {
            Title = username == null ? "" : username + " - ";
            Title += App.Name + " " + App.Version;
#if DEBUG
            Title += " (debug)";
#endif
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Dispatcher.InvokeAsync(() => HandleUnhandledException(e.Exception.InnerException));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        private void HandleUnhandledException(Exception ex)
        {
            try
            {
                if (ex != null)
                {
                    File.WriteAllText("crash_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt",
                        App.Name + " " + App.Version + " crash report: " + Environment.NewLine + ex);
                }
                MessageBox.Show(App.Name + " encountered a fatal error. The application will now terminate." + Environment.NewLine +
                    "An error file has been created next to the application.", App.Name + " - Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            catch
            {
            }
        }

        private void UpdateClients()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    Bot.Game.Update();
                }
                Bot.Update();
            }
            Task.Delay(1).ContinueWith((previous) => UpdateClients());
        }

        private void LoginMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenLoginWindow();
        }

        private void OpenLoginWindow()
        {
            LoginWindow login = new LoginWindow(Bot) { Owner = this };
            bool? result = login.ShowDialog();
            if (result != true)
            {
                return;
            }

            LogMessage("Connecting to the server...", Brushes.SeaGreen);
            LoginButton.IsEnabled = false;
            LoginMenuItem.IsEnabled = false;
            Account account = new Account(login.Username);
            lock (Bot)
            {
                account.Password = login.Password;
                account.Server = login.Server;
                account.MacAddress = login.MacAddress;
                if (login.HasProxy)
                {
                    account.Socks.Version = (SocksVersion)login.ProxyVersion;
                    account.Socks.Host = login.ProxyHost;
                    account.Socks.Port = login.ProxyPort;
                    account.Socks.Username = login.ProxyUsername;
                    account.Socks.Password = login.ProxyPassword;
                }
                Bot.Login(account);
            }
        }

        private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void Logout()
        {
            LogMessage("Logging out...", Brushes.OrangeRed);
            lock (Bot)
            {
                Bot.Logout(false);
            }
        }

        private void MenuPathScript_Click(object sender, RoutedEventArgs e)
        {
            LoadScript();
        }

        private void LoadScript(string filePath = null)
        {
            if (filePath == null)
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = App.Name + " Scripts|*.lua;*.txt|All Files|*.*"
                };
                bool? result = openDialog.ShowDialog();

                if (!(result.HasValue && result.Value))
                    return;

                filePath = openDialog.FileName;
            }

            try
            {
                lock (Bot)
                {
                    Bot.SliderOptions.Clear();
                    Bot.TextOptions.Clear();
                    _sliderOptions.Clear();
                    _textOptions.Clear();
                    OptionSliders.Items.Refresh();
                    TextOptions.Items.Refresh();
                    OptionsButton.Content = "Show Options";
                    OptionsButton.Visibility = Visibility.Collapsed;
                    OptionSliders.Visibility = Visibility.Collapsed;
                    TextOptions.Visibility = Visibility.Collapsed;

                    Bot.LoadScript(filePath);
                    MenuPathScript.Header =
                        "Script: \"" + Bot.Script.Name + "\"" + Environment.NewLine + filePath;
                    LogMessage("Script \"{0}\" by \"{1}\" successfully loaded", Bot.Script.Name, Bot.Script.Author);
                    if (!string.IsNullOrEmpty(Bot.Script.Description))
                    {
                        LogMessage(Bot.Script.Description);
                    }
                    UpdateBotMenu();
                }
            }
            catch (Exception ex)
            {
                string filename = Path.GetFileName(filePath);
#if DEBUG
                LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex);
#else
                LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex.Message);
#endif
            }
        }

        private void BotStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Start();
            }
        }

        private void BotStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Stop();
            }
        }

        private void Client_PlayerAdded(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerUpdated(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerRemoved(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Bot_ConnectionOpened()
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        SetTitle(Bot.Account.Name + " - " + Bot.Game.Server);
                        UpdateBotMenu();
                        LogoutMenuItem.IsEnabled = true;
                        LoginMenuItem.IsEnabled = false;
                        LoginButton.IsEnabled = true;
                        LoginButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.SignOut;
                        LogMessage("Connected, authenticating...", Brushes.SeaGreen);
                    }
                }
            });
        }

        private void Bot_ConnectionClosed()
        {
            Dispatcher.InvokeAsync(delegate
            {
                _lastQueueBreakPoint = null;
                LoginMenuItem.IsEnabled = true;
                LogoutMenuItem.IsEnabled = false;
                LoginButton.IsEnabled = true;
                LoginButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.SignIn;
                UpdateBotMenu();
                StatusText.Text = "Offline";
                StatusText.Foreground = Brushes.Red;
            });
        }

        private void Client_LoggedIn()
        {
            Dispatcher.InvokeAsync(delegate
            {
                _lastQueueBreakPoint = null;
                LogMessage("Authenticated successfully!", Brushes.SeaGreen);
                UpdateBotMenu();
                StatusText.Text = "Online";
                StatusText.Foreground = Brushes.Green;
            });
        }

        private void Client_AuthenticationFailed(AuthenticationResult reason)
        {
            Dispatcher.InvokeAsync(delegate
            {
                string message = "";
                switch (reason)
                {
                    case AuthenticationResult.AlreadyLogged:
                        message = "Already logged in";
                        break;
                    case AuthenticationResult.Banned:
                        message = "You are banned from PRO";
                        break;
                    case AuthenticationResult.EmailNotActivated:
                        message = "Email not activated";
                        break;
                    case AuthenticationResult.InvalidPassword:
                        message = "Invalid password";
                        break;
                    case AuthenticationResult.InvalidUser:
                        message = "Invalid username";
                        break;
                    case AuthenticationResult.InvalidVersion:
                        message = "Outdated client, please wait for an update";
                        break;
                    case AuthenticationResult.Locked:
                    case AuthenticationResult.Locked2:
                        message = "Server locked for maintenance";
                        break;
                    case AuthenticationResult.OtherServer:
                        message = "Already logged in on another server";
                        break;
                }
                LogMessage("Authentication failed: " + message, Brushes.OrangeRed);
            });
        }

        private void Bot_StateChanged(BotClient.State state)
        {
            Dispatcher.InvokeAsync(delegate
            {
                UpdateBotMenu();
                string stateText;
                if (state == BotClient.State.Started)
                {
                    stateText = "started";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                }
                else if (state == BotClient.State.Paused)
                {
                    stateText = "paused";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                }
                else
                {
                    stateText = "stopped";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                }
                if(stateText == "started")
                { LogMessage("Bot " + stateText, Brushes.SeaGreen); }
                else
                { LogMessage("Bot " + stateText, Brushes.Red); }               
            });
        }

        private void Bot_LogMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message);
            });
        }

        private void Bot_AutoReconnectorStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (AutoReconnectSwitch.IsChecked == value) return;
                AutoReconnectSwitch.IsChecked = value;
            });
        }

        private void Bot_StaffAvoiderStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (AvoidStaffSwitch.IsChecked == value) return;
                AvoidStaffSwitch.IsChecked = value;
            });
        }

        private void Bot_PokemonEvolverStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (AutoEvolveSwitch.IsChecked == value) return;
                AutoEvolveSwitch.IsChecked = value;
            });
        }
        private void Bot_BattleTrainersStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (IsTrainerBattlesActiveSwitch.IsChecked == value) return;
                IsTrainerBattlesActiveSwitch.IsChecked = value;
            });
        }

        private void Bot_ClientChanged()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    Bot.Game.LoggedIn += Client_LoggedIn;
                    Bot.Game.AuthenticationFailed += Client_AuthenticationFailed;
                    Bot.Game.QueueUpdated += Client_QueueUpdated;
                    Bot.Game.PositionUpdated += Client_PositionUpdated;
                    Bot.Game.PokemonsUpdated += Client_PokemonsUpdated;
                    Bot.Game.InventoryUpdated += Client_InventoryUpdated;
                    Bot.Game.BattleStarted += Client_BattleStarted;
                    Bot.Game.BattleMessage += Client_BattleMessage;
                    Bot.Game.BattleEnded += Client_BattleEnded;
                    Bot.Game.DialogOpened += Client_DialogOpened;
                    //chat
                    Bot.Game.ChatMessage += Chat.Client_ChatMessage;
                    Bot.Game.ChannelMessage += Chat.Client_ChannelMessage;
                    Bot.Game.EmoteMessage += Chat.Client_EmoteMessage;
                    Bot.Game.ChannelSystemMessage += Chat.Client_ChannelSystemMessage;
                    Bot.Game.ChannelPrivateMessage += Chat.Client_ChannelPrivateMessage;
                    Bot.Game.PrivateMessage += Chat.Client_PrivateMessage;
                    Bot.Game.LeavePrivateMessage += Chat.Client_LeavePrivateMessage;
                    Bot.Game.RefreshChannelList += Chat.Client_RefreshChannelList;
                    //
                    Bot.Game.SystemMessage += Client_SystemMessage;
                    Bot.Game.PlayerAdded += Client_PlayerAdded;
                    Bot.Game.PlayerUpdated += Client_PlayerUpdated;
                    Bot.Game.PlayerRemoved += Client_PlayerRemoved;
                    Bot.Game.InvalidPacket += Client_InvalidPacket;
                    Bot.Game.PokeTimeUpdated += Client_PokeTimeUpdated;
                    Bot.Game.ShopOpened += Client_ShopOpened;
                    Bot.Game.CreatingCharacterAction += Client_CreatingCharacter;
                    //trade
                    Bot.Game.TradeRequested += Trade.TradeRequest;
                    Bot.Game.TradeCanceled += Trade.Reset;
                    Bot.Game.TradeMoneyUpdated += Trade.UpdateMoney;
                    Bot.Game.TradePokemonUpdated += Trade_PokemonsUpdated;
                    Bot.Game.TradeStatusUpdated += Trade.StatusChanged;
                    Bot.Game.TradeStatusReset += Trade.StatusReset;
                    Bot.Game.TradeAccepted += Trade.ChangeToFinalView;
                    //map
                    Bot.Game.MapLoaded += Map.Client_MapLoaded;
                    Bot.Game.PositionUpdated += Map.Client_PositionUpdated;
                    Bot.Game.PlayerAdded += Map.Client_PlayerEnteredMap;
                    Bot.Game.PlayerRemoved += Map.Client_PlayerLeftMap;
                    Bot.Game.PlayerUpdated += Map.Client_PlayerMoved;
                    Bot.Game.NpcReceived += Map.Client_NpcReceived;
                    Bot.Game.SpawnListUpdated += Client_RefreshSpawnList;
                    Bot.Game.PokedexListUpdated += Client_RefreshPokedexList;
                }
            }
            Dispatcher.InvokeAsync(delegate
            {
                if (Bot.Game != null)
                {
                    FileLog.OpenFile(Bot.Account.Name, Bot.Game.Server.ToString());
                }
                else
                {
                    FileLog.CloseFile();
                }
            });
        }

        private void Client_QueueUpdated(int position)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (_queuePosition != position)
                {
                    _queuePosition = position;
                    TimeSpan? queueTimeLeft = null;
                    if (_lastQueueBreakPoint != null && position < _lastQueueBreakPoint)
                    {
                        queueTimeLeft = TimeSpan.FromTicks((DateTime.UtcNow - _lastQueueBreakPointTime).Ticks / (_lastQueueBreakPoint.Value - position) * position);
                    }
                    StatusText.Text = "In Queue" + " (" + position + ")";
                    if (queueTimeLeft != null)
                    {
                        StatusText.Text += " ";
                        if (queueTimeLeft.Value.Hours > 0)
                        {
                            StatusText.Text += queueTimeLeft.Value.ToString(@"hh\:mm\:ss");
                        }
                        else
                        {
                            StatusText.Text += queueTimeLeft.Value.ToString(@"mm\:ss");

                        }
                        StatusText.Text += " left";
                    }
                    StatusText.Foreground = Brushes.DarkBlue;
                    if (_lastQueueBreakPoint == null)
                    {
                        _lastQueueBreakPoint = position;
                        _lastQueueBreakPointTime = DateTime.UtcNow;
                    }
                }
            });
        }

        private void Client_PositionUpdated(string map, int x, int y)
        {
            Dispatcher.InvokeAsync(delegate
            {
                MapNameText.Text = map;
                PlayerPositionText.Text = "(" + x + "," + y + ")";
            });
        }

        private void Client_PokemonsUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                IList<Pokemon> team;
                lock (Bot)
                {
                    team = Bot.Game.Team.ToArray();
                }
                Team.PokemonsListView.ItemsSource = team;
                Team.PokemonsListView.Items.Refresh();
            });
        }
        //Creating Random Male or Female character lol.
        private void Client_CreatingCharacter()
        {
            lock (Bot)
            {
                try
                {
                    if (Bot.Game.CreatingCharacter == true)
                    {
                        if (MessageBox.Show(App.Name + " is creating character. So, should " + App.Name + " create Male or Female character?" + Environment.NewLine +
                            "Yes = Male character." + Environment.NewLine + "No = Female character", "Creating Character!", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                        {
                            Bot.Game.CreatingCharacterMale = true;
                            Bot.Game.CreatingCharacterFemale = false;
                        }
                        else
                        {
                            Bot.Game.CreatingCharacterFemale = true;
                            Bot.Game.CreatingCharacterMale = false;
                        }
                    }
                }
                catch (Exception ex) { }
            }
        }

        private void Trade_PokemonsUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                IList<TradePokemon> First_items;
                IList<TradePokemon> Second_items;
                lock (Bot)
                {
                    First_items = Bot.Game.First_Trade.ToArray();
                    Second_items = Bot.Game.Second_Trade.ToArray();
                }
                Trade.First_list.ItemsSource = First_items;
                Trade.Second_list.ItemsSource = Second_items;
                Trade.First_list.Items.Refresh();
                Trade.Second_list.Items.Refresh();
            });
        }

        private void Client_InventoryUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                string money;
                IList<InventoryItem> items;
                lock (Bot)
                {
                    money = Bot.Game.Money.ToString("#,##0");
                    items = Bot.Game.Items.ToArray();
                }
                MoneyText.Text = money;
                toolTipMoney.Children.Clear();
                toolTipMoney2.Children.Clear();
                var bc = new BrushConverter();
                TextBlock MoneyAmount = new TextBlock();
                MoneyAmount.Foreground = (Brush)bc.ConvertFrom("#99aab5");
                MoneyAmount.Text = money;
                toolTipMoney.Children.Add(MoneyAmount);
                TextBlock moneyAmount = new TextBlock();
                moneyAmount.Foreground = (Brush)bc.ConvertFrom("#99aab5");
                moneyAmount.Text = money;
                toolTipMoney2.Children.Add(moneyAmount);
                Inventory.ItemsListView.ItemsSource = items;
                Inventory.ItemsListView.Items.Refresh();
            });
        }

        private void Client_BattleStarted()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "In battle";
                StatusText.Foreground = Brushes.Blue;
            });
        }

        private void Client_BattleMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                message = Regex.Replace(message, @"\[.+?\]", "");
                LogMessage(message, Brushes.Aqua);
            });
        }

        private void Client_BattleEnded()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "Online";
                StatusText.Foreground = Brushes.Green;
                Bot.Game.AskForPokedex();
            });
        }

        private void Client_DialogOpened(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message, Brushes.DarkOrange);
            });
        }

        private void Client_SystemMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddSystemMessage(message);
            });
        }

        private void Client_InvalidPacket(string packet, string error)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("Received Invalid Packet: " + error + ": " + packet, Brushes.OrangeRed);
            });
        }

        private void Client_PokeTimeUpdated(string pokeTime, string weather)
        {
            lock (Bot)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    if (Bot.Game != null)
                    {
                        PokeTimeText.Text = pokeTime;
                        DateTime dt = Convert.ToDateTime(Bot.Game.PokemonTime);
                        DockPanel d = new DockPanel();
                        Time.Children.Clear();
                        var bc = new BrushConverter();
                        Time.Background = (Brush)bc.ConvertFrom("#2c2f33");
                        d.Background = (Brush)bc.ConvertFrom("#2c2f33");

                        if (dt.Hour >= 20 || dt.Hour < 4)
                        {
                            TextBlock Name = new TextBlock();
                            Name.Foreground = (Brush)bc.ConvertFrom("#99aab5");
                            Name.Background = (Brush)bc.ConvertFrom("#2c2f33");
                            Name.Text = "Night.";
                            d.Children.Add(Name);
                            PokeTimeIconName.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.WeatherNight;
                            Time.Children.Add(d);
                        }
                        if (dt.Hour >= 10 && dt.Hour < 20)
                        {
                            TextBlock Name = new TextBlock();
                            Name.Foreground = (Brush)bc.ConvertFrom("#99aab5");
                            Name.Background = (Brush)bc.ConvertFrom("#2c2f33");
                            PokeTimeIconName.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.WeatherSunny;
                            Name.Text = "Day.";
                            d.Children.Add(Name);
                            Time.Children.Add(d);
                        }
                        if (dt.Hour >= 4 && dt.Hour < 10)
                        {
                            TextBlock Name = new TextBlock();
                            Name.Text = "Morning.";
                            Name.Foreground = (Brush)bc.ConvertFrom("#99aab5");
                            Name.Background = (Brush)bc.ConvertFrom("#2c2f33");
                            PokeTimeIconName.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.WeatherSunsetUp;
                            d.Children.Add(Name);
                            Time.Children.Add(d);
                        }
                    }
                });
            }
        }
        
        private void Client_ShopOpened(Shop shop)
        {
            Dispatcher.InvokeAsync(delegate
            {
                StringBuilder content = new StringBuilder();
                content.Append("Shop opened:");
                foreach (ShopItem item in shop.Items)
                {
                    content.AppendLine();
                    content.Append(item.Name);
                    content.Append(" ($" + item.Price + ")");
                }
                LogMessage(content.ToString());
            });
        }

        private void UpdateBotMenu()
        {
            lock (Bot)
            {
                BotStartMenuItem.IsEnabled = Bot.Game != null && Bot.Game.IsConnected && Bot.Script != null && Bot.Running == BotClient.State.Stopped;
                BotStopMenuItem.IsEnabled = Bot.Game != null && Bot.Game.IsConnected && Bot.Running != BotClient.State.Stopped;
            }
        }

        private void LogMessage(string message, Brush color)
        {
            TextRange test = new TextRange(MessageTextBox.Document.ContentEnd, MessageTextBox.Document.ContentEnd);
            test.Text = "[" + DateTime.Now.ToLongTimeString() + "] " + message + '\r';

            // Coloring there.
            test.ApplyPropertyValue(TextElement.ForegroundProperty, color);
            FileLog.Append(test.Text);
            MessageTextBox.ScrollToEnd();
        }
        private void LogMessage(string message)
        {
            var bc = new BrushConverter();
            LogMessage(message, (Brush)bc.ConvertFrom("#FF99AAB5"));
        }
        private void LogMessage(string format, params object[] args)
        {
            LogMessage(string.Format(format, args));
        }

        private void AddSystemMessage(string message)
        {
            LogMessage("System: " + message, Brushes.SeaGreen);
        }

        public static void AppendLineToTextBox(RichTextBox richTextBox, string message)
        {
            Paragraph para;
            TextRange r = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            string t = Regex.Replace(r.Text, @"\s+", "");
#if DEBUG
                Console.WriteLine(t.Length.ToString());
#endif
            if (t.Length > 2)
            {
               
                para = new Paragraph();
                para.Margin = new Thickness(0);
            }
            else
            {
                para = richTextBox.Document.Blocks.FirstBlock as Paragraph;
                para.LineHeight = 10;
            }

            para.Inlines.Add(new Run(message));

            richTextBox.Document.Blocks.Add(para);
            //string test = new TextRange(textBox.Document.ContentEnd, textBox.Document.ContentEnd).Text;
            //if (test.Length > 12000)
            //{
            //    string text = new TextRange(textBox.Document.ContentEnd, textBox.Document.ContentEnd).Text;
            //    text = text.Substring(text.Length - 10000, 10000);
            //    int index = text.IndexOf(Environment.NewLine);
            //    if (index != -1)
            //    {
            //        text = text.Substring(index + Environment.NewLine.Length);
            //    }
            //    textBox.Document.Blocks.Clear();
            //    Paragraph newPara = new Paragraph();
            //    newPara = textBox.Document.Blocks.FirstBlock as Paragraph;
            //    newPara.LineHeight = 10;
            //    newPara.Inlines.Add(new Run(text + Environment.NewLine));             
            //    textBox.Document.Blocks.Add(newPara);
            //}
            if (richTextBox.Selection.IsEmpty)
            {
                richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
                richTextBox.ScrollToEnd();
            }
        }
        public static void AppendLineToRichTextBox(RichTextBox richTextBox, string message)
        {
            string richText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
            richTextBox.AppendText(message);
            richTextBox.AppendText("\n");
            if (richText.Length > 12000)
            {
                string text = richText;
                text = text.Substring(text.Length - 10000, 10000);
                int index = text.IndexOf(Environment.NewLine);
                if (index != -1)
                {
                    text = text.Substring(index + Environment.NewLine.Length);
                }
                richTextBox.Document.Blocks.Clear();
                richTextBox.Document.Blocks.Add(new Paragraph(new Run(text + "\n")));
            }
            if (richTextBox.Selection.IsEmpty)
            {
                richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
                richTextBox.ScrollToEnd();
            }
        }
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(App.Name + " version " + App.Version + ", by " + App.Author + "." + Environment.NewLine + App.Description, App.Name + " - About");
        }

        private void MenuForum_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://proshine-bot.com/");
        }

        private void MenuGitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Silv3rPRO/proshine");
        }

        private void MenuDonate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.patreon.com/proshine");
        }

        private void StartScriptButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                if (Bot.Running == BotClient.State.Stopped)
                {
                    Bot.Start();
                }
                else if (Bot.Running == BotClient.State.Started || Bot.Running == BotClient.State.Paused)
                {
                    Bot.Pause();
                }
            }
        }

        private void StopScriptButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Stop();
                Bot.CancelInvokes();
            }
        }

        private void LoadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            LoadScript();
        }

        private void AutoEvolveSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = true;
            }
        }

        private void AutoEvolveSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = false;
            }
        }

        private void AvoidStaffSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.StaffAvoider.IsEnabled = true;
            }
        }

        private void AvoidStaffSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.StaffAvoider.IsEnabled = false;
            }
        }

        private void AutoReconnectSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = true;
            }
        }

        private void AutoReconnectSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = false;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            bool shouldLogin = false;
            lock (Bot)
            {
                if (Bot.Game == null || !Bot.Game.IsConnected)
                {
                    shouldLogin = true;
                }
                else
                {
                    Logout();
                }
            }
            if (shouldLogin)
            {
                OpenLoginWindow();
            }
        }

        private void IsTrainerBattlesActiveSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.IsTrainerBattlesActive.IsEnabled = true;
            }
        }

        private void IsTrainerBattlesActiveSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.IsTrainerBattlesActive.IsEnabled = false;
            }
        }
        //Adding Spawn list of the map.
        private void Client_RefreshSpawnList(List<PokemonSpawn> pkmns)
        {
            lock (pkmns)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    SpawnList.Children.Clear();  // Clearing the spawn list before adding new one.
                    pkmns.ForEach(delegate (PokemonSpawn pkmn)
                    {
                    /* Captured : http://fontawesome.io/icon/check/ | Not : http://fontawesome.io/icon/times/ truth is it wasn't used xD
                    * MSOnly : http://fontawesome.io/icon/certificate/
                    * SURF : http://fontawesome.io/icon/ship/ | GROUND : http://fontawesome.io/icon/globe/
                    * MAY HOLD AN ITEM : http://fontawesome.io/icon/wrench/
                    */

                        DockPanel d = new DockPanel();
                        TextBlock Name = new TextBlock();
                        FontAwesome.WPF.FontAwesome c = new FontAwesome.WPF.FontAwesome();
                        FontAwesome.WPF.FontAwesome m = new FontAwesome.WPF.FontAwesome();
                        FontAwesome.WPF.FontAwesome s = new FontAwesome.WPF.FontAwesome();
                        FontAwesome.WPF.FontAwesome i = new FontAwesome.WPF.FontAwesome();

                        if (pkmn.captured)
                        {
                            c.Icon = FontAwesomeIcon.Check;
                            d.Children.Add(c);
                            c.Foreground = Brushes.SeaGreen;
                        }
                        if (pkmn.msonly)
                        {
                            m.Icon = FontAwesomeIcon.Certificate;
                            d.Children.Add(m);
                            Name.Foreground = Brushes.DeepPink;
                            m.Foreground = Brushes.DeepPink;
                        }
                        if (pkmn.surf)
                        {
                            s.Icon = FontAwesomeIcon.Ship;
                            d.Children.Add(s);
                            Name.Foreground = Brushes.LightSkyBlue;
                            s.Foreground = Brushes.LightSkyBlue;
                        }
                        else
                        {
                            s.Icon = FontAwesomeIcon.Globe;
                            d.Children.Add(s);
                            Name.Foreground = Brushes.ForestGreen;
                            s.Foreground = Brushes.ForestGreen;
                        }
                        if (pkmn.msonly)
                        {
                            Name.Foreground = Brushes.DeepPink;
                            s.Foreground = Brushes.DeepPink;
                        }
                        if (pkmn.hitem)
                        {
                            i.Icon = FontAwesomeIcon.Wrench;
                            d.Children.Add(i);
                            i.Foreground = Brushes.Goldenrod;
                        }
                        Name.Text = pkmn.name;
                        d.Children.Add(Name);
                        SpawnList.Children.Add(d);
                    });
                    if (pkmns.Count <= 0)
                    {
                        TextBlock nospawn = new TextBlock();
                        nospawn.Foreground = Brushes.OrangeRed;
                        nospawn.Text = "No Pokemon Spawn.";
                        SpawnList.Children.Add(nospawn);
                    }
                });
            }
        }
        //Adding Pokemon dex informations to the list.
        private void Client_RefreshPokedexList()
        {
            lock (Bot)
            {
                Dispatcher.Invoke(() =>
                {
                    if (Bot.Game.getAreaName.Count > 0 && Bot.Game.isMS.Count > 0 && Bot.Game.timeZone.Count > 0)
                    {
                        for (int i = 0; i < Bot.Game.getAreaName.Count; i++)
                        {
                            items.Add(new Pokedex() { Area_Name = Bot.Game.getAreaName[i], Time_Zone = Bot.Game.timeZone[i], Is_MS = Bot.Game.isMS[i] });
                        }
                    }
                    PokedexList.ItemsSource = items;
                    PokedexList.Items.Refresh();
                });
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CollapseIcon.Icon.Equals(FontAwesomeIcon.AngleDoubleUp))
            {
                MaxHeight = 100;
                MaxWidth = 700;
                CollapseIcon.Icon = FontAwesomeIcon.AngleDoubleDown;
            }
            else
            {
                MaxHeight = 1000;
                Height = 600;
                MaxWidth = 1000;
                Width = 800;
                CollapseIcon.Icon = FontAwesomeIcon.AngleDoubleUp;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
        //To hide log messages or to show.
        private void HideLOGVIEW_Click(object sender, RoutedEventArgs e)
        {
            if (HideLOGVIEW.Header.Equals("Show Log View"))
            {
                row1.Height = new GridLength(1, GridUnitType.Star);
                Height = 600;
                HideLOGVIEW.Header = "Hide Log View";
            }
            else
            {
                row1.Height = new GridLength(0);
                HideLOGVIEW.Header = "Show Log View";
                Height = 600;
            }
        }
        //Asking for pokemon dex informations
        private void PokedexData_Button_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    if (Bot.Game.IsAlreadyCaught(pokedexData.Text) || Bot.Game.HasSeen(pokedexData.Text))
                    {
                        Bot.Game.PokedexList.ForEach(delegate (PokedexPokemon pkmn)
                        {
                            if (pkmn.ToString() == pokedexData.Text)
                            {
                                Bot.Game.SendPacket("p|.|a|" + pkmn.pokeid2);
                            }
                        });
                    }
                    else
                    {
                        Bot.Game.AskForPokedex();
                        var bc = new BrushConverter();
                        LogMessage("Data didn't receive, may be you haven't seen the pokemon. Or Wait for a second and try again", (Brush)bc.ConvertFrom("#FF99AAB5"));
                    }
                }
                FlayoutDex.IsOpen = true;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            FlayoutDex.IsOpen = false;
            if (Bot.Game != null)
            {
                Bot.Game.getAreaName.Clear();
                Bot.Game.timeZone.Clear();
                Bot.Game.isMS.Clear();
                items.Clear();
                PokedexList.ItemsSource = null;
                PokedexList.Items.Refresh();
                foreach(var pk in Bot.Game.PokedexList)
                {
                    pk.Area.Clear();
                }
            }
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) != null)
            {
                string[] file = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (file != null)
                {
                    LoadScript(file[0]);
                }
            }
        }
    }
}
