using Subify.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subify.Domain.Errors;

public static class DomainErrors
{
    public static class Auth
    {
        public static readonly Error InvalidCredentials = Error.Unauthorized("AUTH_001", "Invalid Credentials", "Email or password is incorrect.");
        public static readonly Error EmailNotVerified = Error.Unauthorized("AUTH_002", "Email Not Verified", "Please verify your email before logging in.");
        public static readonly Error InvalidToken = Error.Unauthorized("AUTH_003", "Invalid Token", "The access token is invalid or expired.");
        public static readonly Error InvalidRefreshToken = Error.Unauthorized("AUTH_004", "Invalid Refresh Token", "The refresh token is invalid, expired, or revoked.");
        public static readonly Error AccountLocked = Error.Locked("AUTH_005", "Account Locked", "Too many failed attempts. Try again in {minutes} minutes.");
        public static readonly Error PasswordTooWeak = Error.Failure("AUTH_006", "Password Too Weak", "Password must be at least 8 characters with uppercase, lowercase, and number.");
        public static readonly Error InvalidEmailFormat = Error.Failure("AUTH_007", "Invalid Email Format", "The email address provided is not in a valid format.");
        public static readonly Error EmailAlreadyRegistered = Error.Conflict("AUTH_008", "Email Already Registered", "The email address is already associated with another account.");
        public static readonly Error InvalidResetCode = Error.Failure("AUTH_009", "Invalid Reset Code", "The password reset code is invalid or has expired.");
        public static readonly Error InvalidVerificationCode = Error.Failure("AUTH_010", "Invalid Verification Code", "The email verification code is invalid or has expired.");    
        public static readonly Error SessionExpired = Error.Unauthorized("AUTH_011", "Session Expired", "Your session has expired. Please log in again.");
    }

    public static class Subscription
    {
        public static readonly Error SubscriptionLimitReached = Error.Forbidden("SUBS_001", "Subscription Limit Reached", "Free plan allows maximum 3 subscriptions. Upgrade to premium.");
        public static readonly Error SubscriptionNotFound = Error.NotFound("SUBS_002", "Subscription Not Found", "The subscription with ID {id} was not found.");
        public static readonly Error SubscriptionAccessDenied = Error.Forbidden("SUBS_003", "Subscription Access Denied", "You do not have permission to access this subscription.");
        public static readonly Error InvalidPrice = Error.Failure("SUBS_004", "Invalid Price", "The subscription price must be a positive value.");
        public static readonly Error InvalidBillingCycle = Error.Failure("SUBS_005", "Invalid Billing Cycle", "The billing cycle must be either 'Monthly' or 'Yearly'.");
        public static readonly Error InvalidRenewalDate = Error.Failure("SUBS_006", "Invalid Renewal Date", "Renewal date must be in the future.");
        public static readonly Error ProviderNotActive = Error.Failure("SUBS_007", "Provider Not Active", "The selected provider is no longer active.");
        public static readonly Error CategoryConflict = Error.Failure("SUBS_008", "Category Conflict", "Cannot set both category_id and user_category_id.");
        public static readonly Error CategoryNotFound = Error.NotFound("SUBS_009", "Category Not Found", "The category with ID {id} was not found.");
        public static readonly Error InvalidSharedCount = Error.Failure("SUBS_010", "Invalid Shared Count", "Shared with count must be at least 1.");
    }

    public static class AiErrors
    {
        public static readonly Error PremiumRequired = Error.Forbidden("AI_001", "Premium Required", "AI suggestions require a premium subscription.");
        public static readonly Error RateLimitExceededMinute = Error.TooManyRequest("AI_002", "Rate Limit Exceeded (Minute)", "You have exceeded the rate limit of 5 requests per minute.");
        public static readonly Error RateLimitExceededDaily = Error.Failure("AI_003", "Rate Limit Exceeded (Daily)", "You have exceeded the daily limit of 20 AI requests.");
        public static readonly Error ServiceUnavailable = Error.ServiceUnavailable("AI_004", "AI Service Unavailable", "The AI service is temporarily unavailable.");
        public static readonly Error ProcessingError = Error.Failure("AI_005", "AI Processing Error", "An error occurred while processing your request.");
        public static readonly Error InsufficientData = Error.Failure("AI_006", "Insufficient Data", "You need at least 1 subscription for AI analysis.");
    }

    public static class PaymnetErrors
    {
        public static readonly Error InvalidPlan = Error.Failure("PAY_001", "Invalid Plan", "The selected plan is not valid.");
        public static readonly Error CheckoutCreationFailed = Error.Failure("PAY_002", "Checkout Creation Failed", "Failed to create checkout session.");
        public static readonly Error AlreadyPremium = Error.Failure("PAY_003", "Already Premium", "You already have an active premium subscription.");
        public static readonly Error PaymentProcessingFailed = Error.Failure("PAY_004", "Payment Processing Failed", "Payment could not be processed.");
        public static readonly Error InvalidWebhook = Error.Failure("PAY_005", "Invalid Webhook", "Invalid webhook signature.");
        public static readonly Error SessionNotFound = Error.NotFound("PAY_006", "Session Not Found", "Billing session not found.");
    }

    public static class ProfileErrors
    {
        public static readonly Error ProfileNotFound = Error.NotFound("PRO_001", "Profile Not Found", "User profile not found.");
        public static readonly Error InvalidLocale = Error.Failure("PRO_002", "Invalid Locale", "Locale must be 'tr' or 'en'.");
        public static readonly Error InvalidCurrency = Error.Failure("PRO_003", "Invalid Currency", "Currency must be a valid ISO 4217 code.");
        public static readonly Error InvalidTheme = Error.Failure("PRO_004", "Invalid Theme", "Theme color is not supported.");
        public static readonly Error InvalidBudget = Error.Failure("PRO_005", "Invalid Budget", "Monthly budget must be positive or null.");
        public static readonly Error PushRequiresPremium = Error.Forbidden("PRO_006", "Push Requires Premium", "Push notifications require a premium subscription.");
        public static readonly Error InvalidDeviceToken = Error.Failure("PRO_007", "Invalid Device Token", "The device token format is invalid.");
    }

    public static class ReportErrors
    {
        public static readonly Error PremiumRequired = Error.Forbidden("REP_001", "Premium Required", "Reports require a premium subscription.");
        public static readonly Error InvalidDateRange = Error.Failure("REP_002", "Invalid Date Range", "The date range is invalid.");
        public static readonly Error InsufficientData = Error.Failure("REP_003", "Insufficient Data", "Not enough data for the requested report.");
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
        public static readonly Error GatewayTimeout = Error.Failure("SYS_003", "Gateway Timeout", "The request timed out. Please try again.");
        public static readonly Error TooManyRequests = Error.TooManyRequest("SYS_004", "Too Many Requests", "General rate limit exceeded. Please wait.");
    }

    public static class ValidationErrors
    {
        public static readonly Error ValidationFailed = Error.Validation("VAL_001", "Validation Failed", "One or more validation errors occurred.");
        public static readonly Error RequiredFieldMissing = Error.Validation("VAL_002", "Required Field Missing", "The field '{field}' is required.");
        public static readonly Error InvalidFormat = Error.Validation("VAL_003", "Invalid Format", "The field '{field}' has an invalid format.");
        public static readonly Error MaxLengthExceeded = Error.Validation("VAL_004", "Max Length Exceeded", "The field '{field}' exceeds maximum length of {max}.");
        public static readonly Error MinLengthRequired = Error.Validation("VAL_005", "Min Length Required", "The field '{field}' must be at least {min} characters.");
    }

    public static class User
    {
        public static readonly Error NotFound = Error.NotFound("USER_001", "User Not Found", "The user was not found.");
        public static readonly Error AccessDenied = Error.Forbidden("USER_002", "User Access Denied", "You do not have permission to access this user.");
    }
}
