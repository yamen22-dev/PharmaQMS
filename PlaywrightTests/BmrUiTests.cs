using Microsoft.Playwright.Xunit.v3;
using Microsoft.Playwright;
using System.Text.Json;
using static Microsoft.Playwright.Assertions;

namespace PlaywrightTests;

public sealed class BmrUiTests
{
    private const string DefaultBaseUrl = "http://localhost:4200";
    private const string ProductionAnalystEmail = "production.analyst@pharmaqms.local";
    private const string ProductionAnalystPasswordEnvVar = "PLAYWRIGHT_DEFAULT_USER_PASSWORD";
    private const string SeedPasswordEnvVar = "Seed__DefaultUserPassword";
    private const string ApiUserSecretsId = "5248a472-a5d0-427c-8d84-30fac44dfc98";

    [Fact]
    [Trait("TestId", "UI-Bmr-Create")]
    public async Task CreateBmrViaUiFlow_WithApprovedRecipeAndReleasedLot_ShowsDetailAndOverviewEntry()
    {
        var baseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
            ?? DefaultBaseUrl;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = baseUrl,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();

        await LoginAsProductionAnalystAsync(page);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Authenticated session" })).ToBeVisibleAsync();

        await page.GotoAsync("/bmr/new");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Batch Manufacturing Record aanmaken" })).ToBeVisibleAsync();

        await page.WaitForSelectorAsync("select option");

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

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@".*/bmr/[^/]+$"));

        var batchNumber = (await page.Locator("h1").InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(batchNumber));

        await Expect(page.GetByText("In uitvoering", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText(batchNumber, new() { Exact = true })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Back to overview" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@".*/bmr/?$"));
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = batchNumber })).ToBeVisibleAsync();
    }

    private static async Task LoginAsProductionAnalystAsync(IPage page)
    {
        var productionAnalystPassword = ResolveProductionAnalystPassword();

        await page.GotoAsync("/login");

        await page.GetByLabel("GEBRUIKERSNAAM").FillAsync(ProductionAnalystEmail);
        await page.GetByLabel("WACHTWOORD").FillAsync(productionAnalystPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Aanmelden" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@".*/dashboard/?$"));
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