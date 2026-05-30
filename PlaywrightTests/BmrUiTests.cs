using Microsoft.Playwright.Xunit.v3;
using Microsoft.Playwright;
using System.Text.Json;
using static Microsoft.Playwright.Assertions;
using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog;

namespace PlaywrightTests;

[Collection("ApiServer")]
public sealed class BmrUiTests
{
    private const string DefaultBaseUrl = "http://localhost:4200";
    private const string QaManagerEmail = "qa.manager@pharmaqms.local";
    private const string ProductionAnalystEmail = "production.analyst@pharmaqms.local";
    private const string ProductionAnalystPasswordEnvVar = "PLAYWRIGHT_DEFAULT_USER_PASSWORD";
    private const string SeedPasswordEnvVar = "Seed__DefaultUserPassword";
    private const string ApiUserSecretsId = "5248a472-a5d0-427c-8d84-30fac44dfc98";

    [Fact]
    [Trait("TestId", "UI-Bmr-Create")]
    public async Task CreateBmrViaUiFlow_WithApprovedRecipeAndReleasedLot_ShowsDetailAndOverviewEntry()
    {
        var (page, batchNumber) = await CreateBmrAsync(ProductionAnalystEmail);

        await Expect(page.GetByText("In uitvoering", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 3000 });
        Console.WriteLine($"Created BMR with batch number: {batchNumber}");
        await Expect(page.Locator("main h1")).ToContainTextAsync(batchNumber,
            new() { Timeout = 3000, UseInnerText = true }
        );
        await page.GetByRole(AriaRole.Button, new() { Name = "Back to overview" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex(@".*/bmr$"));
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = batchNumber })).ToBeVisibleAsync(new() { Timeout = 3000 });
    }

    [Fact]
    [Trait("TestId", "UI-UC01-01")]
    public async Task LoginFormShowsPasswordValidationWhenPasswordIsEmpty()
    {
        var page = await CreatePageAsync();

        await page.GotoAsync("/login");
        await page.GetByLabel("GEBRUIKERSNAAM").FillAsync(ProductionAnalystEmail);
        await page.GetByRole(AriaRole.Button, new() { Name = "Aanmelden" }).ClickAsync();

        await Expect(page.GetByText("Wachtwoord is vereist.", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("TestId", "UI-UC01-01")]
    public async Task NavigationMenuShowsOnlyAllowedPagesPerRole()
    {
        var analystPage = await CreatePageAsync();
        await LoginAsync(analystPage, ProductionAnalystEmail);
        await AssertNavigationAsync(
            analystPage,
            ["Dashboard", "BMR"],
            ["Grondstoffen & Lots", "Quality Control", "Audit Trail"]);

        var managerPage = await CreatePageAsync();
        await LoginAsync(managerPage, QaManagerEmail);
        await AssertNavigationAsync(
            managerPage,
            ["Dashboard", "Grondstoffen & Lots", "BMR", "Quality Control", "Audit Trail"],
            Array.Empty<string>());
    }

    [Fact]
    [Trait("TestId", "UI-UC03-01")]
    public async Task BmrDetailFlowExecutesFirstStepAndShowsUpdatedProgress()
    {
        var (page, batchNumber) = await CreateBmrAsync(ProductionAnalystEmail);
        var bmrId = ExtractIdFromCurrentUrl(page.Url, "/bmr/");

        await page.GotoAsync($"/bmr/{bmrId}");
        await Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex(batchNumber, RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Stappenplan", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Productiestap uitvoeren", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();

        await ExecuteFirstOpenStepAsync(page);
        await Expect(page.GetByText("AwaitingVerification", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Verification", RegexOptions.IgnoreCase) })).ToHaveCountAsync(0);

        var qaPage = await CreatePageAsync();
        await LoginAsync(qaPage, QaManagerEmail);
        await qaPage.GotoAsync($"/bmr/{bmrId}/steps");
        await Expect(qaPage.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Verification", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();

        await VerifyFirstAwaitingStepAsync(qaPage);

        await qaPage.GotoAsync($"/bmr/{bmrId}");
        await Expect(qaPage.GetByRole(AriaRole.Heading, new() { Name = batchNumber })).ToBeVisibleAsync();
        await Expect(qaPage.GetByText("1 / 6", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("TestId", "UI-UC03-01")]
    public async Task VerificationButtonIsVisibleForQaManagerAndHiddenForProductionAnalyst()
    {
        var (analystPage, _) = await CreateBmrAsync(ProductionAnalystEmail);
        var bmrId = ExtractIdFromCurrentUrl(analystPage.Url, "/bmr/");

        await analystPage.GotoAsync($"/bmr/{bmrId}/steps");
        await ExecuteFirstOpenStepAsync(analystPage);
        await Expect(analystPage.GetByRole(AriaRole.Link, new() { Name = "Verification" })).ToHaveCountAsync(0);

        var qaPage = await CreatePageAsync();
        await LoginAsync(qaPage, QaManagerEmail);
        await qaPage.GotoAsync($"/bmr/{bmrId}/steps");

        await Expect(qaPage.GetByRole(AriaRole.Link, new() { Name = "Verification" })).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("TestId", "UI-NFR02-01")]
    public async Task DashboardShowsSessionLifetimeNote()
    {
        var page = await CreatePageAsync();

        await LoginAsync(page, ProductionAnalystEmail);
        await Expect(page.GetByText("Sessie verloopt automatisch na 15 minuten inactiviteit.", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("Access token expires at", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("Refresh token expires at", new() { Exact = true })).ToBeVisibleAsync();
    }

    private static async Task<(IPage Page, string BatchNumber)> CreateBmrAsync(string userEmail)
    {
        var page = await CreatePageAsync();
        await LoginAsync(page, userEmail);

        await page.GotoAsync("/bmr/new");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Batch Manufacturing Record aanmaken" })).ToBeVisibleAsync(new() { Timeout = 5000 });
        var createHeading = page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Batch Manufacturing Record aanmaken"
        });

        try
        {
            await createHeading.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        }
        catch (PlaywrightException)
        {
            await page.GotoAsync("/bmr");
            var newBmrButton = page.GetByRole(AriaRole.Button, new() { Name = "New BMR" });
            await Expect(newBmrButton).ToBeVisibleAsync();
            await newBmrButton.ClickAsync();
            await createHeading.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        }

        await page.GetByLabel("Masterrecept").SelectOptionAsync(new SelectOptionValue
        {
            Label = "AMOX-500MG-CAP",
        });

        await page.GetByLabel("Batchgrootte (kg)").FillAsync("250");
        await page.GetByLabel("Productielijn").SelectOptionAsync(new SelectOptionValue
        {
            Label = "Lijn A",
        });

        var lotCheckbox = page.Locator("input[type='checkbox']").First;
        await Expect(lotCheckbox).ToBeVisibleAsync();
        await lotCheckbox.CheckAsync();

        var createButton = page.GetByRole(AriaRole.Button, new() { Name = "BMR aanmaken & starten" });
        await Expect(createButton).ToBeEnabledAsync();
        await createButton.ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex(@".*/bmr/[^/]+$"));

        await Expect(page.Locator("main h1"))
    .ToContainTextAsync(new Regex(@"BATCH-\d{8}-[A-F0-9]+"), new() { Timeout = 10000 });

        var batchNumber = (await page.Locator("main h1").InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(batchNumber));

        return (page, batchNumber);
    }

    private static async Task<IPage> CreatePageAsync()
    {
        var baseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
            ?? DefaultBaseUrl;

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 200,
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = baseUrl,
            IgnoreHTTPSErrors = true,
        });

        return await context.NewPageAsync();
    }

    private static async Task LoginAsync(IPage page, string email)
    {
        var password = ResolveProductionAnalystPassword();

        await page.GotoAsync("/login");

        await page.GetByLabel("GEBRUIKERSNAAM").FillAsync(email);
        await page.GetByLabel("WACHTWOORD").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Aanmelden" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex(@".*/dashboard/?$"));
    }

    private static async Task AssertNavigationAsync(
        IPage page,
        IEnumerable<string> visibleLabels,
        IEnumerable<string> hiddenLabels)
    {
        var nav = page.Locator(".sidebar-nav");

        foreach (var label in visibleLabels)
        {
            await Expect(nav.GetByRole(AriaRole.Link, new() { Name = label })).ToBeVisibleAsync();
        }

        foreach (var label in hiddenLabels)
        {
            await Expect(nav.GetByRole(AriaRole.Link, new() { Name = label })).ToHaveCountAsync(0);
        }
    }

    private static async Task ExecuteFirstOpenStepAsync(IPage page)
    {
        var executeButton = page.GetByRole(AriaRole.Button, new() { Name = "Execution" }).First;
        await Expect(executeButton).ToBeVisibleAsync();

        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        await executeButton.ClickAsync();
        await page.GetByLabel("TEMPERATUUR (°C)").FillAsync("42");
        await page.GetByLabel("DUUR (MIN)").FillAsync("15");
        await page.GetByLabel("OPMERKING").FillAsync("Playwright execution");

        await page.GetByRole(AriaRole.Button, new() { Name = "Uitvoeren" }).ClickAsync();
        await Expect(page.GetByText("AwaitingVerification", new() { Exact = true })).ToBeVisibleAsync( new() { Timeout = 5000 });
    }

    private static async Task VerifyFirstAwaitingStepAsync(IPage page)
    {
        var verificationLink = page.GetByRole(AriaRole.Link, new() { Name = "Verification" }).First;
        await Expect(verificationLink).ToBeVisibleAsync();

        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        await verificationLink.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Four‑eyes verification —" })).ToBeVisibleAsync();
        await page.GetByLabel("ELECTRONIC SIGNATURE (PASSWORD)").FillAsync(ResolveProductionAnalystPassword());
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirm verification" }).ClickAsync();
        await Expect(page.GetByText("Verificatie geslaagd.", new() { Exact = true })).ToBeVisibleAsync();
    }

    private static string ExtractIdFromCurrentUrl(string url, string segment)
    {
        var markerIndex = url.LastIndexOf(segment, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException($"Could not extract id from '{url}'.");
        }

        var id = url[(markerIndex + segment.Length)..].TrimEnd('/');
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException($"Could not extract id from '{url}'.");
        }

        return id;
    }

    private static string ResolveProductionAnalystPassword()
    {
        var password = Environment.GetEnvironmentVariable(ProductionAnalystPasswordEnvVar)
            ?? Environment.GetEnvironmentVariable(SeedPasswordEnvVar)
            ?? ReadPasswordFromApiUserSecrets();

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Set {ProductionAnalystPasswordEnvVar} or Seed__DefaultUserPassword, or configure the API user-secrets value Seed:DefaultUserPassword.");
        }

        return password;
    }

    private static string? ReadPasswordFromApiUserSecrets()
    {
        try
        {
            var userSecretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft",
                "UserSecrets",
                ApiUserSecretsId,
                "secrets.json");

            if (!File.Exists(userSecretsPath))
            {
                return null;
            }

            using var stream = File.OpenRead(userSecretsPath);
            using var document = JsonDocument.Parse(stream);

            if (document.RootElement.TryGetProperty("Seed:DefaultUserPassword", out var passwordProperty))
            {
                return passwordProperty.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}