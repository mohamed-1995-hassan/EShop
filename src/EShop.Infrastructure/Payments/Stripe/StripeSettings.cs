namespace EShop.Infrastructure.Payments.Stripe;

/// <summary>
/// Stripe configuration settings
/// </summary>
public class StripeSettings
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Stripe Secret Key (sk_test_xxx or sk_live_xxx)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe Publishable Key (pk_test_xxx or pk_live_xxx)
    /// For frontend use
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook signing secret (whsec_xxx)
    /// Used to verify webhook signatures
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Default currency (e.g., "usd", "eur", "kwd")
    /// </summary>
    public string DefaultCurrency { get; set; } = "usd";

    /// <summary>
    /// Success URL for Checkout Session (with {CHECKOUT_SESSION_ID} placeholder)
    /// </summary>
    public string SuccessUrl { get; set; } = "https://yoursite.com/payment/success?session_id={CHECKOUT_SESSION_ID}";

    /// <summary>
    /// Cancel URL for Checkout Session
    /// </summary>
    public string CancelUrl { get; set; } = "https://yoursite.com/payment/cancel";

    /// <summary>
    /// Enable automatic tax calculation
    /// </summary>
    public bool AutomaticTax { get; set; } = false;

    /// <summary>
    /// Payment capture method: "automatic" or "manual"
    /// </summary>
    public string CaptureMethod { get; set; } = "automatic";

    /// <summary>
    /// Allowed payment method types for Checkout
    /// </summary>
    public List<string> PaymentMethodTypes { get; set; } = new() { "card" };

    /// <summary>
    /// Statement descriptor shown on customer's card statement
    /// </summary>
    public string? StatementDescriptor { get; set; }

    /// <summary>
    /// Enable 3D Secure for applicable cards
    /// </summary>
    public bool Enable3DSecure { get; set; } = true;
}

/// <summary>
/// Currency configuration for Stripe
/// </summary>
public static class StripeCurrencies
{
    /// <summary>
    /// Zero-decimal currencies (amount = actual value, not cents)
    /// </summary>
    public static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA",
        "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
    };

    /// <summary>
    /// Convert amount to Stripe format (cents for most currencies)
    /// </summary>
    public static long ToStripeAmount(decimal amount, string currency)
    {
        if (ZeroDecimalCurrencies.Contains(currency))
        {
            return (long)Math.Round(amount);
        }
        return (long)Math.Round(amount * 100);
    }

    /// <summary>
    /// Convert Stripe amount back to decimal
    /// </summary>
    public static decimal FromStripeAmount(long amount, string currency)
    {
        if (ZeroDecimalCurrencies.Contains(currency))
        {
            return amount;
        }
        return amount / 100m;
    }
}
