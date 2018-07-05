using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

/*
 * 第一个页面，天气查询页
 */
namespace MyWeatherApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static string currentCity; // 记录当前搜索的城市

        public MainPage()
        {
            this.InitializeComponent();
        }

        /*
         * 后台运行与程序生命周期，程序间通信
         */
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
        /*    if (rootFrame.CanGoBack)
            {
                // Show UI in title bar if opted-in and in-app backstack is not empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if in-app back stack is empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Collapsed;
            }*/

            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New)
            {
                ApplicationData.Current.LocalSettings.Values.Remove("TheWorkInProgress");
            }
            else
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("TheWorkInProgress"))
                {
                    var composite = ApplicationData.Current.LocalSettings.Values["TheWorkInProgress"] as ApplicationDataCompositeValue;
                    weatherSearchBlock.Text = (string)composite["weatherSearchBlock"];
                    currentWeatherBlock.Text = (string)composite["currentWeatherBlock"];
                    weatherForcast1.Text = (string)composite["weatherForcast1"];
                    weatherForcast2.Text = (string)composite["weatherForcast2"];
                    ApplicationData.Current.LocalSettings.Values.Remove("TheWorkInProgress");
                }
            }

            DataTransferManager.GetForCurrentView().DataRequested += OnShareRequested;
        }


        /*
         * 后台运行与程序生命周期，程序间通信
         */
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            bool suspending = ((App)App.Current).isSuspended;
            if (suspending)
            {
                var composite = new ApplicationDataCompositeValue();
                composite["weatherSearchBlock"] = weatherSearchBlock.Text;
                composite["currentWeatherBlock"] = currentWeatherBlock.Text;
                composite["weatherForcast1"] = weatherForcast1.Text;
                composite["weatherForcast2"] = weatherForcast1.Text;
                ApplicationData.Current.LocalSettings.Values["TheWorkInProgress"] = composite;
            }

            DataTransferManager.GetForCurrentView().DataRequested -= OnShareRequested;
        }

        /*
         * 点击分享时的click事件
         */
        private void shareWeather(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        /*
         * 程序间通信
         */
        private async void OnShareRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataPackage data = args.Request.Data;
            if (currentWeatherBlock.Text != "")
            {
                data.Properties.Title = "天气实况";
                data.Properties.Description = currentWeatherBlock.Text;
                data.SetText(currentWeatherBlock.Text);
                StorageFile imageFile = await Package.Current.InstalledLocation.GetFileAsync("Assets\\background.jpg");
                data.Properties.Thumbnail = RandomAccessStreamReference.CreateFromFile(imageFile);
                data.SetBitmap(RandomAccessStreamReference.CreateFromFile(imageFile));
            } else
            {
                var i = new MessageDialog("无可用分享").ShowAsync();
            }
        }

        /*
         * 天气查找按钮的点击事件,当查询得出正确结果时，更新磁贴
         */
        private async void searchWeather(object sender, RoutedEventArgs e)
        {
            currentCity = "";

            if (weatherSearchBlock.Text == "")
            {
                string url = "http://restapi.amap.com/v3/ip?output=xml&key=5fd3b8bd943a505ccfec387943bba945";
                HttpClient client = new HttpClient();
                string result = await client.GetStringAsync(url);
                XmlDocument document = new XmlDocument();
                document.LoadXml(result);
                XmlNodeList list = document.GetElementsByTagName("status");
                IXmlNode node = list.Item(0);
                string querySuccess = node.InnerText;
                if (querySuccess == "0")
                {
                    XmlNodeList li = document.GetElementsByTagName("info");
                    IXmlNode no = li.Item(0);
                    var i = new MessageDialog(no.InnerText).ShowAsync();
                } else
                {
                    list = document.GetElementsByTagName("adcode");
                    node = list.Item(0);
                    string adcode = node.InnerText;

                    string url2 = "http://restapi.amap.com/v3/weather/weatherInfo?key=5fd3b8bd943a505ccfec387943bba945&extensions=all&city=" + adcode;
                    string result2 = await client.GetStringAsync(url2);
                    JObject jo = (JObject)JsonConvert.DeserializeObject(result2);
                    string querySuccess2 = jo["status"].ToString();
                    if (querySuccess2 == "0")
                    {
                        var i = new MessageDialog(jo["info"].ToString()).ShowAsync();
                    } else
                    {
                        currentWeatherBlock.Text = "";
                        weatherForcast1.Text = "";
                        weatherForcast2.Text = "";

                        JArray ja = (JArray)jo["forecasts"];
                        currentWeatherBlock.Text += ja[0]["city"].ToString() + "\n";
                        currentCity = ja[0]["city"].ToString();
                        currentWeatherBlock.Text += "最后更新： " + ja[0]["reporttime"].ToString() + "\n";
                        JArray ja2 = (JArray)ja[0]["casts"];
                        currentWeatherBlock.Text += ja2[0]["date"].ToString() + "\n";
                        currentWeatherBlock.Text += "星期" + ja2[0]["week"].ToString() + "\n";
                        currentWeatherBlock.Text += ja2[0]["dayweather"].ToString() + "\n";
                        if (ja2[0]["dayweather"].ToString() == "晴")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/晴.jpg"));
                            icon1.Source = image;
                        } else if (ja2[0]["dayweather"].ToString() == "小雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/小雨.jpg"));
                            icon1.Source = image;
                        } else if (ja2[0]["dayweather"].ToString() == "中雨" || ja2[0]["dayweather"].ToString() == "大雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/大雨.jpg"));
                            icon1.Source = image;
                        } else if (ja2[0]["dayweather"].ToString() == "暴雨" || ja2[0]["dayweather"].ToString() == "大暴雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/暴雨.jpg"));
                            icon1.Source = image;
                        } else if (ja2[0]["dayweather"].ToString() == "雾")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/雾.jpg"));
                            icon1.Source = image;
                        } else if (ja2[0]["dayweather"].ToString() == "多云")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/多云.jpg"));
                            icon1.Source = image;
                        } else
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/阴.jpg"));
                            icon1.Source = image;
                        }
                        currentWeatherBlock.Text += "日间温度： " + ja2[0]["daytemp"].ToString() + "\n";
                        currentWeatherBlock.Text += "夜间温度： " + ja2[0]["nighttemp"].ToString() + "\n";

                        weatherForcast1.Text += ja2[1]["date"].ToString() + "\n";
                        weatherForcast1.Text += "星期" + ja2[1]["week"].ToString() + "\n";
                        weatherForcast1.Text += ja2[1]["dayweather"].ToString() + "\n";
                        if (ja2[1]["dayweather"].ToString() == "晴")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/晴.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "小雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/小雨.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "中雨" || ja2[0]["dayweather"].ToString() == "大雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/大雨.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "暴雨" || ja2[0]["dayweather"].ToString() == "大暴雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/暴雨.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "雾")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/雾.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "多云")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/多云.jpg"));
                            icon2.Source = image;
                        }
                        else
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/阴.jpg"));
                            icon2.Source = image;
                        }
                        weatherForcast1.Text += "日间温度： " + ja2[1]["daytemp"].ToString() + "\n";
                        weatherForcast1.Text += "夜间温度： " + ja2[1]["nighttemp"].ToString() + "\n";

                        weatherForcast2.Text += ja2[2]["date"].ToString() + "\n";
                        weatherForcast2.Text += "星期" + ja2[2]["week"].ToString() + "\n";
                        weatherForcast2.Text += ja2[2]["dayweather"].ToString() + "\n";
                        if (ja2[2]["dayweather"].ToString() == "晴")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/晴.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "小雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/小雨.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "中雨" || ja2[0]["dayweather"].ToString() == "大雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/大雨.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "暴雨" || ja2[0]["dayweather"].ToString() == "大暴雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/暴雨.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "雾")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/雾.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "多云")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/多云.jpg"));
                            icon3.Source = image;
                        }
                        else
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/阴.jpg"));
                            icon3.Source = image;
                        }
                        weatherForcast2.Text += "日间温度： " + ja2[2]["daytemp"].ToString() + "\n";
                        weatherForcast2.Text += "夜间温度： " + ja2[2]["nighttemp"].ToString() + "\n";
                    }
                }
            } else
            {
                string url = "http://restapi.amap.com/v3/config/district?key=5fd3b8bd943a505ccfec387943bba945&subdistrict=0&showbiz=false&extensions=base&keywords=" + weatherSearchBlock.Text;
                HttpClient client = new HttpClient();
                string result = await client.GetStringAsync(url);
                JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                if (jo["status"].ToString() == "0" || jo["count"].ToString() == "0")
                {
                    var i = new MessageDialog("查无此地区").ShowAsync();
                } else
                {
                    JArray ja = (JArray)jo["districts"];
                    string adcode = ja[0]["adcode"].ToString();
                    string url2 = "http://restapi.amap.com/v3/weather/weatherInfo?key=5fd3b8bd943a505ccfec387943bba945&extensions=all&city=" + adcode;
                    string result2 = await client.GetStringAsync(url2);
                    JObject jo2 = (JObject)JsonConvert.DeserializeObject(result2);
                    string querySuccess2 = jo2["status"].ToString();
                    if (querySuccess2 == "0")
                    {
                        var i = new MessageDialog(jo2["info"].ToString()).ShowAsync();
                    }
                    else
                    {
                        currentWeatherBlock.Text = "";
                        weatherForcast1.Text = "";
                        weatherForcast2.Text = "";

                        JArray ja1 = (JArray)jo2["forecasts"];
                        currentWeatherBlock.Text += ja1[0]["city"].ToString() + "\n";
                        currentCity = ja1[0]["city"].ToString();
                        currentWeatherBlock.Text += "最后更新： " + ja1[0]["reporttime"].ToString() + "\n";
                        JArray ja2 = (JArray)ja1[0]["casts"];
                        currentWeatherBlock.Text += ja2[0]["date"].ToString() + "\n";
                        currentWeatherBlock.Text += "星期" + ja2[0]["week"].ToString() + "\n";
                        currentWeatherBlock.Text += ja2[0]["dayweather"].ToString() + "\n";
                        if (ja2[0]["dayweather"].ToString() == "晴")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/晴.jpg"));
                            icon1.Source = image;
                        }
                        else if (ja2[0]["dayweather"].ToString() == "小雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/小雨.jpg"));
                            icon1.Source = image;
                        }
                        else if (ja2[0]["dayweather"].ToString() == "中雨" || ja2[0]["dayweather"].ToString() == "大雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/大雨.jpg"));
                            icon1.Source = image;
                        }
                        else if (ja2[0]["dayweather"].ToString() == "暴雨" || ja2[0]["dayweather"].ToString() == "大暴雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/暴雨.jpg"));
                            icon1.Source = image;
                        }
                        else if (ja2[0]["dayweather"].ToString() == "雾")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/雾.jpg"));
                            icon1.Source = image;
                        }
                        else if (ja2[0]["dayweather"].ToString() == "多云")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/多云.jpg"));
                            icon1.Source = image;
                        }
                        else
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/阴.jpg"));
                            icon1.Source = image;
                        }
                        currentWeatherBlock.Text += "日间温度： " + ja2[0]["daytemp"].ToString() + "\n";
                        currentWeatherBlock.Text += "夜间温度： " + ja2[0]["nighttemp"].ToString() + "\n";

                        weatherForcast1.Text += ja2[1]["date"].ToString() + "\n";
                        weatherForcast1.Text += "星期" + ja2[1]["week"].ToString() + "\n";
                        weatherForcast1.Text += ja2[1]["dayweather"].ToString() + "\n";
                        if (ja2[1]["dayweather"].ToString() == "晴")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/晴.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "小雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/小雨.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "中雨" || ja2[0]["dayweather"].ToString() == "大雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/大雨.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "暴雨" || ja2[0]["dayweather"].ToString() == "大暴雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/暴雨.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "雾")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/雾.jpg"));
                            icon2.Source = image;
                        }
                        else if (ja2[1]["dayweather"].ToString() == "多云")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/多云.jpg"));
                            icon2.Source = image;
                        }
                        else
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/阴.jpg"));
                            icon2.Source = image;
                        }
                        weatherForcast1.Text += "日间温度： " + ja2[1]["daytemp"].ToString() + "\n";
                        weatherForcast1.Text += "夜间温度： " + ja2[1]["nighttemp"].ToString() + "\n";

                        weatherForcast2.Text += ja2[2]["date"].ToString() + "\n";
                        weatherForcast2.Text += "星期" + ja2[2]["week"].ToString() + "\n";
                        weatherForcast2.Text += ja2[2]["dayweather"].ToString() + "\n";
                        if (ja2[2]["dayweather"].ToString() == "晴")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/晴.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "小雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/小雨.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "中雨" || ja2[0]["dayweather"].ToString() == "大雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/大雨.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "暴雨" || ja2[0]["dayweather"].ToString() == "大暴雨")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/暴雨.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "雾")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/雾.jpg"));
                            icon3.Source = image;
                        }
                        else if (ja2[2]["dayweather"].ToString() == "多云")
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/多云.jpg"));
                            icon3.Source = image;
                        }
                        else
                        {
                            BitmapImage image = new BitmapImage(new Uri("ms-appx:Assets/阴.jpg"));
                            icon3.Source = image;
                        }
                        weatherForcast2.Text += "日间温度： " + ja2[2]["daytemp"].ToString() + "\n";
                        weatherForcast2.Text += "夜间温度： " + ja2[2]["nighttemp"].ToString() + "\n";
                    }
                }
            }

            if (currentWeatherBlock.Text != "")
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                XmlDocument xml = await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///tile.xml")));
                string changeContent = xml.GetXml().Replace("Text", currentWeatherBlock.Text);
                XmlDocument newXml = new XmlDocument();
                newXml.LoadXml(changeContent);
                TileNotification update = new TileNotification(newXml);
                Random a = new Random();
                string id = a.Next(1000).ToString();
                update.Tag = id;
                TileUpdateManager.CreateTileUpdaterForApplication().Update(update);
            }

            if (weatherForcast1.Text != "")
            {
                XmlDocument xml = await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///tile.xml")));
                string changeContent = xml.GetXml().Replace("Text", weatherForcast1.Text);
                XmlDocument newXml = new XmlDocument();
                newXml.LoadXml(changeContent);
                TileNotification update = new TileNotification(newXml);
                Random a = new Random();
                string id = a.Next(1000).ToString();
                update.Tag = id;
                TileUpdateManager.CreateTileUpdaterForApplication().Update(update);
            }

            if (weatherForcast2.Text != "")
            {
                XmlDocument xml = await XmlDocument.LoadFromFileAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///tile.xml")));
                string changeContent = xml.GetXml().Replace("Text", weatherForcast2.Text);
                XmlDocument newXml = new XmlDocument();
                newXml.LoadXml(changeContent);
                TileNotification update = new TileNotification(newXml);
                Random a = new Random();
                string id = a.Next(1000).ToString();
                update.Tag = id;
                TileUpdateManager.CreateTileUpdaterForApplication().Update(update);
            }
        }

        /*
         * 添加收藏
         */
        private void addFavourite(object sender, RoutedEventArgs e)
        {
            if (NewPage2.userName == "")
            {
                var i = new MessageDialog("未登录").ShowAsync();
                return;
            }

        
            if (currentCity == "")
            {
                var i = new MessageDialog("未选中收藏地点").ShowAsync();
            } else
            {
                string id = Guid.NewGuid().ToString();
                var db = App.connection.getInstance().conn;
                using (var statement = db.Prepare("INSERT INTO Favourites (Id, UserName, City) VALUES (?, ?, ?)"))
                {
                    statement.Bind(1, id);
                    statement.Bind(2, NewPage2.userName);
                    statement.Bind(3, currentCity);
                    statement.Step();
                }
            }
        }

        /*
         * 显示收藏
         */
        private void showCollection(object sender, RoutedEventArgs e)
        {
            if (NewPage2.userName == "")
            {
                var j = new MessageDialog("未登录").ShowAsync();
                return;
            }

    
            var db = App.connection.getInstance().conn;
            string result = "收藏的地点： \n";
            using (var statement = db.Prepare("SELECT * FROM Favourites WHERE UserName LIKE ?"))
            {
                statement.Bind(1, NewPage2.userName);
                while (SQLiteResult.ROW == statement.Step())
                {
                    result += (string)statement[2] + "\n";
                }
            }

            var i = new MessageDialog(result).ShowAsync();
        }

        private void gotoMainPage(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage), "");
        }

        private void gotoNewPage1(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewPage1), "");
        }

        private void gotoNewPage2(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewPage2), "");
        }
    }
}
