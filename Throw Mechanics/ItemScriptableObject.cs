using System.Collections.Generic;
using UnityEngine;

namespace Hood.Item {

    
    public class ItemScriptableObject : ScriptableObject {

        [SerializeField] string iDName = "";
        [SerializeField] bool isConsumable = false;
        [SerializeField] DisplayItem displayItem;
        //Contains audio clip information
        [SerializeField] Audio.SoundManager.AudioClipArgumentContainer PickupSoundEffect = 
            new Audio.SoundManager.AudioClipArgumentContainer();
        [SerializeField, TextArea(2,10)] string discription;

        static Dictionary<string, ItemScriptableObject> itemLookupCache;

        public Audio.SoundManager.AudioClipArgumentContainer GetPickupAudio() => PickupSoundEffect;
        public string GetIDName() => iDName; 
        public bool IsConsumable() => isConsumable; 
        public DisplayItem GetDisplayItem() => displayItem;
        public virtual void UseItem(GameObject sender) {
            Debug.Log("Using " + iDName + "....");
        }
        
        
            /// Get the inventory item instance from its UUID.
            /// String UUID that persists between game instances.
            /// Inventory item instance corresponding to the ID.
            public static ItemScriptableObject GetItemFromID(string itemID) {

                if (itemLookupCache == null) {
                    itemLookupCache = new Dictionary<string, ItemScriptableObject>();
                    var itemList = Resources.LoadAll<ItemScriptableObject>("");
                    foreach (var item in itemList) {
                        if (itemLookupCache.ContainsKey(item.GetIDName())) {
                            Debug.LogError(string.Format("Looks like there's a duplicate " +
                                "InventorySystem ID for objects: {0} and {1}", itemLookupCache[item.GetIDName()], item));
                            continue;
                        }

                        itemLookupCache[item.GetIDName()] = item;
                    }
                }

                if (itemID == null || !itemLookupCache.ContainsKey(itemID)) return null;
                return itemLookupCache[itemID];
            }

        
    }

}
