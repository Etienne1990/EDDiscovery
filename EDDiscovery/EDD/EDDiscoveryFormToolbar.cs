﻿/*
 * Copyright © 2015 - 2022 EDDiscovery development team
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

using EDDiscovery.Forms;
using EliteDangerousCore;
using EliteDangerousCore.EDSM;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EDDiscovery
{
    public partial class EDDiscoveryForm
    {
        public void UpdateCommandersListBox()
        {
            comboBoxCommander.Enabled = false;
            comboBoxCommander.Items.Clear();            // comboBox is nicer with items

            if (EDDOptions.Instance.DisableCommanderSelect)
            {
                comboBoxCommander.Items.Add("Jameson");
                comboBoxCommander.SelectedIndex = 0;
            }
            else
            {
                comboBoxCommander.Items.AddRange((from EDCommander c in EDCommander.GetListInclHidden() select c.Name).ToList());
                if (History.CommanderId == -1)  // is hidden log
                {
                    comboBoxCommander.SelectedIndex = 0;
                }
                else
                {
                    comboBoxCommander.SelectedItem = EDCommander.Current.Name;
                }

                comboBoxCommander.Enabled = true;
            }
        }

        private void comboBoxCommander_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxCommander.SelectedIndex >= 0 && comboBoxCommander.Enabled)     // DONT trigger during LoadCommandersListBox
            {
                var itm = (from EDCommander c in EDCommander.GetListInclHidden() where c.Name.Equals(comboBoxCommander.Text) select c).ToList();
                ChangeToCommander(itm[0].Id);
            }
        }

        public void RefreshButton(bool state)
        {
            buttonExtRefresh.Enabled = state;
        }

        private void buttonExtRefresh_Click(object sender, EventArgs e)
        {
            LogLine("Refresh History.".T(EDTx.EDDiscoveryForm_RH));
            RefreshHistoryAsync();
        }

        private void sendUnsyncedEDSMJournalsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EDSMClass edsm = new EDSMClass();

            if (!edsm.ValidCredentials)
            {
                ExtendedControls.MessageBoxTheme.Show(this, "No EDSM API key set".T(EDTx.EDDiscoveryForm_NoEDSMAPI));
                return;
            }

            if (!EDCommander.Current.SyncToEdsm)
            {
                string dlgtext = "You have disabled sync to EDSM for this commander.  Are you sure you want to send unsynced events to EDSM?".T(EDTx.EDDiscoveryForm_ConfirmSyncToEDSM);
                string dlgcapt = "Confirm EDSM sync".T(EDTx.EDDiscoveryForm_ConfirmSyncToEDSMCaption);

                if (ExtendedControls.MessageBoxTheme.Show(this, dlgtext, dlgcapt, MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            EDSMSend();
        }

        private void ComboBoxCustomProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxCustomProfiles.SelectedIndex >= 0 && comboBoxCustomProfiles.Enabled)
            {
                if (comboBoxCustomProfiles.SelectedIndex == comboBoxCustomProfiles.Items.Count - 1)    // last one if edit profiles
                {
                    Forms.ProfileEditor pe = new ProfileEditor();
                    pe.Init(EDDProfiles.Instance, this.Icon);
                    if (pe.ShowDialog() == DialogResult.OK)
                    {
                        bool removedcurprofile = EDDProfiles.Instance.UpdateProfiles(pe.Result, pe.PowerOnIndex);       // see if the current one has changed...

                        if (removedcurprofile)
                            ChangeToProfileId(EDDProfiles.DefaultId, false);
                    }

                    UpdateProfileComboBox();
                }
                else
                {
                    ChangeToProfileId(EDDProfiles.Instance.IdOfIndex(comboBoxCustomProfiles.SelectedIndex), true);
                }
            }
        }

        ExtendedControls.ExtListBoxForm popoutdropdown;

        private void buttonExtPopOut_Click(object sender, EventArgs e)
        {
            popoutdropdown = new ExtendedControls.ExtListBoxForm("", true);
            popoutdropdown.StartPosition = FormStartPosition.Manual;
            popoutdropdown.Items = PanelInformation.GetUserSelectablePanelDescriptions(EDDConfig.Instance.SortPanelsByName).ToList();
            popoutdropdown.ImageItems = PanelInformation.GetUserSelectablePanelImages(EDDConfig.Instance.SortPanelsByName).ToList();
            popoutdropdown.ItemSeperators = PanelInformation.GetUserSelectableSeperatorIndex(EDDConfig.Instance.SortPanelsByName);
            PanelInformation.PanelIDs[] pids = PanelInformation.GetUserSelectablePanelIDs(EDDConfig.Instance.SortPanelsByName);
            popoutdropdown.FlatStyle = FlatStyle.Popup;
            popoutdropdown.PositionBelow(buttonExtPopOut);
            popoutdropdown.FitImagesToItemHeight = true;
            popoutdropdown.SelectedIndexChanged += (s, ea) =>
            {
                PopOuts.PopOut(pids[popoutdropdown.SelectedIndex]);
            };

            ExtendedControls.Theme.Current.ApplyStd(popoutdropdown, true);
            popoutdropdown.SelectionBackColor = ExtendedControls.Theme.Current.ButtonBackColor;
            popoutdropdown.Show(this);
        }

        private void extButtonDrawnHelp_Click(object sender, EventArgs e)
        {
            tabControlMain.HelpOn(this, extButtonDrawnHelp.PointToScreen(new Point(0, extButtonDrawnHelp.Bottom)), tabControlMain.SelectedIndex);
        }

        private void buttonReloadActions_Click(object sender, EventArgs e)
        {
            actioncontroller.ReLoad();
            actioncontroller.CheckWarn();
            actioncontroller.onStartup();
            Controller.ResetUIStatus();

            // keep for debug:

            //var tx = BaseUtils.Translator.Instance.NotUsed();  foreach (var s in tx) System.Diagnostics.Debug.WriteLine(s); // turn on usetracker at top to use

            //if (FrontierCAPI.Active && !EDCommander.Current.ConsoleCommander)
            //  Controller.DoCAPI(history.GetLast.Status.StationName, history.GetLast.System.Name, false, history.Shipyards.AllowCobraMkIV);
        }

        private void extButtonCAPI_Click(object sender, EventArgs e)
        {
            var he = History.GetLast;
            if (he != null && he.Status.IsDocked)
                Controller.DoCAPI(he.WhereAmI, he.System.Name, History.Shipyards.AllowCobraMkIV);
        }
    }
}
