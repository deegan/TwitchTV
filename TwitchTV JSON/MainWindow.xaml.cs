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
using Newtonsoft.Json;

namespace TwitchTV_JSON
{
    /// <summary>
    /// MainWindow.xaml logic.
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<TwitchStream> streamlist = new List<TwitchStream>();
        public MainWindow()
        {
            InitializeComponent();
            GamesList();
            // Refresh(null);
        }

        public void Refresh(string game)
        {
            string uri;
            string res;
            streamlist.Clear();
            streamview.Items.Clear();

            // Get the username on twitch from this file.
            // used to get the channels from the follow.
            string currentDir = Environment.CurrentDirectory;
            string userFile = currentDir + "\\settings.txt";
            StreamReader streamReader = new StreamReader(userFile);
            string user = streamReader.ReadToEnd();
            streamReader.Close();


            if (game == user)
            {
                // Updating the list with the channels the user follows.
                uri = "https://api.twitch.tv/kraken/users/deegantv/follows/channels";
                res = MakeHttpRequest(uri);
                CreateChannelList(res);
            }
            else
            {
                // Updating the list with whatever game the users choose.
                game = game.Replace(" ", "+"); //replace spaces with +.
                uri = "https://api.twitch.tv/kraken/search/streams?q=" + game;
                res = MakeHttpRequest(uri);
                CreateChannelList(res);
            }
            
        }

        public bool IsChannelOnline(string channel)
        {
            // Check if a channel is online.
            string uri2;
            string res2;
            uri2 = "https://api.twitch.tv/kraken/streams/"+channel;
            res2 = MakeHttpRequest(uri2);
            
            var objects2 = JsonConvert.DeserializeObject<twitch_json.RootObject>(res2);

            if (objects2.stream == null)
                return false;
            else
                return true;
        }

        public void CreateChannelList(dynamic res)
        {
            var objects = JsonConvert.DeserializeObject<twitch_json.RootObject>(res);

            if (objects.streams != null)
            {
                // Just a game
                foreach (var item in objects.streams)
                {
                    try
                    {
                        if (IsChannelOnline(item.channel.name) != null)
                        {
                            string stream_id = item.channel._id.ToString();
                            var streams = JsonConvert.DeserializeObject<twitch_json.RootObject>(res);
                            TwitchStream temp = new TwitchStream();
                            foreach (var stream in streams.streams)
                            {
                                string tmp_id = stream.channel._id.ToString();
                                if (stream_id == tmp_id)
                                {
                                    if (stream.channel.status != null)
                                        temp.Title = stream.channel.status;
                                    if (stream.channel.name != null)
                                        temp.ChannelOwner = stream.channel.name;
                                    if (stream.viewers != 0)
                                        temp.ViewerCount = item.viewers;
                                    if (stream.preview.medium != null)
                                        temp.image = stream.preview.medium;
                                    streamlist.Add(temp);
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                // Following
                foreach (var item in objects.follows)
                {
                    try
                    {
                        if (IsChannelOnline(item.channel.name) != false)
                        {
                            string stream_id = item.channel._id.ToString();
                            string uri3 = "https://api.twitch.tv/kraken/search/streams?q=" + item.channel.name;
                            res = MakeHttpRequest(uri3);
                            var channel = JsonConvert.DeserializeObject<twitch_json.RootObject>(res);
                            TwitchStream temp = new TwitchStream();
                            foreach (var stream in channel.streams)
                            {
                                string tmp_id = stream.channel._id.ToString();
                                if (stream_id == tmp_id)
                                {
                                    if (stream.channel.status != null)
                                        temp.Title = stream.channel.status;
                                    if (stream.channel.name != null)
                                        temp.ChannelOwner = stream.channel.name;
                                    if (stream.viewers != 0)
                                        temp.ViewerCount = stream.viewers;
                                    if (stream.preview.medium != null)
                                        temp.image = stream.preview.medium;
                                    streamlist.Add(temp);
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
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

        public dynamic MakeHttpRequest(string uri)
        {
            HttpWebRequest wRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
            wRequest.ContentType = "application/json";
            wRequest.Accept = "application/vnd.twitchtv.v3+json";
            wRequest.Method = "GET";
            dynamic wResponse = wRequest.GetResponse().GetResponseStream();
            StreamReader reader = new StreamReader(wResponse);
            dynamic res = reader.ReadToEnd();
            reader.Close();
            wResponse.Close();

            return res;
        }

        private void CreateGameButtons(string name, string logo)
        {
            ImageBrush brush = new ImageBrush(new BitmapImage(new Uri(logo)));
            brush.Stretch = Stretch.Uniform;
            Button game = new Button();
            game.Content = name;
            game.Height = 40;
            game.Width = 172;
            //game.Background = brush;
            game.Click += game_select_Change;
            games.Items.Add(game);            
        }

        private void GamesList()
        {
            // The point of this is to get the top10 games right now and 
            // weed out stuff like "Game name" the amount of viewers, maybe
            // maybe the poster for each etc. 
            var uri = "https://api.twitch.tv/kraken/games/top?limit=10";
            string res = MakeHttpRequest(uri);
            var objects = JsonConvert.DeserializeObject<twitch_json.RootObject>(res);

            // Clear the list first.
            games.Items.Clear();
            // Create all the buttons.
            foreach (var item in objects.top)
            {
                CreateGameButtons(item.game.name, item.game.logo.small);
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

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btn_refresh2)
            {
                GamesList();
            }
            else
            {
                // streamlist.Clear();
                streamview.Items.Clear();
                string game = Convert.ToString(e.Source.GetType().GetProperty("Content").GetValue(e.Source, null));
                Refresh(game);
            }
        }

        private void btn_follwing_Click(object sender, RoutedEventArgs e)
        {
            // Get the username on twitch from this file.
            // used to get the channels from the follow.
            // StreamReader streamReader = new StreamReader("C:\\Projects\\TwitchTV-master\\TwitchTV\\TwitchTV JSON\\settings.txt");
            // string text = streamReader.ReadToEnd();
            // streamReader.Close();
            string currentDir = Environment.CurrentDirectory;
            string userFile = currentDir + "\\settings.txt";
            StreamReader streamReader = new StreamReader(userFile);
            string user = streamReader.ReadToEnd();
            streamReader.Close();

            if (user != null)
            {
                Refresh(user);
            }
            else
            {
                string game = Convert.ToString(e.Source.GetType().GetProperty("Content").GetValue(e.Source, null));
                Refresh(game);
            }
        }

        private void game_select_Change(object sender, RoutedEventArgs e)
        {
            // Get's the name of the game and calls Refresh();
            string game = Convert.ToString(e.Source.GetType().GetProperty("Content").GetValue(e.Source, null));
            Refresh(game);
        }

        public static string CreateRequest(string queryString)
        {
            string UrlRequest = "https://api.twitch.tv/kraken/games/top?limit=10";
            return (UrlRequest);
        }

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            string quality = "source";
            foreach (RadioButton button in Quality.Children)
            {
                if ((bool)button.IsChecked)
                    quality = button.Content.ToString();
            }
            string cmd = "twitch.tv/" + text_owner.Text.Remove(0, 7) + " " + quality;
            Process.Start("CMD", "/C livestreamer.exe twitch.tv/" + text_owner.Text.Remove(0, 7) + " " + quality);
        }
    }
}
