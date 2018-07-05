using SQLitePCL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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
 * 第三个页面，登录页
 */
namespace MyWeatherApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewPage2 : Page
    {
        public static string userName; // 在此页记录用户名。

        public NewPage2()
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
                    nameBlock.Text = (string)composite["userName"];
                    passwordBlock.Password = (string)composite["userPassword"];
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
                composite["userName"] = nameBlock.Text;
                composite["userPassword"] = passwordBlock.Password;
                ApplicationData.Current.LocalSettings.Values["TheWorkInProgress"] = composite;
            }
        }

        /*
         * 文件管理，选择图片添加到头像框
         */
        private async void selectPicture(object sender, RoutedEventArgs e)
        {
            FileOpenPicker Picker = new FileOpenPicker();
            Picker.ViewMode = PickerViewMode.List;
            Picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            Picker.FileTypeFilter.Add(".jpg");
            Picker.FileTypeFilter.Add(".png");
            Picker.FileTypeFilter.Add(".jpeg");
            StorageFile file = await Picker.PickSingleFileAsync();
            if (file != null)
            {
                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    //这里是选定的图片的宽度，根据UI修改一下
                    //bitmapImage.DecodePixelWidth = 400;
                    await bitmapImage.SetSourceAsync(fileStream);
                    //图片控件的名称为Icon
                    Icon.Source = bitmapImage;

                }
            }
            else
            {
                var message = new MessageDialog("Did not Pick anything !").ShowAsync();
            }
        }

        /*
         * 登录
         */
        private void logIn(object sender, RoutedEventArgs e)
        {
            if (nameBlock.Text == "")
            {
                var i = new MessageDialog("用户名不能为空").ShowAsync();
                return;
            }
            if (passwordBlock.Password == "")
            {
                var i = new MessageDialog("密码不能为空").ShowAsync();
                return;
            }

            var db = App.conn2;
            string result = "";
            string pw = "";
            using (var statement = db.Prepare("SELECT * FROM Info WHERE UserName LIKE ?"))
            {
                statement.Bind(1, nameBlock.Text);
                while (SQLiteResult.ROW == statement.Step())
                {
                    result += (string)statement[0];
                    pw += (string)statement[1];
                }
            }

            if (result == "")
            {
                using (var statement2 = db.Prepare("INSERT INTO Info (UserName, Password) VALUES (?, ?)"))
                {
                    statement2.Bind(1, nameBlock.Text);
                    statement2.Bind(2, passwordBlock.Password);
                    statement2.Step();
                }
                userName = nameBlock.Text;
                var i = new MessageDialog("注册成功").ShowAsync();
            } else
            {
                if (passwordBlock.Password == pw)
                {
                    userName = nameBlock.Text;
                    var i = new MessageDialog("登录成功").ShowAsync();
                } else
                {
                    var i = new MessageDialog("登录失败，密码不正确").ShowAsync();
                }
            }
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
