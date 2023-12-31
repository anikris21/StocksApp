﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace StockAnalyzer.CrossPlatform;

public partial class MainWindow : Window
{
    public DataGrid Stocks => this.FindControl<DataGrid>(nameof(Stocks));
    public ProgressBar StockProgress => this.FindControl<ProgressBar>(nameof(StockProgress));
    public TextBox StockIdentifier => this.FindControl<TextBox>(nameof(StockIdentifier));
    public Button Search => this.FindControl<Button>(nameof(Search));
    public TextBox Notes => this.FindControl<TextBox>(nameof(Notes));
    public TextBlock StocksStatus => this.FindControl<TextBlock>(nameof(StocksStatus));
    public TextBlock DataProvidedBy => this.FindControl<TextBlock>(nameof(DataProvidedBy));
    public TextBlock IEX => this.FindControl<TextBlock>(nameof(IEX));
    public TextBlock IEX_Terms => this.FindControl<TextBlock>(nameof(IEX_Terms));

    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        IEX.PointerPressed += (e, a) => Open("https://iextrading.com/developer/");
        IEX_Terms.PointerPressed += (e, a) => Open("https://iextrading.com/api-exhibit-a/");

        /// Data provided for free by <a href="https://iextrading.com/developer/" RequestNavigate="Hyperlink_OnRequestNavigate">IEX</Hyperlink>. View <Hyperlink NavigateUri="https://iextrading.com/api-exhibit-a/" RequestNavigate="Hyperlink_OnRequestNavigate">IEX’s Terms of Use.</Hyperlink>
    }





    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";

    private Stopwatch stopwatch = new Stopwatch();

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        BeforeLoadingStockData();

        // await GetStocks()

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        var loadLinetask = getFileData();
        //    Task.Run(async () =>
        //{
        //    //var lines = File.ReadAllLines("StockPrices_Small.csv");
        //    using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));
        //    var lines = new List<string>();

        //    //foreach(var line in await stream.ReadLineAsync())
        //    while (true)
        //    {
        //        var line = await stream.ReadLineAsync();
        //        if (line == null)
        //        {
        //            break;
        //        }
        //        lines.Add(line);
        //    }

        //    return lines;

        //});

        loadLinetask.ContinueWith(t =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Notes.Text = t.Exception?.InnerException?.Message;

            });
        }, TaskContinuationOptions.OnlyOnFaulted);

        loadLinetask.ContinueWith(t => {
            var lines = t.Result;
            var data = new List<StockPrice>();
            foreach (var line in lines.Skip(1))
            {
                data.Add(StockPrice.FromCSV(line));

            }

            Dispatcher.UIThread.InvokeAsync(() =>
            {

                Stocks.Items = data.Where(sp => sp.Identifier == StockIdentifier.Text).ToList();
            });

        });


#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed





        //var client = new WebClient();

        //var content = client.DownloadString($"{API_URL}/{StockIdentifier.Text}");

        //// Simulate that the web call takes a very long time
        //Thread.Sleep(10000);

        //var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);

        //// This is the same as ItemsSource in WPF used in the course videos
        //Stocks.Items = data;

        AfterLoadingStockData();
    }

    private Task<List<string>> getFileData()
    {
        return Task.Run(async () =>
        {
            //var lines = File.ReadAllLines("StockPrices_Small.csv");
            using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));
            var lines = new List<string>();

            //foreach(var line in await stream.ReadLineAsync())
            while (true)
            {
                var line = await stream.ReadLineAsync();
                if (line == null)
                {
                    break;
                }
                lines.Add(line);
            }

            return lines;

        });
    }

    private async Task GetStocks()
    {
        try
        {
            var store = new DataStore();
            Stocks.Items = await store.GetStockPrices(StockIdentifier.Text);

        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;

        }
    }








    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.IsVisible = true;
        StockProgress.IsIndeterminate = true;
    }

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.IsVisible = false;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    public static void Open(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
}
