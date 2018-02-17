using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using BootstrapLib;

namespace ImprovedSaves
{
    class DefaultPatch : IPatch
    {
        public void InitializePatch(ModuleDefMD module)
        {
            var importer = new Importer(module);

            // Import and store the patching methods
            var performSaveMethod = importer.Import(typeof(SaveHelper).GetMethod("PerformSave"));
            var startSaveInvokerMethod = importer.Import(typeof(SaveHelper).GetMethod("StartSaveInvoker"));

            var playerType = module.Find("Player", false);

            var autoSaveMethod = new MethodDefUser(
                "Autosave",
                MethodSig.CreateInstance(module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.ReuseSlot
            );

            playerType.Methods.Add(autoSaveMethod);

            var body = autoSaveMethod.Body = new CilBody();

            body.Instructions.Add(OpCodes.Call.ToInstruction(performSaveMethod));
            body.Instructions.Add(OpCodes.Ret.ToInstruction());


            var awakeMethod = playerType.FindMethod("Awake");
            var instructions = awakeMethod.Body.Instructions;

            instructions.Insert(instructions.Count - 1, OpCodes.Ldarg_0.ToInstruction());
            instructions.Insert(instructions.Count - 1, OpCodes.Call.ToInstruction(startSaveInvokerMethod));
        }
    }
}