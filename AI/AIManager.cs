using System.Collections;
using UnityEngine;
using Hood.Interact;

namespace Hood.AI
{
    [RequireComponent(typeof(NPCLocomotionManager))]
    [RequireComponent(typeof(Audio.SoundManager))]
    public class AIManager : MonoBehaviour
    {


        //Manages the NPC's movement speed and animator
        public NPCLocomotionManager nPCLocomotionManager { get; private set; }
        //interacting is suppose to be true when the character is performing an action to prevent the animator
        //to change the animation from the action to standing. Ex lock picking
        

        [Tooltip("Were the ray cast to detect targets starts from")]
        public Transform eyePosition;
        [Tooltip("Patrol path (if any) the NPC takes")]
        public PatrolPath patrolPath;
        [Tooltip("Alert object is the ! appearing when they notice the player")]
        public ItemAlert alertObject;

        [Header("A.I Settings")]
        public float detectionRadius = 20;
        //DetectionAngle is the angle infront of the creature min is left of the creature and max is the right of the creature

        public float detectionAngle = 50f;
        public float interestTimer = 3f;
        public float arrestRange = 1f;

        [Tooltip("Detects the player and start chasing them")]
        public bool detectPlayer = false;

        [SerializeField] float rotationSpeed = 2;
        [Tooltip("Interact target for the NPC to interact with objects")]
        InteractTarget interactTarget;
        float timer = Mathf.Infinity;
        public LayerMask dectectLayer;
        State currentState = State.Patrol;
        [SerializeField] bool deactivateManager = false;
        //Target to chase
        public GameObject currentTarget { private set; get; }
        //The position the NPC was before chasing and will return once stop chasing
        public GuardPosition guardPosition;
        //The current patrol node to walk to
        public int waypointIndex = 0;
        Animator animator;
        NPCStateBehavior stateBehavior;
        Coroutine stunCorutine;
        enum State {
            Patrol,
            Inspect,
            Chase
        }
        [HideInInspector]
        public Player player;

        //Display NPC view area
        private void OnDrawGizmosSelected()
        {

            Quaternion upRayRotation = Quaternion.AngleAxis(-detectionAngle, Vector3.up);
            Quaternion downRayRotation = Quaternion.AngleAxis(detectionAngle, Vector3.up);

            Vector3 upRayDirection = upRayRotation * transform.forward * detectionRadius;
            Vector3 downRayDirection = downRayRotation * transform.forward * detectionRadius;

            Gizmos.DrawRay(transform.position + Vector3.up, upRayDirection);
            Gizmos.DrawRay(transform.position + Vector3.up, downRayDirection);
            Gizmos.DrawLine(transform.position + Vector3.up + downRayDirection, transform.position + Vector3.up + upRayDirection);
        }

        //Get NPC movement manager and sets the guard position
        private void Awake()
        {
            player = FindObjectOfType<Player>();   
            nPCLocomotionManager = GetComponent<NPCLocomotionManager>();
            guardPosition = new GuardPosition(transform);
            interactTarget = GetComponent<InteractTarget>();
            animator = GetComponent<Animator>();

            if (patrolPath != null)
            {
                guardPosition.position = patrolPath.GetWaypoint(waypointIndex);
            } 
        }

        private void Start() {
            stateBehavior = new NPCStateBehavior(this);
        }


        //if not deactivated then look for player
        //Handle the current state action
        //Check if the NPC can interact with something
        // Update is called once per frame
        void Update()
        {
            if (!deactivateManager) {

                HandleDetection();
                HandleCurrentAction();
                stateBehavior.HandleMovement();
                Interacting();
            }
            if (detectPlayer) { 
                Chase(FindObjectOfType<ControlInputAction>().gameObject); 
                detectPlayer = false;
            }

            timer += Time.deltaTime;
        }

        /// <summary>
        /// Show the ! above the NPC head
        /// </summary>
        public void Alert() {
            
            alertObject.StartAlert();
        }

        /// <summary>
        /// Depending on the current State will change the NPC's behavior
        /// </summary>
        private void HandleCurrentAction()
        {
            switch (currentState)
            {
                case State.Patrol:
                    PatrolBehaviour();
                    
                    break;

                case State.Inspect:
                    InspectBehaviour();
                    
                    break;

                case State.Chase:
                    ChaseBehaviour();

                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// Wait before going back to patrol
        /// </summary>
        private void InspectBehaviour()
        {

            if (timer >= interestTimer) ChangeState(State.Patrol);
            currentTarget = null;
             
        }
        /// <summary>
        /// Walking in a patrol path
        /// </summary>
        private void PatrolBehaviour()
        {
            
        }

        /// <summary>
        /// Once the NPC has spotted the player they will chase them and if they get close enough they will "arrest" them
        /// </summary>
        private void ChaseBehaviour()
        {
            //Check if NPC should stop chasing
            if (timer >= interestTimer) {
                ChangeState(State.Inspect);
                return;
            }

        }

        /// <summary>
        /// Change state while reseting the timer
        /// </summary>
        /// <param name="state"></param>
        private void ChangeState(State state)
        {
            currentState = state;
            timer = 0;
            switch (currentState) {
                case State.Patrol:
                    stateBehavior.Patrol();
                    break;
                case State.Inspect:
                    stateBehavior.Inspect();
                    break;
                case State.Chase:
                    stateBehavior.Chase();
                    break;
                default:
                    break;
            }
        }

        public float DistanceToTarget(Vector3 pos) => Vector3.Distance(pos, transform.position);

        public void GetStuned(float time, GameObject user) {
            if(stunCorutine != null) StopCoroutine(stunCorutine);

            
            animator.SetBool("IsHit", false);
            animator.CrossFade("Getting Hit", 0.1f); 
            stunCorutine = StartCoroutine(Stuned(time));
            Chase(user);
        }

        IEnumerator Stuned(float time) {
            yield return null;
            deactivateManager = true;
            nPCLocomotionManager.ActivateMovement(false);
            animator.SetBool("IsHit", true);
            animator.SetBool("Attacking", false);
            yield return new WaitForSeconds(time);

            animator.SetBool("IsHit", false);
            deactivateManager = false;
            nPCLocomotionManager.ActivateMovement(true);
        }

        /// <summary>
        /// Will rotate towards the target and "attack" them
        /// Will start a animation to attack if they are facing the target
        /// </summary>
        public void AttackTarget()
        {

            Vector3 lookPos = currentTarget.transform.position - transform.position;
            lookPos.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos), 
                Time.deltaTime * rotationSpeed);

            if (animator.GetBool("Attacking") || animator.GetBool("IsHit")) return;
            
            if (Vector3.Dot(transform.forward, lookPos.normalized) > 0.9f) {

                animator.SetBool("Attacking", true);
                animator.SetInteger("PunchNum", Random.Range(0, 4));
                animator.Play("Punching", 1);
            }


        }

        /// <summary>
        /// The Punching animation calls this method
        /// Will try to arrest the player
        /// </summary>
        public void AtemptArest() {

            Vector3 lookPos = currentTarget.transform.position - transform.position;
            lookPos.y = 0;
            //Check if facing player
            if (Vector3.Dot(transform.forward, lookPos.normalized) > 0.9f)
            foreach (var target in Physics.RaycastAll(eyePosition.position, 
                ((currentTarget.transform.position + (Vector3.up * currentTarget.GetComponent<Collider>().bounds.size.y * 0.75f)) - eyePosition.position)
                ,arrestRange, dectectLayer)) {
                //If the ray hit the player they lose
                if(target.collider.tag == "Player") {
                        if (TryGetComponent(out Hood.Audio.SoundManager manager))
                        {
                            manager.PlayClip("TargetCaught");
                        }
                        target.collider.TryGetComponent(out Animator _userAnimator);
                        if (animator && !GameSystem.GameManager.gameManager._transitioning) _userAnimator.CrossFade("Arrested", 0.1f);

                        GameSystem.GameManager.gameManager.LoseGame();
                }
            }

        }
        /// <summary>
        /// Will look for Player and start Chase if the player is spotted
        /// </summary>
        public void HandleDetection()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, dectectLayer);

            //Check all GameObjects for player
            for (int i = 0; i < colliders.Length; i++)
            {
                Transform targetTransform = colliders[i].GetComponent<Transform>();

                //Check if creature is facing target
                Vector3 targetDirection = targetTransform.position - transform.position;
                float viewableAngle = Vector3.Angle(targetDirection, transform.forward);

                if ((viewableAngle > -detectionAngle && viewableAngle < detectionAngle))
                {
                    //Check if player is behind an object
                    float dis = Vector3.Distance(transform.position, targetTransform.position);
                    //Check if a object is in the way
                    RaycastHit hit;
                    Debug.DrawRay(eyePosition.position, ((targetTransform.position + (Vector3.up * colliders[i].bounds.size.y * 0.75f)) - eyePosition.position), Color.green, 2f);
                    if (Physics.Raycast(eyePosition.position, ((targetTransform.position + (Vector3.up * colliders[i].bounds.size.y * 0.75f)) - eyePosition.position), out hit, dis, 1) &&
                        hit.collider.tag != "Player")
                        return;

                    Chase(targetTransform.gameObject);
                }
            }
        }

        //Tell AI to chase a source 
        //Set target and chase
        //Will prioritize tag Player
        public void Chase(GameObject sender) {

            //Checks if it already has a target then it prioritize player while refreshing the timer if
            //the new target is the player
            if (currentTarget != null && (currentTarget.tag == "Player" && sender.tag != "Player")) return; 
            currentTarget = sender;
            ChangeState(State.Chase);
        }

        //The chase mechanic can only track GameObjects 
        public void Chase(Vector3 pos) {

            GameObject sender = new GameObject();
            sender.transform.position = pos;
            sender.AddComponent(typeof(BoxCollider));
            sender.GetComponent<BoxCollider>().isTrigger = true;
            Destroy(sender, interestTimer + 1);
            Chase(sender);
        }

        //When the NPC can interact with an object it checks if it's a door in it's path towards the NPC's target
        //If the door in the way is the door the NPC is looking at and it's closed then open 
        private void Interacting() {
            if (interactTarget.currentInteractiveble == null) return;

            Interactiveble door = nPCLocomotionManager.GetTypeInNavMeshPath<DoorInteract>();
            if (door != interactTarget.GetInteractiveble()) return;
            if (((DoorInteract)door).GetController.GetDoorState() == DoorController.State.Open) return;
            //Stoping wont work unless the NPCstateBehavior knows that
            deactivateManager = true;
            nPCLocomotionManager.ActivateMovement(false);
            door.Interact(gameObject);
            StartCoroutine(DelayMovement(((DoorInteract)door).GetController.waitDuration));
        }


        //Wait until the door is open
        IEnumerator DelayMovement(float waitTime) {
            
            yield return new WaitForSeconds(waitTime);

            deactivateManager = false;
            nPCLocomotionManager.ActivateMovement(true);
            
        }
        
        //When leaving the doors trigger close the door
        //Only close doors that needs keys? And leave doors that were open when passing through?
        private void OnTriggerExit(Collider other) {
            if (other.TryGetComponent( out DoorController doorControl)) {
                
                if (doorControl.GetDoorState() == DoorController.State.Close) return;
                //Stoping wont work unless the NPCstateBehavior knows that

                doorControl.Interact(gameObject);
            }
        }

        public class GuardPosition {

            public Vector3 position;
            public Quaternion rotation;

            public GuardPosition(Transform transform) { 
            position = transform.position;
            rotation = transform.rotation;
            }
        }
    }

}