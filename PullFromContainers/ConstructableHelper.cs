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
        private static readonly FieldInfo ConstructableResourceMap;
        private static readonly MethodInfo ConstructableUpdateMaterial;
        public static readonly MethodInfo ConstructableGetResourceId;
        public static readonly MethodInfo ConstructableGetConstructInterval;

        static ConstructableHelper()
        {
            var constructableType = typeof(Constructable);

            ConstructableResourceMap = constructableType.GetField("resourceMap", BindingFlags.Instance | BindingFlags.NonPublic);
            ConstructableUpdateMaterial = constructableType.GetMethod("UpdateMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
            ConstructableGetResourceId = constructableType.GetMethod("GetResourceID", BindingFlags.Instance | BindingFlags.NonPublic);
            ConstructableGetConstructInterval = constructableType.GetMethod("GetConstructInterval", BindingFlags.Static | BindingFlags.NonPublic);
        }

        // Static reproduction of Constructable.Construct to replace the body of the existing method
        public static bool Construct(Constructable constructable)
        {
            if (constructable._constructed)
            {
                return false;
            }

            var resourceMap = ConstructableResourceMap.GetValue(constructable) as List<TechType>;

            var count = resourceMap.Count;

            var resourceID = (int)ConstructableGetResourceId.Invoke(constructable, new object[] { });

            constructable.constructedAmount += Time.deltaTime / ((float)count * (float)ConstructableGetConstructInterval.Invoke(constructable, new object[] { }));
            constructable.constructedAmount = Mathf.Clamp01(constructable.constructedAmount);

            var resourceID2 = (int)ConstructableGetResourceId.Invoke(constructable, new object[] { });
            if (resourceID != resourceID2)
            {
                var techType = resourceMap[resourceID2 - 1];
                if (!CrafterLogicHelper.DestroyItemInLocalContainers(techType) && GameModeUtils.RequiresIngredients())
                {
                    constructable.constructedAmount = (float)resourceID / (float)count;
                    return false;
                }
            }

            ConstructableUpdateMaterial.Invoke(constructable, new object[] { });
            if (constructable.constructedAmount >= 1f)
            {
                constructable.SetState(true, true);
            }

            return true;
        }
    }
}
