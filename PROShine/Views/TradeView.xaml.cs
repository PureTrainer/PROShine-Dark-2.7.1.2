using PROBot;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PROProtocol;
using System.Windows.Controls.Primitives;
using System;
using System.Windows.Media;

namespace PROShine.Views
{

    public partial class TradeView : UserControl
    {
        private BotClient _bot;

        public TradeView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
            OnRequest.Visibility = Visibility.Collapsed;
            Trade.Visibility = Visibility.Collapsed;
            FinalView.Visibility = Visibility.Collapsed;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            lock (_bot)
            {
                lock (this)
                {
                    Reset();
                }
            }
        }

        public void Reset()
        {
            lock (_bot)
            {
                    this.Dispatcher.Invoke(() =>
                    {
                        teamToTrade.Children.Clear();
                        WaitingTrade.Visibility = Visibility.Visible;
                        Trade.Visibility = Visibility.Collapsed;
                        OnRequest.Visibility = Visibility.Collapsed;

                        FinalView.Visibility = Visibility.Collapsed;
                        TradeControls.Visibility = Visibility.Visible;
                        teamToTrade.Visibility = Visibility.Visible;

                        First_list.SetValue(BorderBrushProperty, Brushes.Silver);
                        First_list.BorderThickness = new Thickness(1);
                        Second_list.SetValue(BorderBrushProperty, Brushes.Silver);
                        Second_list.BorderThickness = new Thickness(1);
                        First_list.ItemsSource = null;
                        Second_list.ItemsSource = null;
                    });
                
            }
        }

        public void TradeRequest(string applicant)
        {
            lock (_bot)
            {
                    this.Dispatcher.Invoke(() =>
                    {
                        tradeApplicant.Text = applicant;
                        OnRequest.Visibility = Visibility.Visible;
                        WaitingTrade.Visibility = Visibility.Collapsed;
                    });
                
            }
        }

        private void AcceptTrade_Click(object sender, RoutedEventArgs e)
        {
            lock (_bot)
            {
                    string applicant = tradeApplicant.Text;
                    _bot.Game.SendPacket("mb|.|/trade " + applicant);
                    this.Dispatcher.Invoke(() =>
                    {
                        if (teamToTrade.Children.Count < _bot.Game.Team.Count) { InitTeam(); }
                    });
                    Trade.Visibility = Visibility.Visible;
                    OnRequest.Visibility = Visibility.Collapsed;
                
            }
        }

        public void UpdateMoney(string[] data)
        {
            lock (_bot)
            {

                    this.Dispatcher.Invoke(() =>
                    {
                        if (teamToTrade.Children.Count < _bot.Game.Team.Count) { InitTeam(); }
                        Trade.Visibility = Visibility.Visible;
                        OnRequest.Visibility = Visibility.Collapsed;
                        WaitingTrade.Visibility = Visibility.Collapsed;
                        First_nickname.Text = data[1];        // First exchanger
                    Second_nickname.Text = data[2];       // Second
                    First_money.Text = '$' + data[3];     // First money on exchange
                    Second_money.Text = '$' + data[4];    // Second money on exchange
                });
                
            }
        }

        public void InitTeam()
        {
            lock (_bot)
            {

                    List<Pokemon> team = _bot.Game.Team;
                    team.ForEach(delegate (Pokemon pkmn)
                    {
                        ToggleButton b = new ToggleButton();
                        b.Margin = new Thickness(5, 0, 5, 0);
                        b.Content = pkmn.Name;
                        b.Click += setTradeInfos_Click;
                        teamToTrade.Children.Add(b);
                    });
                
            }
        }

        private void cancelOnTrade_Click(object sender, RoutedEventArgs e)
        {
            lock (_bot)
            {

                    _bot.Game.SendMessage("ftradeadd,1");
                    Reset();
                
            }
        }

        private void setTradeInfos_Click(object sender, RoutedEventArgs e)
        {
            lock (_bot)
            {

                    string pokemonsToTrade = "";
                    for (int i = 0; i < teamToTrade.Children.Count; i++)
                    {
                        pokemonsToTrade += ((((ToggleButton)teamToTrade.Children[i]).IsChecked) == true ? Convert.ToString(i + 1) : "0") + ",";
                    }
                    _bot.Game.SendMessage("ftradeadd,0," + Money.Text + "," + pokemonsToTrade);
                
            }
        }

        private void acceptOnTrade_Click(object sender, RoutedEventArgs e)
        {
            lock (_bot)
            {

                    _bot.Game.SendMessage("ftradeadd,2");
                    if (FinalView.Visibility == Visibility.Visible)
                    {
                        Reset();
                    }
                
            }
        }


        public void StatusChanged(string[] data)
        {
            lock (_bot)
            {
                    string[] sdata = data[1].Split('|');
                    if (sdata[0] == "1")
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            First_list.SetValue(BorderBrushProperty, Brushes.ForestGreen);
                            First_list.BorderThickness = new Thickness(2);
                        });
                    }
                    else if (sdata[0] == "0")
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            First_list.SetValue(BorderBrushProperty, Brushes.Silver);
                            First_list.BorderThickness = new Thickness(2);
                        });
                    }

                    if (sdata[1] == "1")
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Second_list.SetValue(BorderBrushProperty, Brushes.ForestGreen);
                            Second_list.BorderThickness = new Thickness(2);
                        });
                    }
                    else if (sdata[1] == "0")
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Second_list.SetValue(BorderBrushProperty, Brushes.Silver);
                            Second_list.BorderThickness = new Thickness(2);
                        });
                    }
                
            }
        }

        public void StatusReset()
        {
            lock (_bot)
            {

                    Dispatcher.Invoke(() =>
                    {
                        First_list.SetValue(BorderBrushProperty, Brushes.Silver);
                        First_list.BorderThickness = new Thickness(1);
                        Second_list.SetValue(BorderBrushProperty, Brushes.Silver);
                        Second_list.BorderThickness = new Thickness(1);
                    });
                
            }
        }

        public void ChangeToFinalView()
        {
            lock (_bot)
            {
                    Dispatcher.Invoke(() =>
                    {
                        TradeControls.Visibility = Visibility.Collapsed;
                        teamToTrade.Visibility = Visibility.Collapsed;
                        FinalView.Visibility = Visibility.Visible;
                    });
                
            }
        }
    }
}
