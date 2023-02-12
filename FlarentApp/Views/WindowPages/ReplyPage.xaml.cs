﻿using FlarentApp.Helpers;
using FlarentApp.Services;
using FlarentApp.Views.Controls;
using FlarentApp.Views.DetailPages;
using FlarumApi;
using FlarumApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace FlarentApp.Views.WindowPages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ReplyPage : Page
    {
        public Discussion Discussion;
        public Post Post;
        public string Text;
        public string Referer;
        public bool Success = false;
        public AppWindow MyAppWindow { get; set; }

        public ReplyPage()
        {
            this.InitializeComponent();

        }
        private void EditZone_TextChanged(object sender, RoutedEventArgs e)
        {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var tuple = e.Parameter as Tuple<Discussion,Post,string,string>;
            Discussion = tuple.Item1;
            Post = tuple.Item2;
            Referer = tuple.Item4;
            if (tuple.Item3 != "")
                EditZone.Document.Selection.TypeText(tuple.Item3);
        }

        private async void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoadingProgressBar.ShowError = false;
            string text = string.Empty;
            EditZone.Document.GetText(Windows.UI.Text.TextGetOptions.UseCrlf, out text);
            ReplyButton.IsEnabled = false;
            try
            {
                if (Post == null)//发帖
                {
                    var data = await FlarumApiProviders.ReplyAsync(text, $"https://{Flarent.Settings.Forum}/api/posts", (int)Discussion.Id, Flarent.Settings.Token);
                    var reply = data.Item1;
                    if (data.Item2 == "")
                    {
                        await MyAppWindow.CloseAsync();
                        var postId = (int)reply["data"]["id"];
                        var shell = Window.Current.Content as ShellPage;//获取当前正在显示的页面
                        var frame = shell.shellFrame;
                        if (frame.Content is DiscussionDetailPage page)
                        {
                            await page.GetDiscussion();
                            page.TurnToLastPage();
                        }
                        NavigationService.OpenInRightPane(typeof(PostDetailPage), postId);
                    }
                    else
                    {
                        LoadingProgressBar.ShowError = true;
                        ReplyButton.IsEnabled = true;
                    }
                }
                else
                {
                    await Edit(text);
                }
            }

            catch
            {
                LoadingProgressBar.ShowError = true;
                ReplyButton.IsEnabled = true;
            }

        }
        public async Task Edit(string text)
        {
            var data = await FlarumApiProviders.EditAsync(text, $"https://{Flarent.Settings.Forum}/api/posts/{(int)Post.Id}", (int)Post.Id, Flarent.Settings.Token, Referer);
            var reply = data.Item1;
            if (data.Item2 == "")
            {
                Text = data.Item2;
                new Toast("编辑成功").Show();
                var discussion = Post.Discussion;
                Post = data.Item1;
                Post.Discussion = discussion;
                var postId = data.Item1.Id;
                Success = true;
                await MyAppWindow.CloseAsync();
            }
            else
            {
                LoadingProgressBar.ShowError = true;
                ReplyButton.IsEnabled = true;
            }
        }
    }
}
