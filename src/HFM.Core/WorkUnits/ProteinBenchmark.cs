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
using System.Collections.Generic;
using System.Runtime.Serialization;

using HFM.Core.Client;

namespace HFM.Core.WorkUnits
{
    [Serializable]
    [DataContract]
    public sealed class ProteinBenchmark
    {
        private const int DefaultMaxFrames = 300;

        private static readonly object FrameTimesListLock = new object();

        internal SlotIdentifier SlotIdentifier => new SlotIdentifier(OwningClientName, OwningSlotId, OwningClientPath);

        /// <summary>
        /// Fully qualified name of the folding slot that owns this object (includes "Slot" designation).
        /// </summary>
        public string OwningSlotName
        {
            get { return OwningClientName.AppendSlotId(OwningSlotId); }
        }

        /// <summary>
        /// Name of the folding client that owns this object (name given during client setup).
        /// </summary>
        [DataMember(Order = 1)]
        public string OwningClientName { get; set; }

        /// <summary>
        /// Path of the folding slot that own this object.
        /// </summary>
        [DataMember(Order = 2)]
        public string OwningClientPath { get; set; }

        /// <summary>
        /// Identification number of the folding slot on the folding client that owns this object.
        /// </summary>
        [DataMember(Order = 6, IsRequired = true)]
        public int OwningSlotId { get; set; }

        /// <summary>
        /// Project ID
        /// </summary>
        [DataMember(Order = 3)]
        public int ProjectID { get; set; }

        /// <summary>
        /// Minimum Frame Time
        /// </summary>
        [DataMember(Order = 4)]
        public TimeSpan MinimumFrameTime { get; set; }

        /// <summary>
        /// Average Frame Time
        /// </summary>
        public TimeSpan AverageFrameTime
        {
            get
            {
                if (FrameTimes.Count > 0)
                {
                    TimeSpan totalTime = TimeSpan.Zero;
                    lock (FrameTimesListLock)
                    {
                        foreach (ProteinFrameTime time in FrameTimes)
                        {
                            totalTime = totalTime.Add(time.Duration);
                        }
                    }

                    return TimeSpan.FromSeconds(Convert.ToInt32(totalTime.TotalSeconds) / FrameTimes.Count);
                }

                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Frame Times List
        /// </summary>
        [DataMember(Order = 5)]
        public List<ProteinFrameTime> FrameTimes { get; set; }

        public ProteinBenchmark()
        {
            OwningSlotId = -1;
            MinimumFrameTime = TimeSpan.Zero;
            FrameTimes = new List<ProteinFrameTime>(DefaultMaxFrames);
        }

        public static ProteinBenchmark FromSlotIdentifier(SlotIdentifier slotIdentifier)
        {
            return new ProteinBenchmark
            {
                OwningClientName = slotIdentifier.OwningClientName,
                OwningSlotId = slotIdentifier.OwningSlotId,
                OwningClientPath = slotIdentifier.OwningClientPath
            };
        }

        /// <summary>
        /// Set Next Frame Time
        /// </summary>
        /// <param name="frameTime">Frame Time</param>
        public bool SetFrameDuration(TimeSpan frameTime)
        {
            if (frameTime > TimeSpan.Zero)
            {
                if (frameTime < MinimumFrameTime || MinimumFrameTime.Equals(TimeSpan.Zero))
                {
                    MinimumFrameTime = frameTime;
                }

                lock (FrameTimesListLock)
                {
                    // Dequeue once we have the Maximum number of frame times
                    if (FrameTimes.Count == DefaultMaxFrames)
                    {
                        FrameTimes.RemoveAt(DefaultMaxFrames - 1);
                    }
                    FrameTimes.Insert(0, new ProteinFrameTime { Duration = frameTime });
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Refresh the Minimum Frame Time for this Benchmark based on current List of Frame Times
        /// </summary>
        public void UpdateMinimumFrameTime()
        {
            TimeSpan minimumFrameTime = TimeSpan.Zero;
            lock (FrameTimesListLock)
            {
                foreach (ProteinFrameTime frameTime in FrameTimes)
                {
                    if (frameTime.Duration < minimumFrameTime || minimumFrameTime.Equals(TimeSpan.Zero))
                    {
                        minimumFrameTime = frameTime.Duration;
                    }
                }
            }

            if (minimumFrameTime.Equals(TimeSpan.Zero) == false)
            {
                MinimumFrameTime = minimumFrameTime;
            }
        }
    }
}
