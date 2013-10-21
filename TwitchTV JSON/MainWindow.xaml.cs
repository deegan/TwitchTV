using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace TwitchTV_JSON
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<TwitchStream> streamlist = new List<TwitchStream>();
        public MainWindow()
        {
            InitializeComponent();
            GamesList();
            Refresh(null);
        }

        public void Refresh(string game)
        {
            if (game == null)
            {
                game = "Dota+2"; // this is the default, for now.
            }
            else
            {
                game = game.Replace(" ", "+"); //replace spaces with +.
            }
            streamlist.Clear();
            streamview.Items.Clear();
            string uri = "http://api.justin.tv/api/stream/list.xml?meta_game="+game;
            var xmlDocument = XDocument.Load(uri);
            foreach (XElement stream in xmlDocument.Descendants("stream"))
            {
                try
                {
                    TwitchStream temp = new TwitchStream();
                    if (stream.Element("title").Value != null)
                        temp.Title = stream.Element("title").Value;
                    if (stream.Element("channel").Element("login").Value != null)
                        temp.ChannelOwner = stream.Element("channel").Element("login").Value;
                    if (stream.Element("channel_count").Value != null)
                        temp.ViewerCount = Convert.ToInt32(stream.Element("channel_count").Value);
                    if (stream.Element("channel").Element("screen_cap_url_large").Value != null)
                        temp.image = stream.Element("channel").Element("screen_cap_url_large").Value;
                    streamlist.Add(temp);
                }
                catch
                {

                }
            }
            List<TwitchStream> SortedStreams = streamlist.OrderByDescending(o => o.ViewerCount).ToList<TwitchStream>();
            foreach (TwitchStream stream in SortedStreams)
            {
                Button temp = new Button();
                temp.Content = stream.ChannelOwner;
                temp.ToolTip = stream.Title + Environment.NewLine + stream.ViewerCount.ToString();
                temp.Height = 40;
                temp.Width = 150;
                temp.Click += temp_Click;
                streamview.Items.Add(temp);
            }
        }

        void temp_Click(object sender, RoutedEventArgs e)
        {
            string name = ((Button)sender).Content.ToString();
            TwitchStream stream = streamlist[0];
            foreach (TwitchStream get in streamlist)
                if (get.ChannelOwner == name)
                    stream = get;
            text_owner.Text = "Owner: " + stream.ChannelOwner;
            text_title.Text = "Title: " + stream.Title;
            text_title.ToolTip = stream.Title;
            text_viewer.Text = "Viewers: " + stream.ViewerCount.ToString();
            this.DataContext = stream.image;
        }

        private void CreateGameButtons(string name)
        {
            Button game = new Button();
            game.Content = name;
            game.Height = 40;
            game.Width = 172;
            game.Click += game_select_Change;
            games.Items.Add(game);            
        }

        private void GamesList()
        {
            // this can probably be fetched from the api. 
            CreateGameButtons("World of Warcraft: Mists of Pandaria");
            CreateGameButtons("Dota 2");
            CreateGameButtons("Hearthstone: Heroes of Warcraft");
            CreateGameButtons("StarCraft II: Heart of the Swarm");
            CreateGameButtons("Heroes of Newerth");
            CreateGameButtons("League of Legends");
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh(null);
        }

        private void game_select_Change(object sender, RoutedEventArgs e)
        {
            // Get's the name of the game and calls Refresh();
            string game = Convert.ToString(e.Source.GetType().GetProperty("Content").GetValue(e.Source, null));
            Refresh(game);
        }
        public class TwitchStream
        {
            public string Title { get; set; }
            public string ChannelOwner { get; set; }
            public int ViewerCount { get; set; }
            public string image { get; set; }
        }

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            string quality = "mobile_high";
            foreach (RadioButton button in Quality.Children)
            {
                if ((bool)button.IsChecked)
                    quality = button.Content.ToString();
            }
            Process.Start("CMD", "/C livestreamer.exe twitch.tv/" + text_owner.Text.Remove(0, 7) + " " + quality);
        }
    }
}
