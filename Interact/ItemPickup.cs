using UnityEngine;
using Hood.Item;
using Hood.Saving;
using Hood.Audio;
using System;

namespace Hood.Interact {
    [RequireComponent(typeof(SoundManager))]
    public class ItemPickup : MonoBehaviour, Interactiveble, ISaveable {

        [Tooltip("Place an item that the interacter will receive")]
        [SerializeField] ItemScriptableObject itemScriptableObject;

        [Tooltip("Place an Game Object that will be displayed")]
        [SerializeField] Transform displayItem;

        [Tooltip("Place an Game Object that will be displayed")]
        [SerializeField] ParticleSystem particalSystem;

        [Tooltip("The display text for this interactable object")]
        [SerializeField] string interactText;

        [Tooltip("TRUE: Loading scene will check if the item was picked up to change or destroy \n "
            + "FALSE: Loading scene this will be in its starting state")]
        [SerializeField] bool saveItemPickup = false;
        bool itemPickedup = false;

        [Tooltip("Becomes infinite usually for testing")]
        [SerializeField] bool isInfinet = false;
        public bool isItemPickedup => itemPickedup;
        SoundManager _SoundManager;
        Action<ItemPickup> pickuped;

        private void Awake() {
            _SoundManager = GetComponent<SoundManager>();
        }

        private void Start() {
            bool itemHasDisplayItem = itemScriptableObject && itemScriptableObject.GetDisplayItem();

            if (!displayItem && itemHasDisplayItem) DisplayItem();

        }

        /// <summary>
        /// Get the interaction text from this item pickup 
        /// </summary>
        public string GetDisplayText() {  return interactText;  }

        public ItemScriptableObject GetItem => itemScriptableObject;

        /// <summary>
        /// The interacter picks the item up
        /// </summary>
        public void Interact(GameObject user) {
            //Null check
            if (!itemScriptableObject || itemPickedup) return;
            user.GetComponent<Inventory>().AddInventoryItem(itemScriptableObject);

            if(itemScriptableObject is ItemTreasure && UI.UILevelManager.Instance) 
                UI.UILevelManager.Instance?.AddScore(((ItemTreasure)itemScriptableObject).GetValue());

            _SoundManager?.PlayClip(itemScriptableObject.GetPickupAudio());
            user.TryGetComponent(out Animator animator);
            if (animator) animator.CrossFade("Interact", 0.1f);
            if (isInfinet) return;
            //If there is no item or nothing to display then destroy GameObject
            
                Destroy(displayItem.gameObject);
                particalSystem.Stop(true);
                itemPickedup = true;
                GetComponent<Collider>().enabled = false;
                itemScriptableObject = null;
                pickuped?.Invoke(this);
        }

        

        //When saving it checks if the item is null before returning a ID name
        public object CaptureState() {
            
            if (!itemScriptableObject || !saveItemPickup || itemPickedup) return "";
            string name = itemScriptableObject.GetIDName();
            return name;
        }

        //When loading the scene it checks if the item was saved at all.
        //Checks if it should hide or display an item
        public void RestoreState(object state) {

            if (!saveItemPickup) return;

            string item = (string)state;
            itemPickedup = item == null || item == "" ? true : false;

            if (itemPickedup) {
                if(displayItem) Destroy(displayItem.gameObject);
                itemPickedup = true;
                particalSystem.Stop(true);
                GetComponent<Collider>().enabled = false; 
            } else {
                itemScriptableObject = ItemScriptableObject.GetItemFromID(item);
                if (!itemScriptableObject.GetDisplayItem()) return;
                Destroy(displayItem.gameObject);
                displayItem = Instantiate(itemScriptableObject.GetDisplayItem().GetDisplayItemPrefab, transform).transform;
            }

             
        }

        public void SubToPickupEvent(Action<ItemPickup> listener) {
            pickuped += listener;
        }

        public void UnsubToPickupEvent(Action<ItemPickup> listener) {
            pickuped -= listener;
        }

        private void DisplayItem() {
            displayItem = Instantiate(itemScriptableObject.GetDisplayItem().GetDisplayItemPrefab, transform).transform;
        }
    }

}
