using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PullFromContainers
{
    public static class CrafterLogicHelper
    {
        // Change this to adjust the max distance from the player a container can be found within
        readonly static float MaxDistance = 100f;
        readonly static float MaxDistanceSq = MaxDistance * MaxDistance;

        static double lastContainerCheckTime = 0.0;

        static ItemsContainer[] nearbyItemContainers;

        /// <summary>
        /// This finds all the containers - plus your inventory - to pull resources from
        /// </summary>
        /// <returns>All the containers</returns>
        public static ItemsContainer[] FindAllItemsContainersInRange()
        {
            // We use this time-based hack to cache the results since it would require changes in many more files to do it top-down
            if (DayNightCycle.main.timePassed > 1.0 + lastContainerCheckTime || nearbyItemContainers == null)
            {
                // Let Unity to the initial leg-work
                var allStorageContainers = UnityEngine.Object.FindObjectsOfType<StorageContainer>();

                var containersList = allStorageContainers
                    .Where(x => x.container != null && !x.name.StartsWith("Aquarium")) // Ignore the poor fishies!
                    .Select(x => new
                    {
                        Container = x.container,
                        Distance = (Player.main.transform.position - x.transform.position).sqrMagnitude
                    })
                    .Where(x => x.Distance < MaxDistanceSq)
                    .OrderBy(x => x.Distance)
                    .Select(x => x.Container)
                    .ToList();

                containersList.Insert(0, Inventory.main.container);

                // Convert it to prevent List changes
                nearbyItemContainers = containersList.ToArray();

                // Timestamp it
                lastContainerCheckTime = DayNightCycle.main.timePassed;
            }

            // Return the cached value
            return nearbyItemContainers;
        }

        /*
         * These functions are reproductions that use the FindAllItemsContainersInRange function instead of the default Inventory.main container
         */

        public static int GetTotalPickupCount(TechType techType, ItemsContainer[] itemContainers)
        {
            int num = 0;
            foreach (ItemsContainer itemsContainer in itemContainers)
            {
                num += itemsContainer.GetCount(techType);
            }
            return num;
        }

        public static bool DestroyItemInContainers(TechType techType, ItemsContainer[] itemsContainers)
        {
            for (int i = 0; i < itemsContainers.Length; i++)
            {
                if (itemsContainers[i].DestroyItem(techType))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool DestroyItemInLocalContainers(TechType techType)
        {
            var itemsContainers = FindAllItemsContainersInRange();

            return DestroyItemInContainers(techType, itemsContainers);
        }

        public static bool IsCraftRecipeFulfilled(TechType techType)
        {
            if (Inventory.main == null)
            {
                return false;
            }
            if (!GameModeUtils.RequiresIngredients())
            {
                return true;
            }

            var itemContainers = FindAllItemsContainersInRange();
            ITechData techData = CraftData.Get(techType, false);
            if (techData != null)
            {
                int i = 0;
                int ingredientCount = techData.ingredientCount;
                while (i < ingredientCount)
                {
                    IIngredient ingredient = techData.GetIngredient(i);
                    if (GetTotalPickupCount(ingredient.techType, itemContainers) < ingredient.amount)
                    {
                        return false;
                    }
                    i++;
                }
                return true;
            }
            return false;
        }

        public static bool ConsumeResources(TechType techType)
        {
            if (!IsCraftRecipeFulfilled(techType))
            {
                ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
                return false;
            }

            var itemsContainers = FindAllItemsContainersInRange();
            ITechData techData = CraftData.Get(techType, false);
            if (techData == null)
            {
                return false;
            }
            int i = 0;
            int ingredientCount = techData.ingredientCount;
            while (i < ingredientCount)
            {
                IIngredient ingredient = techData.GetIngredient(i);
                TechType techType2 = ingredient.techType;
                int j = 0;
                int amount = ingredient.amount;
                while (j < amount)
                {
                    DestroyItemInContainers(techType2, itemsContainers);
                    uGUI_IconNotifier.main.Play(techType2, uGUI_IconNotifier.AnimationType.To, null);
                    j++;
                }
                i++;
            }
            return true;
        }

        public static void WriteIngredients(ITechData data, List<TooltipIcon> icons)
        {
            int ingredientCount = data.ingredientCount;
            ItemsContainer[] itemContainers = FindAllItemsContainersInRange();
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < ingredientCount; i++)
            {
                stringBuilder.Length = 0;
                IIngredient ingredient = data.GetIngredient(i);
                TechType techType = ingredient.techType;
                int totalPickupCount = GetTotalPickupCount(techType, itemContainers);
                int amount = ingredient.amount;
                bool flag = totalPickupCount >= amount || !GameModeUtils.RequiresIngredients();
                Atlas.Sprite sprite = SpriteManager.Get(techType);
                if (flag)
                {
                    stringBuilder.Append("<color=#94DE00FF>");
                }
                else
                {
                    stringBuilder.Append("<color=#DF4026FF>");
                }
                string orFallback = Language.main.GetOrFallback(TooltipFactory.techTypeIngredientStrings.Get(techType), techType);
                stringBuilder.Append(orFallback);
                if (amount > 1)
                {
                    stringBuilder.Append(" x");
                    stringBuilder.Append(amount);
                }
                if (totalPickupCount > 0 && totalPickupCount < amount)
                {
                    stringBuilder.Append(" (");
                    stringBuilder.Append(totalPickupCount);
                    stringBuilder.Append(")");
                }
                stringBuilder.Append("</color>");
                icons.Add(new TooltipIcon(sprite, stringBuilder.ToString()));
            }
        }
    }
}
