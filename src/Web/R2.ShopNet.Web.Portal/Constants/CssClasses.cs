namespace R2.ShopNet.Web.Portal.Constants;

/// <summary>
/// CSS class constants for common UI patterns
/// </summary>
public static class CssClasses
{
    /// <summary>
    /// Sidebar related CSS classes
    /// </summary>
    public static class Sidebar
    {
        public const string Base = "fixed flex flex-col top-0 px-5 left-0 bg-white dark:bg-gray-900 dark:border-gray-800 text-gray-900 h-screen transition-all duration-300 ease-in-out z-50 border-r border-gray-200";
        public const string WidthExpanded = "w-[290px]";
        public const string WidthCollapsed = "w-[90px]";
        public const string MobileOpen = "translate-x-0";
        public const string MobileClosed = "-translate-x-full";
        public const string DesktopVisible = "xl:translate-x-0";
    }

    /// <summary>
    /// Menu item related CSS classes
    /// </summary>
    public static class MenuItem
    {
        public const string Base = "menu-item group cursor-pointer";
        public const string Active = "menu-item-active";
        public const string Inactive = "menu-item-inactive";
        public const string IconSize = "menu-item-icon-size";
        public const string IconActive = "menu-item-icon-active";
        public const string IconInactive = "menu-item-icon-inactive";
        public const string JustifyStart = "xl:justify-start";
        public const string JustifyCenter = "xl:justify-center";
    }

    /// <summary>
    /// Dropdown menu item related CSS classes
    /// </summary>
    public static class DropdownMenuItem
    {
        public const string Base = "menu-dropdown-item";
        public const string Active = "menu-dropdown-item-active";
        public const string Inactive = "menu-dropdown-item-inactive";
    }

    /// <summary>
    /// Badge related CSS classes
    /// </summary>
    public static class Badge
    {
        public const string Base = "ml-auto absolute right-10 menu-dropdown-badge";
        public const string Active = "menu-dropdown-badge-active";
        public const string Inactive = "menu-dropdown-badge-inactive";
    }

    /// <summary>
    /// Content area related CSS classes
    /// </summary>
    public static class Content
    {
        public const string Base = "flex-1 transition-all duration-300 ease-in-out";
        public const string MarginExpanded = "xl:ml-[290px]";
        public const string MarginCollapsed = "xl:ml-[90px]";
        public const string MarginMobileOpen = "ml-0";
    }

    /// <summary>
    /// Button related CSS classes
    /// </summary>
    public static class Button
    {
        public const string IconButton = "flex items-center justify-center w-10 h-10 text-gray-700 rounded-lg hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-800 lg:h-11 lg:w-11";
        public const string MenuToggle = "items-center justify-center w-10 h-10 text-gray-500 border-gray-200 rounded-lg z-99999 dark:border-gray-800 flex dark:text-gray-400 lg:h-11 lg:w-11 xl:border";
    }

    /// <summary>
    /// Card related CSS classes
    /// </summary>
    public static class Card
    {
        public const string Base = "bg-white dark:bg-gray-900 p-6 rounded-xl shadow-theme-sm border border-gray-200 dark:border-gray-800";
    }

    /// <summary>
    /// Section header related CSS classes
    /// </summary>
    public static class SectionHeader
    {
        public const string Base = "mb-4 text-xs uppercase flex leading-[20px] text-gray-400";
        public const string JustifyStart = "justify-start";
        public const string JustifyCenter = "xl:justify-center";
    }

    /// <summary>
    /// Expand icon related CSS classes
    /// </summary>
    public static class ExpandIcon
    {
        public const string Base = "ml-auto transition-transform duration-200";
        public const string Rotated = "rotate-180 text-brand-500";
    }
}
