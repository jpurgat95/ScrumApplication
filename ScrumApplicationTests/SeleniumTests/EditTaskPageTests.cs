using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrumApplication.Models;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ScrumApplicationTests.SeleniumTests
{
    public class EditTaskPageTests : IClassFixture<TestDependencyInjectionFixture>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ITaskRepository _taskRepository;

        public EditTaskPageTests(TestDependencyInjectionFixture fixture)
        {
            _eventRepository = fixture.ServiceProvider.GetRequiredService<IEventRepository>();
            _taskRepository = fixture.ServiceProvider.GetRequiredService<ITaskRepository>();
        }

        private void LoginAsUser(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://localhost:7264/Login");

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Input_Email")));

            var emailInput = driver.FindElement(By.Id("Input_Email"));
            var passwordInput = driver.FindElement(By.Id("Input_Password"));
            var submitBtn = driver.FindElement(By.CssSelector("button[type='submit']"));

            emailInput.Clear();
            emailInput.SendKeys("kratos@sparta.com");
            passwordInput.Clear();
            passwordInput.SendKeys("Haslo123!");
            submitBtn.Click();

            // Czekaj na zmianę URL, żeby mieć pewność, że logowanie zakończone
            wait.Until(d => d.Url != "https://localhost:7264/Login");
        }

        [Fact]
        public async Task EditTaskPage_Can_Edit_Task_Successfully()
        {
            // Utwórz testowe wydarzenie
            var newEvent = new ScrumEvent
            {
                Title = "Testowe wydarzenie dla zadania",
                Description = "Opis wydarzenia do testu zadania",
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(1).AddHours(2),
                UserId = "bd76f037-c7b5-4e84-ac74-566aa687a23c"
            };
            await _eventRepository.AddEventAsync(newEvent);

            // Utwórz testowe zadanie przypisane do wydarzenia
            var newTask = new TaskItem
            {
                Title = "Testowe zadanie do edycji",
                Description = "Opis zadania do edycji",
                ScrumEventId = newEvent.Id,
                StartDate = DateTime.Now.AddDays(1).AddHours(1),
                EndDate = DateTime.Now.AddDays(1).AddHours(2),
                UserId = "bd76f037-c7b5-4e84-ac74-566aa687a23c"
            };
            await _taskRepository.AddTaskAsync(newTask);

            using var driver = new ChromeDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            try
            {
                LoginAsUser(driver);

                // Przejdź do strony edycji zadania
                driver.Navigate().GoToUrl($"https://localhost:7264/Tasks/Edit/{newTask.Id}");

                // Czekaj na załadowanie formularza
                wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));

                // Edytuj polę tytułu i opisu
                var titleInput = driver.FindElement(By.Id("Title"));
                titleInput.Clear();
                string updatedTitle = "Zmieniony tytuł zadania";
                titleInput.SendKeys(updatedTitle);

                var descriptionInput = driver.FindElement(By.Id("Description"));
                descriptionInput.Clear();
                string updatedDescription = "Zmieniony opis zadania";
                descriptionInput.SendKeys(updatedDescription);

                // Ustaw nowe daty w polach datetime-local przez JS
                DateTime newStartDate = DateTime.Now.AddDays(2).AddHours(9);
                DateTime newEndDate = DateTime.Now.AddDays(2).AddHours(11);

                ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('StartDate').value = '{newStartDate:yyyy-MM-ddTHH:mm}';");
                ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('EndDate').value = '{newEndDate:yyyy-MM-ddTHH:mm}';");

                // Kliknij Zapisz
                var submitBtn = driver.FindElement(By.CssSelector("button.btn-primary[type='submit']"));
                submitBtn.Click();

                // Czekaj na pojawienie się toastu z komunikatem sukcesu
                wait.Until(d =>
                {
                    var toasts = d.FindElements(By.CssSelector(".toast-body"));
                    return toasts.Any(t => t.Text.Contains("zaktualizowane"));
                });

                // Oczekuj przekierowania na listę zadań lub inną stronę
                wait.Until(d => d.Url.EndsWith("/Tasks") || d.Url.Contains("/Tasks/Index"));

                // Sprawdź URL
                Assert.True(driver.Url.EndsWith("/Tasks") || driver.Url.Contains("/Tasks/Index"));
            }
            finally
            {
                // Sprzątnij testowe dane
                await _taskRepository.DeleteTaskAsync(newTask);
                await _eventRepository.DeleteEventAsync(newEvent);

                driver.Quit();
            }
        }
    }
}
