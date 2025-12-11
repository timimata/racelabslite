using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace ChatGptClient
{
    public partial class MainWindow : Window
    {
        private readonly Uri[] _chatUris =
        {
            new Uri("https://chat.openai.com/"),
            new Uri("https://chatgpt.com/")
        };

        private bool _attemptedFallback;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        // Install WebView2 with: dotnet add package Microsoft.Web.WebView2
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                await ChatWebView.EnsureCoreWebView2Async();
                ChatWebView.NavigationCompleted += OnNavigationCompleted;
                NavigateToChat();
            }
            catch (WebView2RuntimeNotFoundException ex)
            {
                MessageBox.Show($"WebView2 runtime is required to run this application. {ex.Message}",
                    "WebView2 Runtime Missing", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToChat()
        {
            if (_chatUris.Length == 0)
            {
                return;
            }

            ChatWebView.Source = _chatUris[0];
        }

        private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                ChatWebView.NavigationCompleted -= OnNavigationCompleted;
                return;
            }

            if (!_attemptedFallback && _chatUris.Length > 1)
            {
                _attemptedFallback = true;
                ChatWebView.Source = _chatUris[1];
            }
            else
            {
                ChatWebView.NavigationCompleted -= OnNavigationCompleted;
            }
        }
    }
}
