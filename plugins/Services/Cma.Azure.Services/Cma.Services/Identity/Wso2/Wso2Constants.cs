// ReSharper disable InconsistentNaming
namespace Cma.Services.Identity.Wso2;

public static class Wso2Constants
{
    public static class Schemas
    {
        public const string User = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";
        public const string CmahId = $"{User}:cmahid";
    }

    public static class Operations
    {
        public const string Add = "add";
        public const string Remove = "remove";
        public const string Replace = "replace";
    }

    /// <summary>
    ///     Error Codes
    /// </summary>
    /// <remarks>https://is.docs.wso2.com/en/latest/references/extend/errors/error-codes-and-descriptions/</remarks>
    public static class ErrorCodes
    {
        /// <summary>
        ///     The user does not exist.
        /// </summary>
        public const int UserNotExist = 17001;

        /// <summary>
        ///     Invalid credentials are provided.
        /// </summary>
        public const int InvalidCredentials = 17002;

        /// <summary>
        ///     The account is locked after multiple incorrect login attempts and the user attempts to log in again.
        /// </summary>
        public const int AccountLockedAfterAttempts = 17003;

        /// <summary>
        ///     The user account is disabled.
        /// </summary>
        public const int UserAccountDisabled = 17004;

        /// <summary>
        ///     The user account is not confirmed.
        /// </summary>
        public const int UserAccountNotConfirmed = 17005;

        /// <summary>
        ///     The admin has forced the user to reset the password via an email link.
        /// </summary>
        public const int AdminForcedResetEmailLink = 17006;

        /// <summary>
        ///     The admin has forced the user to reset the password via OTP.
        /// </summary>
        public const int AdminForcedResetOTP = 17007;

        /// <summary>
        ///     OTP mismatch in admin-forced password reset.
        /// </summary>
        public const int OTPMismatchAdminForcedReset = 17008;

        /// <summary>
        ///     Invalid user credentials.
        /// </summary>
        public const int InvalidUserCredentials = 17010;

        /// <summary>
        ///     Invalid validation code.
        /// </summary>
        public const int InvalidValidationCode = 18001;

        /// <summary>
        ///     The key/confirmation code provided has expired.
        /// </summary>
        public const int ConfirmationCodeExpired = 18002;

        /// <summary>
        ///     Invalid user (invalid username).
        /// </summary>
        public const int InvalidUserInvalidUsername = 18003;

        /// <summary>
        ///     Captcha answer is invalid.
        /// </summary>
        public const int InvalidCaptchaAnswer = 18004;

        /// <summary>
        ///     Unexpected error.
        /// </summary>
        public const int UnexpectedError = 18013;

        /// <summary>
        ///     Sending a recovery notification has failed.
        /// </summary>
        public const int RecoveryNotificationFailed = 18015;

        /// <summary>
        ///     Invalid tenant.
        /// </summary>
        public const int InvalidTenant = 18016;

        /// <summary>
        ///     Challenge question not found.
        /// </summary>
        public const int ChallengeQuestionNotFound = 18017;

        /// <summary>
        ///     Registry exception while getting the challenge question.
        /// </summary>
        public const int RegistryExceptionGettingChallengeQuestion = 20001;

        /// <summary>
        ///     Registry exception while setting the challenge question.
        /// </summary>
        public const int RegistryExceptionSettingChallengeQuestion = 20002;

        /// <summary>
        ///     Error when getting challenge question URIs.
        /// </summary>
        public const int ErrorGettingChallengeQuestionURIs = 20003;

        /// <summary>
        ///     Error while getting the challenge question.
        /// </summary>
        public const int ErrorGettingChallengeQuestion = 20004;

        /// <summary>
        ///     Error while setting the challenge question.
        /// </summary>
        public const int ErrorSettingChallengeQuestion = 20005;

        /// <summary>
        ///     Error while setting the challenge question of the user.
        /// </summary>
        public const int ErrorSettingChallengeQuestionOfUser = 20006;

        /// <summary>
        ///     Error while hashing the security answer.
        /// </summary>
        public const int ErrorHashingSecurityAnswer = 20007;

        /// <summary>
        ///     Invalid answer.
        /// </summary>
        public const int InvalidAnswer = 20008;

        /// <summary>
        ///     Invalid answer for the security question.
        /// </summary>
        public const int InvalidAnswerForSecurityQuestion = 20009;

        /// <summary>
        ///     Need to answer more security questions.
        /// </summary>
        public const int NeedToAnswerMoreSecurityQuestions = 20010;

        /// <summary>
        ///     Error while triggering notifications for the user.
        /// </summary>
        public const int ErrorTriggeringNotificationsForUser = 20011;

        /// <summary>
        ///     Need to answer all requested security questions.
        /// </summary>
        public const int NeedToAnswerAllRequestedSecurityQuestions = 20012;

        /// <summary>
        ///     No valid username found for recovery.
        /// </summary>
        public const int NoValidUsernameFoundForRecovery = 20013;

        /// <summary>
        ///     No fields found for username recovery.
        /// </summary>
        public const int NoFieldsFoundForUsernameRecovery = 20014;

        /// <summary>
        ///     No valid user found.
        /// </summary>
        public const int NoValidUserFound = 20015;

        /// <summary>
        ///     Error loading recovery configurations.
        /// </summary>
        public const int ErrorLoadingRecoveryConfigurations = 20016;

        /// <summary>
        ///     Notification-based password recovery is not enabled.
        /// </summary>
        public const int NotificationBasedRecoveryNotEnabled = 20017;

        /// <summary>
        ///     Security-question based password recovery is not enabled.
        /// </summary>
        public const int SecurityQuestionBasedRecoveryNotEnabled = 20018;

        /// <summary>
        ///     Error adding self-sign-up user.
        /// </summary>
        public const int ErrorAddingSelfSignUpUser = 20019;

        /// <summary>
        ///     Error while locking the user.
        /// </summary>
        public const int ErrorWhileLockingUser = 20020;

        /// <summary>
        ///     Self-sign-up feature is disabled.
        /// </summary>
        public const int SelfSignUpFeatureDisabled = 20021;

        /// <summary>
        ///     Error while locking the user account.
        /// </summary>
        public const int ErrorWhileLockingUserAccount = 20022;

        /// <summary>
        ///     Error while unlocking the user.
        /// </summary>
        public const int ErrorWhileUnlockingUser = 20023;

        /// <summary>
        ///     Old confirmation code not found.
        /// </summary>
        public const int OldConfirmationCodeNotFound = 20024;

        /// <summary>
        ///     Failed to retrieve the user realm from the tenant ID.
        /// </summary>
        public const int FailedToRetrieveUserRealmFromTenantId = 20025;

        /// <summary>
        ///     Failed to retrieve the user store manager.
        /// </summary>
        public const int FailedToRetrieveUserStoreManager = 20026;

        /// <summary>
        ///     Error occurred while retrieving user claims.
        /// </summary>
        public const int ErrorRetrievingUserClaims = 20027;

        /// <summary>
        ///     Error occurred while retrieving account lock connector configuration.
        /// </summary>
        public const int ErrorRetrievingAccountLockConnectorConfig = 20028;

        /// <summary>
        ///     Multiple challenge questions not allowed for this operation.
        /// </summary>
        public const int MultipleChallengeQuestionsNotAllowed = 20029;

        /// <summary>
        ///     Users already exist in the system, please use a different username.
        /// </summary>
        public const int UsersAlreadyExist = 20030;

        /// <summary>
        ///     Username recovery is not enabled.
        /// </summary>
        public const int UsernameRecoveryNotEnabled = 20031;

        /// <summary>
        ///     Multiple users found.
        /// </summary>
        public const int MultipleUsersFound = 20032;

        /// <summary>
        ///     Error loading sign-up configurations.
        /// </summary>
        public const int ErrorLoadingSignUpConfigurations = 20033;

        /// <summary>
        ///     Error occurred while updating user claims.
        /// </summary>
        public const int ErrorUpdatingUserClaims = 20034;

        /// <summary>
        ///     Password policy violation.
        /// </summary>
        public const int PasswordPolicyViolation = 20035;

        /// <summary>
        ///     Provided confirmation code is not valid.
        /// </summary>
        public const int InvalidConfirmationCode = 20036;

        /// <summary>
        ///     No confirmation code is provided for the user.
        /// </summary>
        public const int NoConfirmationCodeProvided = 20037;

        /// <summary>
        ///     No recovery scenario is provided for the user.
        /// </summary>
        public const int NoRecoveryScenarioProvided = 20038;

        /// <summary>
        ///     No recovery step is provided for the user.
        /// </summary>
        public const int NoRecoveryStepProvided = 20039;

        /// <summary>
        ///     Notification type is not provided for the user.
        /// </summary>
        public const int NotificationTypeNotProvided = 20040;

        /// <summary>
        ///     Error while validating the account lock status of the user.
        /// </summary>
        public const int ErrorValidatingAccountLockStatus = 20041;

        /// <summary>
        ///     Error while adding consent for the user.
        /// </summary>
        public const int ErrorAddingConsent = 20042;

        /// <summary>
        ///     This password has been used in the recent password history. Choose a different password.
        /// </summary>
        public const int RecentPasswordHistoryViolation = 22001;

        /// <summary>
        ///     Error while loading history data source.
        /// </summary>
        public const int ErrorLoadingHistoryDataSource = 22002;

        /// <summary>
        ///     Error while validating password history.
        /// </summary>
        public const int ErrorValidatingPasswordHistory = 22003;

        /// <summary>
        ///     Error while storing password history.
        /// </summary>
        public const int ErrorStoringPasswordHistory = 22004;

        /// <summary>
        ///     Error while removing password history from users.
        /// </summary>
        public const int ErrorRemovingPasswordHistory = 22005;

        /// <summary>
        ///     Error occurred while loading password policies.
        /// </summary>
        public const int ErrorLoadingPasswordPolicies = 40001;

        /// <summary>
        ///     Error while validating password policy.
        /// </summary>
        public const int ErrorValidatingPasswordPolicy = 40002;

        /// <summary>
        ///     Scope name is not specified.
        /// </summary>
        public const int ScopeNameNotSpecified = 41001;

        /// <summary>
        ///     Scope display name is not specified.
        /// </summary>
        public const int ScopeDisplayNameNotSpecified = 41002;

        /// <summary>
        ///     Scope name is not found.
        /// </summary>
        public const int ScopeNameNotFound = 41003;

        /// <summary>
        ///     Scope with the name {0} already exists in the system. Please use a different scope name.
        /// </summary>
        public const int ScopeNameAlreadyExists = 41004;

        /// <summary>
        ///     Scope is not specified.
        /// </summary>
        public const int ScopeNotSpecified = 41005;

        /// <summary>
        ///     Error occurred while registering scope.
        /// </summary>
        public const int ErrorRegisteringScope = 51001;

        /// <summary>
        ///     Error occurred while retrieving all available scopes.
        /// </summary>
        public const int ErrorRetrievingAllScopes = 51002;

        /// <summary>
        ///     Error occurred while retrieving scope.
        /// </summary>
        public const int ErrorRetrievingScope = 51003;

        /// <summary>
        ///     Error occurred while deleting scope.
        /// </summary>
        public const int ErrorDeletingScope = 51004;

        /// <summary>
        ///     Error occurred while updating scope.
        /// </summary>
        public const int ErrorUpdatingScope = 51005;

        /// <summary>
        ///     Unexpected error.
        /// </summary>
        public const int UnexpectedErrorGeneral = 51007;
    }
}