﻿/*
 * Copyright © 2016 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */
using EDDiscovery2;
using EDDiscovery2.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EDDiscovery.EliteDangerous
{
    public class EDJournalReader : LogReaderBase
    {
        // Close Quarters Combat
        public bool CQC { get; set; }

        // Time and timezone
        public DateTime LastLogTime { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public TimeSpan TimeZoneOffset { get; set; }

        public static bool disable_beta_commander_check = false;        // strictly for debugging purposes

        private Queue<JournalEntryDB> StartEntries = new Queue<JournalEntryDB>();

        public EDJournalReader(string filename) : base(filename)
        {
        }

        public EDJournalReader(TravelLogUnit tlu) : base(tlu)
        {
        }

        // Journal ID
        public int JournalId { get { return (int)TravelLogUnit.id; } }

        protected JournalEntryDB ProcessLine(string line, bool resetOnError)
        {
            int cmdrid = -2;        //-1 is hidden, -2 is never shown

            if (TravelLogUnit.CommanderId.HasValue)
            {
                cmdrid = TravelLogUnit.CommanderId.Value;
                // System.Diagnostics.Trace.WriteLine(string.Format("TLU says commander {0} at {1}", cmdrid, TravelLogUnit.Name));
            }

            if (line.Length == 0)
                return null;

            JournalEntryDB je = null;

            try
            {
                je = JournalEntryDB.Create(line);
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine($"Bad journal line:\n{line}");

                if (resetOnError)
                {
                    throw;
                }
                else
                {
                    return null;
                }
            }

            if (je.entry.EventTypeID == JournalTypeEnum.Fileheader)
            {
                JournalEvents.JournalFileheader header = (JournalEvents.JournalFileheader)je.entry;

                if (header.Beta && !disable_beta_commander_check)
                {
                    TravelLogUnit.type |= 0x8000;
                }

                if (header.Part > 1)
                {
                    JournalEvents.JournalContinued contd = JournalEntryDB.GetLast<JournalEvents.JournalContinued>(je.entry.EventTimeUTC.AddSeconds(1), e => e.Part == header.Part);

                    // Carry commander over from previous log if it ends with a Continued event.
                    if (contd != null && Math.Abs(header.EventTimeUTC.Subtract(contd.EventTimeUTC).TotalSeconds) < 5 && contd.CommanderId >= 0)
                    {
                        TravelLogUnit.CommanderId = contd.CommanderId;
                    }
                }
            }
            else if (je.entry.EventTypeID == JournalTypeEnum.LoadGame)
            {
                string newname = (je.entry as JournalEvents.JournalLoadGame).LoadGameCommander;

                if ((TravelLogUnit.type & 0x8000) == 0x8000)
                {
                    newname = "[BETA] " + newname;
                }

                EDCommander _commander = EDDiscovery2.EDDConfig.Instance.ListOfCommanders.FirstOrDefault(c => c.Name.Equals(newname, StringComparison.InvariantCultureIgnoreCase));

                if (_commander == null )
                {
                    if (EDDiscovery2.EDDConfig.Instance.ListOfCommanders.Count == 1 && EDDiscovery2.EDDConfig.Instance.ListOfCommanders[0].Name == "Jameson (Default)" )
                    {
                        EDDiscovery2.EDDConfig.Instance.ListOfCommanders[0].Name = newname;
                        EDDiscovery2.EDDConfig.Instance.ListOfCommanders[0].EdsmName = newname;
                        EDDiscovery2.EDDConfig.Instance.UpdateCommanders(EDDiscovery2.EDDConfig.Instance.ListOfCommanders,false); // store back to DB, no need to reload..
                    }
                    else
                        _commander = EDDiscovery2.EDDConfig.Instance.GetNewCommander(newname, null, EDJournalClass.GetDefaultJournalDir().Equals(TravelLogUnit.Path) ? "" : TravelLogUnit.Path);

                }

                cmdrid = _commander.Nr;

                if (!TravelLogUnit.CommanderId.HasValue)
                {
                    TravelLogUnit.CommanderId = cmdrid;
                    TravelLogUnit.Update();
                    System.Diagnostics.Trace.WriteLine(string.Format("TLU {0} updated with commander {1}", TravelLogUnit.Path, cmdrid));
                }
            }

            je.entry.TLUId = (int)TravelLogUnit.id;
            je.entry.CommanderId = cmdrid;

            return je;
        }

        public bool ReadJournalLog(out JournalEntryDB jent, bool resetOnError = false)
        {
            if (StartEntries.Count != 0 && this.TravelLogUnit.CommanderId != null && this.TravelLogUnit.CommanderId >= 0)
            {
                jent = StartEntries.Dequeue();
                jent.entry.CommanderId = (int)TravelLogUnit.CommanderId;
                return true;
            }

            while (ReadLine(out jent, l => ProcessLine(l, resetOnError)))
            {
                if (jent == null)
                    continue;

                if ((this.TravelLogUnit.CommanderId == null || this.TravelLogUnit.CommanderId < 0) && jent.entry.EventTypeID != JournalTypeEnum.LoadGame)
                {
                    StartEntries.Enqueue(jent);
                    continue;
                }

                //System.Diagnostics.Trace.WriteLine(string.Format("Read line {0} from {1}", line, this.FileName));

                return true;
            }

            jent = null;
            return false;
        }

        public IEnumerable<JournalEntryDB> ReadJournalLog(bool continueOnError = false)
        {
            JournalEntryDB entry;
            bool resetOnError = false;
            while (ReadJournalLog(out entry, resetOnError: resetOnError))
            {
                yield return entry;
                resetOnError = !continueOnError;
            }
        }
    }
}
