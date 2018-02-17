using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
    }
}