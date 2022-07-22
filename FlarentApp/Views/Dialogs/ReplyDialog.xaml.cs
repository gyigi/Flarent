﻿using FlarentApp.Helpers;
using FlarentApp.Services;
using FlarentApp.Views.DetailPages;
using FlarumApi;
using FlarumApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace FlarentApp.Views.Dialogs
{
    public sealed partial class ReplyDialog : ContentDialog
    {
        public Discussion Discussion;
        public string Text;
        public ReplyDialog(Discussion discussion,string text = "")
        {
            this.InitializeComponent();
            Discussion = discussion;
            if(text!="")
                EditZone.Document.Selection.TypeText(text);
        }

        private void EditZone_TextChanged(object sender, RoutedEventArgs e)
        {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private async void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoadingProgressBar.ShowError = false;
            string text = string.Empty;
            EditZone.Document.GetText(Windows.UI.Text.TextGetOptions.UseCrlf,out text);
            ReplyButton.IsEnabled = false;
            try
            {
                var data = await FlarumApiProviders.ReplyAsync(text, $"https://{Flarent.Settings.Forum}/api/posts", (int)Discussion.Id, Flarent.Settings.Token);
                var reply = data.Item1;
                if (data.Item2 == "")
                {
                    Hide();
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
            catch
            {
                LoadingProgressBar.ShowError = true;
                ReplyButton.IsEnabled = true;
            }

        }
    }
}
