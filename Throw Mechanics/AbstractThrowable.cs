using System.Collections;
using UnityEngine;
using Hood.AI;

namespace Hood.Item {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    public abstract class AbstractThrowable : MonoBehaviour {

        [SerializeField] float nPCAbstractStunTime = 1f;
        [SerializeField] protected AudioClip audioClip;
        [SerializeField] AudioClip hitNPCAudioClip;
        //The visible item that can be hidden
        [SerializeField] protected GameObject disableObject;
        //The sound range that alert any nearby guards
        [SerializeField] float alertRange = 5f;
        //How far the item needs to travel before making noises.
        //Preventing several sounds to play and hurting the players ears
        [SerializeField] float distanceRequiredForSound = 1f;

        protected bool hasActivated = false;

        //Contains the information of the thrown object
        protected ThrowablesScriptableObject throwable;
        protected GameObject user;
        protected Rigidbody rb;
        Vector3 playPos;

        float timer = 0f;
        Coroutine _Travle;

        private void Awake() {
            rb = GetComponent<Rigidbody>();
        }

        // Destroy the object after a while, to prevent it from existing for a internity

        private void Update() {
            if (timer > throwable.GetExistTimer() + 10f) Destroy(gameObject);
            timer += Time.deltaTime;
        }
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, alertRange);
        }

        public void Setup(ThrowablesScriptableObject throwable, GameObject user, Vector3[] path) {
            this.throwable = throwable;
            this.user = user;
            _Travle = StartCoroutine(Travel(path));
        }

        //If the Thrown object makes contact with a Guard with force, stun them for the stun time
        protected virtual void OnCollisionEnter(Collision collision) {
            if (collision.gameObject.tag == "Guard") {
                GetComponent<AudioSource>().PlayOneShot(hitNPCAudioClip);
                collision.gameObject.GetComponent<AI.AIManager>().GetStuned(nPCAbstractStunTime, user);
            }
            if (!hasActivated) StartCoroutine(Timer());
            if (Vector3.Distance(playPos, transform.position) > distanceRequiredForSound) {

                playPos = transform.position;
                GetComponent<AudioSource>().PlayOneShot(audioClip);
                foreach (var obj in Physics.OverlapSphere(transform.position, alertRange)) {

                    if (obj.tag != "Guard") continue;

                    obj.GetComponent<AIManager>().Chase(gameObject);
                }
            }
        }

        //Hide object, stop it's momentum
        protected void DisableObject() {

            StopCoroutine(_Travle);
            disableObject?.SetActive(false);
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            GetComponent<Collider>().enabled = false;
            GetComponentInChildren<ParticleSystem>().Play();
            
        }

        private IEnumerator Travel(Vector3[] path) {

            Rigidbody rB = GetComponent<Rigidbody>();
            rB.useGravity = false;

            int pathIndex = 1;
            while (true) {

                transform.position = Vector3.MoveTowards(transform.position, path[pathIndex], Time.deltaTime * 12);
                if (Vector3.Distance(transform.position, path[pathIndex]) <= 0.1f) pathIndex++;
                if(pathIndex >= path.Length) break; 

                yield return null;
            }

            rb.useGravity = true;
            //Once the travel path is empty. It adds force to the object.
            rB.AddForce((path[path.Length - 1] - path[path.Length - 2]) * throwable.GetThrowForce * 850);
        }

        protected IEnumerator Timer() {
            hasActivated = true;
            if (throwable.IsDestroyOnImpact()) DisableObject();

            yield return new WaitForSeconds(Mathf.Clamp(throwable.GetExistTimer(), 1f, 300f));

            Destroy(gameObject);
        }

    }

}
