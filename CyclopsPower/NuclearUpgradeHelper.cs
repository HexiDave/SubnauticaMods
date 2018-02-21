using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using BootstrapLib;
using UnityEngine;

namespace CyclopsPower
{
    public class NuclearUpgradeHelper : IMod
    {
        private static readonly FieldInfo EquipmentTypesField;

        private static readonly FieldInfo SlotNamesField;
        private static readonly FieldInfo LiveField;
        private static readonly FieldInfo NuclearUpgradeField;
        public static readonly int NuclearReactorOrCyclopsModule = 100;

        static NuclearUpgradeHelper()
        {
            var craftDataType = typeof(CraftData);

            EquipmentTypesField = craftDataType.GetField("equipmentTypes", BindingFlags.Static | BindingFlags.Public);

            var subRootType = typeof(SubRoot);
            SlotNamesField = subRootType.GetField("slotNames", BindingFlags.Static | BindingFlags.NonPublic);
            LiveField = subRootType.GetField("live", BindingFlags.NonPublic | BindingFlags.Instance);
            NuclearUpgradeField =
                subRootType.GetField("nuclearUpgrade", BindingFlags.Public | BindingFlags.Instance);
        }

        public static void ChangeNuclearRodEquipmentTypes()
        {
            var equipmentTypes = EquipmentTypesField.GetValue(null) as Dictionary<TechType, EquipmentType>;
            equipmentTypes[TechType.ReactorRod] = (EquipmentType) NuclearReactorOrCyclopsModule;
        }

        public static bool IsCompatible(EquipmentType itemType, EquipmentType slotType)
        {
            return
                itemType == slotType ||
                (itemType == EquipmentType.VehicleModule &&
                 (slotType == EquipmentType.SeamothModule || slotType == EquipmentType.ExosuitModule)) ||
                (itemType == (EquipmentType) NuclearReactorOrCyclopsModule &&
                 (slotType == EquipmentType.NuclearReactor || slotType == EquipmentType.CyclopsModule));
        }

        public static void SetCyclopsUpgrades(SubRoot subRoot)
        {
            var live = LiveField.GetValue(subRoot) as LiveMixin;
            if (subRoot.upgradeConsole == null || !live.IsAlive()) return;

            subRoot.shieldUpgrade = false;
            subRoot.sonarUpgrade = false;
            subRoot.vehicleRepairUpgrade = false;
            subRoot.decoyTubeSizeIncreaseUpgrade = false;
            subRoot.thermalReactorUpgrade = false;
            var nuclearUpgrades = 0;

            var refreshArray = new TechType[6];
            var modules = subRoot.upgradeConsole.modules;
            var slotNames = SlotNamesField.GetValue(null) as string[];

            for (var i = 0; i < 6; i++)
            {
                var techTypeInSlot = modules.GetTechTypeInSlot(slotNames[i]);

                switch (techTypeInSlot)
                {
                    case TechType.CyclopsShieldModule:
                        subRoot.shieldUpgrade = true;
                        break;
                    case TechType.CyclopsSonarModule:
                        subRoot.sonarUpgrade = true;
                        break;
                    case TechType.CyclopsSeamothRepairModule:
                        subRoot.vehicleRepairUpgrade = true;
                        break;
                    case TechType.CyclopsDecoyModule:
                        subRoot.decoyTubeSizeIncreaseUpgrade = true;
                        break;
                    case TechType.CyclopsThermalReactorModule:
                        subRoot.thermalReactorUpgrade = true;
                        break;
                    case TechType.ReactorRod:
                        nuclearUpgrades++;
                        break;
                }

                refreshArray[i] = techTypeInSlot;
            }

            NuclearUpgradeField.SetValue(subRoot, nuclearUpgrades);

            if (subRoot.slotModSFX != null)
            {
                subRoot.slotModSFX.Play();
            }

            subRoot.BroadcastMessage("RefreshUpgradeConsoleIcons", refreshArray,
                SendMessageOptions.RequireReceiver);
        }

        public static void UpdateNuclearRecharge(SubRoot subRoot)
        {
            var nuclearUpgrades = (int) NuclearUpgradeField.GetValue(subRoot);
            if (nuclearUpgrades > 0)
            {
                const float powerPerUpgrade = 1.5f;
                subRoot.powerRelay.AddEnergy(Time.deltaTime * powerPerUpgrade * (float) nuclearUpgrades,
                    out var outAmount);
            }
        }

        public void InitializeMod()
        {
            ChangeNuclearRodEquipmentTypes();
        }
    }
}