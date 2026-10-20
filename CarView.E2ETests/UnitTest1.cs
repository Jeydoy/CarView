using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CarView.E2ETests
{
    [TestClass]
    public class SimpleCrudE2ETests : PageTest
    {
        private const string BaseUrl = "https://localhost:44342";


        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            };
        }

        [TestMethod]
        public async Task Should_Successfully_Load_Catalog_And_Open_Details()
        {
            
            await Page.GotoAsync($"{BaseUrl}/Cars");
            await Page.Locator("a[href*='/Cars/Details']").First.ClickAsync();
            await Expect(Page).ToHaveURLAsync(new Regex($"{BaseUrl}/Cars/Details"));
        }
    }
}