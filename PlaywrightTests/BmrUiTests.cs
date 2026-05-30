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

        await Expect(page.GetByText("In progress", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 3000 });
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
        await page.GetByLabel("USERNAME").FillAsync(ProductionAnalystEmail);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(page.GetByText("Password is required.", new() { Exact = true })).ToBeVisibleAsync();
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
            ["Raw Materials & Lots", "Quality Control", "Audit Trail"]);

        var managerPage = await CreatePageAsync();
        await LoginAsync(managerPage, QaManagerEmail);
        await AssertNavigationAsync(
            managerPage,
            ["Dashboard", "Raw Materials & Lots", "BMR", "Quality Control", "Audit Trail"],
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
        await page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Steps", RegexOptions.IgnoreCase) }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Execute production step", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();

        await ExecuteFirstOpenStepAsync(page);
        await Expect(page.GetByText("Awaiting verification", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Verify", RegexOptions.IgnoreCase) })).ToHaveCountAsync(0);

        var qaPage = await CreatePageAsync();
        await LoginAsync(qaPage, QaManagerEmail);
        await qaPage.GotoAsync($"/bmr/{bmrId}/steps");
        await Expect(qaPage.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("Verify", RegexOptions.IgnoreCase) })).ToBeVisibleAsync();

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
        await Expect(analystPage.GetByRole(AriaRole.Link, new() { Name = "Verify" })).ToHaveCountAsync(0);

        var qaPage = await CreatePageAsync();
        await LoginAsync(qaPage, QaManagerEmail);
        await qaPage.GotoAsync($"/bmr/{bmrId}/steps");

        await Expect(qaPage.GetByRole(AriaRole.Link, new() { Name = "Verify" })).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("TestId", "UI-NFR02-01")]
    public async Task DashboardShowsSessionLifetimeNote()
    {
        var page = await CreatePageAsync();

        await LoginAsync(page, ProductionAnalystEmail);
        await Expect(page.GetByText("Session expires automatically after 15 minutes of inactivity.", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("Access token expires at", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("Refresh token expires at", new() { Exact = true })).ToBeVisibleAsync();
    }

    private static async Task<(IPage Page, string BatchNumber)> CreateBmrAsync(string userEmail)
    {
        var page = await CreatePageAsync();
        await LoginAsync(page, userEmail);

        await page.GotoAsync("/bmr/new");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Create Batch Manufacturing Record" })).ToBeVisibleAsync(new() { Timeout = 5000 });
        var createHeading = page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Create Batch Manufacturing Record"
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

        await page.GetByLabel("Master recipe").SelectOptionAsync(new SelectOptionValue
        {
            Label = "AMOX-500MG-CAP",
        });

        await page.GetByLabel("Batch size (kg)").FillAsync("250");
        await page.GetByLabel("Production line").SelectOptionAsync(new SelectOptionValue
        {
            Label = "Lijn A",
        });

        var lotCheckbox = page.Locator("input[type='checkbox']").First;
        await Expect(lotCheckbox).ToBeVisibleAsync();
        await lotCheckbox.CheckAsync();

        var createButton = page.GetByRole(AriaRole.Button, new() { Name = "Create & start BMR" });
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

        await page.GetByLabel("USERNAME").FillAsync(email);
        await page.GetByLabel("PASSWORD").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

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
        var executeButton = page.GetByRole(AriaRole.Button, new() { Name = "Execute" }).First;

        await Expect(executeButton).ToBeVisibleAsync();

        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        await executeButton.ClickAsync();
        await page.GetByLabel("TEMPERATURE (°C)").FillAsync("42");
        await page.GetByLabel("DURATION (MIN)").FillAsync("15");
        await page.GetByLabel("REMARK").FillAsync("Playwright execution");

        await page.Locator("#confirm-execute-button").ClickAsync();
        // await page.GetByRole(AriaRole.Button, new() { Name = "Execute" }).ClickAsync();
        await Expect(page.GetByText("Awaiting verification", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    private static async Task VerifyFirstAwaitingStepAsync(IPage page)
    {
        var verificationLink = page.GetByRole(AriaRole.Link, new() { Name = "Verify" }).First;
        await Expect(verificationLink).ToBeVisibleAsync();

        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        await verificationLink.ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Four‑eyes verification —" })).ToBeVisibleAsync();
        await page.GetByLabel("ELECTRONIC SIGNATURE (PASSWORD)").FillAsync(ResolveProductionAnalystPassword());
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirm verification" }).ClickAsync();
        await Expect(page.GetByText("Verification succeeded.", new() { Exact = true })).ToBeVisibleAsync();
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