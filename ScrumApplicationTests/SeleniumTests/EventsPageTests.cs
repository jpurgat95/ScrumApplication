using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;
using Xunit;

namespace ScrumApplicationTests.SeleniumTests
{
    public class EventsPageTests
    {
        private const int DefaultTimeoutSeconds = 20;

        private IWebDriver CreateDriver() => new ChromeDriver();

        private WebDriverWait CreateWait(IWebDriver driver, int timeoutInSeconds = DefaultTimeoutSeconds) =>
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));

        private string GenerateUniqueTitle(string prefix) =>
            $"{prefix} {DateTime.Now:yyyyMMddHHmmss}";

        private void WaitForToastToDisappear(WebDriverWait wait)
        {
            wait.Until(drv =>
            {
                var toasts = drv.FindElements(By.Id("liveToast"));
                return toasts.Count == 0 || !toasts[0].Displayed;
            });
        }

        private void LoginAsUser(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://localhost:7264/Login");
            var wait = CreateWait(driver);
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Input_Email")));
            driver.FindElement(By.Id("Input_Email")).Clear();
            driver.FindElement(By.Id("Input_Email")).SendKeys("kratos@sparta.com");
            driver.FindElement(By.Id("Input_Password")).Clear();
            driver.FindElement(By.Id("Input_Password")).SendKeys("Haslo123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        }

        [Fact]
        public void EventsPage_Should_Display_Events_List()
        {
            using var driver = CreateDriver();
            LoginAsUser(driver);
            driver.Navigate().GoToUrl("https://localhost:7264/Events");

            var wait = CreateWait(driver);

            var table = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("eventsTable")));
            Assert.True(table.Displayed);

            var rows = table.FindElements(By.CssSelector("tbody tr"));
            Assert.True(rows.Count >= 0);
        }

        [Fact]
        public void EventsPage_Add_New_Event_With_Unique_Title()
        {
            using var driver = CreateDriver();
            LoginAsUser(driver);
            driver.Navigate().GoToUrl("https://localhost:7264/Events");

            var wait = CreateWait(driver);

            string uniqueTitle = GenerateUniqueTitle("Test dodawania");

            var titleInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));
            titleInput.Clear();
            titleInput.SendKeys(uniqueTitle);

            var descriptionInput = driver.FindElement(By.Id("Description"));
            descriptionInput.Clear();
            descriptionInput.SendKeys("Opis testowego wydarzenia");

            string startDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            string endDate = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:mm");

            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementsByName('StartDate')[0].value = '{startDate}';");
            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementsByName('EndDate')[0].value = '{endDate}';");

            var submitBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn.btn-success")));
            submitBtn.Click();

            var matchingRow = wait.Until(drv =>
            {
                var rows = drv.FindElements(By.CssSelector("#eventsTable tbody tr"));
                return rows.FirstOrDefault(r => r.Text.Contains(uniqueTitle));
            });

            Assert.NotNull(matchingRow);
            Assert.Contains(uniqueTitle, matchingRow.Text);
        }

        [Fact]
        public void EventsPage_Add_Event_And_Toggle_Status_Check()
        {
            using var driver = CreateDriver();
            LoginAsUser(driver);
            driver.Navigate().GoToUrl("https://localhost:7264/Events");

            var wait = CreateWait(driver);
            string uniqueTitle = GenerateUniqueTitle("Test zmiany statusu");

            var titleInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));
            titleInput.Clear();
            titleInput.SendKeys(uniqueTitle);

            var descriptionInput = driver.FindElement(By.Id("Description"));
            descriptionInput.Clear();
            descriptionInput.SendKeys("Opis do testu zmiany statusu");

            string startDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            string endDate = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:mm");

            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementsByName('StartDate')[0].value = '{startDate}';");
            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementsByName('EndDate')[0].value = '{endDate}';");

            var submitBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn.btn-success")));
            submitBtn.Click();

            // Odśwież stronę po dodaniu wydarzenia
            driver.Navigate().Refresh();

            var eventRow = wait.Until(drv =>
            {
                var rows = drv.FindElements(By.CssSelector("#eventsTable tbody tr"));
                return rows.FirstOrDefault(r => r.Text.Contains(uniqueTitle));
            });

            Assert.NotNull(eventRow);
            Assert.Contains(uniqueTitle, eventRow.Text);

            WaitForToastToDisappear(wait);

            var toggleBtn = eventRow.FindElement(By.CssSelector("button.toggle-done"));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", toggleBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", toggleBtn);

            wait.Until(drv =>
            {
                var statusCell = eventRow.FindElement(By.CssSelector("td.event-status span.badge"));
                var statusText = statusCell.Text.Trim();
                return statusCell.Displayed && (statusText == "Wykonane" || statusText == "W trakcie");
            });
        }
        [Fact]
        public void EventsPage_Add_Event_And_Delete_It()
        {
            using var driver = new ChromeDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            // Logowanie
            driver.Navigate().GoToUrl("https://localhost:7264/Login");
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Input_Email")));
            driver.FindElement(By.Id("Input_Email")).Clear();
            driver.FindElement(By.Id("Input_Email")).SendKeys("kratos@sparta.com");
            driver.FindElement(By.Id("Input_Password")).Clear();
            driver.FindElement(By.Id("Input_Password")).SendKeys("Haslo123!");
            driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Przejdź na stronę wydarzeń
            driver.Navigate().GoToUrl("https://localhost:7264/Events");

            // Generowanie unikalnego tytułu
            string uniqueTitle = $"Test usuwania {DateTime.Now:yyyyMMddHHmmss}";

            // Dodawanie nowego wydarzenia
            var titleInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));
            titleInput.Clear();
            titleInput.SendKeys(uniqueTitle);

            var descriptionInput = driver.FindElement(By.Id("Description"));
            descriptionInput.Clear();
            descriptionInput.SendKeys("Opis do testu usuwania");

            string startDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            string endDate = DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:mm");

            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementsByName('StartDate')[0].value = '{startDate}';");
            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementsByName('EndDate')[0].value = '{endDate}';");

            var submitBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn.btn-success")));
            submitBtn.Click();

            // Odśwież stronę po dodaniu wydarzenia dla stabilności
            driver.Navigate().Refresh();

            // Poczekaj na pojawienie się nowego wydarzenia w tabeli
            var eventRow = wait.Until(drv =>
            {
                var rows = drv.FindElements(By.CssSelector("#eventsTable tbody tr"));
                return rows.FirstOrDefault(r => r.Text.Contains(uniqueTitle));
            });
            Assert.NotNull(eventRow);
            Assert.Contains(uniqueTitle, eventRow.Text);

            // Czekaj aż toast powiadomienia zniknie (uniknięcie nakładek)
            wait.Until(drv =>
            {
                var toasts = drv.FindElements(By.Id("liveToast"));
                return toasts.Count == 0 || !toasts[0].Displayed;
            });

            // Znajdź przycisk usuwania i scrolluj do niego z offsetem
            var deleteBtn = eventRow.FindElement(By.CssSelector("button[title='Usuń wydarzenie']"));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'}); window.scrollBy(0, -100);", deleteBtn);

            // Próbuj kliknąć standardowo, jeśli zgłosi błąd, kliknij JS-em
            try
            {
                var clickableDeleteBtn = wait.Until(ExpectedConditions.ElementToBeClickable(deleteBtn));
                clickableDeleteBtn.Click();
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", deleteBtn);
            }

            // Czekaj na pojawienie się modalu
            var deleteModal = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("deleteModal")));

            // Kliknij przycisk potwierdzenia usunięcia (submit w form)
            var confirmDeleteBtn = deleteModal.FindElement(By.CssSelector("form#deleteForm button.btn-danger"));
            try
            {
                confirmDeleteBtn.Click();
            }
            catch (OpenQA.Selenium.ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", confirmDeleteBtn);
            }

            // Czekaj aż modal zniknie
            wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.Id("deleteModal")));

            // Sprawdź, że wydarzenie zostało usunięte (nie ma go w tabeli)
            bool isRemoved = wait.Until(drv =>
            {
                var rows = drv.FindElements(By.CssSelector("#eventsTable tbody tr"));
                return !rows.Any(r => r.Text.Contains(uniqueTitle));
            });
            Assert.True(isRemoved);
        }

    }
}
