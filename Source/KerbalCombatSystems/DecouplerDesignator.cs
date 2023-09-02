﻿using System;
using UnityEngine;

namespace KerbalCombatSystems
{
    public class ModuleDecouplerDesignate : PartModule
    {
        [KSPField(isPersistant = true)]
        public string seperatorType = "";

        [KSPField(isPersistant = true)]
        public bool seperated = false;

        const string groupName = "KCS Designation";
        readonly static string[] types = new string[] { "Default", "Warhead", "Escape Pod" };

        [KSPField(
            isPersistant = true,
            guiActive = true,
            guiActiveEditor = true,
            guiName = "Type",
            groupName = groupName,
            groupDisplayName = groupName)]
        [UI_ChooseOption(controlEnabled = true, affectSymCounterparts = UI_Scene.None)]
        public string decouplerDesignation = "Default";

        public override void OnAwake()
        {
            UI_ChooseOption optionsField;

            if (HighLogic.LoadedSceneIsEditor)
                optionsField = Fields[nameof(decouplerDesignation)].uiControlEditor as UI_ChooseOption;
            else
                optionsField = Fields[nameof(decouplerDesignation)].uiControlFlight as UI_ChooseOption;

            optionsField.options = types;
        }

        public void Separate()
        {
            switch(seperatorType)
            {
                case "anchor":
                    part.GetComponent<ModuleAnchoredDecoupler>().Decouple();
                    break;
                case "stack":
                    part.GetComponent<ModuleDecouple>().Decouple();
                    break;
                case "port":
                    ModuleDockingNode node = part.GetComponent<ModuleDockingNode>();
                    if (node == null || node.state == "Ready")
                        break;

                    if (node.state == "Disengage")
                        Debug.Log("hi");

                    if (node.state == "Disengage" || node.state == "PreAttached")
                        node.Decouple();
                    else
                        node.Undock();

                    break;
                default:
                    Debug.Log("[KCS]: Improper Decoupler Designation");
                    break;
            }

            seperated = true;
        }
    }

    public enum DecouplerDesignation
    {
        Default,
        Warhead,
        EscapePod
    }
}
