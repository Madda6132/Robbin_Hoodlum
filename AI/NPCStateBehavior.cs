using UnityEngine;
using UnityEngine.AI;
using Hood.GameSystem;


// NPCs are in one state in a given time changing their behavior

namespace Hood.AI {


    public interface IStateBehaviorContext {
        void SetState(IBehaviorState state);
    }

    //Enter another state through these methods

    public interface IBehaviorState {
        void HandleMovement();
        void Patrol(IStateBehaviorContext context);
        void Inspect(IStateBehaviorContext context);
        void Chase(IStateBehaviorContext context);
        void Wait(IStateBehaviorContext context);
         
    }
     public class NPCStateBehavior : IStateBehaviorContext {

            IBehaviorState currentState;

            public NPCStateBehavior(AIManager aIManager) {
                currentState = new PatrolState(this, aIManager);
            }


            public void HandleMovement() => currentState.HandleMovement();

            public void Patrol() => currentState.Patrol(this);
            public void Inspect() => currentState.Inspect(this);
            public void Chase() => currentState.Chase(this);
            public void Wait() => currentState.Wait(this);

            void IStateBehaviorContext.SetState(IBehaviorState newState) {
                currentState = newState;
            }

        }

    // The NPC will travle along nodes in a path class

    public class PatrolState : IBehaviorState {

        AIManager aIManager;
        NPCStateBehavior StateBehavior;
        public PatrolState(NPCStateBehavior StateBehavior,AIManager aIManager) {
            this.aIManager = aIManager;
            this.StateBehavior = StateBehavior;
            aIManager.nPCLocomotionManager.ActivateMovement(true);
            //Tell the background music manager that the AI is no longer inspecting nor chasing
            ThreatManager.instance?.NPCChangeMusicState(Audio.BackgroundMusicManager.BackgroundState.Normal, aIManager);
        }

        public void Chase(IStateBehaviorContext context) {
            context.SetState(new ChaseState(StateBehavior, aIManager));
        }

        //Wait state before continuing?

        public void HandleMovement() {
            if (aIManager.patrolPath != null) {
                if (aIManager.DistanceToTarget(aIManager.guardPosition.position) <= 1f) {
                    //Start wait condition
                    StateBehavior.Wait();
                }

            } else {
                if (aIManager.DistanceToTarget(aIManager.guardPosition.position) <= 1f) {
                    //Rotate back if NPC is close to its guard position
                    if (aIManager.nPCLocomotionManager.Rotate(aIManager.guardPosition.rotation)) StateBehavior.Wait();
                    
                }
            }
                    aIManager.nPCLocomotionManager.HandleMoveToTarget(aIManager.guardPosition.position, NPCLocomotionManager.TravleSpeed.Walk);
        }

        public void Inspect(IStateBehaviorContext context) {
            context.SetState(new InspectState(StateBehavior, aIManager));
        }

        public void Patrol(IStateBehaviorContext context) {
            
        }

        public void Wait(IStateBehaviorContext context) {
            context.SetState(new WaitState(StateBehavior, aIManager));
        }
    }

    // Will take the shortest path towards the target in AIManager

    public class ChaseState : IBehaviorState {

        AIManager _aIManager;
        NPCStateBehavior _StateBehavior;
        Vector3 _targetPosition;
        float timeInState = 0f;

        public ChaseState(NPCStateBehavior StateBehavior, AIManager aIManager) {
            this._aIManager = aIManager;
            this._StateBehavior = StateBehavior; 
            aIManager.Alert();
            _targetPosition = aIManager.currentTarget.transform.position;
            ThreatManager.instance?.NPCChangeMusicState(Audio.BackgroundMusicManager.BackgroundState.Chase, aIManager);
            _aIManager.TryGetComponent(out ScoreAdderChased scoreAdder);
            scoreAdder?.AddToScore();
            if (aIManager.TryGetComponent(out Hood.Audio.SoundManager manager))
            {
                if(aIManager.currentTarget.tag=="Player")
                manager.PlayClip("FoundTarget", -1, -1, 1, 1, true);
                else
                {
                    manager.PlayClip("Alert", -1, -1, 1, 1, true);
                }
            }

        }

        public void Chase(IStateBehaviorContext context) {
            
        }

        public void HandleMovement() {

            if (_aIManager.currentTarget) { 
                _targetPosition = _aIManager.currentTarget.transform.position + (Vector3.up * _aIManager.currentTarget.GetComponent<Collider>().bounds.size.y * 0.75f);

                if (_aIManager.DistanceToTarget(_targetPosition) <= _aIManager.arrestRange) {
                
                Vector3 eyePosition = _aIManager.eyePosition.position;

                //Checks if a wall is in the way as to prevent the NPC from arresting the player on a nother side of a wall
                Debug.DrawRay(eyePosition, (_targetPosition - eyePosition), Color.blue, 2f);
                if (Physics.Raycast(eyePosition, _targetPosition - eyePosition, out RaycastHit hit,
                _aIManager.arrestRange) && hit.collider.tag == "Player") {

                        _aIManager.nPCLocomotionManager.ActivateMovement(false);
                        _aIManager.AttackTarget();
                        return;
                    }
                } 
                
            }


            _aIManager.nPCLocomotionManager.ActivateMovement(true);
            if(_aIManager.currentTarget.tag == "Player") {
                _aIManager.nPCLocomotionManager.HandleMoveToTarget(_targetPosition, NPCLocomotionManager.TravleSpeed.Run, timeInState);
                timeInState += Time.deltaTime;
            } else {
                _aIManager.nPCLocomotionManager.HandleMoveToTarget(_targetPosition, NPCLocomotionManager.TravleSpeed.Jog);
            }


        }

        public void Inspect(IStateBehaviorContext context) {
            context.SetState(new InspectState(_StateBehavior, _aIManager));
        }

        public void Patrol(IStateBehaviorContext context) {
            context.SetState(new PatrolState(_StateBehavior, _aIManager));
        }

        public void Wait(IStateBehaviorContext context) {
            
        }
    }

    // Inspect state happens when the guard stops chasing, casing them to wander forwards

    public class InspectState : IBehaviorState {

        AIManager aIManager;
        NPCStateBehavior StateBehavior;
        NavMeshAgent _NavMeshAgent;

        public InspectState(NPCStateBehavior StateBehavior, AIManager aIManager) {
            this.aIManager = aIManager;
            this.StateBehavior = StateBehavior;
            _NavMeshAgent = aIManager.GetComponent<NavMeshAgent>();
            ThreatManager.instance?.NPCChangeMusicState(Audio.BackgroundMusicManager.BackgroundState.Inspect, aIManager);
            if (aIManager.TryGetComponent(out Hood.Audio.SoundManager manager))
            {
                manager.PlayClip("LostTarget");
            }
        }

        public void Chase(IStateBehaviorContext context) {
            context.SetState(new ChaseState(StateBehavior, aIManager));
        }

        //Walk forward unless no path forward 
        public void HandleMovement() {
            NavMeshPath path = new();
            Vector3 movePosition = _NavMeshAgent.transform.forward + _NavMeshAgent.transform.position;

            int loopBreaker = 0;

            //If the path forwards dosen't work try to find another path
            while (!(_NavMeshAgent.CalculatePath(movePosition, path) 
                && GetDistanceFromPath(path) < 5f)) {
                movePosition = GetNewDirection();

                loopBreaker++;
                if(loopBreaker > 10) {
                    Debug.Log("10 attempts to find another path before breaking");
                    movePosition = aIManager.transform.position;
                    break;
                }
            }

            aIManager.nPCLocomotionManager.HandleMoveToTarget(movePosition, NPCLocomotionManager.TravleSpeed.Walk);
            
           
        }

        public void Inspect(IStateBehaviorContext context) {
            
        }

        public void Patrol(IStateBehaviorContext context) {
            context.SetState(new PatrolState(StateBehavior, aIManager));
        }

        public void Wait(IStateBehaviorContext context) {
            
        }

        private float GetDistanceFromPath(NavMeshPath path) {

            float distance = 0f;
            Vector3 lastPos = aIManager.transform.position;
            foreach (var pos in path.corners) {
                distance += Vector3.Distance(lastPos, pos);
                lastPos = pos;
            }
            return distance;
        }

        private Vector3 GetNewDirection() {

            Vector2 randomDir = Random.insideUnitCircle * 10;
            Vector3 dir = new Vector3(randomDir.x, 0, randomDir.y).normalized * 2;
            dir += aIManager.transform.position;

            NavMeshHit hit; 
            NavMesh.SamplePosition(dir, out hit, 2f, 1<< NavMesh.GetAreaFromName("Walkable"));
            Vector3 dirToTarget = Vector3.Normalize(hit.position - aIManager.transform.position);
            if (Vector3.Dot(aIManager.transform.forward, dirToTarget) < 0) return hit.position * -1;

            return hit.position;
        }
    }

    // Wait state happens when the NPC is waiting at a path node or has no path node

    public class WaitState : IBehaviorState {


        AIManager aIManager;
        NPCStateBehavior StateBehavior;
        float timePast = 0;
        float pauseSoundTime = 0;
        Audio.SoundManager soundManager;

        public WaitState(NPCStateBehavior StateBehavior, AIManager aIManager) {
            this.aIManager = aIManager;
            this.StateBehavior = StateBehavior;
            soundManager = aIManager.GetComponent<Audio.SoundManager>();
            aIManager.nPCLocomotionManager.ActivateMovement(false);
        }

        public void Chase(IStateBehaviorContext context) {
            context.SetState(new ChaseState(StateBehavior, aIManager));
        }

        public void HandleMovement() {

            timePast += Time.deltaTime;
            if(aIManager.patrolPath != null) { 
                if(timePast >= aIManager.patrolPath.GetWaitTime(aIManager.waypointIndex)) {
                aIManager.waypointIndex = aIManager.patrolPath.GetNextIndex(aIManager.waypointIndex);
                //Once reaching the patrol destination it changes to the next node location
                aIManager.guardPosition.position = aIManager.patrolPath.GetWaypoint(aIManager.waypointIndex);
                StateBehavior.Patrol();
                return;
                }
            }

            //Play wait sounds
            WaitSounds();

        }

        public void Inspect(IStateBehaviorContext context) {

        }

        public void Patrol(IStateBehaviorContext context) {
            context.SetState(new PatrolState(StateBehavior, aIManager));
        }

        public void Wait(IStateBehaviorContext context) {
            
        }

        private void WaitSounds() {

            if (soundManager == null) return;
            //Wait sound length + Random time
            if (pauseSoundTime <= timePast) {
                //Play sound
                if (aIManager.patrolPath == null) timePast = 0;
                if (Mathf.Abs(aIManager.player.transform.position.y - aIManager.transform.position.y) < 3.5f)
                {
                    AudioClip clip = soundManager.PlayClip("Idle");
                    float clipTime = clip ? clip.length : 0;
                    pauseSoundTime = clipTime + timePast + Random.Range(4f, 8f);

                }
            }      
        }
    }
}
