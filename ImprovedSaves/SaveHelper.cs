using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImprovedSaves
{
    public static class SaveHelper
    {
        private const float AutosaveTime = 5f * 60f;

        public static void PerformSave()
        {
            if (IngameMenu.main.gameObject.activeSelf)
            {
                ErrorMessage.AddMessage("Unable to autosave - menu was open.");
                return;
            }

            IngameMenu.main.SaveGame();
            IngameMenu.main.OnDeselect();
        }

        public static void StartSaveInvoker(Player player)
        {
            player.InvokeRepeating("Autosave", AutosaveTime, AutosaveTime);
        }
    }
}