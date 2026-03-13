using System;
using System.Reflection;

namespace Ed.Logging.Common
{
    /// <summary>
    ///     Gets the values from the EntryAssemblyInfo.cs file for the current executing assembly
    /// </summary>
    /// <example>
    ///     string company = EntryAssemblyInfo.Company;
    ///     string product = EntryAssemblyInfo.Product;
    ///     string copyright = EntryAssemblyInfo.Copyright;
    ///     string trademark = EntryAssemblyInfo.Trademark;
    ///     string title = EntryAssemblyInfo.Title;
    ///     string description = EntryAssemblyInfo.Description;
    ///     string configuration = EntryAssemblyInfo.Configuration;
    ///     string fileversion = EntryAssemblyInfo.FileVersion;
    ///     string version = EntryAssemblyInfo.Version;
    ///     string versionFull = EntryAssemblyInfo.VersionFull;
    ///     string versionMajor = EntryAssemblyInfo.VersionMajor;
    ///     string versionMinor = EntryAssemblyInfo.VersionMinor;
    ///     string versionBuild = EntryAssemblyInfo.VersionBuild;
    ///     string versionRevision = EntryAssemblyInfo.VersionRevision;
    /// </example>
    public static class EntryAssemblyInfo
    {
        public static string Company
        {
            get { return GetEntryAssemblyAttribute<AssemblyCompanyAttribute>(a => a.Company); }
        }

        public static string Product
        {
            get { return GetEntryAssemblyAttribute<AssemblyProductAttribute>(a => a.Product); }
        }

        public static string Copyright
        {
            get { return GetEntryAssemblyAttribute<AssemblyCopyrightAttribute>(a => a.Copyright); }
        }

        public static string Trademark
        {
            get { return GetEntryAssemblyAttribute<AssemblyTrademarkAttribute>(a => a.Trademark); }
        }

        public static string Title
        {
            get { return GetEntryAssemblyAttribute<AssemblyTitleAttribute>(a => a.Title); }
        }

        public static string Description
        {
            get { return GetEntryAssemblyAttribute<AssemblyDescriptionAttribute>(a => a.Description); }
        }

        public static string Configuration
        {
            get { return GetEntryAssemblyAttribute<AssemblyDescriptionAttribute>(a => a.Description); }
        }

        public static string FileVersion
        {
            get { return GetEntryAssemblyAttribute<AssemblyFileVersionAttribute>(a => a.Version); }
        }

        public static string FilePathname
        {
            get
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly == null)
                    return string.Empty;
                return entryAssembly.Location;
            }
        }

        private static string GetEntryAssemblyAttribute<T>(Func<T, string> value) where T : Attribute
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
                return string.Empty;

            var attribute = (T) Attribute.GetCustomAttribute(entryAssembly, typeof(T));
            return value.Invoke(attribute);
        }
    }
}