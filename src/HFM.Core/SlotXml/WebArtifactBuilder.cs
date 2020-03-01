﻿/*
 * HFM.NET
 * Copyright (C) 2009-2017 Ryan Harlamert (harlam357)
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
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using HFM.Core.Client;
using HFM.Core.Logging;
using HFM.Preferences;

namespace HFM.Core.SlotXml
{
    public class WebArtifactBuilder
    {
        public ILogger Logger { get; }
        public IPreferenceSet Preferences { get; }
        public string Path { get; }

        public WebArtifactBuilder(ILogger logger, IPreferenceSet preferences)
            : this(logger, preferences, System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()))
        {

        }

        public WebArtifactBuilder(ILogger logger, IPreferenceSet preferences, string path)
        {
            Logger = logger ?? NullLogger.Instance;
            Preferences = preferences;
            Path = path;
        }

        public string Build(ICollection<SlotModel> slots)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));

            Directory.CreateDirectory(Path);

            var copyHtml = Preferences.Get<bool>(Preference.WebGenCopyHtml);
            var copyLogs = Preferences.Get<bool>(Preference.WebGenCopyFAHlog);

            var xmlBuilder = new XmlBuilder(Preferences);
            var result = xmlBuilder.Build(slots, Path);

            if (copyHtml)
            {
                var htmlBuilder = new HtmlBuilder(Preferences);
                htmlBuilder.Build(result, Path);
            }

            if (copyLogs)
            {
                CopyLogs(slots);
            }

            return Path;
        }

        private void CopyLogs(IEnumerable<SlotModel> slots)
        {
            var logCache = Preferences.Get<string>(Preference.CacheDirectory);
            var logPaths = slots.Select(x => System.IO.Path.Combine(logCache, x.Settings.ClientLogFileName)).Distinct();
            int maximumLength = Preferences.Get<bool>(Preference.WebGenLimitLogSize)
                ? Preferences.Get<int>(Preference.WebGenLimitLogSizeLength) * 1024
                : -1;

            foreach (var path in logPaths.Where(File.Exists))
            {
                using (var readStream = Internal.FileSystem.TryFileOpen(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (readStream == null)
                    {
                        Logger.Warn($"Could not open {path} for web generation.");
                        continue;
                    }

                    if (maximumLength >= 0)
                    {
                        readStream.Position = readStream.Length - maximumLength;
                    }

                    using (var writeStream = new FileStream(System.IO.Path.Combine(Path, System.IO.Path.GetFileName(path)), FileMode.Create))
                    {
                        readStream.CopyTo(writeStream);
                    }
                }
            }
        }
    }
}
