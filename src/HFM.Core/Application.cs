/*
 * HFM.NET
 * Copyright (C) 2009-2016 Ryan Harlamert (harlam357)
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; version 2
 * of the License. See the included file GPLv2.TXT.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HFM.Core
{
    public static partial class Application
    {
        public const string CssFolderName = "CSS";
        public const string XsltFolderName = "XSL";

        public const string ProjectSiteUrl = "https://github.com/harlam357/hfm-net";
        public const string SupportForumUrl = "https://groups.google.com/group/hfm-net/";

        public static readonly bool IsRunningOnMono = Type.GetType("Mono.Runtime") != null;

        #region Name and Version Strings

        /// <summary>
        /// The application name.
        /// </summary>
        public const string Name = "HFM.NET";

        /// <summary>
        /// Gets a string in the format Name vMajor.Minor.Build.
        /// </summary>
        public static string NameAndVersion
        {
            get { return String.Concat(Name, " v", CreateVersionString("{0}.{1}.{2}")); }
        }

        /// <summary>
        /// Gets a string in the format Name vMajor.Minor.Build.Revision
        /// </summary>
        public static string NameAndVersionWithRevision
        {
            get { return String.Concat(Name, " v", CreateVersionString("{0}.{1}.{2}.{3}")); }
        }

        /// <summary>
        /// Gets a string in the format Major.Minor.Build.
        /// </summary>
        public static string Version
        {
            get { return CreateVersionString("{0}.{1}.{2}"); }
        }

        /// <summary>
        /// Gets a string in the format Major.Minor.Build.Revision.
        /// </summary>
        public static string VersionWithRevision
        {
            get { return CreateVersionString("{0}.{1}.{2}.{3}"); }
        }

        private static string CreateVersionString(string format)
        {
            Debug.Assert(format != null);

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            return String.Format(CultureInfo.InvariantCulture, format, fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart,
                                                                       fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart);
        }

        #endregion

        #region Version Numbers

        public static int VersionNumber
        {
            get
            {
                // Example: 0.3.1.50 == 30010050 / 1.3.4.75 == 1030040075
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                return GetVersionFromArray(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart,
                                           fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart);
            }
        }

        /// <summary>
        /// Parses a version number from a 'x.x.x.x' or 'x.x.x' formatted string.
        /// </summary>
        /// <exception cref="ArgumentNullException">version is null.</exception>
        /// <exception cref="FormatException">Throws when given version cannot be parsed.</exception>
        public static int ParseVersion(string version)
        {
            if (version == null) throw new ArgumentNullException("version");

            var versionNumbers = GetVersionNumbers(version);
            return GetVersionFromArray(versionNumbers);
        }

        private static int GetVersionFromArray(params int[] versionNumbers)
        {
            Debug.Assert(versionNumbers != null);

            int value = 0;

            if (versionNumbers.Length > 0)
            {
                value += versionNumbers[0] * 1000000000;
            }
            if (versionNumbers.Length > 1)
            {
                value += versionNumbers[1] * 10000000;
            }
            if (versionNumbers.Length > 2)
            {
                value += versionNumbers[2] * 10000;
            }
            if (versionNumbers.Length > 3)
            {
                value += versionNumbers[3];
            }

            return value;
        }

        public static int[] GetVersionNumbers()
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var versionNumbers = new int[4];
            versionNumbers[0] = fileVersionInfo.FileMajorPart;
            versionNumbers[1] = fileVersionInfo.FileMinorPart;
            versionNumbers[2] = fileVersionInfo.FileBuildPart;
            versionNumbers[3] = fileVersionInfo.FilePrivatePart;
            return versionNumbers;
        }

        private static int[] GetVersionNumbers(string version)
        {
            Debug.Assert(version != null);

            var regex = new Regex("^(?<Major>(\\d+))\\.(?<Minor>(\\d+))\\.(?<Build>(\\d+))\\.(?<Revision>(\\d+))$", RegexOptions.ExplicitCapture);
            var match = regex.Match(version);
            if (match.Success)
            {
                var versionNumbers = new int[4];
                versionNumbers[0] = Int32.Parse(match.Result("${Major}"), CultureInfo.InvariantCulture);
                versionNumbers[1] = Int32.Parse(match.Result("${Minor}"), CultureInfo.InvariantCulture);
                versionNumbers[2] = Int32.Parse(match.Result("${Build}"), CultureInfo.InvariantCulture);
                versionNumbers[3] = Int32.Parse(match.Result("${Revision}"), CultureInfo.InvariantCulture);
                return versionNumbers;
            }

            regex = new Regex("^(?<Major>(\\d+))\\.(?<Minor>(\\d+))\\.(?<Build>(\\d+))$", RegexOptions.ExplicitCapture);
            match = regex.Match(version);
            if (match.Success)
            {
                var versionNumbers = new int[3];
                versionNumbers[0] = Int32.Parse(match.Result("${Major}"), CultureInfo.InvariantCulture);
                versionNumbers[1] = Int32.Parse(match.Result("${Minor}"), CultureInfo.InvariantCulture);
                versionNumbers[2] = Int32.Parse(match.Result("${Build}"), CultureInfo.InvariantCulture);
                return versionNumbers;
            }

            throw new FormatException(String.Format(CultureInfo.CurrentCulture,
               "Given version '{0}' is not in the correct format.", version));
        }

        #endregion

        #region Mono Version

        /// <summary>
        /// Uses the GetDisplayName method to get the name. It most likely contains a version number.
        /// </summary>
        private static string GetMonoDisplayName()
        {
            return ((string)typeof(object).Assembly.GetType("Mono.Runtime").InvokeMember("GetDisplayName", BindingFlags.InvokeMethod |
                                                                                                           BindingFlags.NonPublic |
                                                                                                           BindingFlags.Static |
                                                                                                           BindingFlags.DeclaredOnly |
                                                                                                           BindingFlags.ExactBinding, null, null, null));
        }

        /// <summary>
        /// Extracts Complete version number from the Display Name.
        /// </summary>
        public static Version GetMonoVersionNumber()
        {
            string[] tokens = GetMonoDisplayName().Split(' ');
            foreach (string word in tokens)
            {
                // This if statement is needed because mono 2.6 display name is "tarball"
                string versionString;
                if (word == "tarball")
                {
                    versionString = "2.6";
                }
                else
                {
                    versionString = word;
                }

                Version result;
                if (System.Version.TryParse(versionString, out result))
                {
                    return result;
                }
            }
            return null;
        }

        #endregion
    }
}
