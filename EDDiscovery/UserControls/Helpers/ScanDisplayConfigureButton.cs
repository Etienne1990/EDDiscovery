﻿/*
 * Copyright © 2023-2023 EDDiscovery development team
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
 */

using System.Linq;

namespace EDDiscovery.UserControls
{
    // Control providing edsm/spansh settings, saving loading

    public class ScanDisplayConfigureButton : ExtendedControls.ExtButtonWithCheckedIconListBoxGroup
    {
        // use ValueChanged to pick up the change 

        public string Setting { get; private set; }
        public string[] DisplayFilters { get { return Setting.Split(";"); } }

        public void Init(EliteDangerousCore.DB.IUserDatabaseSettingsSaver ucb, string settingname)
        {
            Setting = ucb.GetSetting(settingname, "moons;icons;mats;allg;habzone;starclass;planetclass;dist;starage;starmass");

            Init(new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption[]
            {
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("icons", "Show body status icons".T(EDTx.UserControlScan_StatusIcons), global::EDDiscovery.Icons.Controls.Scan_ShowOverlays),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("mats", "Show Materials".T(EDTx.UserControlScan_ShowMaterials), global::EDDiscovery.Icons.Controls.Scan_ShowAllMaterials),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("rares", "Show rare materials only".T(EDTx.UserControlScan_ShowRaresOnly), global::EDDiscovery.Icons.Controls.Scan_ShowRareMaterials),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("matfull", "Hide materials which have reached their storage limit".T(EDTx.UserControlScan_MatFull), global::EDDiscovery.Icons.Controls.Scan_HideFullMaterials),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("moons", "Show Moons".T(EDTx.UserControlScan_ShowMoons), global::EDDiscovery.Icons.Controls.Scan_ShowMoons),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("allg", "Show G on all planets".T(EDTx.UserControlScan_AllG), global::EDDiscovery.Icons.Controls.ShowAllG),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("planetmass", "Show mass of planets".T(EDTx.UserControlScan_PlanetMass), global::EDDiscovery.Icons.Controls.ShowAllG),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("starmass", "Show mass of stars".T(EDTx.UserControlScan_StarMass), global::EDDiscovery.Icons.Controls.ShowAllG),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("starage", "Show age of stars".T(EDTx.UserControlScan_StarAge), global::EDDiscovery.Icons.Controls.ShowAllG),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("habzone", "Show Habitation Zone".T(EDTx.UserControlScan_HabZone), global::EDDiscovery.Icons.Controls.ShowHabZone),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("starclass", "Show Classes of Stars".T(EDTx.UserControlScan_StarClass), global::EDDiscovery.Icons.Controls.ShowStarClasses),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("planetclass", "Show Classes of Planets".T(EDTx.UserControlScan_PlanetClass), global::EDDiscovery.Icons.Controls.ShowPlanetClasses),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("dist", "Show distance of bodies".T(EDTx.UserControlScan_Distance), global::EDDiscovery.Icons.Controls.ShowDistances),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("sys", "Show system and value in main display".T(EDTx.UserControlScan_SystemValue), global::EDDiscovery.Icons.Controls.Scan_DisplaySystemAlways),
            new ExtendedControls.CheckedIconListBoxFormGroup.StandardOption("starsondiffline", "Show bodyless stars on separate lines".T(EDTx.UserControlScan_StarsOnDiffLines), global::EDDiscovery.Icons.Controls.ShowStarClasses),
            }, 
            Setting,
            (newsetting,ch) => {
                Setting = newsetting;
                ucb.PutSetting(settingname, newsetting);
            },
            allornoneshown:true,
            closeboundaryregion:new System.Drawing.Size(64,64));
        }

        public void ApplyDisplayFilters(ScanDisplayUserControl sduc)
        {
            var displayfilters = DisplayFilters;
            bool all = displayfilters.Contains("All");
            sduc.SystemDisplay.ShowMoons = displayfilters.Contains("moons") || all;
            sduc.SystemDisplay.ShowOverlays = displayfilters.Contains("icons") || all;
            sduc.SystemDisplay.ShowMaterials = displayfilters.Contains("mats") || all;
            sduc.SystemDisplay.ShowOnlyMaterialsRare = displayfilters.Contains("rares") || all;
            sduc.SystemDisplay.HideFullMaterials = displayfilters.Contains("matfull") || all;
            sduc.SystemDisplay.ShowAllG = displayfilters.Contains("allg") || all;
            sduc.SystemDisplay.ShowPlanetMass = displayfilters.Contains("planetmass") || all;
            sduc.SystemDisplay.ShowStarMass = displayfilters.Contains("starmass") || all;
            sduc.SystemDisplay.ShowStarAge = displayfilters.Contains("starage") || all;
            sduc.SystemDisplay.ShowHabZone = displayfilters.Contains("habzone") || all;
            sduc.SystemDisplay.ShowStarClasses = displayfilters.Contains("starclass") || all;
            sduc.SystemDisplay.ShowPlanetClasses = displayfilters.Contains("planetclass") || all;
            sduc.SystemDisplay.ShowDist = displayfilters.Contains("dist") || all;
            sduc.SystemDisplay.NoPlanetStarsOnSameLine = displayfilters.Contains("starsondiffline") || all;
        }

    }
}
