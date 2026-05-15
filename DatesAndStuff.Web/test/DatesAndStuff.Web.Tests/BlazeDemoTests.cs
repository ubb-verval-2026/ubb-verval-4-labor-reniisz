using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class BlazeDemoTests
{
    private IWebDriver driver;

    [SetUp]
    public void Setup()
    {
        driver = new ChromeDriver();
    }

    [TearDown]
    public void TearDown()
    {
        driver.Quit();
        driver.Dispose();
    }

    [Test]
    public void BlazeDemo_MexicoCityToDublin_ShouldHaveAtLeastThreeFlights()
    {
        // BlazeDemo oldal megnyitasa
        driver.Navigate().GoToUrl("https://blazedemo.com");

        // Mexico City kivalasztasa
        var fromPort = driver.FindElement(By.Name("fromPort"));
        fromPort.FindElement(
            By.XPath(".//option[text()='Mexico City']")
        ).Click();

        // Dublin kivalasztasa
        var toPort = driver.FindElement(By.Name("toPort"));
        toPort.FindElement(
            By.XPath(".//option[text()='Dublin']")
        ).Click();

        // Submit gomb megnyomasa
        driver.FindElement(
            By.CssSelector("input[type='submit']")
        ).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        // Megvarjuk az eredmenyoldalt
        wait.Until(d => d.Url.Contains("reserve.php"));

        // A jaratokhoz tartozo gombok kigyujtese
        var flights = wait.Until(d =>
        {
            var buttons = d.FindElements(
                By.XPath("//input[@value='Choose This Flight']")
            );

            return buttons.Count > 0 ? buttons : null;
        });

        // Ellenorzes: legalabb 3 jarat van
        flights.Count.Should().BeGreaterThanOrEqualTo(3);
    }
}