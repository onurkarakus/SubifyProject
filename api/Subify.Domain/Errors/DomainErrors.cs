using Subify.Domain.Shared;

namespace Subify.Domain.Errors;

/// <summary>
/// Subify OS domain error catalog.
/// No freemium limits, no premium gating, no payment/RevenueCat codes.
/// </summary>
public static class DomainErrors
{
    public static class Auth
    {
        public static readonly Error InvalidCredentials = Error.Unauthorized("AUTH_001", "Invalid Credentials", "Email or password is incorrect.");
        public static readonly Error EmailNotVerified = Error.Unauthorized("AUTH_002", "Email Not Verified", "Please verify your email before logging in.");
        public static readonly Error InvalidToken = Error.Unauthorized("AUTH_003", "Invalid Token", "The access token is invalid or expired.");
        public static readonly Error InvalidRefreshToken = Error.Unauthorized("AUTH_004", "Invalid Refresh Token", "The refresh token is invalid, expired, or revoked.");
        /// <summary>Presented a already-rotated/revoked refresh token (possible theft). User sessions may be bulk-revoked.</summary>
        public static readonly Error RefreshTokenReuseDetected = Error.Unauthorized(
            "AUTH_016",
            "Refresh Token Reuse Detected",
            "This refresh token was already used. Active sessions for this account were revoked. Please log in again.");
        public static readonly Error AccountLocked = Error.Locked("AUTH_005", "Account Locked", "Too many failed attempts. Try again in {minutes} minutes.");
        public static readonly Error PasswordTooWeak = Error.Failure("AUTH_006", "Password Too Weak", "Password must be at least 8 characters with uppercase, lowercase, and number.");
        public static readonly Error InvalidEmailFormat = Error.Failure("AUTH_007", "Invalid Email Format", "The email address provided is not in a valid format.");
        public static readonly Error EmailAlreadyRegistered = Error.Conflict("AUTH_008", "Email Already Registered", "The email address is already associated with another account.");
        public static readonly Error InvalidResetCode = Error.Failure("AUTH_009", "Invalid Reset Code", "The password reset code is invalid or has expired.");
        public static readonly Error InvalidVerificationCode = Error.Failure("AUTH_010", "Invalid Verification Code", "The email verification code is invalid or has expired.");
        public static readonly Error SessionExpired = Error.Unauthorized("AUTH_011", "Session Expired", "Your session has expired. Please log in again.");
        public static readonly Error EmailAlreadyConfirmed = Error.Failure("AUTH_012", "Email Already Confirmed", "The email address has already been confirmed.");
        public static readonly Error EmailNotConfirmed = Error.Failure("AUTH_013", "Email Not Confirmed", "The email address is not confirmed.");
        public static readonly Error RegistrationDisabled = Error.Forbidden("AUTH_014", "Registration Disabled", "Public registration is disabled. Ask an administrator for an invite.");
        public static readonly Error InvalidInviteToken = Error.Failure("AUTH_015", "Invalid Invite Token", "The invite token is invalid or has expired.");
        public static readonly Error SetupRequired = Error.Forbidden(
            "AUTH_017",
            "Setup Required",
            "First-run setup is not complete. Create the Super Admin via POST /api/setup/admin.");
        public static readonly Error SuperAdminAlreadyExists = Error.Conflict(
            "AUTH_018",
            "Super Admin Already Exists",
            "A Super Admin already exists for this instance.");
        public static readonly Error SuperAdminBootstrapRace = Error.Conflict(
            "AUTH_019",
            "Super Admin Bootstrap Race",
            "Another Super Admin was created concurrently. Sign in with the existing Super Admin or use setup status.");
    }

    public static class Setup
    {
        public static readonly Error AlreadyComplete = Error.Conflict(
            "SETUP_001",
            "Setup Already Complete",
            "First-run setup is already finished.");

        public static readonly Error SuperAdminRequired = Error.Failure(
            "SETUP_002",
            "Super Admin Required",
            "Create a Super Admin before completing setup (POST /api/setup/admin).");

        public static readonly Error SettingsNotInitialized = Error.NotFound(
            "SETUP_003",
            "Settings Not Initialized",
            "System settings row is missing. Restart the API so seed can create it.");
    }

    public static class Subscription
    {
        // NOTE: No subscription count limit (legacy SUB_001 / freemium limit removed for Subify OS).

        public static readonly Error SubscriptionNotFound = Error.NotFound("SUB_001", "Subscription Not Found", "The subscription with ID {id} was not found.");
        public static readonly Error SubscriptionAccessDenied = Error.Forbidden("SUB_002", "Subscription Access Denied", "You do not have permission to access this subscription.");
        public static readonly Error InvalidPrice = Error.Failure("SUB_003", "Invalid Price", "The subscription price must be a positive value.");
        public static readonly Error InvalidBillingCycle = Error.Failure("SUB_004", "Invalid Billing Cycle", "The billing cycle must be either 'monthly' or 'yearly'.");
        public static readonly Error InvalidRenewalDate = Error.Failure("SUB_005", "Invalid Renewal Date", "Renewal date must be in the future.");
        public static readonly Error ProviderNotActive = Error.Failure("SUB_006", "Provider Not Active", "The selected provider is no longer active.");
        public static readonly Error CategoryConflict = Error.Failure("SUB_007", "Category Conflict", "Cannot set both category_id and user_category_id.");
        public static readonly Error CategoryNotFound = Error.NotFound("SUB_008", "Category Not Found", "The category with ID {id} was not found.");
        public static readonly Error InvalidSharedCount = Error.Failure("SUB_009", "Invalid Shared Count", "Shared with count must be at least 1.");
    }

    public static class AiErrors
    {
        /// <summary>Instance has no LLM API key in SystemSettings (BYOK). Not a premium plan error.</summary>
        public static readonly Error ApiKeyMissing = Error.ServiceUnavailable(
            "AI_KEY_MISSING",
            "AI API Key Missing",
            "AI is not configured. A Super Admin must set an LLM API key in System Settings.");

        public static readonly Error RateLimitExceededMinute = Error.TooManyRequest("AI_002", "Rate Limit Exceeded (Minute)", "You have exceeded the rate limit of 5 requests per minute.");
        public static readonly Error RateLimitExceededDaily = Error.TooManyRequest("AI_003", "Rate Limit Exceeded (Daily)", "You have exceeded the daily limit of 20 AI requests.");
        public static readonly Error ServiceUnavailable = Error.ServiceUnavailable("AI_004", "AI Service Unavailable", "The AI service is temporarily unavailable.");
        public static readonly Error ProcessingError = Error.Failure("AI_005", "AI Processing Error", "An error occurred while processing your request.");
        public static readonly Error InsufficientData = Error.Failure("AI_006", "Insufficient Data", "You need at least 1 subscription for AI analysis.");
    }

    public static class ProfileErrors
    {
        public static readonly Error ProfileNotFound = Error.NotFound("PRO_001", "Profile Not Found", "User profile not found.");
        public static readonly Error InvalidLocale = Error.Failure("PRO_002", "Invalid Locale", "Locale must be 'tr' or 'en'.");
        public static readonly Error InvalidCurrency = Error.Failure("PRO_003", "Invalid Currency", "Currency must be a valid ISO 4217 code.");
        public static readonly Error InvalidTheme = Error.Failure("PRO_004", "Invalid Theme", "Theme color is not supported.");
        public static readonly Error InvalidBudget = Error.Failure("PRO_005", "Invalid Budget", "Monthly budget must be positive or null.");
        public static readonly Error InvalidDeviceToken = Error.Failure("PRO_006", "Invalid Device Token", "The device token format is invalid.");
    }

    public static class ReportErrors
    {
        // Reports are available to all authenticated users (no premium gate).

        public static readonly Error InvalidDateRange = Error.Failure("REP_001", "Invalid Date Range", "The date range is invalid.");
        public static readonly Error InsufficientData = Error.Failure("REP_002", "Insufficient Data", "Not enough data for the requested report.");
    }

    public static class ResourceErrors
    {
        public static readonly Error ResourceNotFound = Error.NotFound("RES_001", "Resource Not Found", "The requested resource was not found.");
        public static readonly Error ResourceAccessDenied = Error.Forbidden("RES_002", "Resource Access Denied", "You do not have permission to access this resource.");
        public static readonly Error InvalidLanguage = Error.Failure("RES_003", "Invalid Language", "Language code must be 'tr' or 'en'.");
        public static readonly Error ResourceConflict = Error.Conflict("RES_004", "Resource Conflict", "The resource already exists.");
        public static readonly Error InvalidSinceDate = Error.Failure("RES_005", "Invalid Since Date", "The 'since' parameter must be a valid ISO 8601 date.");
    }

    public static class SystemErrors
    {
        public static readonly Error InternalServerError = Error.InternalServerError("SYS_001", "Internal Server Error", "An unexpected error occurred. Please try again, and if the issue persists, contact support.");
        public static readonly Error ServiceUnavailable = Error.ServiceUnavailable("SYS_002", "Service Unavailable", "The service is temporarily unavailable. Please try again later.");
        public static readonly Error GatewayTimeout = Error.GatewayTimeout("SYS_003", "Gateway Timeout", "The request timed out. Please try again.");
        public static readonly Error TooManyRequests = Error.TooManyRequest("SYS_004", "Too Many Requests", "General rate limit exceeded. Please wait.");
    }

    public static class SystemSettingsErrors
    {
        public static readonly Error NotFound = Error.NotFound("SET_001", "Settings Not Found", "System settings have not been initialized.");
        public static readonly Error AccessDenied = Error.Forbidden("SET_002", "Settings Access Denied", "Only Super Admin can manage system settings.");
        public static readonly Error SmtpNotConfigured = Error.Failure("SET_003", "SMTP Not Configured", "SMTP is not configured. Configure it in System Settings to send email.");
        public static readonly Error SmtpTestFailed = Error.Failure("SET_004", "SMTP Test Failed", "Failed to send test email. Check SMTP settings.");
    }

    public static class ValidationErrors
    {
        public static readonly Error ValidationFailed = Error.Validation("VAL_001", "Validation Failed", "One or more validation errors occurred.");
        public static readonly Error RequiredFieldMissing = Error.Validation("VAL_002", "Required Field Missing", "The field '{field}' is required.");
        public static readonly Error InvalidFormat = Error.Validation("VAL_003", "Invalid Format", "The field '{field}' has an invalid format.");
        public static readonly Error MaxLengthExceeded = Error.Validation("VAL_004", "Max Length Exceeded", "The field '{field}' exceeds maximum length of {max}.");
        public static readonly Error MinLengthRequired = Error.Validation("VAL_005", "Min Length Required", "The field '{field}' must be at least {min} characters.");
    }

    public static class UserErrors
    {
        public static readonly Error NotFound = Error.NotFound("USER_001", "User Not Found", "The user was not found.");
        public static readonly Error AccessDenied = Error.Forbidden("USER_002", "User Access Denied", "You do not have permission to access this user.");
        public static readonly Error UnAuthorized = Error.Unauthorized("USER_003", "User Not Authorized", "You must be logged in to access this user.");
        public static readonly Error CannotModifySuperAdmin = Error.Forbidden("USER_004", "Cannot Modify Super Admin", "The Super Admin account cannot be modified this way.");
    }

    public static class CategoryErrors
    {
        public static readonly Error NotFound = Error.NotFound("CAT_001", "Category Not Found", "The category was not found.");
        public static readonly Error CannotDeleteSystemCategory = Error.Forbidden("CAT_002", "Cannot Delete System Category", "System-defined categories cannot be deleted.");
        public static readonly Error HasActiveSubscriptions = Error.Conflict("CAT_003", "Has Active Subscriptions", "Cannot delete a category that has active subscriptions.");
        public static readonly Error DuplicateSlug = Error.Conflict("CAT_004", "Duplicate slug", "A category with this slug already exists.");
    }

    public static class UserCategoryErrors
    {
        public static readonly Error NotFound = Error.NotFound("UCAT_001", "User Category Not Found", "The user category was not found.");
        public static readonly Error AccessDenied = Error.Forbidden("UCAT_002", "User Category Access Denied", "You do not have permission to access this user category.");
        public static readonly Error HasActiveSubscriptions = Error.Conflict("UCAT_003", "Has Active Subscriptions", "Cannot delete a category that has active subscriptions.");
        public static readonly Error DuplicateName = Error.Conflict("UCAT_004", "Duplicate Name", "A user category with this name already exists.");
    }

    public static class ProviderErrors
    {
        public static readonly Error NotFound = Error.NotFound("PROV_001", "Provider Not Found", "The provider was not found.");
        public static readonly Error DuplicateName = Error.Conflict("PROV_002", "Duplicate Name", "A provider with the same name already exists.");
        public static readonly Error DuplicateSlug = Error.Conflict("PROV_003", "Duplicate Slug", "A provider with the same slug already exists.");
        public static readonly Error InactiveProvider = Error.Failure("PROV_004", "Inactive Provider", "The selected provider is not active.");
        public static readonly Error HasActiveSubscriptions = Error.Conflict("PROV_005", "Has Active Subscriptions", "Cannot delete a provider that has active subscriptions.");
    }
}
