using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WesternUnionAutomationTask.Rpa.Pages;

[DebuggerStepThrough]
internal abstract class BrowserPage
{
    protected BrowserPage(IWebDriver driver, int waitSeconds = 12)
    {
        Driver = driver;
        Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Math.Clamp(waitSeconds, 3, 30)))
        {
            PollingInterval = TimeSpan.FromMilliseconds(120)
        };
        Wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    protected IWebDriver Driver { get; }
    protected WebDriverWait Wait { get; }

    protected IWebElement WaitFor(By locator)
    {
        return Wait.Until(driver => TryGetVisibleEnabledElement(driver, locator));
    }

    protected bool IsPresent(By locator)
    {
        try
        {
            return Driver.FindElements(locator).Count > 0;
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    protected string TryText(By locator)
    {
        try
        {
            var element = Driver.FindElements(locator).FirstOrDefault(e => e.Displayed);
            return element?.Text.Trim() ?? string.Empty;
        }
        catch (WebDriverException)
        {
            return string.Empty;
        }
    }

    protected void NavigateTo(string url)
    {
        Driver.Navigate().GoToUrl(url);
        WaitForPageReady();
    }

    protected void WaitForPageReady()
    {
        if (Driver is not IJavaScriptExecutor js)
        {
            return;
        }

        Wait.Until(_ =>
        {
            var state = js.ExecuteScript("return document.readyState")?.ToString();
            return string.Equals(state, "interactive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "complete", StringComparison.OrdinalIgnoreCase);
        });
    }

    protected void Type(By locator, string value)
    {
        Wait.Until(driver =>
        {
            var element = TryGetVisibleEnabledElement(driver, locator);
            if (element is null)
            {
                return false;
            }

            element.Clear();
            element.SendKeys(value ?? string.Empty);
            return true;
        });
    }

    protected void Click(By locator)
    {
        Wait.Until(driver =>
        {
            var element = TryGetVisibleEnabledElement(driver, locator);
            if (element is null)
            {
                return false;
            }

            ScrollIntoView(element);
            SafeClick(element);
            return true;
        });

        WaitForPageReady();
    }

    protected void SubmitForm(By submitButtonLocator)
    {
        Wait.Until(driver =>
        {
            var button = TryGetVisibleEnabledElement(driver, submitButtonLocator);
            if (button is null)
            {
                return false;
            }

            ScrollIntoView(button);
            SafeClick(button);
            return true;
        });

        WaitForPageReady();
    }

    protected string Text(By locator)
    {
        return Wait.Until(driver =>
        {
            var element = TryGetVisibleElement(driver, locator);
            return element?.Text.Trim();
        }) ?? string.Empty;
    }

    protected string BodyText()
    {
        return Text(By.TagName("body"));
    }

    private static IWebElement? TryGetVisibleEnabledElement(ISearchContext context, By locator)
    {
        var element = TryGetVisibleElement(context, locator);
        return element is not null && element.Enabled ? element : null;
    }

    private static IWebElement? TryGetVisibleElement(ISearchContext context, By locator)
    {
        var elements = context.FindElements(locator);
        foreach (var element in elements)
        {
            if (element.Displayed)
            {
                return element;
            }
        }

        return null;
    }

    private void ScrollIntoView(IWebElement element)
    {
        if (Driver is IJavaScriptExecutor js)
        {
            js.ExecuteScript("arguments[0].scrollIntoView({ block: 'center', inline: 'nearest' });", element);
        }
    }

    private void SafeClick(IWebElement element)
    {
        if (Driver is IJavaScriptExecutor js)
        {
            js.ExecuteScript("arguments[0].click();", element);
            return;
        }

        element.Click();
    }
}
