﻿using FlarentApp.Helpers;
using FlarentApp.Helpers.Converters;
using FlarentApp.Services;
using FlarentApp.Views.DetailPages;
using FlarentApp.Views.Dialogs;
using FlarentApp.Views.WindowPages;
using FlarumApi;
using FlarumApi.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace FlarentApp.Views
{

    public sealed partial class HomePage : Page, INotifyPropertyChanged
    {
        public HomePage()
        {
            InitializeComponent();
            RequestedTheme = (Window.Current.Content as FrameworkElement).RequestedTheme;
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Discussion> Discussions = new ObservableCollection<Discussion>();
        public string SortBy = "";
        public string Filter = "";
        public bool ClearData = false;
        public Tag DiscussionTag;

        //public string LinkNext = "";
        public string LinkNext
        {
            get => _linkNext;
            set
            {
                if (_linkNext != value)
                {
                    _linkNext = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinkNext)));
                }
            }
        }
        private string _linkNext;
        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode != NavigationMode.Back || ClearData != false)
            {
                ClearData = false;
                DiscussionTag = null;
                if (e.Parameter is Tag tag)
                {
                    DiscussionTag = tag;
                }
                else if (e.Parameter is string filter)
                    Filter = filter;
                else
                    Filter = "";
                Discussions.Clear();
                LoadingProgressRing.Visibility = Windows.UI.Xaml.Visibility.Visible;
                
                GetDiscussions();
            }
            var shell = Window.Current.Content as ShellPage;//获取当前正在显示的页面
            if(DiscussionTag!=null)
            {
                shell.PageNameTextBlock.Text = DiscussionTag.Name;
            }
            else if(Filter == "&filter[subscription]=following")
            {
                shell.PageNameTextBlock.Text = "我的收藏";
            }
            else
            {
                shell.PageNameTextBlock.Text = "主页";

            }

        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                ClearData = true;
            }
        }

        private void DiscussionsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            //var item = (ListViewItem)DiscussionsListView.ContainerFromItem(e.ClickedItem);
            //ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", item);
            var clicked = e.ClickedItem as Discussion;
            var id = clicked.Id;
            NavigationService.Navigate<DiscussionDetailPage>(id);
        }

        private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var item = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender.SelectedItem;
            SortBy = item.Tag.ToString();

            if (SortBy == "-frontdate")
                SortBy = "-frontdate&filter[q]=is:frontpage";
            LoadingProgressRing.Visibility = Windows.UI.Xaml.Visibility.Visible;
            GetDiscussions();

        }
        private async void GetDiscussions()
        {
            try
            {
                ErrorControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                if (DiscussionTag != null)
                {
                    FavoriteButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    TagNameTextBlock.Text = DiscussionTag.Name;
                    TagDescriptionTextBlock.Text = DiscussionTag.Description;
                    var converter = new FontAwesomeConverter();
                    FontAwesomeIcon.Icon = (FontAwesome.UWP.FontAwesomeIcon)converter.Convert(DiscussionTag.Icon, null, null, null);
                }
                else
                {
                    FavoriteButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                LoadMoreButton.IsEnabled = false;
                DiscussionsListView.IsEnabled = false;
                string sort = "";
                string tag = "";
                if (SortBy != "")
                    sort = $"&sort={SortBy}";
                if (DiscussionTag != null)
                    tag = $"&filter[tag]={DiscussionTag.Slug}";
                var query = $"{sort}{tag}{Filter}";
                var data = await FlarumApiProviders.GetDiscussions(query, null, Flarent.Settings.Forum, Flarent.Settings.Token);
                Discussions = data.Item1;
                LinkNext = data.Item2;
                DiscussionsListView.ItemsSource = Discussions;
                // DiscussionListView.ItemsSource = ViewModel.Discussions;
                LoadMoreButton.IsEnabled = true;
                DiscussionsListView.IsEnabled = true;
            }
            catch
            {
                Discussions.Clear();
                DiscussionsListView.IsEnabled = true;
                LoadMoreButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                ErrorControl.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            finally
            {
                LoadingProgressRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private async void LoadMoreButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            LoadMoreButton.IsEnabled = false;
            var data = await FlarumApiProviders.GetDiscussions(null, LinkNext, Flarent.Settings.Forum,Flarent.Settings.Token);
            LinkNext = data.Item2;
            foreach (var discussion in data.Item1)
                Discussions.Add(discussion);
            LoadMoreButton.IsEnabled = true;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadingProgressRing.Visibility = Windows.UI.Xaml.Visibility.Visible;
            GetDiscussions();
        }

        private async void NewDiscussionButton_Click(object sender, RoutedEventArgs e)
        {
            await new NewDiscussionDialog().ShowAsync();

        }
    }
}
