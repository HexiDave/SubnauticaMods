using BootstrapLib;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace PullFromContainers
{
    class DefaultPatch : IPatch
    {
        private IMethod _constructableConstructPatch;
        private MethodDef _crafterLogicConsumeResourcesOriginal;

        private IMethod _crafterLogicConsumeResourcesPatch;
        private MethodDef _crafterLogicIsCraftRecipeFulfilledOriginal;
        private IMethod _crafterLogicIsCraftRecipeFulfilledPatch;
        private MethodDef _tooltipFactoryWriteIngredientsOriginal;

        private IMethod _tooltipFactoryWriteIngredientsPatch;

        public void InitializePatch(ModuleDefMD module)
        {
            var importer = new Importer(module);

            // Import and store the patching methods
            _crafterLogicIsCraftRecipeFulfilledPatch =
                importer.Import(typeof(CrafterLogicHelper).GetMethod("IsCraftRecipeFulfilled"));
            _crafterLogicConsumeResourcesPatch =
                importer.Import(typeof(CrafterLogicHelper).GetMethod("ConsumeResources"));
            _constructableConstructPatch = importer.Import(typeof(ConstructableHelper).GetMethod("Construct"));
            _tooltipFactoryWriteIngredientsPatch =
                importer.Import(typeof(CrafterLogicHelper).GetMethod("WriteIngredients"));

            // CrafterLogic
            var originalCrafterLogic = module.Find("CrafterLogic", false);

            // Get the original methods we're going to replace
            _crafterLogicIsCraftRecipeFulfilledOriginal = originalCrafterLogic.FindMethod("IsCraftRecipeFulfilled");
            _crafterLogicConsumeResourcesOriginal = originalCrafterLogic.FindMethod("ConsumeResources");

            // Patch CrafterLogic
            Patch_CrafterLogic_IsCraftRecipeFulfilled(module);

            // Patch Construtable
            Patch_Constructable_Construct(module);

            // Patch the tooltip
            var originalTooltipFactory = module.Find("TooltipFactory", false);
            _tooltipFactoryWriteIngredientsOriginal = originalTooltipFactory.FindMethod("WriteIngredients");

            Patch_Tooltip_WriteIngredients(module, originalTooltipFactory);
        }

        private void Patch_CrafterLogic_IsCraftRecipeFulfilled(ModuleDef module)
        {
            var parentMethod = module.Find("uGUI_CraftingMenu", false).FindMethod("ActionAvailable");

            PatchHelper.ReplaceCall(parentMethod, _crafterLogicIsCraftRecipeFulfilledOriginal,
                _crafterLogicIsCraftRecipeFulfilledPatch);

            // Continue the patch chain
            var parentMethods = new[]
            {
                module.Find("ConstructorInput", false).FindMethod("Craft"),
                module.Find("GhostCrafter", false).FindMethod("Craft"),
                module.Find("RocketConstructor", false).FindMethod("StartRocketConstruction")
            };

            foreach (var method in parentMethods)
                PatchHelper.ReplaceCall(method, _crafterLogicConsumeResourcesOriginal,
                    _crafterLogicConsumeResourcesPatch);
        }

        private void Patch_Constructable_Construct(ModuleDef module)
        {
            var parentMethod = module.Find("Constructable", false).FindMethod("Construct");

            // Just replace the call with the new static version

            parentMethod.Body = new CilBody();

            var instructions = parentMethod.Body.Instructions;

            instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            instructions.Add(OpCodes.Call.ToInstruction(_constructableConstructPatch));
            instructions.Add(OpCodes.Ret.ToInstruction());
        }

        private void Patch_Tooltip_WriteIngredients(ModuleDef module, TypeDef parentType)
        {
            var parentMethods = new[]
            {
                parentType.FindMethod("BuildTech"),
                parentType.FindMethod("Recipe")
            };

            foreach (var method in parentMethods)
                PatchHelper.ReplaceCall(method, _tooltipFactoryWriteIngredientsOriginal,
                    _tooltipFactoryWriteIngredientsPatch);
        }
    }
}