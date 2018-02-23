using System;
using System.Collections;
using System.Collections.Generic;
using BootstrapLib;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using UnityEngine;

namespace NewRecipes
{
    public class DefaultMod : IMod, IPatch
    {
        public void InitializeMod()
        {
            var craftData = typeof(CraftData);

            var techDataField = craftData.GetField("techData", BindingFlags.NonPublic | BindingFlags.Static);
            var techData = techDataField.GetValue(null) as IDictionary;

            var TechDataClass = craftData.GetNestedType("TechData", BindingFlags.NonPublic);

            var ingredientsField =
                TechDataClass.GetField("_ingredients", BindingFlags.Public | BindingFlags.Instance);
            var craftAmountField =
                TechDataClass.GetField("_craftAmount", BindingFlags.Public | BindingFlags.Instance);

            var IngredientClass = craftData.GetNestedType("Ingredient", BindingFlags.NonPublic);
            var IngredientsClass = craftData.GetNestedType("Ingredients", BindingFlags.NonPublic);

            // Adjust Scrap Metal -> Titanium (+1)
            var titaniumTech = techData[TechType.Titanium];
            craftAmountField.SetValue(titaniumTech, 5);

            // Add Scrap Metal to the crafting tree
            var ingredient = Activator.CreateInstance(IngredientClass, new object[] {TechType.TitaniumIngot, 1});
            var ingredients = Activator.CreateInstance(IngredientsClass) as IList;
            ingredients.Add(ingredient);

            var scrapTechData = Activator.CreateInstance(TechDataClass);
            ingredientsField.SetValue(scrapTechData, ingredients);
            craftAmountField.SetValue(scrapTechData, 2);

            techData.Add(TechType.ScrapMetal, scrapTechData);

            // Add the Scrap to the crafting menu
            var craftTree = typeof(CraftTree);
            var fabricatorTree = CraftTree.GetTree(CraftTree.Type.Fabricator);
            var basicMaterials = fabricatorTree.nodes["Resources"]["BasicMaterials"] as CraftNode;
            basicMaterials.AddNode(new CraftNode("ScrapMetal", TreeAction.Craft, TechType.ScrapMetal));
        }

        public static void AddNewKnownTech()
        {
            KnownTech.Add(TechType.ScrapMetal, false);
        }

        public void InitializePatch(ModuleDefMD module)
        {
            // Patching this separately as the default mod method fires too early
            var importer = new Importer(module);
            var addKnownTechMethod = importer.Import(typeof(DefaultMod).GetMethod("AddNewKnownTech"));

            var knownTechType = module.Find("KnownTech", false);
            var initializeMethod = knownTechType.FindMethod("Initialize");
            initializeMethod.Body.Instructions.Insert(initializeMethod.Body.Instructions.Count - 1,
                OpCodes.Call.ToInstruction(addKnownTechMethod));
        }
    }
}