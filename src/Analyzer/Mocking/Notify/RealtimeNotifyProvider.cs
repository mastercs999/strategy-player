using Common;
using Common.Loggers;
using Common.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Notify
{
    public class RealtimeNotifyProvider : INotifyProvider
    {
        private string PhoneNumber;
        private string Email;
        private ILogger _____________________________________________________________________________Logger;




        public RealtimeNotifyProvider(string phoneNumber, string email, ILogger logger)
        {
            PhoneNumber = phoneNumber;
            Email = email;
            _____________________________________________________________________________Logger = logger;
        }



        
        public void SendSms(string message)
        {
            try
            {
                _____________________________________________________________________________Logger.Info($"Sending SMS to {PhoneNumber} with message: {message}");
                SendSms(PhoneNumber, message);
                _____________________________________________________________________________Logger.Info("SMS was sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sending sms error: " + ex.Message);
                _____________________________________________________________________________Logger.Error(ex);
            }
        }

        public void SendMail(string subject, string text)
        {
            try
            {
                _____________________________________________________________________________Logger.Info($"Sending email to {Email} with subject {subject} and text: {text}");
                SendMail(Email, subject, text);
                _____________________________________________________________________________Logger.Info("Email was sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sending mail error: " + ex.Message);
                _____________________________________________________________________________Logger.Error(ex);
            }
        }




        private void SendMail(string email, string subject, string text)
        {
            using (SmtpClient client = new SmtpClient())
            using (MailMessage mail = new MailMessage(new MailAddress("", "Trading admin"), new MailAddress(email)))
            {
                client.Port = 587;
                client.EnableSsl = true;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential("", "");
                client.Host = "smtp.gmail.com";
                client.SendCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                        throw e.Error;
                };

                mail.Subject = subject;
                mail.Body = text;

                client.Send(mail);
            }
        }

        private void SendSms(string number, string message)
        {
            // Waiting for JS end execution
            int waitForJsDelay = 5000;

            // Go to the executable directory
            string currentDirectory = Directory.GetCurrentDirectory();
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(assemblyDirectory);

            // Go to O2
            try
            {
                // Prepare profile - this should prevent showing Firefox has stopped working at the end
                FirefoxProfile profile = new FirefoxProfile();
                profile.SetPreference("browser.tabs.remote.autostart.2", false);

                // Start browser
                using (IWebDriver driver = new FirefoxDriver(profile))
                {
                    driver.Url = "http://sms.1188.cz/";

                    // Peform login
                    Thread.Sleep(waitForJsDelay);
                    if (driver.FindElements(By.CssSelector("a.login")).Count > 0)
                    {
                        driver.FindElement(By.CssSelector("a.login")).Click();
                        driver.FindElement(By.Name("user_session[login]")).SendKeys("");
                        driver.FindElement(By.Name("user_session[password]")).SendKeys("");
                        driver.FindElement(By.CssSelector("form.login input[type=submit]")).Click();
                    }
                    else if (!driver.FindElements(By.CssSelector(".MainMenu ")).First().Text.ToLowerInvariant().Contains(""))
                        throw new InvalidOperationException("Unknown 1188 state");

                    // Send SMS
                    Thread.Sleep(waitForJsDelay);
                    driver.FindElement(By.Name("sms[phone_numbers][]")).SendKeys(number);
                    driver.FindElement(By.Name("sms[text]")).SendKeys(message.Substring(0, Math.Min(message.Length, 159)));
                    driver.FindElement(By.CssSelector("#sms_form input[type=submit]")).Click();

                    // Wait till end
                    Thread.Sleep(waitForJsDelay);
                }
            }
            catch (Exception ex)
            {
                ex.Rethrow();
            }
            finally
            {
                // Go back
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }
    }
}
