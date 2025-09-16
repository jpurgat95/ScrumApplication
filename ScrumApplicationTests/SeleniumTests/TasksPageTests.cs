using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrumApplication.Models;
using SeleniumExtras.WaitHelpers;
using System;
using Xunit;

namespace ScrumApplicationTests.SeleniumTests
{
    public class TasksPageTests : IClassFixture<TestDependencyInjectionFixture>
    {
        private readonly IEventRepository _eventRepository;

        public TasksPageTests(TestDependencyInjectionFixture fixture)
        {
            _eventRepository = fixture.ServiceProvider.GetRequiredService<IEventRepository>();
        }
        private IWebDriver CreateDriver() => new ChromeDriver();

        private WebDriverWait CreateWait(IWebDriver driver, int timeoutSeconds = 15) =>
            new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
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

            var emailInput = driver.FindElement(By.Id("Input_Email"));
            emailInput.Clear();
            emailInput.SendKeys("kratos@sparta.com");

            var passwordInput = driver.FindElement(By.Id("Input_Password"));
            passwordInput.Clear();
            passwordInput.SendKeys("Haslo123!");

            var submitBtn = driver.FindElement(By.CssSelector("button[type='submit']"));
            submitBtn.Click();
        }

        [Fact]
        public void TasksPage_Should_Display_Tasks_List_For_LoggedUser()
        {
            using var driver = CreateDriver();
            LoginAsUser(driver);

            driver.Navigate().GoToUrl("https://localhost:7264/Tasks");

            var wait = CreateWait(driver);

            // Czekamy aż tabela z zadaniami będzie widoczna
            var tasksTable = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("tasksTable")));
            Assert.True(tasksTable.Displayed);

            // Sprawdzamy liczbę wierszy (może być 0, gdy baza pusta, ale tabela istnieje)
            var rows = tasksTable.FindElements(By.CssSelector("tbody tr"));
            Assert.NotNull(rows);
        }
        [Fact]
        public async Task TasksPage_Add_New_Task_With_Unique_Title()
        {
            // Utworzenie wydarzenia programowo bezpośrednio przez repozytorium
            var newEvent = new ScrumEvent
            {
                Title = "Testowe wydarzenie dodawania zadania",
                Description = "Wydarzenie utworzone do testu dodawania zadania",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(2),
                UserId = "bd76f037-c7b5-4e84-ac74-566aa687a23c"
            };
            await _eventRepository.AddEventAsync(newEvent);
            int eventId = newEvent.Id;

                using var driver = new ChromeDriver();
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

                // Logowanie
                LoginAsUser(driver);

                driver.Navigate().GoToUrl("https://localhost:7264/Tasks");

                // Odśwież stronę, aby lista eventów zawierała nowo dodane
                driver.Navigate().Refresh();

                string uniqueTaskTitle = GenerateUniqueTitle("Test dodawania");
                string taskDescription = "Opis testowego zadania.";

                var titleInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));
                titleInput.Clear();
                titleInput.SendKeys(uniqueTaskTitle);

                var descriptionInput = driver.FindElement(By.Id("Description"));
                descriptionInput.Clear();
                descriptionInput.SendKeys(taskDescription);

                var eventSelect = driver.FindElement(By.Id("EventId"));
                var options = eventSelect.FindElements(By.TagName("option"));
                var eventOption = options.FirstOrDefault(o => o.GetAttribute("value") == eventId.ToString());
                Assert.NotNull(eventOption);
                eventOption.Click();

                DateTime startDate = DateTime.Now.AddMinutes(5);
                DateTime endDate = DateTime.Now.AddHours(1);

                ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('StartDate').value = '{startDate:yyyy-MM-ddTHH:mm}';");
                ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('EndDate').value = '{endDate:yyyy-MM-ddTHH:mm}';");

                var submitBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn-success")));
                submitBtn.Click();

                WaitForToastToDisappear(wait);

                driver.Navigate().Refresh();

                var taskRow = wait.Until(drv =>
                {
                    var rows = drv.FindElements(By.CssSelector("#tasksTable tbody tr"));
                    return rows.FirstOrDefault(r => r.Text.Contains(uniqueTaskTitle));
                });

                Assert.NotNull(taskRow);
                Assert.Contains(uniqueTaskTitle, taskRow.Text);
        }
        [Fact]
        public async Task TasksPage_Add_Task_And_Toggle_Status_Check()
        {
            var newEvent = new ScrumEvent
            {
                Title = "Testowe wydarzenie zmiany statusu zadania",
                Description = "Wydarzenie do testu zmiany statusu",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(2),
                UserId = "bd76f037-c7b5-4e84-ac74-566aa687a23c"
            };
            await _eventRepository.AddEventAsync(newEvent);
            int eventId = newEvent.Id;

            using var driver = new ChromeDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            // Logowanie użytkownika
            LoginAsUser(driver);

            driver.Navigate().GoToUrl("https://localhost:7264/Tasks");
            wait.Until(drv => ((IJavaScriptExecutor)drv).ExecuteScript("return document.readyState").ToString() == "complete");

            string uniqueTaskTitle = GenerateUniqueTitle("Test zmiany statusu zadania");
            string taskDescription = "Opis do testu przełączania statusu";

            var titleInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));
            titleInput.Clear();
            titleInput.SendKeys(uniqueTaskTitle);

            var descriptionInput = driver.FindElement(By.Id("Description"));
            descriptionInput.Clear();
            descriptionInput.SendKeys(taskDescription);

            var eventSelect = driver.FindElement(By.Id("EventId"));
            var options = eventSelect.FindElements(By.TagName("option"));
            var eventOption = options.FirstOrDefault(o => o.GetAttribute("value") == eventId.ToString());
            Assert.NotNull(eventOption);
            eventOption.Click();

            DateTime startDate = DateTime.Now.AddMinutes(5);
            DateTime endDate = DateTime.Now.AddHours(1);
            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('StartDate').value = '{startDate:yyyy-MM-ddTHH:mm}';");
            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('EndDate').value = '{endDate:yyyy-MM-ddTHH:mm}';");

            var submitBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn-success")));
            submitBtn.Click();

            WaitForToastToDisappear(wait);

            driver.Navigate().Refresh();
            wait.Until(drv => ((IJavaScriptExecutor)drv).ExecuteScript("return document.readyState").ToString() == "complete");

            IWebElement taskRow = null;
            taskRow = wait.Until(drv =>
            {
                var rows = drv.FindElements(By.CssSelector("#tasksTable tbody tr"));
                var row = rows.FirstOrDefault(r => r.Text.Contains(uniqueTaskTitle));
                return row != null && row.Displayed ? row : null;
            });
            Assert.NotNull(taskRow);

            var toggleBtn = taskRow.FindElement(By.CssSelector("button.toggle-done-task"));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", toggleBtn);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", toggleBtn);

            // Po kliknięciu ponownie pobierz taskRow, bo DOM się zmienił
            taskRow = wait.Until(drv =>
            {
                var rows = drv.FindElements(By.CssSelector("#tasksTable tbody tr"));
                var row = rows.FirstOrDefault(r => r.Text.Contains(uniqueTaskTitle));
                return row != null && row.Displayed ? row : null;
            });

            var statusChanged = wait.Until(drv =>
            {
                var statusSpan = taskRow.FindElement(By.CssSelector("td.task-status span.badge"));
                string statusText = statusSpan.Text.Trim();
                return statusText == "Wykonane" || statusText == "W trakcie";
            });

            Assert.True(statusChanged);
        }

        [Fact]
        public async Task TasksPage_Add_Task_And_Delete_It()
        {
            // Programowe utworzenie wydarzenia
            var newEvent = new ScrumEvent
            {
                Title = "Testowe wydarzenie do zadania do usunięcia",
                Description = "Wydarzenie do testu usuwania zadania",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(2),
                UserId = "bd76f037-c7b5-4e84-ac74-566aa687a23c" // Twój testowy UserId
            };
            await _eventRepository.AddEventAsync(newEvent);
            int eventId = newEvent.Id;

            using var driver = new ChromeDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            try
            {
                // Logowanie użytkownika
                LoginAsUser(driver);

                driver.Navigate().GoToUrl("https://localhost:7264/Tasks");
                driver.Navigate().Refresh();

                string uniqueTaskTitle = GenerateUniqueTitle("ZadanieDoUsuniecia");
                string taskDescription = "Opis testowego zadania do usunięcia";

                // Wypełnianie formularza zadania
                var titleInput = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));
                titleInput.Clear();
                titleInput.SendKeys(uniqueTaskTitle);

                var descriptionInput = driver.FindElement(By.Id("Description"));
                descriptionInput.Clear();
                descriptionInput.SendKeys(taskDescription);

                var eventSelect = driver.FindElement(By.Id("EventId"));
                var options = eventSelect.FindElements(By.TagName("option"));
                var eventOption = options.FirstOrDefault(o => o.GetAttribute("value") == eventId.ToString());
                Assert.NotNull(eventOption);
                eventOption.Click();

                DateTime startDate = DateTime.Now.AddMinutes(5);
                DateTime endDate = DateTime.Now.AddHours(1);

                ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('StartDate').value = '{startDate:yyyy-MM-ddTHH:mm}';");
                ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('EndDate').value = '{endDate:yyyy-MM-ddTHH:mm}';");

                var submitBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn-success")));
                submitBtn.Click();

                WaitForToastToDisappear(wait);

                driver.Navigate().Refresh();

                // Znalezienie wiersza zadania
                var taskRow = wait.Until(drv =>
                {
                    var rows = drv.FindElements(By.CssSelector("#tasksTable tbody tr"));
                    return rows.FirstOrDefault(r => r.Text.Contains(uniqueTaskTitle));
                });
                Assert.NotNull(taskRow);

                // Znalezienie i kliknięcie przycisku usuń zadanie
                var deleteBtn = taskRow.FindElement(By.CssSelector("button[title='Usuń zadanie']"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", deleteBtn);
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", deleteBtn);

                // Potwierdzenie usunięcia w modalnym oknie
                var deleteModal = wait.Until(ExpectedConditions.ElementIsVisible(By.Id("deleteModal")));
                var confirmDeleteBtn = deleteModal.FindElement(By.CssSelector("button.btn-sm.btn-danger[type='submit']"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", confirmDeleteBtn);

                WaitForToastToDisappear(wait);

                // Weryfikacja, że zadanie zniknęło z tabeli
                bool taskDeleted = wait.Until(drv =>
                {
                    var rows = drv.FindElements(By.CssSelector("#tasksTable tbody tr"));
                    return rows.All(r => !r.Text.Contains(uniqueTaskTitle));
                });

                Assert.True(taskDeleted);
            }
            finally
            {
                // Usunięcie wydarzenia po teście
                await _eventRepository.DeleteEventAsync(newEvent);
            }
        }

    }
}
