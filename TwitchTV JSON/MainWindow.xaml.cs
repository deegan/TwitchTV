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
            Refresh(null);
        }

        public void Refresh(string game)
        {
            string uri;
            string res;
            streamlist.Clear();
            streamview.Items.Clear();

            if (game == null)
            {
                // this is the default, for now.
                uri = "https://api.twitch.tv/kraken/games/top";
                res = MakeHttpRequest(uri);
            }
            else
            {
                game = game.Replace(" ", "+"); //replace spaces with +.
                uri = "https://api.twitch.tv/kraken/search/streams?q=" + game;

                res = MakeHttpRequest(uri);
                var objects = JsonConvert.DeserializeObject<twitch_json.RootObject>(res);

                foreach ( var item in objects.streams)
                {
                    try
                    {
                        if (item.viewers > 0)
                        {
                            string stream_id = item._id.ToString();
                            var streams = JsonConvert.DeserializeObject<twitch_json.RootObject>(res);
                            TwitchStream temp = new TwitchStream();
                            foreach (var stream in streams.streams)
                            {
                                string tmp_id = stream._id.ToString();
                                if (stream_id == tmp_id)
                                {
                                    if (stream.channel.status != null)
                                        temp.Title = stream.channel.status;
                                    if (stream.channel.name != null)
                                        temp.ChannelOwner = stream.channel.name;
                                    if (item.viewers != 0)
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
            //var name = _download_serialized_json_data<Game>(url);
            string res = MakeHttpRequest(uri);
            var objects = JsonConvert.DeserializeObject<twitch_json.RootObject>(res);

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

        private static T _download_serialized_json_data<T>(string url) where T : new()
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;
                // attempt to download JSON data as a string
                try
                {
                    json_data = w.DownloadString(url);
                }
                catch (Exception) { }
                // if string with JSON data is not empty, deserialize it to class and return its instance 
                return !string.IsNullOrEmpty(json_data) ? JsonConvert.DeserializeObject<T>(json_data) : new T();
            }
        } 

        public static string CreateRequest(string queryString)
        {
            string UrlRequest = "https://api.twitch.tv/kraken/games/top?limit=10";
            return (UrlRequest);
        }

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            string quality = "mobile_high";
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
