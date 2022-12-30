using UnityEngine;
using UnityEngine.AI;


namespace Hood.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCLocomotionManager : MonoBehaviour
    {

        NavMeshAgent navMeshAgent;
        Animator animator;
        public enum animationStyle
        {
            Happy, Ogre, Sad, Strut, Normal
        }
        public animationStyle style = 0;

        //interacting is suppose to be true when the character is performing an action to prevent the animator
        //to change the animation from the action to standing. Ex lock picking
        bool isInteractiong = false;

        //How fast this particular Character is
        public AnimationCurve runSpeed;
        public float walkSpeed = 3f;
        [HideInInspector] public float speedModifier = 1f;
        float speed = 1f;
        public enum TravleSpeed
        {
            Walk,
            Jog,
            Run
        }
        private void Awake()
        {

            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            animator.SetInteger("NPCAnimID", (int)style);
        }

        private void Update()
        {

            UpdateAnimation();
        }


        //Used to rotate NPC back to its guard rotation to prevent them from stairing into a wall
        //when returning to guard position
        public bool Rotate(Quaternion rotationPos) {
           transform.rotation = Quaternion.Lerp(transform.rotation, rotationPos, speed * Time.deltaTime);
            float rotateTarget = Quaternion.Angle(transform.rotation, rotationPos);
            return rotateTarget <= 5;
        }

        public void HandleMoveToTarget(Vector3 pos, TravleSpeed travleSpeed = TravleSpeed.Run, float timeInState = 0)
        {
           
            if (isInteractiong) return;

            float _speed = 1;
            switch (travleSpeed)
            {
                case TravleSpeed.Walk:
                    _speed = walkSpeed * speedModifier; 
                    break; 

                    case TravleSpeed.Jog:
                    _speed = (speed + ((runSpeed.Evaluate(timeInState) - walkSpeed)/2)) * speedModifier; 
                    break;

                default:
                case TravleSpeed.Run:
                    _speed = runSpeed.Evaluate(timeInState) * speedModifier;
                    break;

            }
            navMeshAgent.speed = _speed;
            navMeshAgent.SetDestination(pos); 
        }

        //Check if Type is in the path of the NPC's travel path
        public T GetTypeInNavMeshPath<T>() where T : class {

            T foundType = default;
            if (navMeshAgent.path.corners.Length > 1)
                for (int i = 1; i < navMeshAgent.path.corners.Length; i++) {
                    

                    Vector3 startPos = navMeshAgent.path.corners[i - 1] + Vector3.up;
                    Vector3 dir = navMeshAgent.path.corners[i] - navMeshAgent.path.corners[i - 1];
                    float distance = Vector3.Distance(navMeshAgent.path.corners[i], navMeshAgent.path.corners[i - 1]);

                    Debug.DrawLine(startPos, navMeshAgent.path.corners[i] + Vector3.up, Color.cyan, 3f);
                    RaycastHit[] hits = Physics.RaycastAll(startPos, dir.normalized, distance);
                    foreach (var item in hits) {

                        if (item.collider.GetComponent<T>() == null) continue;

                         foundType = item.collider.GetComponent<T>();
                         return foundType;
                    }
                }

            return foundType;
        }

        /// <summary>
        /// To stop NavMeshAgent from moving
        /// </summary>
        public void ActivateMovement(bool AllowMovement)
        {
            navMeshAgent.isStopped = !AllowMovement;
        }

        /// <summary>
        /// Will calculate how fast the NavMeshAgent is moving and update animator
        /// </summary>
        private void UpdateAnimation()
        {
            Vector3 velocity = navMeshAgent.velocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            if (speed != 0f) localVelocity /= runSpeed.Evaluate(0);
            
            animator.SetFloat("VelocityZ", localVelocity.z, 0.1f, Time.deltaTime);
            
        }


    }

}