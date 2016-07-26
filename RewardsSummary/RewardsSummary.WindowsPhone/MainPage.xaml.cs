﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Wallet;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RewardsSummary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            this._walletStore = await WalletManager.RequestStoreAsync();
        }

        private const string StoreItemName = "RewardsSummary";

        private const string ItemName = "Rewards Summary Card";

        private const string DisplayName = "Rewards Summary";

        private const string RetailerName = "Store";

        private static string NearestStoreID = "Store";

        private static Store[] StoreList = new Store[10];

        private static ManualResetEvent GetStoreLocations = new ManualResetEvent(false);

        private async void btnLaunch_Click(object sender, RoutedEventArgs e)
        {
            this.NotifyUser(string.Empty, NotifyType.StatusMessage);
            this.GetNearbyStores("Wallgreens");
            await this.AddItemAsync();
        }

        private async Task AddItemAsync()
        {
            try
            {
                // Create the membership card.
                WalletItem card = new WalletItem(WalletItemKind.MembershipCard, ItemName);

                // Set colors, to give the card our distinct branding.
                card.BodyColor = Windows.UI.Colors.Brown;
                card.BodyFontColor = Windows.UI.Colors.White;
                card.HeaderColor = Windows.UI.Colors.SaddleBrown;
                card.HeaderFontColor = Windows.UI.Colors.White;

                // Set basic properties.
                card.IssuerDisplayName = DisplayName;

                // Set some images.
                card.Logo336x336 = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///assets/coffee336x336.png"));

                card.Logo99x99 = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///assets/coffee99x99.png"));

                card.Logo159x159 = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///assets/coffee159x159.png"));

                card.HeaderBackgroundImage = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///assets/header640x130.png"));

                GetStoreLocations.WaitOne();

                // ToDo: Value of retailer name has to be populated from Geo Api lookup
                WalletItemCustomProperty prop = new WalletItemCustomProperty(NearestStoreID, "Walgreens");
                prop.DetailViewPosition = WalletDetailViewPosition.HeaderField1;
                prop.SummaryViewPosition = WalletSummaryViewPosition.Field1;
                card.DisplayProperties["Retailer"] = prop;

                // ToDo: This needs to be accessed from Walgreens dev Apis
                string accountNumber = "123456";
                prop = new WalletItemCustomProperty("Account Number", accountNumber);
                prop.DetailViewPosition = WalletDetailViewPosition.PrimaryField1;

                // We don't want this field entity extracted as it will be interpreted as a phone number.
                prop.AutoDetectLinks = false;
                card.DisplayProperties["AcctId"] = prop;


                // ToDo: This needs to be accessed from Walgreens dev Apis
                prop = new WalletItemCustomProperty("Points", "2000");
                prop.DetailViewPosition = WalletDetailViewPosition.PrimaryField2;
                card.DisplayProperties["Points"] = prop;

                // Encode the user's account number as a Qr Code to be used in the store.
                card.Barcode = new WalletBarcode(WalletBarcodeSymbology.Qr, accountNumber);


                await this._walletStore.AddAsync(StoreItemName, card);

                await this.ShowWalletItemAsync();
            }
            catch (Exception ex)
            {
                this.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
            }
        }

        private static void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            // TODO: Get the location from device and pass the lat lng value.
            string json = @"{
                        ""affId"":""extest1"",
                        ""apiKey"":""oOuIuu6mEYSaAcNmd4Jd0VLUnD1pj0BI"",
                        ""lat"":""47.67683"",
                        ""lng"":""-122.11"",
                        ""srchOpt"":"""",
                        ""nxtPrev"":"""",
                        ""requestType"":""locator"",
                        ""act"":""fndStore"",
                        ""view"":""fndStoreJSON"",
                        ""devinf"":""iPhone,9.0"",
                        ""appver"":""1.0""
                        }";
            StreamWriter sw = new StreamWriter(postStream);
            sw.Write(json);
            sw.Flush();
            postStream.Flush();
            postStream.Dispose();

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);

        }

        private static void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
            // End the operation
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
            Stream streamResponse = response.GetResponseStream();
            StreamReader streamRead = new StreamReader(streamResponse);
            string responseString = streamRead.ReadToEnd();

            JsonSerializer serializer = new JsonSerializer();
            var responseObj = (JObject)JsonConvert.DeserializeObject(responseString);

            NearestStoreID = (string)responseObj["stores"][0]["stnm"];

            for (int i = 0; i < 10; i++)
            {
                StoreList[i] = new Store("Wallgreens",
                    (string)responseObj["stores"][0]["stnm"],
                    (string)responseObj["stores"][0]["stadd"] + ", " + (string)responseObj["stores"][0]["stct"],
                    (string)responseObj["stores"][0]["stph"],
                    (string)responseObj["stores"][0]["stdist"]);
            }


            // Close the stream object
            streamResponse.Dispose();
            streamRead.Dispose();

            // Release the HttpWebResponse
            response.Dispose();
            GetStoreLocations.Set();


        }

        public async Task ShowWalletItemAsync()
        {
            WalletItem walletItem = await this._walletStore.GetWalletItemAsync(StoreItemName);

            // If the item exists, show it in Wallet
            if (walletItem != null)
            {
                // Launch Wallet and navigate to item
                await this._walletStore.ShowAsync(walletItem.Id);
            }
            else
            {
                this.NotifyUser(string.Format("{0} wallet item not available in Wallet", StoreItemName), NotifyType.ErrorMessage);
            }
        }

        private void GetNearbyStores(String storeName)
        {
            var webAddr = "https://services-qa.walgreens.com/api/stores/search";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), httpWebRequest);
        }

        private void NotifyUser(string strMessage, NotifyType type)
        {
            if (StatusBlock != null)
            {
                switch (type)
                {
                    case NotifyType.StatusMessage:
                        StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                        break;
                    case NotifyType.ErrorMessage:
                        StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                        break;
                }
                StatusBlock.Text = strMessage;

                // Collapse the StatusBlock if it has no text to conserve real estate.
                if (StatusBlock.Text != String.Empty)
                {
                    StatusBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    StatusBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
        }

        private WalletItemStore _walletStore;
    }

    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };

}
