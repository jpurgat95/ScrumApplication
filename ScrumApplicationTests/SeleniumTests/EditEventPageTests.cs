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
namespace ScrumApplicationTests.SeleniumTests;
public class EditEventPageTests : IClassFixture<TestDependencyInjectionFixture>
{
    private readonly IEventRepository _eventRepository;

    public EditEventPageTests (TestDependencyInjectionFixture fixture)
    {
        _eventRepository = fixture.ServiceProvider.GetRequiredService<IEventRepository>();
    }
    public void LoginAsUser(IWebDriver driver)
    {
        driver.Navigate().GoToUrl("https://localhost:7264/Login");

        var emailInput = driver.FindElement(By.Id("Input_Email"));
        var passwordInput = driver.FindElement(By.Id("Input_Password"));
        var submitBtn = driver.FindElement(By.CssSelector("button[type='submit']"));

        emailInput.SendKeys("kratos@sparta.com");
        passwordInput.SendKeys("Haslo123!");
        submitBtn.Click();
    }
    [Fact]
    public async Task EditEventPage_Can_Edit_Event_Successfully()
    {
        // Programowo utworzenie testowego wydarzenia
        var newEvent = new ScrumEvent
        {
            Title = "Testowy tytuł do edycji",
            Description = "Opis do edycji",
            StartDate = DateTime.Now.AddDays(1).Date.AddHours(10),
            EndDate = DateTime.Now.AddDays(1).Date.AddHours(12),
            UserId = "bd76f037-c7b5-4e84-ac74-566aa687a23c" // Twój testowy UserId
        };

        await _eventRepository.AddEventAsync(newEvent); // Dodaj event do bazy

        using var driver = new ChromeDriver();
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        try
        {
            // Logowanie użytkownika - napisz kod logowania odpowiadający Twojej aplikacji
            LoginAsUser(driver);

            // Przejdź do strony edycji eventu
            driver.Navigate().GoToUrl($"https://localhost:7264/Events/Edit/{newEvent.Id}");

            // Czekaj na załadowanie formularza edycji
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("Title")));

            // Edytuj pola formularza
            var titleInput = driver.FindElement(By.Id("Title"));
            titleInput.Clear();
            string updatedTitle = "Zmieniony tytuł wydarzenia";
            titleInput.SendKeys(updatedTitle);

            var descriptionInput = driver.FindElement(By.Id("Description"));
            descriptionInput.Clear();
            string updatedDescription = "Zmieniony opis wydarzenia";
            descriptionInput.SendKeys(updatedDescription);

            // Ustaw nowe daty w polach datetime-local za pomocą JS
            DateTime newStartDate = DateTime.Now.AddDays(2).Date.AddHours(9);
            DateTime newEndDate = DateTime.Now.AddDays(2).Date.AddHours(11);

            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('StartDate').value = '{newStartDate:yyyy-MM-ddTHH:mm}';");
            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('EndDate').value = '{newEndDate:yyyy-MM-ddTHH:mm}';");

            // Kliknij przycisk "Zapisz"
            var submitButton = driver.FindElement(By.CssSelector("button.btn-primary[type='submit']"));
            submitButton.Click();

            // Oczekuj przekierowania na stronę listy wydarzeń
            wait.Until(d => d.Url.EndsWith("/Events"));
            // Czekaj na pojawienie się toasta z komunikatem sukcesu
            wait.Until(d =>
            {
                var toasts = d.FindElements(By.CssSelector(".toast-body"));
                return toasts.Any(t => t.Text.Contains("zaktualizowane"));
            });
            // Sprawdzenie, czy URL jest poprawny
            Assert.EndsWith("/Events", driver.Url);
        }
        finally
        {
            // Usuwamy testowe wydarzenie po wykonaniu testu
            await _eventRepository.DeleteEventAsync(newEvent);

            // Zamykamy przeglądarkę
            driver.Quit();
        }
    }

}
