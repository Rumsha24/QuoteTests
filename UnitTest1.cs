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
        private IWebDriver driver;
        private WebDriverWait wait;
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
            // Personal Information
            driver.FindElement(By.Id("firstName")).SendKeys("Rumsha");
            driver.FindElement(By.Id("lastName")).SendKeys("Ahmed");
            driver.FindElement(By.Id("address")).SendKeys("123 Upper James Street");
            driver.FindElement(By.Id("city")).SendKeys("Hamilton");
            driver.FindElement(By.Id("postalCode")).SendKeys("N2L 3G1");
            driver.FindElement(By.Id("phone")).SendKeys("519-555-1234");
            driver.FindElement(By.Id("email")).SendKeys("rum@sha.com");

            // Driving Information
            driver.FindElement(By.Id("age")).SendKeys(age);
            driver.FindElement(By.Id("experience")).SendKeys(experience);
            driver.FindElement(By.Id("accidents")).SendKeys(accidents);

            if (submitForm)
            {
                var submitBtn = driver.FindElement(By.Id("btnSubmit"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", submitBtn);
                submitBtn.Click();
                wait.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("finalQuote")).GetAttribute("value")));
            }
        }

        private string GetValidationMessage(string fieldId)
        {
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

        // Test 1: Valid Data → $5500 Quote
        [Test]
        public void Test1_ValidData_Quote5500()
        {
            FillCompleteForm("24", "3", "0");
            Assert.That(driver.FindElement(By.Id("finalQuote")).GetAttribute("value"),
                Is.EqualTo("$5500"));
        }

        // Test 2: 4 Accidents → Insurance Denied
        [Test]
        public void Test2_InsuranceDenied_4Accidents()
        {
            FillCompleteForm("25", "3", "4");
            Assert.That(driver.FindElement(By.Id("finalQuote")).GetAttribute("value"),
                Is.EqualTo("No Insurance for you!!  Too many accidents - go take a course!"));
        }

        // Test 3: Valid With Discount → $3905 Quote
        [Test]
        public void Test3_ValidWithDiscount_Quote3905()
        {
            FillCompleteForm("35", "9", "2");
            Assert.That(driver.FindElement(By.Id("finalQuote")).GetAttribute("value"),
                Is.EqualTo("$3905"));
        }

        // Test 4: Invalid Phone Number → Error
        [Test]
        public void Test4_InvalidPhoneNumber_Error()
        {
            FillCompleteForm("27", "3", "0", submitForm: false);
            driver.FindElement(By.Id("phone")).Clear();
            driver.FindElement(By.Id("phone")).SendKeys("123");
            driver.FindElement(By.Id("btnSubmit")).Click();
            Assert.That(GetValidationMessage("phone"), Is.Not.Empty);
        }

        // Test 5: Invalid Email → Error
        [Test]
        public void Test5_InvalidEmail_Error()
        {
            FillCompleteForm("28", "3", "0", submitForm: false);
            driver.FindElement(By.Id("email")).Clear();
            driver.FindElement(By.Id("email")).SendKeys("test@");
            driver.FindElement(By.Id("btnSubmit")).Click();
            Assert.That(GetValidationMessage("email"), Is.Not.Empty);
        }

        // Test 6: Invalid Postal Code → Error
        [Test]
        public void Test6_InvalidPostalCode_Error()
        {
            FillCompleteForm("35", "15", "1", submitForm: false);
            driver.FindElement(By.Id("postalCode")).Clear();
            driver.FindElement(By.Id("postalCode")).SendKeys("12345");
            driver.FindElement(By.Id("btnSubmit")).Click();
            Assert.That(GetValidationMessage("postalCode"), Is.Not.Empty);
        }

        // Test 7: Age Omitted → Error
        [Test]
        public void Test7_AgeOmitted_Error()
        {
            FillCompleteForm("30", "5", "0", submitForm: false);
            driver.FindElement(By.Id("age")).Clear();
            driver.FindElement(By.Id("btnSubmit")).Click();
            Assert.That(GetValidationMessage("age"), Is.Not.Empty);
        }

        // Test 8: Accidents Omitted → Error
        [Test]
        public void Test8_AccidentsOmitted_Error()
        {
            FillCompleteForm("37", "8", "0", submitForm: false);
            driver.FindElement(By.Id("accidents")).Clear();
            driver.FindElement(By.Id("btnSubmit")).Click();
            Assert.That(GetValidationMessage("accidents"), Is.Not.Empty);
        }

        // Test 9: Experience Omitted → Error
        [Test]
        public void Test9_ExperienceOmitted_Error()
        {
            FillCompleteForm("45", "10", "0", submitForm: false);
            driver.FindElement(By.Id("experience")).Clear();
            driver.FindElement(By.Id("btnSubmit")).Click();
            Assert.That(GetValidationMessage("experience"), Is.Not.Empty);
        }

        // Test 10: Minimum Age (16) → $7000 Quote
        [Test]
        public void Test10_MinimumAge_Quote7000()
        {
            FillCompleteForm("16", "0", "0");
            Assert.That(driver.FindElement(By.Id("finalQuote")).GetAttribute("value"),
                Is.EqualTo("$7000"));
        }

        // Test 11: Age 30 with 2 Years Exp → $3905 Quote
        [Test]
        public void Test11_Age30_2YearsExp_Quote3905()
        {
            FillCompleteForm("30", "2", "1");
            Assert.That(driver.FindElement(By.Id("finalQuote")).GetAttribute("value"),
                Is.EqualTo("$3905"));
        }

        // Test 12: Max Experience Difference → $2840 Quote
        [Test]
        public void Test12_MaxExperienceDiff_Quote2840()
        {
            FillCompleteForm("45", "29", "1");
            Assert.That(driver.FindElement(By.Id("finalQuote")).GetAttribute("value"),
                Is.EqualTo("$2840"));
        }

        // Test 13: Invalid Age (15) → Error
        [Test]
        public void Test13_InvalidAge15_Error()
        {
            FillCompleteForm("16", "0", "0", submitForm: false);
            driver.FindElement(By.Id("age")).Clear();
            driver.FindElement(By.Id("age")).SendKeys("15");
            driver.FindElement(By.Id("btnSubmit")).Click();
            Assert.That(GetValidationMessage("age"), Is.Not.Empty);
        }

        // Test 14: Invalid Experience → Error
        [Test]
        public void Test14_InvalidExperience_Error()
        {
            // ARRANGE
            FillCompleteForm("20", "0", "0", submitForm: false);

            // Set invalid experience (5 when max should be 4 for age 20)
            var experienceField = wait.Until(d => d.FindElement(By.Id("experience")));
            experienceField.Clear();
            experienceField.SendKeys("5");

            // ACT
            driver.FindElement(By.Id("btnSubmit")).Click();

            // Wait for response
            var finalQuote = wait.Until(d => d.FindElement(By.Id("finalQuote"))).GetAttribute("value");

            // ASSERT
            Assert.That(finalQuote, Is.EqualTo("No Insurance for you!! Driver Age / Experience Not Correct"),
                "Should show specific error message for invalid experience");
        }

        // Test 15: Valid Data → $2840 Quote
        [Test]
        public void Test15_ValidData_Quote2840()
        {
            FillCompleteForm("40", "10", "2");
            Assert.That(driver.FindElement(By.Id("finalQuote")).GetAttribute("value"),
                Is.EqualTo("$2840"));
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
        }
    }
}