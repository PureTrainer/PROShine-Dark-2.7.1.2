﻿using PROBot.Modules;
using PROBot.Scripting;
using PROProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Media;

namespace PROBot
{
    public class BotClient
    {
        public enum State
        {
            Stopped,
            Started,
            Paused
        };

        public GameClient Game { get; private set; }
        public BattleAI AI { get; private set; }
        public BaseScript Script { get; private set; }
        public AccountManager AccountManager { get; private set; }
        public Random Rand { get; private set; }
        public Account Account { get; set; }

        public State Running { get; private set; }
        public bool IsPaused { get; private set; }

        

        public event Action<State> StateChanged;
        public event Action<string> MessageLogged;
        public event Action<string, Brush> CMessageLogged;
        public event Action ClientChanged;
        public event Action ConnectionOpened;
        public event Action ConnectionClosed;
        public event Action<OptionSlider> SliderCreated;
        public event Action<OptionSlider> SliderRemoved;
        public event Action<TextOption> TextboxCreated;
        public event Action<TextOption> TextboxRemoved;

        public PokemonEvolver PokemonEvolver { get; private set; }
        public MoveTeacher MoveTeacher { get; private set; }
        public StaffAvoider StaffAvoider { get; private set; }
        public AutoReconnector AutoReconnector { get; private set; }
        public IsTrainerBattlesActive IsTrainerBattlesActive { get; private set; }
        public MovementResynchronizer MovementResynchronizer { get; private set; }
        public Dictionary<int, OptionSlider> SliderOptions { get; set; }
        public Dictionary<int, TextOption> TextOptions { get; set; }

        public DateTime scriptPauserTime;

        private bool _loginRequested;

        private ProtocolTimeout _actionTimeout = new ProtocolTimeout();
        
        public bool isNpcBattleActive { get; set; } 
        public int countGMTele { get; set; }
        public bool CallingPaueScript { get; set; }
        public BotClient()
        {
            AccountManager = new AccountManager("Accounts");
            PokemonEvolver = new PokemonEvolver(this);
            MoveTeacher = new MoveTeacher(this);
            StaffAvoider = new StaffAvoider(this);
            AutoReconnector = new AutoReconnector(this);
            IsTrainerBattlesActive = new IsTrainerBattlesActive(this);
            MovementResynchronizer = new MovementResynchronizer(this);
            Rand = new Random();
            SliderOptions = new Dictionary<int, OptionSlider>();
            TextOptions = new Dictionary<int, TextOption>();
            countGMTele = 0;
            CallingPaueScript = false;
        }
        public void CancelInvokes()
        {
            if (Script != null)
                foreach (Invoker invoker in Script.Invokes)
                    invoker.Called = true;
            CallingPaueScript = false;
        }
        
        public void CallInvokes()
        {
            if (Script != null)
            {
                for (int i = Script.Invokes.Count - 1; i >= 0; i--)
                {
                    if (Script.Invokes[i].Time < DateTime.UtcNow)
                    {
                        if (Script.Invokes[i].Called)
                            Script.Invokes.RemoveAt(i);
                        else
                            Script.Invokes[i].Call();
                    }
                }
                if (CallingPaueScript)
                {
                    if (scriptPauserTime < DateTime.UtcNow)
                    {
                        if (Running == State.Paused)
                        {
                            Pause();
                        }
                    }
                }
            }
        }

        
        public void RemoveText(int index)
        {
            TextboxRemoved?.Invoke(TextOptions[index]);
            TextOptions.Remove(index);
        }

        public void RemoveSlider(int index)
        {
            SliderRemoved?.Invoke(SliderOptions[index]);
            SliderOptions.Remove(index);
        }

        public void CreateText(int index, string content)
        {
            TextOptions[index] = new TextOption("Text " + index + ": ", "Custom text option " + index + " for use in scripts.", content);
            TextboxCreated?.Invoke(TextOptions[index]);
        }

        public void CreateText(int index, string content, bool isName)
        {
            if (isName)
                TextOptions[index] = new TextOption(content, "Custom text option " + index + " for use in scripts.", "");
            else
                TextOptions[index] = new TextOption("Text " + index + ": ", content, "");

            TextboxCreated?.Invoke(TextOptions[index]);
        }

        public void CreateSlider(int index, bool enable)
        {
            SliderOptions[index] = new OptionSlider("Option " + index + ": ", "Custom option " + index + " for use in scripts.");
            SliderOptions[index].IsEnabled = enable;
            SliderCreated?.Invoke(SliderOptions[index]);
        }

        public void CreateSlider(int index, string content, bool isName)
        {
            if (isName)
                SliderOptions[index] = new OptionSlider(content, "Custom option " + index + " for use in scripts.");
            else
                SliderOptions[index] = new OptionSlider("Option " + index + ": ", content);

            SliderCreated?.Invoke(SliderOptions[index]);
        }

        public void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }
        public void LogMessage(string message, Brush color)
        {
            CMessageLogged?.Invoke(message, color);
        }

        public void SetClient(GameClient client)
        {
            Game = client;
            AI = null;
            Stop();
            
            if (client != null)
            {
                AI = new BattleAI(client);
                client.ConnectionOpened += Client_ConnectionOpened;
                client.ConnectionFailed += Client_ConnectionFailed;
                client.ConnectionClosed += Client_ConnectionClosed;
                client.BattleMessage += Client_BattleMessage;
                client.SystemMessage += Client_SystemMessage;
                client.DialogOpened += Client_DialogOpened;
                client.TeleportationOccuring += Client_TeleportationOccuring;
                client.LogMessage += LogMessage;
            }
            ClientChanged?.Invoke();
        }

        public void Login(Account account)
        {
            Account = account;
            _loginRequested = true;
        }

        private void LoginUpdate()
        {
            GameClient client;
            GameServer server = GameServerExtensions.FromName(Account.Server);
            if (Account.Socks.Version != SocksVersion.None)
            {
                // TODO: Clean this code.
                client = new GameClient(new GameConnection(server, (int)Account.Socks.Version, Account.Socks.Host, Account.Socks.Port, Account.Socks.Username, Account.Socks.Password),
                    new MapConnection((int)Account.Socks.Version, Account.Socks.Host, Account.Socks.Port, Account.Socks.Username, Account.Socks.Password));
            }
            else
            {
                client = new GameClient(new GameConnection(server), new MapConnection());
            }
            SetClient(client);
            client.Open();
        }

        public void Logout(bool allowAutoReconnect)
        {
            if (!allowAutoReconnect)
            {
                AutoReconnector.IsEnabled = false;
            }
            else
            {
                AutoReconnector.IsEnabled = true;
            }
            Game.Close();
        }

        public void Update()
        {
            CallInvokes();
            AutoReconnector.Update();
            if (_loginRequested)
            {
                LoginUpdate();
                _loginRequested = false;
                return;
            }

            if (Running != State.Started)
            {
                return;
            }
            if (PokemonEvolver.Update()) return;
            if (MoveTeacher.Update()) return;

            if (Game.IsMapLoaded && Game.AreNpcReceived && Game.IsInactive)
            {
                ExecuteNextAction();
            }
        }

        public void Start()
        {
            if (Game != null && Script != null && Running == State.Stopped)
            {
                _actionTimeout.Set();
                Running = State.Started;
                StateChanged?.Invoke(Running);
                Script.Start();
            }
        }

        public void Pause()
        {
            if (Game != null && Script != null && Running != State.Stopped)
            {
                if (Running == State.Started)
                {
                    Running = State.Paused;
                    StateChanged?.Invoke(Running);
                    Script.Pause();
                }
                else
                {
                    Running = State.Started;
                    StateChanged?.Invoke(Running);
                    Script.Resume();
                }
            }
        }

        public void Stop()
        {
            if (Game != null)
                Game.ClearPath();
            if (Game != null && Script != null && Game.IsConnected)
            {
                Game.scriptStarted = false ;
            }
            if (Running != State.Stopped)
            {
                Running = State.Stopped;
                StateChanged?.Invoke(Running);
                if (Script != null)
                {
                    Script.Stop();
                }
            }
        }

        public void LoadScript(string filename)
        {
            string input = File.ReadAllText(filename);

            List<string> libs = new List<string>();
            if (Directory.Exists("Libs"))
            {
                string[] files = Directory.GetFiles("Libs");
                foreach (string file in files)
                {
                    if (file.ToUpperInvariant().EndsWith(".LUA"))
                    {
                        libs.Add(File.ReadAllText(file));
                    }
                }
            }

            BaseScript script = new LuaScript(this, Path.GetFullPath(filename), input, libs);

            Stop();
            Script = script;
            try
            {
                Script.ScriptMessage += Script_ScriptMessage;
                Script.Initialize();
            }
            catch (Exception ex)
            {
                Script = null;
                throw ex;
            }
        }

        public bool MoveToLink(string destinationMap)
        {
            IEnumerable<Tuple<int, int>> nearest = Game.Map.GetNearestLinks(destinationMap, Game.PlayerX, Game.PlayerY);
            
            if (nearest != null)
            {
                foreach (Tuple<int, int> link in nearest)
                {
                    if (MoveToCell(link.Item1, link.Item2)) return true;
                }
            }
            return false;
        }

        public bool MoveToCell(int x, int y, int requiredDistance = 0)
        {
            MovementResynchronizer.CheckMovement(x, y);

            Pathfinding path = new Pathfinding(Game);
            bool result;

            if (Game.PlayerX == x && Game.PlayerY == y)
            {
                result = path.MoveToSameCell();
            }
            else
            {
                result = path.MoveTo(x, y, requiredDistance);
            }

            if (result)
            {
                MovementResynchronizer.ApplyMovement(x, y);
            }
            return result;
        }

        public bool TalkToNpc(Npc target)
        {
            bool canInteract = Game.Map.CanInteract(Game.PlayerX, Game.PlayerY, target.PositionX, target.PositionY);
            if (canInteract)
            {
                Game.TalkToNpc(target.Id);
                return true;
            }
            else
            {
                return MoveToCell(target.PositionX, target.PositionY, 1);
            }
        }

        public bool OpenPC()
        {
            Tuple<int, int> pcPosition = Game.Map.GetPC();
            if (pcPosition == null || Game.IsPCOpen)
            {
                return false;
            }
            int distance = Game.DistanceTo(pcPosition.Item1, pcPosition.Item2);
            if (distance == 1)
            {
                return Game.OpenPC();
            }
            else
            {
                return MoveToCell(pcPosition.Item1, pcPosition.Item2 + 1);
            }
        }

        public bool RefreshPCBox(int boxId)
        {
            if (!Game.IsPCOpen)
            {
                return false;
            }
            if (!Game.RefreshPCBox(boxId))
            {
                return false;
            }
            _actionTimeout.Set();
            return true;
        }

        private void ExecuteNextAction()
        {
            try
            {
                bool executed = Script.ExecuteNextAction();
                if (!executed && Running != State.Stopped && !_actionTimeout.Update())
                {
                    LogMessage("No action executed: stopping the bot.", Brushes.Firebrick);
                    Stop();
                }
                else if (executed)
                {
                    _actionTimeout.Set();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                LogMessage("Error during the script execution: " + ex);
#else
                LogMessage("Error during the script execution: " + ex.Message, Brushes.Firebrick);
#endif
                Stop();
            }
        }
        
        private void Client_ConnectionOpened()
        {
            ConnectionOpened?.Invoke();
            Game.SendAuthentication(Account.Name, Account.Password, Account.MacAddress ?? HardwareHash.GenerateRandom());
        }

        private void Client_ConnectionClosed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Disconnected from the server: " + ex);
#else
                LogMessage("Disconnected from the server: " + ex.Message, Brushes.Firebrick);
#endif
            }
            else
            {
                LogMessage("Disconnected from the server.", Brushes.Firebrick);
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }

        private void Client_ConnectionFailed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Could not connect to the server: " + ex);
#else
                LogMessage("Could not connect to the server: " + ex.Message, Brushes.Firebrick);
#endif
            }
            else
            {
                LogMessage("Could not connect to the server.", Brushes.Firebrick);
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }

        private void Client_DialogOpened(string message)
        {
            if (Running == State.Started)
            {
                Script.OnDialogMessage(message);
            }
        }

        private void Client_SystemMessage(string message)
        {
            if (Running == State.Started)
            {
                Script.OnSystemMessage(message);
            }
        }
        private void Client_BattleMessage(string message)
        {
            if (Running == State.Started)
            {
                Script.OnBattleMessage(message);
            }
            //if(Game.IsTrainerBattlesActive && message.Contains("The Trainer sends") && !Game.ActiveBattle.IsWild && Game != null)
            //{
            //    Game.IsInNpcBattle = true;                   
            //}
            //if(Game.IsTrainerBattlesActive && Game.IsInNpcBattle && Game != null)
            //{
            //    if (Running == State.Started)
            //    {
            //        Pause();
            //    }
            //    AI.Attack();
            //}
            //if (message.Contains("won") && Game != null)
            //{
            //    if (Running == State.Paused)
            //    {
            //        Pause();
            //    }
            //    Game.IsInNpcBattle = false;
            //}
        }

        private void Client_TeleportationOccuring(string map, int x, int y)
        {
            string message = "Position updated: " + map + " (" + x + ", " + y + ")";
            if (Game.Map == null || Game.IsTeleporting)
            {
                message += " [OK]";              
            }
            else if (Game.MapName != map)
            {
                message += " [WARNING, different map] /!\\";
                bool flag = map.Contains("Pokecenter") ? false : true;
                bool anotherFlag = map.Contains("Player") ? false : true;
                if (flag && anotherFlag && Game.PreviousMapBeforeTeleport != Game.MapName && countGMTele >= 2)
                {
                    if (File.Exists("Assets/Teleported.wav"))
                    {
                        using (SoundPlayer player = new SoundPlayer("Assets/Teleported.wav"))
                        {
                            player.Play();
                        }
                    }
                    countGMTele = 0;
                }
                //if ((!Game.MapName.Contains("Pokecenter") || !Game.MapName.Contains("Player")) && countGMTele >= 2)
                //{
                //    bool process = false;
                //    string[] messagesToSent = new string[10];
                //    messagesToSent[0] = "wtf how I get to here.";
                //    messagesToSent[1] = "what the hell i am doing here";
                //    messagesToSent[2] = "damn how i got to here";
                //    int randomTimes = Rand.Next(0, 2);
                //    switch (randomTimes)
                //    {
                //        case 0:
                //            Game.SendMessage(messagesToSent[0]);
                //            process = true;
                //            break;
                //        case 1:
                //            Game.SendMessage(messagesToSent[1]);
                //            process = true;
                //            break;
                //        case 2:
                //            Game.SendMessage(messagesToSent[2]);
                //            process = true;
                //            break;
                //        default:
                //            Game.SendMessage("what is going on, how i got to here!!???");
                //            process = true;
                //            break;
                //    }
                //    if(process)
                //    {
                //        Script.Pause();
                //        Thread.Sleep(2000);
                //        Script.Start();
                //        countGMTele = 0;
                //    }
                //}
                //else if(Game.MapName.Contains("Prof. Antibans Classroom"))
                //{
                //    bool process = false;
                //    string[] messagesToSent = new string[10];
                //    messagesToSent[0] = "wtf how I get to here.";
                //    messagesToSent[1] = "what the hell i am doing here";
                //    messagesToSent[2] = "damn how i got to here";
                //    int randomTimes = Rand.Next(0, 2);
                //    switch (randomTimes)
                //    {
                //        case 0:
                //            Game.SendMessage(messagesToSent[0]);
                //            process = true;
                //            break;
                //        case 1:
                //            Game.SendMessage(messagesToSent[1]);
                //            process = true;
                //            break;
                //        case 2:
                //            Game.SendMessage(messagesToSent[2]);
                //            process = true;
                //            break;
                //        default:
                //            Game.SendMessage("what is going on, how i got to here!!???");
                //            process = true;
                //            break;
                //    }
                //    if (process)
                //    {
                //        Thread.Sleep(2000);
                //        countGMTele = 0;
                //        Logout(false);                       
                //    }
                //}
            }
            else
            {
                int distance = GameClient.DistanceBetween(x, y, Game.PlayerX, Game.PlayerY);
                if (distance < 8)
                {
                    message += " [OK, lag, distance=" + distance + "]";
                }
                else
                {
                    message += " [WARNING, distance=" + distance + "] /!\\";
                    countGMTele++;
                    if(countGMTele > 2)
                    {
                        PauseScript(10);
                        countGMTele = 0;
                    }
                }
            }
            if(message.Contains("[OK]"))
            {
                LogMessage(message, Brushes.MediumSeaGreen);
            }
            if(message.Contains("WARNING"))
            {
                LogMessage(message, Brushes.OrangeRed);
            }
        }

        private void Script_ScriptMessage(string message)
        {
            LogMessage(message);
        }

        private void PauseScript(float seconds)
        {
            scriptPauserTime = DateTime.UtcNow.AddSeconds(seconds);
            Pause();
            CallingPaueScript = true;
        }
    }
}
