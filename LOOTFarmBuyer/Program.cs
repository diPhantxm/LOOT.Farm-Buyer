using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TradeBotLibrary;
using TradeBotLibrary.Models;

namespace LOOTFarmBuyer
{
    class Program
    {
        private static string[] exceptItems = new string[] { "Souvenir", "Package", "Capsule", "Major", "RMR", "Sticker", "Case", "Patch" };

        static void Main(string[] args)
        {
            // Set up price for each sticker on skin
            const float stickerMarkup = .02f;

            // Start Market API
            var mApi = new MarketAPI("CSGO TM");
            mApi.Start();
            var marketItems = mApi.GetAllItemsAverage().Result;

            // Create Google Chrome Driver Options
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--start-maximized");

            // Create Google Chrome Driver and go to LOOT.Farm
            var chrome = new ChromeDriver(chromeOptions);
            var wait = new WebDriverWait(chrome, TimeSpan.FromSeconds(10));
            chrome.Navigate().GoToUrl("https://loot.farm/");

            // Log in and Press <Enter> in console
            Console.WriteLine("Press <Enter> when logged in");
            Console.ReadLine();

            var priceList = new List<ShortItem>();
            var prevFileName = String.Empty;

            // Infinite bot loop
            while (true)
            {
                var startTime = DateTime.Now;

                // Set up min and max price for searching items
                SetMinMax(chrome, .0f, .2f);

                var scrollDiv = (WebElement)chrome.FindElement(By.XPath(@"//*[@id='bots_inv']"));

                var js = (IJavaScriptExecutor)chrome;

                long prevHeight = 0;
                long currentHeight = 0;

                var i = 0;

                // Scroll down loop
                do
                {
                    var circlePrice = 0.0;

                    #region Scroll Down

                    js.ExecuteScript("document.getElementsByClassName('oitems')[1].scrollBy(0, 5000);");
                    prevHeight = currentHeight;

                    currentHeight = (long)(js.ExecuteScript("return document.getElementsByClassName('oitems')[1].scrollHeight") as long?);

                    #endregion

                    var lootFarmItems = chrome.FindElements(By.XPath(@"//*[@id='bots_inv']/div"));

                    // Items in iter
                    for (; i < lootFarmItems.Count; i++)
                    {
                        var lootFarmItem = lootFarmItems[i];

                        var price = .0;
                        var banDays = 0;
                        var quantity = 0;
                        var stickersQuantity = 0;
                        var classId = String.Empty;

                        var name = lootFarmItem.FindElement(By.ClassName(@"itemblock")).GetAttribute("data-name");
                        var priceText = lootFarmItem.FindElement(By.ClassName(@"it_price")).Text;

                        if (exceptItems.Any(it => name.Contains(it))) continue;

                        #region Price and Quantity

                        if (priceText.Contains("x"))
                        {
                            price = double.Parse(priceText.Split('x')[0].Replace("$", ""));
                            quantity = int.Parse(priceText.Split('x')[1]);
                        }
                        else
                        {
                            price = double.Parse(priceText.Replace("$", ""));
                            quantity = 1;
                        }

                        #endregion

                        #region Ban days

                        try
                        {
                            var banDaysText = lootFarmItem.FindElement(By.ClassName(@"it_tradehold")).Text;

                            banDays = int.Parse(Regex.Match(banDaysText, @"\d").Value);
                        }
                        catch (Exception)
                        {
                            banDays = 0;
                        }

                        #endregion

                        #region Stickers

                        try
                        {
                            var stickerElements = lootFarmItem.FindElement(By.ClassName("it_s")).FindElements(By.XPath(".//*"));
                            stickersQuantity = stickerElements.Count;
                        }
                        catch (Exception)
                        {
                            stickersQuantity = 0;
                        }

                        #endregion

                        #region Find market item

                        ShortItem item = null;
                        var match = marketItems.Where(x => x.Name.Contains(name) && !exceptItems.Any(it => x.Name.Contains(it)));
                        if (match.Count() == 0) continue;
                        else item = match.First();

                        #endregion

                        #region Class Id

                        try
                        {
                            var src = lootFarmItem.FindElement(By.ClassName("it_image")).GetAttribute("src");
                            classId = Regex.Match(src, @"\/730\/(\d+)").Groups[1].Value;
                        }
                        catch (Exception)
                        {
                            throw;
                        }

                        #endregion

                        if (((item.Price + stickersQuantity * stickerMarkup) * 0.95 - price) >= 0)
                        {
                            // If item's available, skip. Otherwise code trade confirmation
                            if (banDays == 0) continue;

                            if (priceList.Find(x => x.Name == name) == null)
                            {
                                priceList.Add(new ShortItem
                                {
                                    Price = item.Price + stickersQuantity * stickerMarkup,
                                    Name = name,
                                    ClassId = classId
                                });
                            }

                            for (int q = 0; q < quantity; q++)
                            {
                                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(lootFarmItem));
                                lootFarmItem.Click();
                                circlePrice += price;
                            }
                        }
                    }

                    #region Complete trade

                    var button = chrome.FindElement(By.Id("tradeButton"));
                    if (button.Text.ToLower().Contains("error")) break;
                    else if (circlePrice != 0)
                    {
                        button.Click();
                        var alertButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.AlertIsPresent());
                        alertButton.Accept();
                        while (!button.Text.ToLower().Contains("completed")) System.Threading.Thread.Sleep(300);
                        button.Click();
                    }

                    #endregion

                } while (prevHeight != currentHeight);

                Console.WriteLine($"Work Time: {(DateTime.Now - startTime).TotalSeconds} seconds");

                #region Save Price List

                prevFileName = SavePriceList(priceList);

                #endregion

                chrome.Navigate().Refresh();

                #region Close notification

                var notificationButton = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.Id("closeSubscribe")));
                if (notificationButton.Displayed && notificationButton.Enabled) notificationButton.Click();

                #endregion
            }
        }

        private static void SetMinMax(ChromeDriver driver, float min, float max)
        {
            // open filters
            driver.FindElement(By.Id("moreSearchBack")).Click();

            // set min filter
            driver.FindElement(By.Id("PriceLowFilter")).SendKeys(min.ToString());
            // set max filter
            driver.FindElement(By.Id("PriceHighFilter")).SendKeys(max.ToString());

            // close filters
            driver.FindElement(By.Id("moreSearchBack")).Click();
        }

        private static string SavePriceList(List<ShortItem> items)
        {
            var rootPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            if (!Directory.Exists(rootPath + "/Temp")) Directory.CreateDirectory("Temp");
            var bf = new BinaryFormatter();

            var fileName = rootPath + "/Temp/prices-" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm") + ".bin";

            using (var file = new FileStream(fileName, FileMode.Create))
            {
                bf.Serialize(file, items);
            }

            return fileName;
        }
    }
}
