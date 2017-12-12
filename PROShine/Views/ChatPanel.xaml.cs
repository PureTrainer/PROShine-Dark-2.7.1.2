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
            //Removing some messages to prevent lag.
            TextRange range = new TextRange((sender as RichTextBox).Document.ContentStart, (sender as RichTextBox).Document.ContentEnd); //To get the richtextbox texts.
            if (range.Text.Length > 12000)//Richtextbox text length
            {
                if((sender as RichTextBox).Document.Blocks.Count >= 207)//if richtextbox got over or 207 blocks.
                {
                    //Detecting how many blocks we need to remove
                    string text = range.Text;
                    text = text.Substring(text.Length - 10000, 10000);
                    int index = text.IndexOf(Environment.NewLine);
                    if (index != -1)
                    {
                        text = text.Substring(index + Environment.NewLine.Length);
                    }
                    long lines = Extentions.Lines(text);
                    int ln = Convert.ToInt32(lines);
                    ln = (sender as RichTextBox).Document.Blocks.Count - ln;
                    //End of detection
                    for (int i = 0; i <= (sender as RichTextBox).Document.Blocks.Count - 1; i++)
                    {
                        if (i <= ln && ln > 0) //Value to remove blocks
                        {
                            //Removing blocks
                            (sender as RichTextBox).Document.Blocks.Remove((sender as RichTextBox).Document.Blocks.ToList()[i]);
                        }
                        else 
                        {
                            if (i <= 48) //Default value
                            {
                                //Removing blocks
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
    //Special Class to find out text lines
    public static class Extentions
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
