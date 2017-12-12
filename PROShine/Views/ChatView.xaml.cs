using PROBot;
using PROProtocol;
using PROShine.Controls;
using PROShine.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;

namespace PROShine
{
    public partial class ChatView : UserControl
    {
        private Dictionary<string, ButtonTab> _channelTabs;
        private Dictionary<string, ButtonTab> _pmTabs;
        private Dictionary<string, ButtonTab> _channelPmTabs; // fuck that
        private TabItem _localChatTab;
        private BotClient _bot;
        public List<string> PokeLinks;
        public List<string> pokeName;
        public List<string> PokeLinksLocal;
        public List<string> pokeNameLocal;
        public List<string> messagesAfterLink;
        public List<string> messagesAfterLinkTest = new List<string>();

        public ChatView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
            _localChatTab = new TabItem();
            _localChatTab.Header = "Local";
            _localChatTab.Content = new ChatPanel();
            TabControl.Items.Add(_localChatTab);
            var bc = new BrushConverter();
            _localChatTab.Background = (Brush)bc.ConvertFrom("#FF2C2F33");
            _localChatTab.Foreground = (Brush)bc.ConvertFrom("#FF99AAB5");
            _channelTabs = new Dictionary<string, ButtonTab>();
            AddChannelTab("All");
            AddChannelTab("Trade");
            AddChannelTab("Battle");
            AddChannelTab("Other");
            AddChannelTab("Help");
            _pmTabs = new Dictionary<string, ButtonTab>();
            _channelPmTabs = new Dictionary<string, ButtonTab>();
            PokeLinks = new List<string>();
            PokeLinksLocal = new List<string>();
            pokeName = new List<string>();
            pokeNameLocal = new List<string>();
            messagesAfterLink = new List<string>();
        }

        public void Client_RefreshChannelList()
        {
            Dispatcher.InvokeAsync(delegate
            {
                IList<ChatChannel> channelList;
                lock (_bot)
                {
                    channelList = _bot.Game.Channels.ToArray();
                }
                foreach (ChatChannel channel in channelList)
                {
                    if (!_channelTabs.ContainsKey(channel.Name))
                    {
                        AddChannelTab(channel.Name);
                    }
                }
                foreach (string key in _channelTabs.Keys.ToArray())
                {
                    if (!(channelList.Any(e => e.Name == key)))
                    {
                        RemoveChannelTab(key);
                    }
                }
            });
        }

        public void Client_LeavePrivateMessage(string conversation, string mode, string leaver)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (leaver == _bot.Game.PlayerName)
                {
                    return;
                }
                AddPrivateSystemMessage(conversation, mode, leaver, "has closed the PM window");
            });
        }

        public void Client_ChatMessage(string mode, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChatMessage(mode, author, message);
            });
        }

        public void Client_ChannelMessage(string channelName, string mod, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChannelMessage(channelName, mod, author, message);
            });
        }

        public void Client_ChannelSystemMessage(string channelName, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChannelSystemMessage(channelName, message);
            });
        }

        public void Client_ChannelPrivateMessage(string conversation, string mode, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChannelPrivateMessage(conversation, mode, author, message);
            });
        }

        public void Client_PrivateMessage(string conversation, string mode, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                PlayNotification();
                AddPrivateMessage(conversation, mode, author, message);
            });
        }

        public void Client_EmoteMessage(string mode, string author, int emoteId)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddEmoteMessage(mode, author, emoteId);
            });
        }

        private void AddChannelTab(string tabName)
        {
            ButtonTab tab = new ButtonTab();
            var bc = new BrushConverter();
            (tab.Header as ButtonTabHeader).TabName.Content = '#' + tabName;
            (tab.Header as ButtonTabHeader).TabName.Foreground = (Brush)bc.ConvertFrom("#FF99AAB5");
            (tab.Header as ButtonTabHeader).CloseButton += () => CloseChannelTab(tabName);
            tab.Background = (Brush)bc.ConvertFrom("#FF2C2F33");
            tab.Tag = tabName;
            tab.Content = new ChatPanel();
            _channelTabs[tabName] = tab;
            TabControl.Items.Add(tab);
        }

        private void CloseChannelTab(string channelName)
        {
            if (!_channelTabs.ContainsKey(channelName))
            {
                return;
            }
            if (_bot.Game != null && _bot.Game != null && _bot.Game.IsMapLoaded && _bot.Game.Channels.Any(e => e.Name == channelName))
            {
                _bot.Game.CloseChannel(channelName);
            }
            else
            {
                RemoveChannelTab(channelName);
            }
        }

        private void RemoveChannelTab(string tabName)
        {
            TabControl.Items.Remove(_channelTabs[tabName]);
            _channelTabs.Remove(tabName);
        }

        private void AddChannelPmTab(string tabName)
        {
            ButtonTab tab = new ButtonTab();
            var bc = new BrushConverter();
            (tab.Header as ButtonTabHeader).TabName.Content = "*" + tabName;
            (tab.Header as ButtonTabHeader).TabName.Foreground = (Brush)bc.ConvertFrom("#FF99AAB5");
            (tab.Header as ButtonTabHeader).CloseButton += () => CloseChannelPmTab(tabName);
            tab.Background = (Brush)bc.ConvertFrom("#FF2C2F33");
            tab.Tag = tabName;
            tab.Content = new ChatPanel();
            _channelPmTabs[tabName] = tab;
            TabControl.Items.Add(tab);
        }

        private void CloseChannelPmTab(string channelName)
        {
            if (!_channelPmTabs.ContainsKey(channelName))
            {
                return;
            }
            RemoveChannelPmTab(channelName);
        }

        private void RemoveChannelPmTab(string tabName)
        {
            TabControl.Items.Remove(_channelPmTabs[tabName]);
            _channelPmTabs.Remove(tabName);
        }
        private void AddPmTab(string tabName)
        {
            ButtonTab tab = new ButtonTab();
            var bc = new BrushConverter();
            (tab.Header as ButtonTabHeader).TabName.Content = tabName;
            (tab.Header as ButtonTabHeader).TabName.Foreground = (Brush)bc.ConvertFrom("#FF99AAB5");
            (tab.Header as ButtonTabHeader).CloseButton += () => ClosePmTab(tabName);
            tab.Background = (Brush)bc.ConvertFrom("#FF2C2F33");
            tab.Tag = tabName;
            tab.Content = new ChatPanel();
            _pmTabs[tabName] = tab;
            TabControl.Items.Add(tab);
        }

        private void ClosePmTab(string pmName)
        {
            if (!_pmTabs.ContainsKey(pmName))
            {
                return;
            }
            if (_bot.Game != null && _bot.Game != null && _bot.Game.IsMapLoaded && _bot.Game.Conversations.Contains(pmName))
            {
                _bot.Game.CloseConversation(pmName);
            }
            RemovePmTab(pmName);
        }

        private void RemovePmTab(string tabName)
        {
            TabControl.Items.Remove(_pmTabs[tabName]);
            _pmTabs.Remove(tabName);
        }

        private void AddChannelMessage(string channelName, string mode, string author, string message)
        {
            bool containsChatPoke = false;
            Regex linkRg = new Regex(@"\[([\dMF?,]+)\]<([\w]+)>\[-\]");
            MatchCollection linkMatch = linkRg.Matches(message);
            if (message.Contains("[-]") && linkMatch.Count >= 0)
            {
                containsChatPoke = true;
                //int position = message.LastIndexOf(']');
                //string updateUser1 = "";
                //string updateUser2 = "";
                //string link = "";
                //string Poke = "";
                //Regex namePoke = new Regex(@"<([\w\s'-]+)>");
                //MatchCollection namePokeMatch = namePoke.Matches(message);
                try
                {
                    //                    updateUser1 = message.Substring(0, message.IndexOf("["));
                    //                    if (position > -1)
                    //                        updateUser2 = message.Substring(position + 1);
                    //                    string test = "";
                    //                    test = Regex.Replace(message, @"\[.+?\]", "");

                    //                    foreach (Match match in linkMatch)
                    //                    {
                    //                        link = match.Value;
                    //                        PokeLinks.Add(link);
                    //                        Poke = "<" + match.Groups[2].Value + ">";
                    //                        pokeName.Add(Poke);
                    //                        test = test.Replace("<" + match.Groups[2].Value + ">", "`");
                    //                        //link = link.Replace("[", "");
                    //                        //link = link.Replace("]", "");
                    //                        //link = link.Replace("-", "");

                    //#if DEBUG
                    //                        Console.WriteLine(link);
                    //#endif                        
                    //                    }
                    //                    string test2 = Regex.Replace(updateUser2, @"\s+", "");
                    //                    string test1 = Regex.Replace(updateUser1, @"\s+", "");
                    //                    string tester = "";
                    //                    string[] sts = test.Split('`');
                    //                    if (sts.Length > 0)
                    //                    {
                    //                        for (var s = 0; s <= sts.Length - 1; s++)
                    //                        {
                    //                            tester = Regex.Replace(sts[s], @"\s+", "");
                    //                            if (sts[s] != " " && sts[s] != "" && tester != test1 && tester != test2)
                    //                            {
                    //                                messagesAfterLink.Add(sts[s]);
                    //                            }
                    //                        }
                    //                    }
                    //                    foreach (var m in messagesAfterLink)
                    //                    {
                    //                        if (m == " ")
                    //                        {
                    //                            messagesAfterLink.Remove(m);
                    //                        }
                    //                    }
                    //                    //                    foreach (Match NameMatch in namePokeMatch)
                    //                    //                    {
                    //                    //                        link = link.Replace(NameMatch.Value, "");
                    //                    //                        Poke = NameMatch.Value;
                    //                    //#if DEBUG
                    //                    //                        Console.WriteLine(Poke);
                    //                    //#endif
                    //                    //                        pokeName.Add(Poke);
                    //                    //                    }
                    //                    string st = linkMatch[0].Value;
                    //                    //st = link.Replace("[", "");
                    //                    //st = link.Replace("]", "");
                    //                    //st = link.Replace("-", "");
                    //#if DEBUG
                    //                    Console.WriteLine(st);
                    //#endif
                    //                    st = st.Replace(namePokeMatch[0].Value, "");
                    /*AddHyperlinkText((_channelTabs[channelName].Content as ChatPanel).ChatBox, "[" + mode + "]" + author, st, "<" + linkMatch[0].Groups[2].Value + ">", updateUser1, updateUser2)*/
                    AppendTextToRichTextBox((_channelTabs[channelName].Content as ChatPanel).ChatBox,
                            "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": ", message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#if DEBUG
                Console.WriteLine(updateUser1);
                Console.WriteLine(updateUser2);
                Console.WriteLine(position);
                Console.WriteLine(link);
#endif
            }
            message = Regex.Replace(message, @"\[.+?\]", "");

            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            if (!_channelTabs.ContainsKey(channelName))
            {
                AddChannelTab(channelName);
            }

            if (!containsChatPoke)
            {
                MainWindow.AppendLineToTextBox((_channelTabs[channelName].Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message);
                pokeName.Clear();
                PokeLinks.Clear();
            }
            // (_channelTabs[channelName].Content as ChatPanel).ChatBox.AppendText(
            //"[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message + '\r');
        }

        private void AddChannelSystemMessage(string channelName, string message)
        {

            message = Regex.Replace(message, @"\[.+?\]", "");

            if (!_channelTabs.ContainsKey(channelName))
            {
                AddChannelTab(channelName);
            }

            MainWindow.AppendLineToTextBox((_channelTabs[channelName].Content as ChatPanel).ChatBox,
                "[" + DateTime.Now.ToLongTimeString() + "] SYSTEM: " + message);

            //(_channelTabs[channelName].Content as ChatPanel).ChatBox.AppendText(
            //    "[" + DateTime.Now.ToLongTimeString() + "] SYSTEM: " + message + '\r');
        }

        private void AddChannelPrivateMessage(string conversation, string mode, string author, string message)
        {
            bool containsChatPoke = false;
            Regex linkRg = new Regex(@"\[([\dMF?,]+)\]<([\w]+)>\[-\]");
            MatchCollection linkMatch = linkRg.Matches(message);
            if (message.Contains("[-]") && linkMatch.Count >= 0)
            {
                containsChatPoke = true;
                //int position = message.LastIndexOf(']');
                //string updateUser1 = "";
                //string updateUser2 = "";
                //string link = "";
                //string Poke = "";
                //Regex namePoke = new Regex(@"<([\w\s'-]+)>");
                //MatchCollection namePokeMatch = namePoke.Matches(message);
                try
                {
                    //                    updateUser1 = message.Substring(0, message.IndexOf("["));
                    //                    if (position > -1)
                    //                        updateUser2 = message.Substring(position + 1);
                    //                    string test = "";
                    //                    test = Regex.Replace(message, @"\[.+?\]", "");

                    //                    foreach (Match match in linkMatch)
                    //                    {
                    //                        link = match.Value;
                    //                        PokeLinks.Add(link);
                    //                        Poke = "<" + match.Groups[2].Value + ">";
                    //                        pokeName.Add(Poke);
                    //                        test = test.Replace("<" + match.Groups[2].Value + ">", "`");
                    //                        //link = link.Replace("[", "");
                    //                        //link = link.Replace("]", "");
                    //                        //link = link.Replace("-", "");

                    //#if DEBUG
                    //                        Console.WriteLine(link);
                    //#endif                        
                    //                    }
                    //                    string test2 = Regex.Replace(updateUser2, @"\s+", "");
                    //                    string test1 = Regex.Replace(updateUser1, @"\s+", "");
                    //                    string tester = "";
                    //                    string[] sts = test.Split('`');
                    //                    if (sts.Length > 0)
                    //                    {
                    //                        for (var s = 0; s <= sts.Length - 1; s++)
                    //                        {
                    //                            tester = Regex.Replace(sts[s], @"\s+", "");
                    //                            if (sts[s] != " " && sts[s] != "" && tester != test1 && tester != test2)
                    //                            {
                    //                                messagesAfterLink.Add(sts[s]);
                    //                            }
                    //                        }
                    //                    }
                    //                    foreach (var m in messagesAfterLink)
                    //                    {
                    //                        if (m == " ")
                    //                        {
                    //                            messagesAfterLink.Remove(m);
                    //                        }
                    //                    }
                    //                    //                    foreach (Match NameMatch in namePokeMatch)
                    //                    //                    {
                    //                    //                        link = link.Replace(NameMatch.Value, "");
                    //                    //                        Poke = NameMatch.Value;
                    //                    //#if DEBUG
                    //                    //                        Console.WriteLine(Poke);
                    //                    //#endif
                    //                    //                        pokeName.Add(Poke);
                    //                    //                    }
                    //                    string st = linkMatch[0].Value;
                    //                    //st = link.Replace("[", "");
                    //                    //st = link.Replace("]", "");
                    //                    //st = link.Replace("-", "");
                    //#if DEBUG
                    //                    Console.WriteLine(st);
                    //#endif
                    //                    st = st.Replace(namePokeMatch[0].Value, "");
                    //AddHyperlinkText((_channelPmTabs[conversation].Content as ChatPanel).ChatBox, "[" + mode + "]" + author, st, "<" + linkMatch[0].Groups[2].Value + ">", updateUser1, updateUser2);
                    AppendTextToRichTextBox((_channelPmTabs[conversation].Content as ChatPanel).ChatBox,
                            "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": ", message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#if DEBUG
                Console.WriteLine(updateUser1);
                Console.WriteLine(updateUser2);
                Console.WriteLine(position);
                Console.WriteLine(link);
#endif
            }
            message = Regex.Replace(message, @"\[.+?\]", "");

            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            if (!_channelPmTabs.ContainsKey(conversation))
            {
                AddChannelPmTab(conversation);
            }

            if (!containsChatPoke)
            {
                MainWindow.AppendLineToTextBox((_channelPmTabs[conversation].Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message);
                pokeName.Clear();
                PokeLinks.Clear();
            }

            //(_channelPmTabs[conversation].Content as ChatPanel).ChatBox.AppendText(
            //    "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message + '\r');
        }

        private void AddChatMessage(string mode, string author, string message)
        {
            bool containsChatPoke = false;

            Regex linkRg = new Regex(@"\[([\dMF?,]+)\]<([\w]+)>\[-\]");
            MatchCollection linkMatch = linkRg.Matches(message);

            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }

            if (message.Contains("[-]") && linkMatch.Count >= 0)
            {
                containsChatPoke = true;
                //int position = message.LastIndexOf(']');
                //int count = message.Count(f => f == '[');
                //int count2 = message.Count(f => f == ']');
                //int index = message.IndexOf(']', message.IndexOf(']') + 1);
                //int index2 = message.IndexOf('[', message.IndexOf('[') + 1);
                //string updateUser1 = "";
                //string updateUser2 = "";
                //string link = "";
                //string Poke = "";
                Regex namePoke = new Regex(@"<([\w\s'-]+)>");
                MatchCollection namePokeMatch = namePoke.Matches(message);            
                try
                {
                    //                    updateUser1 = message.Substring(0, message.IndexOf("["));
                    //                    if (position > -1)
                    //                        updateUser2 = message.Substring(position + 1);
                    //                    string test = "";
                    //                    test = Regex.Replace(message, @"\[.+?\]", "");

                    //                    foreach (Match match in linkMatch)
                    //                    {
                    //                        link = match.Value;
                    //                        PokeLinksLocal.Add(link);
                    //                        Poke = "<" + match.Groups[2].Value + ">";
                    //                        pokeNameLocal.Add(Poke);
                    //                        test = test.Replace("<" + match.Groups[2].Value + ">", "`");
                    //                        //link = link.Replace("[", "");
                    //                        //link = link.Replace("]", "");
                    //                        //link = link.Replace("-", "");

                    //#if DEBUG
                    //                        Console.WriteLine(link);
                    //#endif                        
                    //                    }
                    //                    string test2 = Regex.Replace(updateUser2, @"\s+", "");
                    //                    string test1 = Regex.Replace(updateUser1, @"\s+", "");
                    //                    string tester = "";
                    //                    string[] sts = test.Split('`');
                    //                    if (sts.Length > 0)
                    //                    {
                    //                        for (var s = 0; s <= sts.Length - 1; s++)
                    //                        {
                    //                            tester = Regex.Replace(sts[s], @"\s+", "");
                    //                            if (sts[s] != " " && sts[s] != "" && tester != test1 && tester != test2)
                    //                            {
                    //                                messagesAfterLink.Add(sts[s]);
                    //                            }
                    //                        }
                    //                    }
                    //                    if (messagesAfterLink.Count > 0)
                    //                    {
                    //                        foreach (var m in messagesAfterLink)
                    //                        {
                    //                            //                        if(m.Contains(updateUser1) || m.Contains(updateUser2))
                    //                            //                        {
                    //                            //                            messagesAfterLink.Remove(m);
                    //                            //                        }
                    //                            if(m == " ")
                    //                            {
                    //                                messagesAfterLink.Remove(m);
                    //                            }
                    //                        }
                    //                    }
                    //                    foreach (Match NameMatch in namePokeMatch)
                    //                    {
                    //                        link = link.Replace(NameMatch.Value, "");
                    //                        //Poke = NameMatch.Value;
                    //                        //pokeNameLocal.Add(Poke);

                    //#if DEBUG
                    //                        Console.WriteLine(Poke);
                    //#endif
                    //                    }
                    //                    //for (int u = 1; u <= namePokeMatch.Count - 1; u++)
                    //                    //{
                    //                    //    pokeNameLocal.Add(namePokeMatch[u].Value);
                    //                    //}
                    //                    for (int u = 0; u <= linkMatch.Count - 1; u++)
                    //                    {

                    //                    }
                    //                    string st = linkMatch[0].Value;
                    //st = link.Replace("[", "");
                    //st = link.Replace("]", "");
                    //st = link.Replace("-", "");
                    //st = st.Replace(namePokeMatch[0].Value, "");
                    AppendTextToRichTextBox((_localChatTab.Content as ChatPanel).ChatBox,
                           "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": ", message);
                    //LocalAddHyperlinkText((_localChatTab.Content as ChatPanel).ChatBox, "[" + mode + "]" + author, st, "<" + linkMatch[0].Groups[2].Value + ">", updateUser1, updateUser2);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#if DEBUG
                Console.WriteLine(updateUser1);
                Console.WriteLine(updateUser2);
                Console.WriteLine(position);
                Console.WriteLine(link);
#endif
            }
            message = Regex.Replace(message, @"\[.+?\]", "");           

            if (!containsChatPoke)
            {
                MainWindow.AppendLineToTextBox((_localChatTab.Content as ChatPanel).ChatBox,
                       "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": "+ message);
                PokeLinksLocal.Clear();
                pokeNameLocal.Clear();
            }
            //(_localChatTab.Content as ChatPanel).ChatBox.AppendText("[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message + '\r');
        }
        private void AddEmoteMessage(string mode, string author, int emoteId)
        {
            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }

            MainWindow.AppendLineToTextBox((_localChatTab.Content as ChatPanel).ChatBox,
                "[" + DateTime.Now.ToLongTimeString() + "] " + author + " is " + ChatEmotes.GetDescription(emoteId));

            //(_localChatTab.Content as ChatPanel).ChatBox.AppendText("[" + DateTime.Now.ToLongTimeString() + "] " + author + " is " + ChatEmotes.GetDescription(emoteId) + '\r');
        }

        private void AddPrivateMessage(string conversation, string mode, string author, string message)
        {
            bool containsChatPoke = false;
            Regex linkRg = new Regex(@"\[([\dMF?,]+)\]<([\w]+)>\[-\]");
            MatchCollection linkMatch = linkRg.Matches(message);
            if (message.Contains("[-]")&& linkMatch.Count >= 0)
            {
                containsChatPoke = true;
                //int position = message.LastIndexOf(']');
                //string updateUser1 = "";
                //string updateUser2 = "";
                //string link = "";
                //string Poke = "";
                Regex namePoke = new Regex(@"<([\w\s'-]+)>");
                MatchCollection namePokeMatch = namePoke.Matches(message);
                try
                {
                    //                    updateUser1 = message.Substring(0, message.IndexOf("["));
                    //                    if (position > -1)
                    //                        updateUser2 = message.Substring(position + 1);
                    //                    string test = "";
                    //                    test = Regex.Replace(message, @"\[.+?\]", "");

                    //                    foreach (Match match in linkMatch)
                    //                    {
                    //                        link = match.Value;
                    //                        PokeLinks.Add(link);
                    //                        Poke = "<" + match.Groups[2].Value + ">";
                    //                        pokeName.Add(Poke);
                    //                        test = test.Replace("<" + match.Groups[2].Value + ">", "`");
                    //                        //link = link.Replace("[", "");
                    //                        //link = link.Replace("]", "");
                    //                        //link = link.Replace("-", "");

                    //#if DEBUG
                    //                        Console.WriteLine(link);
                    //#endif                        
                    //                    }
                    //                    string test2 = Regex.Replace(updateUser2, @"\s+", "");
                    //                    string test1 = Regex.Replace(updateUser1, @"\s+", "");
                    //                    string tester = "";
                    //                    string[] sts = test.Split('`');
                    //                    if (sts.Length > 0)
                    //                    {
                    //                        for (var s = 0; s <= sts.Length - 1; s++)
                    //                        {
                    //                            tester = Regex.Replace(sts[s], @"\s+", "");
                    //                            if (sts[s] != " " && sts[s] != "" && tester != test1 && tester != test2)
                    //                            {
                    //                                messagesAfterLink.Add(sts[s]);
                    //                            }
                    //                        }
                    //                    }
                    //                    foreach (var m in messagesAfterLink)
                    //                    {
                    //                        if (m == " ")
                    //                        {
                    //                            messagesAfterLink.Remove(m);
                    //                        }
                    //                    }
                    //                    //                    foreach (Match NameMatch in namePokeMatch)
                    //                    //                    {
                    //                    //                        link = link.Replace(NameMatch.Value, "");
                    //                    //                        Poke = NameMatch.Value;
                    //                    //#if DEBUG
                    //                    //                        Console.WriteLine(Poke);
                    //                    //#endif
                    //                    //                        pokeName.Add(Poke);
                    //                    //                    }
                    //                    string st = linkMatch[0].Value;
                    //                    //st = link.Replace("[", "");
                    //                    //st = link.Replace("]", "");
                    //                    //st = link.Replace("-", "");
                    //#if DEBUG
                    //                    Console.WriteLine(st);
                    //#endif
                    //                    st = st.Replace(namePokeMatch[0].Value, "");
                    //AddHyperlinkText((_pmTabs[conversation].Content as ChatPanel).ChatBox, "[" + mode + "]" + author, st, "<" + linkMatch[0].Groups[2].Value + ">", updateUser1, updateUser2);
                    AppendTextToRichTextBox((_pmTabs[conversation].Content as ChatPanel).ChatBox,
                            "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": ", message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#if DEBUG
                Console.WriteLine(updateUser1);
                Console.WriteLine(updateUser2);
                Console.WriteLine(position);
                Console.WriteLine(link);
#endif
            }
            message = Regex.Replace(message, @"\[.+?\]", "");

            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            if (!_pmTabs.ContainsKey(conversation))
            {
                AddPmTab(conversation);
            }

            if (!containsChatPoke)
            {
                MainWindow.AppendLineToTextBox((_pmTabs[conversation].Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message);
                pokeName.Clear();
                PokeLinks.Clear();
            }

            //(_pmTabs[conversation].Content as ChatPanel).ChatBox.AppendText("[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message + '\r');
        }

        private void AddPrivateSystemMessage(string conversation, string mode, string author, string message)
        {
            bool containsChatPoke = false;
            Regex linkRg = new Regex(@"\[([\dMF?,]+)\]<([\w]+)>\[-\]");
            MatchCollection linkMatch = linkRg.Matches(message);
            if (message.Contains("[-]") && linkMatch.Count >= 0)
            {
                containsChatPoke = true;
                //int position = message.LastIndexOf(']');
                //string updateUser1 = "";
                //string updateUser2 = "";
                //string link = "";
                //string Poke = "";
                //Regex namePoke = new Regex(@"<([\w\s'-]+)>");
                //MatchCollection namePokeMatch = namePoke.Matches(message);
                try
                {
                    //                    updateUser1 = message.Substring(0, message.IndexOf("["));
                    //                    if (position > -1)
                    //                        updateUser2 = message.Substring(position + 1);
                    //                    string test = "";
                    //                    test = Regex.Replace(message, @"\[.+?\]", "");

                    //                    foreach (Match match in linkMatch)
                    //                    {
                    //                        link = match.Value;
                    //                        PokeLinks.Add(link);
                    //                        Poke = "<" + match.Groups[2].Value + ">";
                    //                        pokeName.Add(Poke);
                    //                        test = test.Replace("<" + match.Groups[2].Value + ">", "`");
                    //                        //link = link.Replace("[", "");
                    //                        //link = link.Replace("]", "");
                    //                        //link = link.Replace("-", "");

                    //#if DEBUG
                    //                        Console.WriteLine(link);
                    //#endif                        
                    //                    }
                    //                    string test2 = Regex.Replace(updateUser2, @"\s+", "");
                    //                    string test1 = Regex.Replace(updateUser1, @"\s+", "");
                    //                    string tester = "";
                    //                    string[] sts = test.Split('`');
                    //                    if (sts.Length > 0)
                    //                    {
                    //                        for (var s = 0; s <= sts.Length - 1; s++)
                    //                        {
                    //                            tester = Regex.Replace(sts[s], @"\s+", "");
                    //                            if (sts[s] != " " && sts[s] != "" && tester != test1 && tester != test2)
                    //                            {
                    //                                messagesAfterLink.Add(sts[s]);
                    //                            }
                    //                        }
                    //                    }
                    //                    foreach (var m in messagesAfterLink)
                    //                    {
                    //                        if (m == " ")
                    //                        {
                    //                            messagesAfterLink.Remove(m);
                    //                        }
                    //                    }
                    //                    //                    foreach (Match NameMatch in namePokeMatch)
                    //                    //                    {
                    //                    //                        link = link.Replace(NameMatch.Value, "");
                    //                    //                        Poke = NameMatch.Value;
                    //                    //#if DEBUG
                    //                    //                        Console.WriteLine(Poke);
                    //                    //#endif
                    //                    //                        pokeName.Add(Poke);
                    //                    //                    }
                    //                    string st = linkMatch[0].Value;
                    //                    //st = link.Replace("[", "");
                    //                    //st = link.Replace("]", "");
                    //                    //st = link.Replace("-", "");
                    //#if DEBUG
                    //                    Console.WriteLine(st);
                    //#endif
                    //                    st = st.Replace(namePokeMatch[0].Value, "");
                    //AddHyperlinkText((_pmTabs[conversation].Content as ChatPanel).ChatBox, "[" + mode + "]" + author, st, "<" + linkMatch[0].Groups[2].Value + ">", updateUser1, updateUser2);
                    AppendTextToRichTextBox((_pmTabs[conversation].Content as ChatPanel).ChatBox,
                            "[" + DateTime.Now.ToLongTimeString() + "] " + author + " ", message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
#if DEBUG
                Console.WriteLine(updateUser1);
                Console.WriteLine(updateUser2);
                Console.WriteLine(position);
                Console.WriteLine(link);
#endif
            }
            message = Regex.Replace(message, @"\[.+?\]", "");

            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            if (!_pmTabs.ContainsKey(conversation))
            {
                AddPmTab(conversation);
            }
            if (!containsChatPoke)
            {
                MainWindow.AppendLineToTextBox((_pmTabs[conversation].Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + author + " " + message);
                pokeName.Clear();
                PokeLinks.Clear();
            }
            //(_pmTabs[conversation].Content as ChatPanel).ChatBox.AppendText("[" + DateTime.Now.ToLongTimeString() + "] " + author + " " + message + '\r');
        }

        private void InputChatBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && _bot.Game != null && _bot.Game.IsMapLoaded)
            {
                SendChatInput(InputChatBox.Text);
                InputChatBox.Clear();
            }
        }

        private void SendChatInput(string text)
        {
            if (text == "" || text.Replace(" ", "") == "")
            {
                return;
            }
            lock (_bot)
            {
                if (_bot.Game == null)
                {
                    return;
                }
                TabItem tab = TabControl.SelectedItem as TabItem;
                text = Regex.Replace(text, @"\[(-|.{6})\]", "");
                if (text.Length == 0) return;
                if (_localChatTab == tab)
                {
                    text = text.Replace('|', '#');
                    _bot.Game.SendMessage(text);
                }
                else if (_channelTabs.ContainsValue(tab as ButtonTab))
                {
                    text = text.Replace('|', '#');
                    if (text[0] == '/')
                    {
                        _bot.Game.SendMessage(text);
                        return;
                    }
                    string channelName = (string)tab.Tag;
                    ChatChannel channel = _bot.Game.Channels.FirstOrDefault(e => e.Name == channelName);
                    if (channel == null)
                    {
                        return;
                    }
                    _bot.Game.SendMessage("/" + channel.Id + " " + text);
                }
                else if (_pmTabs.ContainsValue(tab as ButtonTab))
                {
                    text = text.Replace("|.|", "");
                    _bot.Game.SendPrivateMessage((string)tab.Tag, text);
                }
                else if (_channelPmTabs.ContainsValue(tab as ButtonTab))
                {
                    text = text.Replace('|', '#');
                    if (text[0] == '/')
                    {
                        _bot.Game.SendMessage(text);
                        return;
                    }
                    string conversation = (string)tab.Tag;
                    _bot.Game.SendMessage("/send " + conversation + ", " + text);
                }
            }
        }

        private void PlayNotification()
        {
            Window window = Window.GetWindow(this);
            if (!window.IsActive || !IsVisible)
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                FlashWindowHelper.Flash(handle);

                if (File.Exists("Assets/message.wav"))
                {
                    using (SoundPlayer player = new SoundPlayer("Assets/message.wav"))
                    {
                        player.Play();
                    }
                }
            }
        }

        private void AddHyperlinkText(RichTextBox richTextBox, string author, string linkURL, string linkName,
              string TextBeforeLink, string TextAfterLink)
        {
            string[] pokeData;

            Paragraph para = new Paragraph();
            para = richTextBox.Document.Blocks.FirstBlock as Paragraph;
            para.LineHeight = 10;

            Hyperlink link = new Hyperlink();
            link.IsEnabled = true;
            link.Inlines.Add(linkName);
            if (!linkURL.StartsWith("http:"))
                linkURL = "http://" + linkURL.Replace(",", "-").Substring(1, 75);
            string[] pokeData2 = linkURL.Substring(7, 75).Split('-');
            if (pokeData2[2] == "1")
            {
                link.Foreground = Brushes.DeepPink;
            }
            else if (pokeData2[19] != "000")
            {
                link.Foreground = Brushes.OrangeRed;
            }
            else
            {
                link.Foreground = Brushes.Aquamarine;
            }
            link.NavigateUri = new Uri(linkURL);
            author = author.Replace("[", "");
            author = author.Replace("]", "");
            author += ":";
            para.Inlines.Add(new Run("[" + DateTime.Now.ToLongTimeString() + "] " + author));
            para.Inlines.Add(" " + TextBeforeLink);
            para.Inlines.Add(link);
            if (messagesAfterLink.Count > 0)
            {
                para.Inlines.Add(messagesAfterLink[0]);
            }       
            if (pokeName.Count > 0 && PokeLinks.Count > 0)
            {
                for (var i = 1; i <= pokeName.Count - 1; i++)
                {
                    string url2 = "";
                    Hyperlink link2 = new Hyperlink();
                    link2.IsEnabled = true;
                    link2.Inlines.Add(pokeName[i]);
                    if (PokeLinks[i].Contains(pokeName[i]))
                    {
                        url2 = PokeLinks[i].Replace(pokeName[i], "");
                        if (!url2.StartsWith("http:"))
                            url2 = "http://" + url2.Replace(",", "-").Substring(1, 75);
                        pokeData = url2.Substring(7, 75).Split('-');
                        if (pokeData[2] == "1")
                        {
                            link2.Foreground = Brushes.DeepPink;
                        }
                        else if(pokeData[19] != "000")
                        {
                            link2.Foreground = Brushes.OrangeRed;
                        }
                        else
                        {
                            link2.Foreground = Brushes.Aquamarine;
                        }
                        link2.NavigateUri = new Uri(url2);
                        para.Inlines.Add(link2);
                        if (messagesAfterLink.Count > 0)
                        {
                            for (int s = 1; s <= messagesAfterLink.Count - 1; s++)
                            {
                                if (i <= messagesAfterLink.Count - 1)
                                {
                                    para.Inlines.Add(new Run(messagesAfterLink[s]));
                                }
                            }
                        }
                    }
                }
            }
            PokeLinks.Clear();
            pokeName.Clear();
            messagesAfterLink.Clear();
            para.Inlines.Add(new Run(TextAfterLink));
            para.Inlines.Add(Environment.NewLine);

            richTextBox.Document.Blocks.Clear();
            richTextBox.Document.Blocks.Add(para);

            //string myText = new TextRange((Tab[conversation].Content as ChatPanel).ChatBox.Document.ContentStart, (Tab[conversation].Content as ChatPanel).ChatBox.Document.ContentEnd).Text;
            //var resultString = Regex.Replace(myText, @"( |\r?\n)\1+", "$1");
            richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
            richTextBox.ScrollToEnd();
        }
        private void LocalAddHyperlinkText(RichTextBox richTextBox, string author, string linkURL, string linkName,
              string TextBeforeLink, string TextAfterLink)
        {
            string[] pokeData;

            Paragraph para = new Paragraph();
            para = richTextBox.Document.Blocks.FirstBlock as Paragraph;
            para.LineHeight = 10;
            List<string> notBeforeOrAfter = new List<string>();

            Hyperlink link = new Hyperlink();
            link.IsEnabled = true;
            link.Inlines.Add(linkName);
            if (!linkURL.StartsWith("http:"))
                linkURL = "http://" + linkURL.Replace(",", "-").Substring(1, 75);
            string[] pokeData2 = linkURL.Substring(7, 75).Split('-');
            if (pokeData2[2] == "1")
            {
                link.Foreground = Brushes.DeepPink;
            }
            else if (pokeData2[19] != "000")
            {
                link.Foreground = Brushes.OrangeRed;
            }
            else
            {
                link.Foreground = Brushes.Aquamarine;
            }           
            link.NavigateUri = new Uri(linkURL);
            author = author.Replace("[", "");
            author = author.Replace("]", "");
            author += ":";
            para.Inlines.Add(new Run("[" + DateTime.Now.ToLongTimeString() + "] " + author));
            para.Inlines.Add(" " + TextBeforeLink);
            para.Inlines.Add(link);
            if (messagesAfterLink.Count > 0)
            {
                para.Inlines.Add(messagesAfterLink[0]);
            }
            
            if (pokeNameLocal.Count > 0 && PokeLinksLocal.Count > 0)
            {
                for (var i = 1; i <= pokeNameLocal.Count - 1; i++)
                {
                    string url2 = "";
                    Hyperlink link2 = new Hyperlink();
                    link2.IsEnabled = true;
                    if (PokeLinksLocal[i].Contains(pokeNameLocal[i]))
                    {
                        link2.Inlines.Add(pokeNameLocal[i]);
                        url2 = PokeLinksLocal[i].Replace(pokeNameLocal[i], "");
                        if (!url2.StartsWith("http:"))
                            url2 = "http://" + url2.Replace(",", "-").Substring(1, 75);
                        pokeData = url2.Substring(7, 75).Split('-');
                        if (pokeData[2] == "1")
                        {
                            link2.Foreground = Brushes.DeepPink;
                        }
                        else if (pokeData[19] != "000")
                        {
                            link2.Foreground = Brushes.OrangeRed;
                        }
                        else
                        {
                            link2.Foreground = Brushes.Aquamarine;
                        }
                        link2.NavigateUri = new Uri(url2);
                        para.Inlines.Add(link2);
                        if (messagesAfterLink.Count > 0)
                        {
                            for (int s = 1; s <= messagesAfterLink.Count - 1; s++)
                            {
                                if (i <= messagesAfterLink.Count - 1)
                                {
                                    para.Inlines.Add(new Run(messagesAfterLink[s]));
                                }
                            }
                        }
                    }
                }
            }
            PokeLinks.Clear();
            pokeName.Clear();
            messagesAfterLink.Clear();
            para.Inlines.Add(new Run(TextAfterLink));
            para.Inlines.Add(Environment.NewLine);

            richTextBox.Document.Blocks.Clear();
            richTextBox.Document.Blocks.Add(para);
            //string myText = new TextRange((Tab[conversation].Content as ChatPanel).ChatBox.Document.ContentStart, (Tab[conversation].Content as ChatPanel).ChatBox.Document.ContentEnd).Text;
            //var resultString = Regex.Replace(myText, @"( |\r?\n)\1+", "$1");
            richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
            richTextBox.ScrollToEnd();
        }
        private void AppendTextToRichTextBox(RichTextBox richTextBox,string author, string message)
        {
            //I have got a lot of trash codes here
            bool gonZ = false;
            if (message.Contains("Porygon-Z")) //Porygon-Z -> This Pokemon has one problem so checking does the chat contains this pokemon name
                gonZ = true;
            int count = 0;
            Regex linkRg = new Regex(@"\[([\dMF?,]+)\]<([\w]+)>\[-\]");//Finding out is there any Pokemon link or not in Chat.
            MatchCollection linkMatch = linkRg.Matches(message);
            if (linkMatch.Count >= 0) //If we find any pokemon link in Chat.
            {
                //Important things for checking Paragraphs can't explain.
                Paragraph para;
                TextRange r = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                //Console.WriteLine(r.Text.Length.ToString());
                string t = Regex.Replace(r.Text, @"\s+", "");
#if DEBUG
                Console.WriteLine(t.Length.ToString());
#endif
                //Important
                if (t.Length > 0)
                {
                    para = new Paragraph();
                    para.Margin = new Thickness(0);
                }
                else
                {
                    para = richTextBox.Document.Blocks.FirstBlock as Paragraph;
                    para.LineHeight = 10;
                }
                count++;
                para.Inlines.Add(new Run(author));
                int position = message.LastIndexOf(']');

                string[] help = Regex.Replace(message, @"\[([\dMF?,]+)\]<([\w]+)>\[-\]", "~").Split('~'); //Trying to get messages before and after of pokemon links.

                string textBeforeAlllink = Regex.Replace(message, @"\[([\dMF?,]+)\]<([\w]+)>\[-\]", "~").Split('~')[0]; //Message of before all pokemon links.
                para.Inlines.Add(textBeforeAlllink);

                string textAfterAllLink = "";
                if (position > -1)
                    textAfterAllLink = message.Substring(position + 1);//Message of after all pokemon links.

                string textaftertcurrentlink = message; //A helping string to find out messages after each pokemon links
                if (textAfterAllLink != string.Empty)
                {
                    //Removing message after all links coz we don't need it in this string.
                    textaftertcurrentlink = textaftertcurrentlink.Replace(textAfterAllLink, "");
                }
                if (textBeforeAlllink != string.Empty)
                {
                    //Removing message before all links coz we don't need it in this string.
                    if (help.Length >= 0)
                    {                       
                        textaftertcurrentlink = textaftertcurrentlink.Remove(textaftertcurrentlink.IndexOf(help[0]));
                    }
                    else
                    {
                        textaftertcurrentlink = textaftertcurrentlink.Remove(textaftertcurrentlink.IndexOf(textBeforeAlllink));
                    }
                }
                //Links process
                for (var i = 0; i <= linkMatch.Count - 1; i++)
                {
                    string url = "http://" + linkMatch[i].Value.Replace(",", "-").Substring(1, 75); //Preparing links for uri else it will give an error.

                    Hyperlink link = new Hyperlink();
                    link.IsEnabled = true;
                    var pokeData = url.Substring(7, 75).Split('-');

                    if (gonZ && linkMatch[i].Value.ToLower().Contains("porygon-z"))
                    {
                        link.Inlines.Add("<Porygon-Z>");//For Proygon-Z problem
                    }
                    else
                    {
                        //Else
                        link.Inlines.Add("<" + linkMatch[i].Groups[2].Value + ">");
                    }

                    link.NavigateUri = new Uri(url);//Setting url for the link.

                    if (pokeData[2] == "1")//If the link poke is Shiny we going to change the HyperLink Color
                    {
                        link.Foreground = Brushes.DeepPink;
                    }
                    else if (pokeData[19] != "000")//If event poke changing HyperLink Color
                    {
                        link.Foreground = Brushes.DarkRed;
                    }
                    else 
                    {
                        //Non event or shiny poke changing HyperLink Color
                        link.Foreground = Brushes.Aquamarine;
                    }
                    //Setting fonts for the HyperLink
                    link.FontStyle = FontStyles.Italic;
                    //Adding link to our paragraph
                    para.Inlines.Add(link);
                    //Strat of Message after the pokemon link process.
                    textaftertcurrentlink = textaftertcurrentlink.Replace(linkMatch[i].Value, "");
                    if (textaftertcurrentlink.Length - 1 >= 0)
                    {
                        para.Inlines.Add(new Run(textaftertcurrentlink.Substring(0, textaftertcurrentlink.IndexOf("["))));
                        textaftertcurrentlink = textaftertcurrentlink.Substring(textaftertcurrentlink.IndexOf("["));
                    }
                    //End of Message after the pokemon link process.
                }
                //Adding Message of after all pokemon links to paragraph.
                para.Inlines.Add(new Run(textAfterAllLink));
                //Adding Paragraph to our richtextbox
                richTextBox.Document.Blocks.Add(para);

                if (richTextBox.Selection.IsEmpty)
                {
                    //Important Settings
                    richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
                    richTextBox.ScrollToEnd();
                }
            }
        }
    }
}
