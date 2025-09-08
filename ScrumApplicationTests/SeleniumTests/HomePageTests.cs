using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Xunit;
namespace ScrumApplicationTests.SeleniumTests;
public class HomePageTests
{
    private const string baseUrl = "https://localhost:7264/";

    [Fact]
    public void HomePage_Should_Display_Main_Heading()
    {
        using var driver = new ChromeDriver();
        driver.Navigate().GoToUrl(baseUrl);

        var heading = driver.FindElement(By.CssSelector("h1.mb-4"));
        Assert.True(heading.Displayed);
        Assert.Equal("Witaj w ScrumApp", heading.Text);
    }
    [Fact]
    public void HomePage_Should_Show_Task_Button_Disabled_For_Anonymous()
    {
        using var driver = new ChromeDriver();
        driver.Navigate().GoToUrl(baseUrl);

        var tasksButton = driver.FindElement(By.CssSelector("a.btn-primary.disabled"));
        Assert.True(tasksButton.Displayed);
        Assert.Equal("Przejdź do zadań", tasksButton.Text);
    }
    public void LoginAsAdmin(IWebDriver driver)
    {
        driver.Navigate().GoToUrl("https://localhost:7264/Login");

        var emailInput = driver.FindElement(By.Id("Input_Email"));
        var passwordInput = driver.FindElement(By.Id("Input_Password"));
        var submitBtn = driver.FindElement(By.CssSelector("button[type='submit']"));

        emailInput.SendKeys("admin@local.com");
        passwordInput.SendKeys("Admin123!");
        submitBtn.Click();
    }
    public void WaitForElementToBeVisible(IWebDriver driver, By locator, int timeoutInSeconds = 10)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
        wait.Until(drv => drv.FindElement(locator).Displayed);
    }
    [Fact]
    public void HomePage_Should_Display_Admin_Panel_For_Admin()
    {
        using var driver = new ChromeDriver();
        LoginAsAdmin(driver);
        driver.Navigate().GoToUrl("https://localhost:7264");

        // Czekaj aż panel admina się pojawi
        WaitForElementToBeVisible(driver, By.CssSelector(".card-body.text-center h5.card-title"));

        var adminPanel = driver.FindElement(By.CssSelector(".card-body.text-center h5.card-title"));
        Assert.True(adminPanel.Displayed);
        Assert.Equal("Panel Administratora", adminPanel.Text);
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
    public void HomePage_Should_Not_Display_Admin_Panel_For_User()
    {
        using var driver = new ChromeDriver();

        LoginAsUser(driver);
        driver.Navigate().GoToUrl("https://localhost:7264");

        // Poczekaj krótko, żeby upewnić się, że strona jest załadowana
        Thread.Sleep(1000);
        // Sprawdź, czy element panelu admina NIE istnieje lub NIE jest widoczny
        var adminPanels = driver.FindElements(By.CssSelector(".card-body.text-center h5.card-title"));
        Assert.True(adminPanels.Count == 0 || !adminPanels[0].Displayed);
    }
    [Fact]
    public void HomePage_Should_Have_Active_Task_And_Events_Buttons_For_User()
    {
        using var driver = new ChromeDriver();

        LoginAsUser(driver);

        driver.Navigate().GoToUrl("https://localhost:7264");

        // Poczekaj krótko, żeby upewnić się, że strona jest załadowana
        Thread.Sleep(1000);
        // Znajdź przyciski "Przejdź do zadań" i "Przejdź do wydarzeń"
        var tasksButton = driver.FindElement(By.LinkText("Przejdź do zadań"));
        var eventsButton = driver.FindElement(By.LinkText("Przejdź do wydarzeń"));

        Assert.True(tasksButton.Displayed);
        Assert.False(tasksButton.GetAttribute("class").Contains("disabled"));

        Assert.True(eventsButton.Displayed);
        Assert.False(eventsButton.GetAttribute("class").Contains("disabled"));
    }
}
