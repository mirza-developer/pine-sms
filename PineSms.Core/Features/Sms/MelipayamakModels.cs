using System.Text.Json.Serialization;

namespace PineSms.Core.Features.Sms;

/// <summary>JSON shape returned by the Melipayamak bulk-send API.</summary>
public class MelipayamakSendApiResponse
{
    [JsonPropertyName("recIds")]
    public long[]? RecIds { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>JSON shape returned by the Melipayamak delivery-status API.</summary>
public class MelipayamakStatusApiResponse
{
    [JsonPropertyName("results")]
    public string[]? Results { get; set; }

    [JsonPropertyName("resultsAsCode")]
    public int[]? ResultsAsCode { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Structured result of a single call to the Melipayamak send API.
/// <para>
/// <see cref="Submitted"/> is <c>true</c> when the provider accepted the request
/// (HTTP 2xx, no exception). The provider will then attempt delivery; call
/// <c>GetSmsDeliveryStatus</c> with the <see cref="Recipients"/> recIds to track progress.
/// </para>
/// </summary>
public class MelipayamakSendResult
{
    /// <summary>True when the HTTP call succeeded and the provider accepted the batch.</summary>
    public bool Submitted { get; set; }

    /// <summary>Per-recipient records with the provider-assigned tracking ID.</summary>
    public List<SmsRecipientRecord> Recipients { get; set; } = new();

    /// <summary>Status message returned by the provider (may describe errors).</summary>
    public string ProviderStatus { get; set; } = string.Empty;

    /// <summary>Exception or HTTP error message when <see cref="Submitted"/> is false.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>Maps a phone number to the provider's per-message tracking ID (recId).</summary>
public class SmsRecipientRecord
{
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Provider-assigned tracking ID. <c>null</c> when submission failed or the provider
    /// did not return a recId for this position.
    /// </summary>
    public long? RecId { get; set; }
}

/// <summary>Result of querying delivery status for one or more messages.</summary>
public class GetDeliveryStatusResult
{
    public bool Success { get; set; }
    public string ProviderStatus { get; set; } = string.Empty;
    public List<RecIdStatus> Statuses { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>Delivery status for a single recId.</summary>
public class RecIdStatus
{
    public long RecId { get; set; }

    /// <summary>Human-readable status text, e.g. "ارسال شده".</summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>Numeric status code returned by the provider.</summary>
    public int ResultCode { get; set; }
}
