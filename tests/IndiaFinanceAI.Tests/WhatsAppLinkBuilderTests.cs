using IndiaFinanceAI.Application.WhatsApp;

namespace IndiaFinanceAI.Tests;

public sealed class WhatsAppLinkBuilderTests
{
    [Theory]
    [InlineData("9876543210", "919876543210")]
    [InlineData("+91 98765-43210", "919876543210")]
    [InlineData("09876543210", "919876543210")]
    public void NormalizeIndianMobile_AddsCountryCode(string input, string expected)
    {
        Assert.Equal(expected, WhatsAppLinkBuilder.NormalizeIndianMobile(input));
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("+1 202 555 0100")]
    [InlineData("98765abc210")]
    [InlineData("+91+9876543210")]
    public void NormalizeIndianMobile_RejectsInvalidNumbers(string input)
    {
        Assert.Null(WhatsAppLinkBuilder.NormalizeIndianMobile(input));
    }

    [Fact]
    public void ManualLink_UsesWhatsAppNumberAndEncodesProfessionalMessage()
    {
        var link = WhatsAppLinkBuilder.CreateManualChatLink(
            "+91 98765 43210",
            "Fictional Customer",
            "Home Loan");

        Assert.NotNull(link);
        Assert.StartsWith("https://wa.me/919876543210?text=", link, StringComparison.Ordinal);
        var encodedMessage = link[(link.IndexOf("text=", StringComparison.Ordinal) + 5)..];
        var message = Uri.UnescapeDataString(encodedMessage);
        Assert.Contains("Fictional Customer", message, StringComparison.Ordinal);
        Assert.Contains("Home Loan enquiry", message, StringComparison.Ordinal);
        Assert.Contains("Reply STOP", message, StringComparison.Ordinal);
    }
}
