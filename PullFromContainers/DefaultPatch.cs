using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using BootstrapLib;

namespace PullFromContainers
{
    class DefaultPatch : IPatch
    {
        IMethod CrafterLogic_IsCraftRecipeFulfilled_Patch;
        MethodDef CrafterLogic_IsCraftRecipeFulfilled_Original;

        IMethod CrafterLogic_ConsumeResources_Patch;
        MethodDef CrafterLogic_ConsumeResources_Original;

        IMethod Constructable_Construct_Patch;

        IMethod TooltipFactory_WriteIngredients_Patch;
        MethodDef TooltipFactory_WriteIngredients_Original;

        public void InitializePatch(ModuleDefMD module)
        {
            Importer importer = new Importer(module);

            // Import and store the patching methods
            CrafterLogic_IsCraftRecipeFulfilled_Patch = importer.Import(typeof(CrafterLogicHelper).GetMethod("IsCraftRecipeFulfilled"));
            CrafterLogic_ConsumeResources_Patch = importer.Import(typeof(CrafterLogicHelper).GetMethod("ConsumeResources"));
            Constructable_Construct_Patch = importer.Import(typeof(ConstructableHelper).GetMethod("Construct"));
            TooltipFactory_WriteIngredients_Patch = importer.Import(typeof(CrafterLogicHelper).GetMethod("WriteIngredients"));

            // CrafterLogic
            var originalCrafterLogic = module.Find("CrafterLogic", false);

            // Get the original methods we're going to replace
            CrafterLogic_IsCraftRecipeFulfilled_Original = originalCrafterLogic.FindMethod("IsCraftRecipeFulfilled");
            CrafterLogic_ConsumeResources_Original = originalCrafterLogic.FindMethod("ConsumeResources");

            // Patch CrafterLogic
            Patch_CrafterLogic_IsCraftRecipeFulfilled(module);

            // Patch Construtable
            Patch_Constructable_Construct(module);

            // Patch the tooltip
            var originalTooltipFactory = module.Find("TooltipFactory", false);
            TooltipFactory_WriteIngredients_Original = originalTooltipFactory.FindMethod("WriteIngredients");

            Patch_Tooltip_WriteIngredients(module, originalTooltipFactory);
        }

        void Patch_CrafterLogic_IsCraftRecipeFulfilled(ModuleDef module)
        {
            var parentMethod = module.Find("uGUI_CraftingMenu", false).FindMethod("ActionAvailable");

            PatchHelper.ReplaceCall(parentMethod, CrafterLogic_IsCraftRecipeFulfilled_Original, CrafterLogic_IsCraftRecipeFulfilled_Patch);

            // Continue the patch chain
            var parentMethods = new[]
            {
                module.Find("ConstructorInput", false).FindMethod("Craft"),
                module.Find("GhostCrafter", false).FindMethod("Craft"),
                module.Find("RocketConstructor", false).FindMethod("StartRocketConstruction")
            };

            foreach (var method in parentMethods)
            {
                PatchHelper.ReplaceCall(method, CrafterLogic_ConsumeResources_Original, CrafterLogic_ConsumeResources_Patch);
            }
        }

        void Patch_Constructable_Construct(ModuleDef module)
        {
            var parentMethod = module.Find("Constructable", false).FindMethod("Construct");

            // Just replace the call with the new static version

            parentMethod.Body = new CilBody();

            var instructions = parentMethod.Body.Instructions;

            instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            instructions.Add(OpCodes.Call.ToInstruction(Constructable_Construct_Patch));
            instructions.Add(OpCodes.Ret.ToInstruction());
        }

        void Patch_Tooltip_WriteIngredients(ModuleDef module, TypeDef parentType)
        {
            var parentMethods = new[]
            {
                parentType.FindMethod("BuildTech"),
                parentType.FindMethod("Recipe")
            };

            foreach (var method in parentMethods)
            {
                PatchHelper.ReplaceCall(method, TooltipFactory_WriteIngredients_Original, TooltipFactory_WriteIngredients_Patch);
            }
        }
    }
}
