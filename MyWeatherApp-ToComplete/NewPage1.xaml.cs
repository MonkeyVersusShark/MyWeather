using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

/*
 * 第二个页面，地图查询页
 */
namespace MyWeatherApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewPage1 : Page
    {
        public static string currentCity; // 记录当前搜索的城市

        public NewPage1()
        {
            this.InitializeComponent();
        }

        /*
         * 后台运行与程序生命周期
         */
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
      /*      if (rootFrame.CanGoBack)
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
                    mapSearchBlock.Text = (string)composite["mapSearchBlock"];
                    ApplicationData.Current.LocalSettings.Values.Remove("TheWorkInProgress");
                }
            }
        }

        /*
         * 后台运行与程序生命周期
         */
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            bool suspending = ((App)App.Current).isSuspended;
            if (suspending)
            {
                var composite = new ApplicationDataCompositeValue();
                composite["mapSearchBlock"] = mapSearchBlock.Text;
                ApplicationData.Current.LocalSettings.Values["TheWorkInProgress"] = composite;
            }
        }

        /*
         * 分为地图查找和交通路况显示，由checkbox控制
         */
        private async void searchMap(object sender, RoutedEventArgs e)
        {
            currentCity = "";
            if (searchTraffic.IsChecked == false)
            {
                if (mapSearchBlock.Text != "")
                {
                    string url = "http://restapi.amap.com/v3/geocode/geo?key=5fd3b8bd943a505ccfec387943bba945&address=" + mapSearchBlock.Text;
                    HttpClient client = new HttpClient();
                    string result = await client.GetStringAsync(url);
                    JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                    if (jo["status"].ToString() == "0" || jo["count"].ToString() == "0")
                    {
                        var i = new MessageDialog("查无此地区").ShowAsync();
                    } else
                    {
                        JArray ja = (JArray)jo["geocodes"];
                        string location = ja[0]["location"].ToString();
                        currentCity = ja[0]["formatted_address"].ToString();

                        string url2 = "http://restapi.amap.com/v3/staticmap?key=5fd3b8bd943a505ccfec387943bba945&location=" + location + "&zoom=10&size=731*458&labels=" + mapSearchBlock.Text + ",2,0,16,0xFFFFFF,0x008000:" + location;
                        HttpResponseMessage response = await client.GetAsync(url2);
                        BitmapImage bitmap = new BitmapImage();
                        Stream stream = await response.Content.ReadAsStreamAsync();
                        IInputStream inputStream = stream.AsInputStream();
                        using (IRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
                        {
                            using (IOutputStream outputStream = randomAccessStream.GetOutputStreamAt(0))
                            {
                                await RandomAccessStream.CopyAsync(inputStream, outputStream);
                                randomAccessStream.Seek(0);
                                bitmap.SetSource(randomAccessStream);
                                map.Source = bitmap;
                            }
                        }
                    }
                }
            } else
            {
                if (mapSearchBlock.Text != "")
                {
                    string url = "http://restapi.amap.com/v3/geocode/geo?key=5fd3b8bd943a505ccfec387943bba945&address=" + mapSearchBlock.Text;
                    HttpClient client = new HttpClient();
                    string result = await client.GetStringAsync(url);
                    JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                    if (jo["status"].ToString() == "0" || jo["count"].ToString() == "0")
                    {
                        var i = new MessageDialog("查无此地区").ShowAsync();
                    } else
                    {
                        JArray ja = (JArray)jo["geocodes"];
                        string location = ja[0]["location"].ToString();
                        currentCity = ja[0]["formatted_address"].ToString();

                        string url2 = "http://restapi.amap.com/v3/staticmap?key=5fd3b8bd943a505ccfec387943bba945&zoom=10&size=731*458&traffic=1&location=" + location;
                        HttpResponseMessage response = await client.GetAsync(url2);
                        BitmapImage bitmap = new BitmapImage();
                        Stream stream = await response.Content.ReadAsStreamAsync();
                        IInputStream inputStream = stream.AsInputStream();
                        using (IRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
                        {
                            using (IOutputStream outputStream = randomAccessStream.GetOutputStreamAt(0))
                            {
                                await RandomAccessStream.CopyAsync(inputStream, outputStream);
                                randomAccessStream.Seek(0);
                                bitmap.SetSource(randomAccessStream);
                                map.Source = bitmap;
                            }
                        }
                    }
                }
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
            }
            else
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
