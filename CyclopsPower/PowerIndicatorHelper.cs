using System.Reflection;
using UnityEngine;

namespace CyclopsPower
{
    public static class PowerIndicatorHelper
    {
        private static readonly FieldInfo Field;

        static PowerIndicatorHelper()
        {
            Field = typeof(uGUI_PowerIndicator).GetField("initialized",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static bool IsPowerInitialized(uGUI_PowerIndicator powerIndicator)
        {
            return (bool) Field.GetValue(powerIndicator);
        }

        public static bool IsPowerEnabled(uGUI_PowerIndicator powerIndicator, out int power, out int maxPower,
            out PowerSystem.Status status)
        {
            power = 0;
            maxPower = 0;
            status = PowerSystem.Status.Offline;

            if (
                !IsPowerInitialized(powerIndicator) ||
                !uGUI.isMainLevel ||
                uGUI.isIntro ||
                LaunchRocket.isLaunching ||
                !GameModeUtils.RequiresPower()
            )
            {
                return false;
            }

            var main = Player.main;
            if (main == null)
            {
                return false;
            }

            var pda = main.GetPDA();
            if (pda != null && pda.isInUse)
            {
                return false;
            }

            if (main.escapePod.value)
            {
                var currentEscapePod = main.currentEscapePod;
                if (currentEscapePod != null)
                {
                    var component = currentEscapePod.GetComponent<PowerRelay>();
                    if (component != null)
                    {
                        power = Mathf.RoundToInt(component.GetPower());
                        maxPower = Mathf.RoundToInt(component.GetMaxPower());
                        status = component.GetPowerStatus();
                        return true;
                    }
                }
            }

            var currentSub = main.currentSub;
            if (currentSub == null) return false;

            var powerRelay = currentSub.powerRelay;
            if (powerRelay == null) return false;

            power = Mathf.RoundToInt(powerRelay.GetPower());
            maxPower = Mathf.RoundToInt(powerRelay.GetMaxPower());
            status = powerRelay.GetPowerStatus();

            return true;
        }
    }
}