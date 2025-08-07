using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace InsuranceQuoteTests
{
    [TestFixture]
    public class InsuranceQuoteTests : IDisposable
    {
        private IWebDriver? driver;
        private WebDriverWait? wait;
        private bool disposed = false;

        [SetUp]
        public void Setup()
        {
            // Initialize Chrome with options
            var options = new ChromeOptions();
            options.AddArguments("--start-maximized", "--disable-notifications");
            driver = new ChromeDriver(options);

            // Configure timeouts
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            // Navigate to application
            driver.Navigate().GoToUrl("http://localhost/prog8170a04/getQuote.html");
            wait.Until(d => d.FindElement(By.Id("btnSubmit")).Displayed);
        }

        private void FillCompleteForm(string age, string experience, string accidents, bool submitForm = true)
        {
            if (driver == null) throw new InvalidOperationException("Driver not initialized");

            // Personal Information
            driver.FindElement(By.Id("firstName")).SendKeys("Test");
            driver.FindElement(By.Id("lastName")).SendKeys("User");
            driver.FindElement(By.Id("address")).SendKeys("123 Test Street");
            driver.FindElement(By.Id("city")).SendKeys("Waterloo");
            driver.FindElement(By.Id("postalCode")).SendKeys("N2L 3G1");
            driver.FindElement(By.Id("phone")).SendKeys("519-555-1234");
            driver.FindElement(By.Id("email")).SendKeys("test@example.com");

            // Driving Information
            driver.FindElement(By.Id("age")).SendKeys(age);
            driver.FindElement(By.Id("experience")).SendKeys(experience);
            driver.FindElement(By.Id("accidents")).SendKeys(accidents);

            if (submitForm)
            {
                var submitBtn = driver.FindElement(By.Id("btnSubmit"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", submitBtn);
                submitBtn.Click();
                wait?.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("finalQuote")).GetAttribute("value")));
            }
        }

        private string GetValidationMessage(string fieldId)
        {
            if (driver == null) throw new InvalidOperationException("Driver not initialized");

            try
            {
                // Check HTML5 validation first
                var field = driver.FindElement(By.Id(fieldId));
                var html5Message = field?.GetAttribute("validationMessage") ?? string.Empty;
                if (!string.IsNullOrEmpty(html5Message)) return html5Message;

                // Check for common validation message patterns
                string[] validationSelectors = {
                    $"[data-valmsg-for='{fieldId}']",
                    $"#{fieldId}-error",
                    $"#{fieldId} + .validation-message",
                    $"#{fieldId} ~ .error-message"
                };

                foreach (var selector in validationSelectors)
                {
                    try
                    {
                        var msgElement = driver.FindElement(By.CssSelector(selector));
                        if (msgElement != null && !string.IsNullOrEmpty(msgElement.Text))
                            return msgElement.Text;
                    }
                    catch (NoSuchElementException) { }
                }

                // Check if field is marked with error/invalid class
                var classAttribute = field?.GetAttribute("class") ?? string.Empty;
                if (classAttribute.Contains("error") || classAttribute.Contains("invalid"))
                    return "Invalid field (marked with error class)";

                return string.Empty;
            }
            catch (Exception ex) when (ex is NoSuchElementException || ex is NullReferenceException)
            {
                return string.Empty;
            }
        }

        // ============ TEST CASES ============

        // Test 1 - Valid data gets $5500 quote
        [Test]
        public void Test1_ValidData_Quote5500()
        {
            // Arrange
            string expectedQuote = "$5500";

            // Act
            FillCompleteForm("24", "3", "0");
            string actualQuote = driver?.FindElement(By.Id("finalQuote")).GetAttribute("value") ?? string.Empty;

            // Assert
            Assert.That(actualQuote, Is.EqualTo(expectedQuote));
        }


        
        [TearDown]
        public void Teardown()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    driver?.Quit();
                    driver?.Dispose();
                }
                catch { }
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}