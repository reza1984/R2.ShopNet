namespace R2.ShopNet.Framework.Common;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class Constants
{
    public static class Pagination
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
    }

    public static class Validation
    {
        public const int MaxStringLength = 500;
        public const int MaxDescriptionLength = 2000;
        public const int MaxNameLength = 100;
        public const int MinNameLength = 2;
        public const int MaxEmailLength = 256;
    }

    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-Id";
        public const string RequestId = "X-Request-Id";
        public const string TenantId = "X-Tenant-Id";
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Manager = "Manager";
        public const string Guest = "Guest";
    }
}
