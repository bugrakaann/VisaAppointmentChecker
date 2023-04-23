using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Timers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AisUsVisa
{
    public class VisaScrapper
    {
        //Telegram
        private const string botToken = "<botToken>";
        private const string group = "<telegramchatID>";
        private const string myself = "<telegramchatID>";
        
        private static Timer timer;
        private static Timer megaTimer;
        private static int delayInMS = 50000;
        Stopwatch stopwatch = new Stopwatch();

        private const string loginURL = "https://ais.usvisa-info.com/tr-tr/niv/users/sign_in";
        //change userID
        private const string appointmentsURL = "https://ais.usvisa-info.com/tr-tr/niv/schedule/<userID>/payment";
        private const string _mail = "<mail>";
        private const string _password = "<password>";

        private const int sendNotifNum = 30;
        private static int sentNotifs = 0;
        IWebDriver driver;

        public VisaScrapper()
        {
            driver = new ChromeDriver();
            TelegramSendMessage("System Initialized " + DateTime.Now, myself);
        }
        
        public void Login()
        {
            driver.Navigate().GoToUrl(loginURL);
            IWebElement email = driver.FindElement(By.Name("user[email]"));
            IWebElement password = driver.FindElement(By.Name("user[password]"));
            
            email.Clear();
            email.SendKeys(_mail);
            password.Clear();
            password.SendKeys(_password);

            IWebElement checkbox = driver.FindElement(By.XPath("//*[@id=\"sign_in_form\"]/div[3]/label/div"));
            checkbox.Click();
            IWebElement loginButton = driver.FindElement(By.Name("commit"));
            loginButton.Click();
            Console.WriteLine(driver.Title);
        }

        private void CheckAppointments(object source, ElapsedEventArgs e)
        {
            driver.Navigate().GoToUrl(appointmentsURL);
            
            if (!CheckSession())
            {
                StopChecking();
                Login();
                StartChecking();
                Console.WriteLine("Refreshed session");
            }
            IWebElement ankara =
                driver.FindElement(By.XPath("//*[@id=\"paymentOptions\"]/div[2]/table/tbody/tr[1]/td[2]"));
            IWebElement istanbul =
                driver.FindElement(By.XPath("//*[@id=\"paymentOptions\"]/div[2]/table/tbody/tr[2]/td[2]"));
            IWebElement description =
                driver.FindElement(By.XPath("//*[@id=\"paymentOptions\"]/div[1]/div[2]/h3[2]"));
            

            if (!ankara.Text.Equals("Uygun Randevu Yok") || !istanbul.Text.Equals("Uygun Randevu Yok")
                                                         || !description.Text.Equals("Şu anda uygun randevu yok. Lütfen birkaç gün sonra tekrar kontrol edin, Konsolosluk Bölümü yeni randevular açmış olacaktır."))
            {
                TelegramSendMessage("Güncel Randevular\n" +
                                    "Ankara: " + ankara.Text + "\n" +
                                    "Istanbul: " + istanbul.Text + "\n" +
                                    description.Text,group);
                SendMegaAlarm();
            }
            
            stopwatch.Stop();
            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds} ms] Checked appointment dates");
            stopwatch.Restart();
        }

        bool CheckSession()
        {
            try
            {
                IWebElement alert = driver.FindElement(By.XPath("//*[@id=\"flash_messages\"]/div"));
                if (alert.Text.Equals("You need to sign in or sign up before continuing."))
                {
                    return false;
                }

                return false;
            }
            catch (NoSuchElementException)
            {
                return true;
            }
            catch (Exception e)
            {
                TelegramSendMessage(e.Message,group);
                return false;
            }
        }

        public void StartChecking()
        {
            Random random = new Random();
            int interval = random.Next(delayInMS / 2, delayInMS);
            
            timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler(CheckAppointments);
            timer.Elapsed += new ElapsedEventHandler(ChangeTimerInterval);
            timer.Interval = interval;
            timer.Enabled = true;
        }

        private void StopChecking()
        {
            timer.Close();
            driver.Quit();
            driver = new ChromeDriver();
            TelegramSendMessage("Bot relogin" + DateTime.Now,myself);
        }

        private void ChangeTimerInterval(object source, ElapsedEventArgs e)
        {
            System.Random rnd = new Random();
            var timer = source as Timer;
            timer.Interval = rnd.Next(delayInMS / 2, delayInMS);
        }
        
        public static void TelegramSendMessage(string text,string destID)
        {
            try
                {
                    ServicePointManager.Expect100Continue = true;                
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                        
                    string urlString = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={destID}&text={text}";

                    WebClient webclient = new WebClient();
                        
                    webclient.DownloadString(urlString);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Couldnt sent notification message to chatID: {destID}");
                    Console.WriteLine($"\n {e.Message}");
                    Console.WriteLine();
                }
        }
        
        private void SendMegaNotification(object o, ElapsedEventArgs e)
        {
            Timer timer1 = o as Timer;
            sentNotifs++;
            TelegramSendMessage("RANDEVULAR AÇILDI!!!!!",group);
            if (sentNotifs == sendNotifNum)
            {
                timer1.Enabled = false;
                timer1.Close();
                timer1 = null;
                sentNotifs = 0;
            }
        }

        public void SendMegaAlarm()
        {
            megaTimer = new Timer();
            megaTimer.Elapsed += new ElapsedEventHandler(SendMegaNotification);
            megaTimer.Interval = 5454;
            megaTimer.Enabled = true;
        }
    }
}