using UnityEngine;
using Hood.Interact;
using Hood.UI;



public class Interacter : MonoBehaviour
{
    [Tooltip("Were the ray starts from and moves the Interact target")]
    [SerializeField] Transform startRayPosition;
    InteractableUI interactableUI;
    [SerializeField] float reach = 2f;
    [SerializeField] LayerMask layerMaskForRaycast;
    bool isPlayer = false;
    //Updated value for any interactive object the user able to interact with
    public GameObject currentInteractiveble { get; private set; }

    
    public Interactiveble GetInteractiveble() { 
        if(currentInteractiveble == null) return null;
        return currentInteractiveble.GetComponent<Interactiveble>(); }

    private void Awake() {
        interactableUI = FindObjectOfType<InteractableUI>();
        isPlayer = tag == "Player";
    }

    private void Start() {
        if(isPlayer) reach += Vector3.Distance(transform.position, Camera.main.transform.position);
        
    }

    //Declare ray for Player or NPC
    //Sends ray in a direction if it hits something thats the distance the raycast all will travel
    //hits will then be filtered through UpdateCurrentInteractable(hits) and update interactibles
    //If the script comes from player, interactableUI isn't null and it found a interact object update DisplayUI
    private void Update() {

        Ray ray;
        
        float rayRange = reach;
        if (isPlayer) {
            
            Transform camera = Camera.main.transform;
            ray = new Ray(camera.position, camera.forward);
            
        } else {

            ray = new Ray(startRayPosition.position, startRayPosition.forward);
            
        }

        if (Physics.Raycast(ray, out RaycastHit hit, rayRange, layerMaskForRaycast)) rayRange = Vector3.Distance(hit.point, ray.origin);

        Debug.DrawRay(ray.origin, ray.direction * rayRange, Color.red);
        
        UpdateCurrentInteractable(Physics.SphereCastAll(ray, 0.1f, rayRange));
        if (isPlayer && interactableUI && GetInteractiveble() == null) HideDisplayUI();
    }

    //Update the interact object to an interactive object or null
    //Check all raycasthits and filters through if any are interactive
    private void UpdateCurrentInteractable(RaycastHit[] rayCastHits) {

        
        foreach (var rayHit in rayCastHits) {

            Collider hit = rayHit.collider;
            if (hit.GetComponent<Interactiveble>() != null) {
               

               Interactiveble interact = hit.GetComponent<Interactiveble>();

                currentInteractiveble = hit.gameObject;
                if (isPlayer && interactableUI != null) interactableUI.DisplayInteractText(interact.GetDisplayText());
                return;
            } 
        }

        currentInteractiveble = null;
        if (isPlayer && interactableUI != null) HideDisplayUI();
             
    }


    /// <summary>
    /// If the player isn't in range of interacting with an object then hide the display
    /// </summary>
    private void HideDisplayUI() {
        interactableUI.HideDisplay();
    }
    
}
