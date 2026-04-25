namespace TabFlow.Shared.Domain;

public static class ErrorCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string SessionExpired = "session_expired";
    public const string TokenUsed = "token_used";
    public const string TokenExpired = "token_expired";
    public const string CheckoutProofMissing = "checkout_proof_missing";
    public const string CheckoutProofInvalid = "checkout_proof_invalid";
    public const string CheckoutProofExpired = "checkout_proof_expired";
    public const string CartEmpty = "cart_empty";
    public const string CatalogStale = "catalog_stale";
    public const string OrderDuplicate = "order_duplicate";
    public const string RateLimited = "rate_limited";
    public const string DeviceAuthFailed = "device_auth_failed";
    public const string DeviceAlreadyConnected = "device_already_connected";
    public const string InternalError = "internal_error";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
}
