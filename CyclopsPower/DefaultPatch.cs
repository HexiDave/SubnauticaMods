using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using BootstrapLib;

namespace CyclopsPower
{
    class DefaultPatch : IPatch
    {
        public void InitializePatch(ModuleDefMD module)
        {
            PatchPowerIndicator(module);
            PatchNuclearUpgrade(module);
        }

        private void PatchPowerIndicator(ModuleDefMD module)
        {
            var importer = new Importer(module);
            var patchIsPowerEnabled = importer.Import(typeof(PowerIndicatorHelper).GetMethod("IsPowerEnabled"));

            var powerIndicatorType = module.Find("uGUI_PowerIndicator", false);
            var isPowerEnabledMethod = powerIndicatorType.FindMethod("IsPowerEnabled");

            var body = isPowerEnabledMethod.Body = new CilBody();

            foreach (var instruction in new[]
            {
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldarg_1.ToInstruction(),
                OpCodes.Ldarg_2.ToInstruction(),
                OpCodes.Ldarg_3.ToInstruction(),
                OpCodes.Call.ToInstruction(patchIsPowerEnabled),
                OpCodes.Ret.ToInstruction()
            })
            {
                body.Instructions.Add(instruction);
            }
        }

        private void PatchNuclearUpgrade(ModuleDefMD module)
        {
            PatchNuclearUpgrade_EquipmentType(module);
            PatchNuclearUpgrade_CraftData(module);
            PatchNuclearUpgrade_SubRoot(module);
        }

        private void PatchNuclearUpgrade_EquipmentType(ModuleDefMD module)
        {
            var equipmentType = module.Find("EquipmentType", false);

            FieldDef fieldToAdd =
                new FieldDefUser("NuclearReactorOrCyclopsModule", new FieldSig(new ValueTypeSig(equipmentType)))
                {
                    Constant = module.UpdateRowId(new ConstantUser(NuclearUpgradeHelper.NuclearReactorOrCyclopsModule,
                        equipmentType.GetEnumUnderlyingType().ElementType)),
                    Attributes = FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.HasDefault |
                                 FieldAttributes.Public
                };

            equipmentType.Fields.Add(fieldToAdd);
        }

        private void PatchNuclearUpgrade_CraftData(ModuleDefMD module)
        {
            var craftDataType = module.Find("CraftData", false);
            var equipmentTypesField = craftDataType.FindField("equipmentTypes");
            equipmentTypesField.Attributes = FieldAttributes.Public | FieldAttributes.Static;

            var equipmentType = module.Find("Equipment", false);
            var isCompatibleMethod = equipmentType.FindMethod("IsCompatible");

            var importer = new Importer(module);
            var isCompatiblePatchMethod = importer.Import(typeof(NuclearUpgradeHelper).GetMethod("IsCompatible"));

            var body = isCompatibleMethod.Body = new CilBody();
            body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
            body.Instructions.Add(OpCodes.Call.ToInstruction(isCompatiblePatchMethod));
            body.Instructions.Add(OpCodes.Ret.ToInstruction());
        }

        private void PatchNuclearUpgrade_SubRoot(ModuleDefMD module)
        {
            var subRootType = module.Find("SubRoot", false);

            var fieldToAdd = new FieldDefUser("nuclearUpgrade",
                new FieldSig(module.CorLibTypes.Int32))
            {
                Attributes = FieldAttributes.Public | FieldAttributes.NotSerialized
            };

            subRootType.Fields.Add(fieldToAdd);

            var setCyclopsUpgradesMethod = subRootType.FindMethod("SetCyclopsUpgrades");

            var importer = new Importer(module);
            var setCyclopsUpgradesPatchMethod =
                importer.Import(typeof(NuclearUpgradeHelper).GetMethod("SetCyclopsUpgrades"));

            var body = setCyclopsUpgradesMethod.Body = new CilBody();

            body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            body.Instructions.Add(OpCodes.Call.ToInstruction(setCyclopsUpgradesPatchMethod));
            body.Instructions.Add(OpCodes.Ret.ToInstruction());

            PatchNuclearUpgrade_SubRoot_Update(module, subRootType, importer);
        }

        private void PatchNuclearUpgrade_SubRoot_Update(ModuleDefMD module, TypeDef subRootType, Importer importer)
        {
            var updateMethod = subRootType.FindMethod("Update");

            var updateNuclearRechargePatchMethod = importer.Import(typeof(NuclearUpgradeHelper).GetMethod("UpdateNuclearRecharge"));

            var instructions = updateMethod.Body.Instructions;

            var instructionsToAdd = new[]
            {
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(updateNuclearRechargePatchMethod)
            };

            foreach (var instruction in instructionsToAdd)
            {
                instructions.Insert(instructions.Count - 1, instruction);
            }
        }
    }
}