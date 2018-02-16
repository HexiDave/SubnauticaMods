using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace PullFromContainers
{

    public static class ConstructableHelper
    {
        static readonly FieldInfo Constructable_resourceMap;
        static readonly MethodInfo Constructable_UpdateMaterial;
        static readonly MethodInfo Constructable_GetResourceID;
        static readonly MethodInfo Constructable_GetConstructInterval;

        static ConstructableHelper()
        {
            var constructableType = typeof(Constructable);

            Constructable_resourceMap = constructableType.GetField("resourceMap", BindingFlags.Instance | BindingFlags.NonPublic);
            Constructable_UpdateMaterial = constructableType.GetMethod("UpdateMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
            Constructable_GetResourceID = constructableType.GetMethod("GetResourceID", BindingFlags.Instance | BindingFlags.NonPublic);
            Constructable_GetConstructInterval = constructableType.GetMethod("GetConstructInterval", BindingFlags.Static | BindingFlags.NonPublic);
        }

        // Static reproduction of Constructable.Construct to replace the body of the existing method
        public static bool Construct(Constructable constructable)
        {
            if (constructable._constructed)
            {
                return false;
            }

            var resourceMap = Constructable_resourceMap.GetValue(constructable) as List<TechType>;

            var count = resourceMap.Count;

            var resourceID = (int)Constructable_GetResourceID.Invoke(constructable, new object[] { });

            constructable.constructedAmount += Time.deltaTime / ((float)count * (float)Constructable_GetConstructInterval.Invoke(constructable, new object[] { }));
            constructable.constructedAmount = Mathf.Clamp01(constructable.constructedAmount);

            var resourceID2 = (int)Constructable_GetResourceID.Invoke(constructable, new object[] { });
            if (resourceID != resourceID2)
            {
                var techType = resourceMap[resourceID2 - 1];
                if (!CrafterLogicHelper.DestroyItemInLocalContainers(techType) && GameModeUtils.RequiresIngredients())
                {
                    constructable.constructedAmount = (float)resourceID / (float)count;
                    return false;
                }
            }

            Constructable_UpdateMaterial.Invoke(constructable, new object[] { });
            if (constructable.constructedAmount >= 1f)
            {
                constructable.SetState(true, true);
            }

            return true;
        }
    }
}
