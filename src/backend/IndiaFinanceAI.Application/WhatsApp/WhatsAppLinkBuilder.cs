using System.Text;

namespace IndiaFinanceAI.Application.WhatsApp;

public static class WhatsAppLinkBuilder
{
    public static string? NormalizeIndianMobile(string? mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber)) return null;
        var trimmed = mobileNumber.Trim();
        if (trimmed.Count(character => character == '+') > 1 || (trimmed.Contains('+') && !trimmed.StartsWith('+'))) return null;
        if (mobileNumber.Any(character => !char.IsDigit(character) && character is not '+' and not ' ' and not '-' and not '(' and not ')')) return null;

        var digits = new string(mobileNumber.Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits[0] == '0')
        {
            digits = digits[1..];
        }

        return digits.Length == 10 && digits[0] is >= '6' and <= '9'
            ? $"91{digits}"
            : null;
    }

    public static string? CreateManualChatLink(string? mobileNumber, string? customerName, string? loanType)
    {
        var normalizedNumber = NormalizeIndianMobile(mobileNumber);
        if (normalizedNumber is null) return null;

        var name = CleanText(customerName);
        var loan = CleanText(loanType);
        var message = $"Hello {name}, this is IndiaFinance AI following up on your {loan} enquiry. Please reply here if you would like to continue. Reply STOP if you no longer want WhatsApp follow-up.";
        return $"https://wa.me/{normalizedNumber}?text={Uri.EscapeDataString(message)}";
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "there";
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            if (!char.IsControl(character)) builder.Append(character);
        }
        return builder.ToString();
    }
}
