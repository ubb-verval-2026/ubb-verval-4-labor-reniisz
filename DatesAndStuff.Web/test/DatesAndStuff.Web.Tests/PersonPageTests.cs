using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [TestCase("5", 5250)]
    [TestCase("50", 7500)]
    // fizetesemeles szazaleka, elvart uj fizetes
    public void Person_SalaryIncrease_ShouldIncrease(string percentage, double expectedSalary)
    {
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        //megprobaljuk megkeresni a szazalek input mezot, majd kitolteni a megadott ertekkel
        wait.Until(driver =>
        {
            try
            {
                //input mezo megkeresese
                var input = driver.FindElement(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']"));
                input.Clear();
                input.SendKeys(percentage);
                return true;
            }
            // ha blazer ujrarendereli az oldalt akkor ujraprobaljuk
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        //megprobalok submit gombra kattintani
        wait.Until(driver =>
        {
            try
            {
                driver.FindElement(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")).Click();
                return true;
            }
            // ha az elem idokozben ujrageneralodott, akkor ujraprobaljuk
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        Thread.Sleep(500);

        // megkeressuk a fizetest megjelenito elemet
        var salaryLabel = wait.Until(ExpectedConditions.ElementExists(
            By.XPath("//*[@data-test='DisplayedSalary']")
        ));

        var salaryAfterSubmission = double.Parse(salaryLabel.Text);
        // ellenorizzuk h az ertek megfelelo-e
        salaryAfterSubmission.Should().BeApproximately(expectedSalary, 0.001);
    }

    [Test]
    public void Person_SalaryIncrease_LessThanMinus10_ShouldShowValidationMessages()
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        // ervenytelen ertek megadasa
        wait.Until(driver =>
        {
            try
            {
                var input = driver.FindElement(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']"));
                input.Clear();
                input.SendKeys("-11");
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        // Act
        //submit gomb megnyomasa
        wait.Until(driver =>
        {
            try
            {
                driver.FindElement(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")).Click();
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        // Assert
        //hibauzenetek megkeresese
        var validationSummary = wait.Until(ExpectedConditions.ElementExists(
            By.CssSelector(".validation-errors")
        ));

        var fieldValidationMessage = wait.Until(ExpectedConditions.ElementExists(
            By.CssSelector(".validation-message")
        ));

       //ellen. h megjelennek a hibauzenetek
       validationSummary.Text.Should().NotBeNullOrWhiteSpace();
       fieldValidationMessage.Text.Should().NotBeNullOrWhiteSpace();

       //ellen. h a -10 szabalyhoz kapcsolodnak
       validationSummary.Text.Should().Contain("-10");
       fieldValidationMessage.Text.Should().Contain("-10");
    }

    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}