using PROProtocol;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace PROShine
{
    public partial class ChatPanel : UserControl
    {
        public ChatPanel()
        {
            InitializeComponent();
        }
        string[] data;
        private void Hyperlink_MouseEnter(object sender, MouseEventArgs e)
        {
            var pokeData = ((Hyperlink)sender).NavigateUri.ToString().Substring(7, 75).Split('-');
            data = pokeData;
            DataContext = new ChatPokemon(pokeData);
            ChatPokemon cPk = new ChatPokemon(pokeData);
            if (cPk.IsShiny)
            {
                ((Hyperlink)sender).Foreground = Brushes.Aqua;
            }
            else if (cPk.Form != 0)
            {
                ((Hyperlink)sender).Foreground = Brushes.Aqua;
            }
            else
            {
                ((Hyperlink)sender).Foreground = Brushes.OrangeRed;
            }
        }

        private void ChatBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if((sender as RichTextBox).Selection.IsEmpty)
            {
                (sender as RichTextBox).CaretPosition = (sender as RichTextBox).Document.ContentEnd;
                (sender as RichTextBox).ScrollToEnd();
            }
#if DEBUG
            Console.WriteLine((sender as RichTextBox).Document.Blocks.Count.ToString());
#endif
            TextRange range = new TextRange((sender as RichTextBox).Document.ContentStart, (sender as RichTextBox).Document.ContentEnd);
            if (range.Text.Length > 12000)
            {
                if((sender as RichTextBox).Document.Blocks.Count >= 207)
                {
                    string text = range.Text;
                    text = text.Substring(text.Length - 10000, 10000);
                    int index = text.IndexOf(Environment.NewLine);
                    if (index != -1)
                    {
                        text = text.Substring(index + Environment.NewLine.Length);
                    }
                    long lines = extentions.Lines(text);
                    int ln = Convert.ToInt32(lines);
                    ln = (sender as RichTextBox).Document.Blocks.Count - ln;
                    for (int i = 0; i <= (sender as RichTextBox).Document.Blocks.Count - 1; i++)
                    {
                        if (i <= ln && ln > 0)
                        {
                            (sender as RichTextBox).Document.Blocks.Remove((sender as RichTextBox).Document.Blocks.ToList()[i]);
                        }
                        else 
                        {
                            if (i <= 168)
                            {
                                (sender as RichTextBox).Document.Blocks.Remove((sender as RichTextBox).Document.Blocks.ToList()[i]);
                            }
                        }
                    }
                }
            }
        }

        private void Hyperlink_MouseLeave(object sender, MouseEventArgs e)
        {
            var pokeData = ((Hyperlink)sender).NavigateUri.ToString().Substring(7, 75).Split('-');
            ChatPokemon cPk = new ChatPokemon(pokeData);
            if (cPk.IsShiny)
            {
                ((Hyperlink)sender).Foreground = Brushes.DeepPink;
            }
            else if (cPk.Form != 0)
            {
                ((Hyperlink)sender).Foreground = Brushes.DarkRed;
            }
            else
            {
                ((Hyperlink)sender).Foreground = Brushes.Aquamarine;
            }
        }
    }

    //Helper Class
    public static class extentions
    {
        public static long Lines(this string s)
        {
            long count = 1;
            int position = 0;
            while ((position = s.IndexOf('\n', position)) != -1)
            {
                count++;
                position++;
            }
            return count;
        }
    }
}
