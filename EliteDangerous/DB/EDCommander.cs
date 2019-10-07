﻿/*
 * Copyright © 2015 - 2016 EDDiscovery development team
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
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using EliteDangerousCore.DB;

namespace EliteDangerousCore
{

    public class EDCommander
    {
        #region Static interface

        #region Properties

        public static int CurrentCmdrID
        {
            get
            {
                if (commanderID == Int32.MinValue)
                {
                    commanderID = EliteDangerousCore.DB.UserDatabase.Instance.GetSettingInt("ActiveCommander", 0);
                }

                if (commanderID >= 0 && !commanders.ContainsKey(commanderID) && commanders.Count != 0)
                {
                    commanderID = commanders.Values.First().Nr;
                }

                return commanderID;
            }
            set
            {
                if (value != commanderID)
                {
                    if (!commanders.ContainsKey(value))
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    commanderID = value;
                    EliteDangerousCore.DB.UserDatabase.Instance.PutSettingInt("ActiveCommander", value);
                }
            }
        }

        public static EDCommander Current           // always returns
        {
            get
            {
                return commanders[CurrentCmdrID];
            }
        }

        public static int NumberOfCommanders
        {
            get
            {
                return commanders.Count;
            }
        }

        #endregion

        #region Methods

        public static EDCommander GetCommander(int nr)      // null if not valid - cope with it. Hidden gets returned.
        {
            if (commanders.ContainsKey(nr))
            {
                return commanders[nr];
            }
            else
            {
                return null;
            }
        }

        public static EDCommander GetCommander(string name)
        {
            return commanders.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool IsCommanderPresent(string name)
        {
            return commanders.Values.ToList().FindIndex(x=>x.Name.Equals(name,StringComparison.InvariantCultureIgnoreCase)) != -1;
        }

        public static List<EDCommander> GetListInclHidden()
        {
            return commanders.Values.OrderBy(v => v.Nr).ToList();
        }

        public static List<EDCommander> GetListCommanders()
        {
            return commanders.Values.Where(v=>v.Nr>=0).OrderBy(v => v.Nr).ToList();
        }

        public static void Delete(int cmdrid)
        {
            commanders.Remove(cmdrid);

            UserDatabase.Instance.ExecuteWithDatabase(cn =>
            {
                using (DbCommand cmd = cn.Connection.CreateCommand("UPDATE Commanders SET Deleted = 1 WHERE Id = @Id"))
                {
                    cmd.AddParameterWithValue("@Id", cmdrid);
                    cmd.ExecuteNonQuery();
                }
            });
        }

        public static void Delete(EDCommander cmdr)
        {
            Delete(cmdr.Nr);
        }

        public static EDCommander Create(EDCommander other )
        {
            return Create(other.name, other.EdsmName, other.EDSMAPIKey, other.JournalDir, other.syncToEdsm, other.SyncFromEdsm, 
                                other.SyncToEddn, other.SyncToEGO, other.EGOName, other.EGOAPIKey, other.SyncToInara, other.InaraName, other.InaraAPIKey);
        }

        public static EDCommander Create(string name = null, string edsmName = null, string edsmApiKey = null, string journalpath = null, 
                                        bool toedsm = true, bool fromedsm = false, bool toeddn = true, bool toego = false, string egoname = null, string egoapi = null,
                                        bool toinara = false, string inaraname = null, string inaraapikey = null, string homesystem = null,
                                        float mapzoom = 1.0f, bool mapcentreonselection = true, int mapcolour = -1)
        {
            EDCommander cmdr = UserDatabase.Instance.ExecuteWithDatabase<EDCommander>(cn =>
            {
                using (DbCommand cmd = cn.Connection.CreateCommand("INSERT INTO Commanders (Name,EdsmName,EdsmApiKey,JournalDir,Deleted, SyncToEdsm, SyncFromEdsm, SyncToEddn, NetLogDir, SyncToEGO, EGOName, EGOAPIKey, SyncToInara, InaraName, InaraAPIKey, HomeSystem, MapColour,MapCentreOnSelection,MapZoom) " +
                                                          "VALUES (@Name,@EdsmName,@EdsmApiKey,@JournalDir,@Deleted, @SyncToEdsm, @SyncFromEdsm, @SyncToEddn, @NetLogDir, @SyncToEGO, @EGOName, @EGOApiKey, @SyncToInara, @InaraName, @InaraAPIKey, @HomeSystem, @MapColour,@MapCentreOnSelection,@MapZoom)"))
                {

                    cmd.AddParameterWithValue("@Name", name ?? "");
                    cmd.AddParameterWithValue("@EdsmName", edsmName ?? name ?? "");
                    cmd.AddParameterWithValue("@EdsmApiKey", edsmApiKey ?? "");
                    cmd.AddParameterWithValue("@JournalDir", journalpath ?? "");
                    cmd.AddParameterWithValue("@Deleted", false);
                    cmd.AddParameterWithValue("@SyncToEdsm", toedsm);
                    cmd.AddParameterWithValue("@SyncFromEdsm", fromedsm);
                    cmd.AddParameterWithValue("@SyncToEddn", toeddn);
                    cmd.AddParameterWithValue("@NetLogDir", "");        // Unused field, null out
                    cmd.AddParameterWithValue("@SyncToEGO", toego);
                    cmd.AddParameterWithValue("@EGOName", egoname ?? "");
                    cmd.AddParameterWithValue("@EGOApiKey", egoapi ?? "");
                    cmd.AddParameterWithValue("@SyncToInara", toinara);
                    cmd.AddParameterWithValue("@InaraName", inaraname ?? "");
                    cmd.AddParameterWithValue("@InaraApiKey", inaraapikey ?? "");
                    cmd.AddParameterWithValue("@HomeSystem", homesystem ?? "");
                    cmd.AddParameterWithValue("@MapColour", mapcolour == -1 ? System.Drawing.Color.Red.ToArgb() : mapcolour);
                    cmd.AddParameterWithValue("@MapCentreOnSelection", mapcentreonselection);
                    cmd.AddParameterWithValue("@MapZoom", mapzoom);
                    cmd.ExecuteNonQuery();
                }

                using (DbCommand cmd = cn.Connection.CreateCommand("SELECT Id FROM Commanders WHERE rowid = last_insert_rowid()"))
                {
                    int nr = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (DbCommand cmd = cn.Connection.CreateCommand("SELECT * FROM Commanders WHERE rowid = last_insert_rowid()"))
                {
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        {
                            cmdr = new EDCommander(reader);
                        }
                    }
                }

                if (name == null)
                {
                    using (DbCommand cmd = cn.Connection.CreateCommand("UPDATE Commanders SET Name = @Name WHERE rowid = last_insert_rowid()"))
                    {
                        cmd.AddParameterWithValue("@Name", cmdr.Name);
                        cmd.ExecuteNonQuery();
                    }
                }

                return cmdr;
            });

            commanders[cmdr.Nr] = cmdr;

            return cmdr;
        }

        public static void Update(List<EDCommander> cmdrlist, bool reload)
        {
            UserDatabase.Instance.ExecuteWithDatabase(cn =>
            {
                using (DbCommand cmd = cn.Connection.CreateCommand("UPDATE Commanders SET Name=@Name, EdsmName=@EdsmName, EdsmApiKey=@EdsmApiKey, NetLogDir=@NetLogDir, JournalDir=@JournalDir, " +
                                                          "SyncToEdsm=@SyncToEdsm, SyncFromEdsm=@SyncFromEdsm, SyncToEddn=@SyncToEddn, SyncToEGO=@SyncToEGO, EGOName=@EGOName, " +
                                                          "EGOAPIKey=@EGOApiKey, SyncToInara=@SyncToInara, InaraName=@InaraName, InaraAPIKey=@InaraAPIKey, HomeSystem=@HomeSystem, " +
                                                          "MapColour=@MapColour, MapCentreOnSelection=@MapCentreOnSelection, MapZoom=@MapZoom " +
                                                          "WHERE Id=@Id"))
                {
                    cmd.AddParameter("@Id", DbType.Int32);
                    cmd.AddParameter("@Name", DbType.String);
                    cmd.AddParameter("@EdsmName", DbType.String);
                    cmd.AddParameter("@EdsmApiKey", DbType.String);
                    cmd.AddParameter("@NetLogDir", DbType.String);
                    cmd.AddParameter("@JournalDir", DbType.String);
                    cmd.AddParameter("@SyncToEdsm", DbType.Boolean);
                    cmd.AddParameter("@SyncFromEdsm", DbType.Boolean);
                    cmd.AddParameter("@SyncToEddn", DbType.Boolean);
                    cmd.AddParameter("@SyncToEGO", DbType.Boolean);
                    cmd.AddParameter("@EGOName", DbType.String);
                    cmd.AddParameter("@EGOApiKey", DbType.String);
                    cmd.AddParameter("@SyncToInara", DbType.Boolean);
                    cmd.AddParameter("@InaraName", DbType.String);
                    cmd.AddParameter("@InaraApiKey", DbType.String);
                    cmd.AddParameter("@HomeSystem", DbType.String);
                    cmd.AddParameter("@MapColour", DbType.Int32);
                    cmd.AddParameter("@MapCentreOnSelection", DbType.Boolean);
                    cmd.AddParameter("@MapZoom", DbType.Double);

                    foreach (EDCommander edcmdr in cmdrlist) // potential NRE
                    {
                        cmd.Parameters["@Id"].Value = edcmdr.Nr;
                        cmd.Parameters["@Name"].Value = edcmdr.Name;
                        cmd.Parameters["@EdsmName"].Value = edcmdr.EdsmName;
                        cmd.Parameters["@EdsmApiKey"].Value = edcmdr.EDSMAPIKey != null ? edcmdr.EDSMAPIKey : "";
                        cmd.Parameters["@NetLogDir"].Value = ""; // unused field
                        cmd.Parameters["@JournalDir"].Value = edcmdr.JournalDir != null ? edcmdr.JournalDir : "";
                        cmd.Parameters["@SyncToEdsm"].Value = edcmdr.SyncToEdsm;
                        cmd.Parameters["@SyncFromEdsm"].Value = edcmdr.SyncFromEdsm;
                        cmd.Parameters["@SyncToEddn"].Value = edcmdr.SyncToEddn;
                        cmd.Parameters["@SyncToEGO"].Value = edcmdr.SyncToEGO;
                        cmd.Parameters["@EGOName"].Value = edcmdr.EGOName != null ? edcmdr.EGOName : "";
                        cmd.Parameters["@EGOApiKey"].Value = edcmdr.EGOAPIKey != null ? edcmdr.EGOAPIKey : "";
                        cmd.Parameters["@SyncToInara"].Value = edcmdr.SyncToInara;
                        cmd.Parameters["@InaraName"].Value = edcmdr.InaraName != null ? edcmdr.InaraName : "";
                        cmd.Parameters["@InaraAPIKey"].Value = edcmdr.InaraAPIKey != null ? edcmdr.InaraAPIKey : "";
                        cmd.Parameters["@HomeSystem"].Value = edcmdr.homesystem != null ? edcmdr.homesystem : "";
                        cmd.Parameters["@MapColour"].Value = edcmdr.MapColour;
                        cmd.Parameters["@MapCentreOnSelection"].Value = edcmdr.MapCentreOnSelection;
                        cmd.Parameters["@MapZoom"].Value = edcmdr.MapZoom;
                        cmd.ExecuteNonQuery();

                        commanders[edcmdr.Nr] = edcmdr;
                    }
                }
            });

            if (reload)
                Load(true);       // refresh in-memory copy

            // For  some people sharing their user DB between different computers and having different paths to their journals on those computers.
            JObject jo = new JObject();
            foreach (EDCommander cmdr in commandersDict.Values)
            {
                JObject j = new JObject();
                if (cmdr.JournalDir != null)
                    jo["JournalDir"] = cmdr.JournalDir;
                jo[cmdr.Name] = j;
            }

            using (Stream stream = File.OpenWrite(Path.Combine(EliteDangerousCore.EliteConfigInstance.InstanceOptions.AppDataDirectory, "CommanderPaths.json.tmp")))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    using (JsonTextWriter jwriter = new JsonTextWriter(writer))
                    {
                        jo.WriteTo(jwriter);
                    }
                }
            }

            File.Delete(Path.Combine(EliteDangerousCore.EliteConfigInstance.InstanceOptions.AppDataDirectory, "CommanderPaths.json"));
            File.Move(Path.Combine(EliteDangerousCore.EliteConfigInstance.InstanceOptions.AppDataDirectory, "CommanderPaths.json.tmp"), 
                Path.Combine(EliteDangerousCore.EliteConfigInstance.InstanceOptions.AppDataDirectory, "CommanderPaths.json"));

        }


        public static void Load(bool write = true)
        {
            if (commandersDict == null)
                commandersDict = new Dictionary<int, EDCommander>();

            lock (commandersDict)
            {
                commandersDict.Clear();

                var cmdrs = GetCommanders();
                int maxnr = cmdrs.Count == 0 ? 0 : cmdrs.Max(c => c.Nr);

                foreach (EDCommander cmdr in cmdrs)
                {
                    if (!cmdr.Deleted)
                    {
                        commandersDict[cmdr.Nr] = cmdr;
                    }
                }

                if (commandersDict.Count == 0)
                {
                    if (write)
                    {
                        Create("Jameson (Default)");
                    }
                    else
                    {
                        commandersDict[maxnr + 1] = new EDCommander(maxnr + 1, "Jameson (Default)");
                    }
                }

                EDCommander hidden = new EDCommander(-1, "Hidden Log");     // -1 is the hidden commander, add to list to make it
                commandersDict[-1] = hidden;        // so we give back a valid entry when its selected
            }

            // For  some people sharing their user DB between different computers and having different paths to their journals on those computers.
            if (File.Exists(Path.Combine(EliteDangerousCore.EliteConfigInstance.InstanceOptions.AppDataDirectory, "CommanderPaths.json")))
            {
                JObject jo;

                using (Stream stream = File.OpenRead(Path.Combine(EliteDangerousCore.EliteConfigInstance.InstanceOptions.AppDataDirectory, "CommanderPaths.json")))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        using (JsonTextReader jreader = new JsonTextReader(reader))
                        {
                            jo = JObject.Load(jreader);
                        }
                    }
                }

                foreach (var kvp in jo)
                {
                    string name = kvp.Key;
                    JObject props = kvp.Value as JObject;
                    EDCommander cmdr = GetCommander(name);
                    if (props != null && cmdr != null)
                    {
                        cmdr.JournalDir = props["JournalDir"].Str(cmdr.JournalDir);
                    }
                }
            }
        }


        public static List<EDCommander> GetCommanders()
        {
            List<EDCommander> commanders = new List<EDCommander>();

            if (EliteDangerousCore.DB.UserDatabase.Instance.GetSettingInt("DBVer", 1) >= 102)
            {
                UserDatabase.Instance.ExecuteWithDatabase(cn =>
                {
                    using (DbCommand cmd = cn.Connection.CreateCommand("SELECT * FROM Commanders"))
                    {
                        using (DbDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                EDCommander edcmdr = new EDCommander(reader);

                                string name = Convert.ToString(reader["Name"]);
                                string edsmapikey = Convert.ToString(reader["EdsmApiKey"]);

                                commanders.Add(edcmdr);
                            }
                        }
                    }
                });
            }

            return commanders;
        }


        #endregion

        #endregion

        #region Private properties and methods
        private static Dictionary<int, EDCommander> commandersDict;
        private static int commanderID = Int32.MinValue;

        private static Dictionary<int, EDCommander> commanders
        {
            get
            {
                if (commandersDict == null)
                {
                    Load(false);
                }
                return commandersDict;
            }
        }

#endregion

#region Instance
       
        private int nr;
        private bool deleted;
        private string name;
        private string edsmname;
        private string edsmapikey;
        private string egoname;
        private string egoapikey;
        private string inaraname;
        private string inaraapikey;
        private string journalDir;
        private bool syncToEdsm;
        private bool syncFromEdsm;
        private bool syncToEddn;
        private bool syncToEGO;
        private bool syncToInara;
        private string homesystem;
        private float mapzoom;
        private bool mapcentreonselection;
        private int mapcolour;

        public EDCommander()
        {
        }

        public EDCommander(DbDataReader reader)
        {
            nr = Convert.ToInt32(reader["Id"]);
            name = Convert.ToString(reader["Name"]);
            deleted = Convert.ToBoolean(reader["Deleted"]);
            journalDir = Convert.ToString(reader["JournalDir"]);

            syncToEdsm = Convert.ToBoolean(reader["SyncToEdsm"]);
            syncFromEdsm = Convert.ToBoolean(reader["SyncFromEdsm"]);
            edsmname = reader["EDSMName"] == DBNull.Value ? name : Convert.ToString(reader["EDSMName"]) ?? name;
            edsmapikey = Convert.ToString(reader["EdsmApiKey"]);

            syncToEGO = Convert.ToBoolean(reader["SyncToEGO"]);
            egoname = Convert.ToString(reader["EGOName"]);
            egoapikey = Convert.ToString(reader["EGOAPIKey"]);

            syncToInara = Convert.ToBoolean(reader["SyncToInara"]);
            inaraname = Convert.ToString(reader["InaraName"]);
            inaraapikey = Convert.ToString(reader["InaraAPIKey"]);

            syncToEddn = Convert.ToBoolean(reader["SyncToEddn"]);

            homesystem = Convert.ToString(reader["HomeSystem"]);        // may be null

            mapzoom = reader["MapZoom"] is System.DBNull ? 1.0f : (float)Convert.ToDouble(reader["MapZoom"]);
            mapcolour = reader["MapColour"] is System.DBNull ? System.Drawing.Color.Red.ToArgb() : Convert.ToInt32(reader["MapColour"]);
            mapcentreonselection = reader["MapCentreOnSelection"] is System.DBNull ? true : Convert.ToBoolean(reader["MapCentreOnSelection"]);
            
        }

        public EDCommander(int id, string Name )
        {
            this.nr = id;
            this.name = Name;

            this.syncToEdsm = false;
            this.syncFromEdsm = false;
            this.edsmname = "";
            this.edsmapikey = "";

            this.syncToEGO = false;
            this.egoname = "";
            this.egoapikey = "";

            this.SyncToInara = false;
            this.inaraname = "";
            this.inaraapikey = "";
            this.homesystem = "";
            this.mapcentreonselection = true;
            this.mapzoom = 1.0f;
            this.MapColour = System.Drawing.Color.Red.ToArgb();

            this.syncToEddn = false;
        }

        public int Nr { get { return nr; }  private set { nr = value;  } }

        public string Name { get { return name; } set { name = value; } }
        public string EdsmName { get { return edsmname; } set { edsmname = value; } }
        public string EDSMAPIKey { get { return edsmapikey; } set { edsmapikey = value; } }
        public string EGOName { get { return egoname; } set { egoname = value; } }
        public string EGOAPIKey { get { return egoapikey; } set { egoapikey = value; } }
        public string InaraName { get { return inaraname; } set { inaraname = value; } }
        public string InaraAPIKey { get { return inaraapikey; } set { inaraapikey = value; } }
        public string HomeSystemTextOrSol { get { return homesystem.HasChars() ? homesystem : "Sol"; } set { homesystem = value; } }
        public ISystem HomeSystemIOrSol { get
            {
                return SystemCache.FindSystem(HomeSystemTextOrSol) ?? new SystemClass("Sol", 0, 0, 0);
            } }

        public float MapZoom { get { return mapzoom; } set { mapzoom = value; } }
        public int MapColour { get { return mapcolour; } set { mapcolour = value; } }
        public bool MapCentreOnSelection { get { return mapcentreonselection; } set { mapcentreonselection = value; } }

        public string JournalDir { get { return journalDir; } set { journalDir = value; } }
        public bool SyncToEdsm { get { return syncToEdsm; } set { syncToEdsm = value; } }
        public bool SyncFromEdsm { get { return syncFromEdsm; } set { syncFromEdsm = value; } }
        public bool SyncToEddn {  get { return syncToEddn; } set { syncToEddn = value;  } }
        //public bool SyncToEGO { get { return syncToEGO; } set { syncToEGO = value; } } disabled
        public bool SyncToEGO { get { return false; } set {  } } 
        public bool SyncToInara { get { return syncToInara; } set { syncToInara = value; } }
        public bool Deleted { get { return deleted; } set { deleted = value; } }

        public string Info { get
            {
                return BaseUtils.FieldBuilder.Build(";To EDDN", syncToEddn, ";To EDSM", syncToEdsm, ";From EDSM", syncFromEdsm, ";To Inara" , syncToInara, ";To EGO", syncToEGO);
            } }

#endregion
    }
}
