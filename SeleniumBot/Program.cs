using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SeleniumBot
{
    public class Action
    {
        public string action { get; set; }
        public string xpath { get; set; }
        public string id { get; set; }
        public string value { get; set; }
        public string description { get; set; }
        public string linkText { get; set; }
        public string url { get; set; }
        public int timespan { get; set; }
        
    }

    class Program
    {
        static bool extended_logging = false;
        static string username = "";
        static string password = "";
        static int minWait = 10;
        static int maxWait = 30;
        
        
        static void Main(string[] args)
        {
            var parameters = ParseArguments(args);

            // Fallback: Environment Variables (TrueNAS Eingabemaske setzt diese automatisch)
            string usernameEnv = Environment.GetEnvironmentVariable("USERNAME");
            string passwordEnv = Environment.GetEnvironmentVariable("PASSWORD");
            string entryIdsEnv = Environment.GetEnvironmentVariable("ENTRY_IDS");
            string minWaitEnv = Environment.GetEnvironmentVariable("MIN_WAIT");
            string maxWaitEnv = Environment.GetEnvironmentVariable("MAX_WAIT");
            string loggingEnv = Environment.GetEnvironmentVariable("LOGGING");

            if (!parameters.ContainsKey("username") && !string.IsNullOrEmpty(usernameEnv))
                parameters["username"] = usernameEnv;

            if (!parameters.ContainsKey("password") && !string.IsNullOrEmpty(passwordEnv))
                parameters["password"] = passwordEnv;

            if (!parameters.ContainsKey("entryIds") && !string.IsNullOrEmpty(entryIdsEnv))
                parameters["entryIds"] = entryIdsEnv;

            if (!parameters.ContainsKey("minWait") && !string.IsNullOrEmpty(minWaitEnv))
                parameters["minWait"] = minWaitEnv;

            if (!parameters.ContainsKey("maxWait") && !string.IsNullOrEmpty(maxWaitEnv))
                parameters["maxWait"] = maxWaitEnv;

            if (!parameters.ContainsKey("logging") && !string.IsNullOrEmpty(loggingEnv))
                parameters["logging"] = loggingEnv;

            // Parameter-Check
            if (!parameters.ContainsKey("username") || !parameters.ContainsKey("password") || !parameters.ContainsKey("entryIds"))
            {
                Console.WriteLine("Fehlende Parameter! Bitte übergeben: --username <user> --password <pass> --entryIds <id1,id2,idx>");
                return;
            }

            username = parameters["username"];
            password = parameters["password"];
            string[] entryIds = parameters["entryIds"].Split(',');

            if (parameters.ContainsKey("logging") && parameters["logging"].ToLower() == "true")
                extended_logging = true;

            if (parameters.ContainsKey("minWait"))
                minWait = Convert.ToInt32(parameters["minWait"]);

            if (parameters.ContainsKey("maxWait"))
                maxWait = Convert.ToInt32(parameters["maxWait"]);

            if (extended_logging)
            {
                Console.WriteLine($"Benutzername: {username}");
                Console.WriteLine($"Entry IDs: {string.Join(", ", entryIds)}");
            }

            while (true)
            {
                foreach (var entryId in entryIds)
                {
                    UpdateEntry(entryId);
                    Wait(minWait, maxWait);
                }
            }
        }

        static Dictionary<string, string> ParseArguments(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    string key = args[i].Substring(2);
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        dict[key] = args[i + 1];
                        i++;
                    }
                    else
                    {
                        dict[key] = "true"; // Für Flags ohne Wert
                    }
                }
            }
            return dict;
        }

        static void UpdateEntry(string entryNumber)
        {
            List<Action> actions = LoadActions("actions.json");
            
            var chromeOptions = new ChromeOptions();
            // Turn off questions for default search engine (not needed for headless start)
            chromeOptions.AddArgument("--disable-search-engine-choice-screen");
            // Open Chrome headless
            chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("--disable-dev-shm-usage");
            // Set browser size shown to page to 1920x1200
            chromeOptions.AddArgument("--windows-size=1920,1200");

            // Make sure that every container uses its own user data profile directory
            chromeOptions.AddArgument($"--user-data-dir=/tmp/chrome-profile-{Guid.NewGuid()}");
            
            // Configure ChromeDriver to redirect output to null
            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.SuppressInitialDiagnosticInformation = true;  // Suppresses startup diagnostics
            chromeDriverService.HideCommandPromptWindow = true;  // Hides the command prompt window

            // Redirect the standard output and error streams
            chromeDriverService.LogPath = "NUL";
            chromeDriverService.EnableAppendLog = true;

            // Initialize the Chrome driver with options
            IWebDriver driver = new ChromeDriver(chromeDriverService, chromeOptions);
            
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            
            Console.WriteLine($"Attempting to update entry {entryNumber} at {DateTime.Now.ToString("hh:mm:ss tt")}"  );
            
            string url = "https://www.wg-gesucht.de/angebot-bearbeiten.html?action=update_offer&offer_id=" + entryNumber;
            driver.Navigate().GoToUrl(url);
            
            foreach (var action in actions)
                {
                    try
                    {
                        IWebElement element = null;
                        if (!string.IsNullOrEmpty(action.xpath))
                        {
                            if (extended_logging)
                            {
                                Console.WriteLine("---------------------------------------------------------");
                                Console.WriteLine($"Attempting to locate element {action.description} by xpath");
                                Console.WriteLine("---------------------------------------------------------");
                            }
                            element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.XPath(action.xpath)));
                        }
                        else if (!string.IsNullOrEmpty(action.id))
                        {
                            if (extended_logging)
                            {
                                Console.WriteLine("---------------------------------------------------------");
                                Console.WriteLine($"Attempting to locate element {action.description} by id");
                                Console.WriteLine("---------------------------------------------------------");
                            }
                            element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id(action.id)));
                        }
                        else if (!string.IsNullOrEmpty(action.linkText))
                        {
                            if (extended_logging)
                            {
                                Console.WriteLine("---------------------------------------------------------");
                                Console.WriteLine($"Attempting to locate element {action.description} by link text");
                                Console.WriteLine("---------------------------------------------------------");
                            }
                            element = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.PartialLinkText(action.linkText)));
                        }

                        if (element != null || action.action == "goto" || action.action == "wait")
                        {
                            switch (action.action)
                            {
                                case "goto":
                                    driver.Navigate().GoToUrl(action.url);
                                    break;
                                case "click":
                                    element.Click();
                                    break;
                                case "send_keys":
                                    if (action.value == "password")
                                    {
                                        element.SendKeys(password);
                                    } else if (action.value == "username")
                                    {
                                        element.SendKeys(username);
                                    }
                                    break;
                                case "wait":
                                    System.Threading.Thread.Sleep(action.timespan);
                                    break;
                            }
                            if (extended_logging)
                            {
                                Console.WriteLine("---------------------------------------------------------");
                                Console.WriteLine($"Performed {action.action} on element {action.description}");
                                Console.WriteLine("---------------------------------------------------------");
                            }
                        }
                        else
                        {
                            if (extended_logging)
                            {
                                Console.WriteLine("---------------------------------------------------------");
                                Console.WriteLine($"Element not found for {action.description}");
                                Console.WriteLine("---------------------------------------------------------");
                            }
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        if (extended_logging)
                        {
                            Console.WriteLine("---------------------------------------------------------");
                            Console.WriteLine($"Element {action.description} not found.");
                            Console.WriteLine("---------------------------------------------------------");
                        } else
                        {
                            Console.WriteLine("Error");
                        }
                    }
                    catch (WebDriverTimeoutException)
                    {
                        if (extended_logging)
                        {
                            Console.WriteLine("---------------------------------------------------------");
                            Console.WriteLine(
                                $"Timeout waiting for element with {(action.xpath != null ? "XPath" : "ID")} {(action.xpath ?? action.id)}.");
                            Console.WriteLine("---------------------------------------------------------");
                        } else
                        {
                            Console.WriteLine("Error");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (extended_logging)
                        {
                            Console.WriteLine("---------------------------------------------------------");
                            Console.WriteLine($"An error occurred: {ex.Message}");
                            Console.WriteLine("---------------------------------------------------------");
                        } else
                        {
                            Console.WriteLine("Error");
                        }
                    }
                }
            
            // Close the browser
            driver.Quit();
        }

        static void Wait(int minDurationInMinutes, int maxDurationInMinutes)
        {
            Random random = new Random();
            // Wait for a random interval between minDuration and maxDuration minutes
            int waitTime = random.Next(minDurationInMinutes, maxDurationInMinutes) * 60000;
            
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine($"Waiting for {waitTime / 60000} minutes before next update.");
            Console.WriteLine("---------------------------------------------------------");
            Thread.Sleep(waitTime);
        }

        static List<Action> LoadActions(string filePath)
        {
            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<List<Action>>(json);
            }
        }
    }
}
