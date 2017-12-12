using PROBot;
using PROProtocol;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace PROShine
{
    public partial class InventoryView : UserControl
    {
        private GridViewColumnHeader _lastColumn;
        private ListSortDirection _lastDirection;
        private BotClient bot;

        public InventoryView(BotClient _bot)
        {                  
            InitializeComponent();
            bot = _bot;
        }

        private void ItemsListViewHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);

            ListSortDirection direction = ListSortDirection.Ascending;
            if (column == _lastColumn && direction == _lastDirection)
            {
                direction = ListSortDirection.Descending;
            }

            ItemsListView.Items.SortDescriptions.Clear();
            ItemsListView.Items.SortDescriptions.Add(new SortDescription((string)column.Content, direction));

            _lastColumn = column;
            _lastDirection = direction;
        }

        private void UseItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsListView.SelectedItems.Count <= 0)
                return;
            else
            {
                InventoryItem item = (InventoryItem)ItemsListView.SelectedItems[0];
                lock(bot)
                {
                    if (item.Id == 0)
                        return;
                    else if (item.Id == 82)
                        bot.Game.AskForPokedex();
                    else if (item.Name.Contains("TM"))
                        bot.LogMessage("Cant's use TM from Inventory view. Try to use it from Teamview.", System.Windows.Media.Brushes.OrangeRed);
                    else
                        bot.Game.UseItem(item.Id);
                }
            }
        }

        private void ItemsListView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ItemsListView.SelectedItems.Count <= 0)
                return;
            else
            {
                lock (bot)
                {
                    if (bot.Game != null)
                    {
                        if (bot.Game.IsConnected)
                        {
                            MenuItem useItem = new MenuItem();
                            useItem.Header = "Use Item";
                            ContextMenu contextMenu = new ContextMenu();

                            useItem.Click += UseItem_Click;
                            contextMenu.Items.Add(useItem);
                            ItemsListView.ContextMenu = contextMenu;
                        }
                    }
                }
            }
        }
    }
}
