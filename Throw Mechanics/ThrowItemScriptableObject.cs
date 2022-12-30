using UnityEngine;


namespace Hood.Item {
    [CreateAssetMenu(fileName = "Throwable", menuName = "Items/Throw item", order = 0)]
    public class ThrowItemScriptableObject : ItemScriptableObject {

        [SerializeField] AbstractThrowable throwItemPrefab;
        [Tooltip("The maximum throw force for this object")]
        [SerializeField] float throwForce = 1f;
        [SerializeField] bool isDestroyedOnImpact = false;
        [SerializeField] float existTimer = 0f;
        [SerializeField] Sprite icon;

        //Spawn object in front of the sender
        //Ignore sender collider as to not get stuck
        //Add force and rotation force to send the object flying in the cameras direction
        public override void UseItem(GameObject sender) {
            base.UseItem(sender);

            if (sender.TryGetComponent(out TrajectoryManager manager)) manager.ActivateTrajectoryManager(this);
        }

        public float GetExistTimer() => existTimer;
        public bool IsDestroyOnImpact() => isDestroyedOnImpact;   
        public Sprite GetIcon => icon;
        public float GetThrowForce => throwForce;
        public AbstractThrowable GetThrowable => throwItemPrefab;
        public Rigidbody GetObjectRigidbody() { 
            
            if (throwItemPrefab.TryGetComponent(out Rigidbody rig)) {
                return rig;
            } else {
                return throwItemPrefab.GetComponentInChildren<Rigidbody>();
            }
        
        }
    }


}
