using System.Collections;
using UnityEngine;
using System;


namespace Hood.Item { 
public class TrajectoryManager : MonoBehaviour
{

    [Header("Line renderer variables")]
    public LineRenderer line;
    [Tooltip("How smooth the line should be"), Range(2, 30)]
    public int resolution;

    [Header("Formula variables")]
    Vector3 velocity;
    public float yLimit;
    private float g;

    [Header("Linecast variables")]
    [Tooltip("How many steps before firing a raycast to find obstacles"), Range(2, 30)] 
    public int linecastResolution;
    [SerializeField] float throwRange = 10;
    public LayerMask canHit;

    Transform _camera;
    bool _Activated = false;
    float _Mass = 0;
    float _MaxForce = 0;
    Vector3 _FirePos;
    [SerializeField]  float aimYaxisOffset = 0.4f;
        //Get gravity as a positive value
        //Get the camera
        //Hide the line

    private void Start() {
        g = Mathf.Abs(Physics.gravity.y);
        _camera = Camera.main.transform;
        line.enabled = false;
    }

        //ThrowablesScriptableObject calls this method when an throw object is called
        //Get the values of mass and force to calculate the throw objects velocity
        //Start coroutine for aiming
        public void ActivateTrajectoryManager(ThrowablesScriptableObject throwablesScriptableObject) {

        if (throwablesScriptableObject.GetObjectRigidbody() == null || _Activated) return;

        _Mass = throwablesScriptableObject.GetObjectRigidbody().mass;
        _MaxForce = throwablesScriptableObject.GetThrowForce * 1000;
        
        StartCoroutine(AimEnumerator(throwablesScriptableObject));
        _Activated = true;

    }

    //The AimEnumerator will wait for the throwInput to be released in the while loop
    private IEnumerator AimEnumerator(ThrowablesScriptableObject throwObject) {

        if (!TryGetComponent(out ControlInputAction input)) yield break;
        Inventory _Inventory = GetComponent<Inventory>();
        _Inventory.throwing = true;
        Collider collider = GetComponent<Collider>();
        line.enabled = true;
        Vector3 _throwDirection = (_camera.forward + (Vector3.up * aimYaxisOffset));
        Animator _Animator = GetComponent<Animator>();
            
            Ray ray = new();
            Vector3[] path = new Vector3[resolution];

            
            while (input.isAiming) {

                _FirePos = collider.bounds.center + (_throwDirection / 3);
                
                RaycastHit hit;
                ray = new(_camera.position, _camera.forward);
                
            if (Physics.Raycast(ray, out hit, throwRange, canHit)) {
                    _throwDirection = ((hit.point - _FirePos).normalized + (Vector3.up * aimYaxisOffset));
                    Debug.DrawLine(_camera.position, hit.point);
                    _FirePos = collider.bounds.center + (_throwDirection / 3);
                    
                    path = new Vector3[resolution];
                    for (float i = 0; i < resolution; i++) {
                       float div = (float)i / ((float)resolution - 1);
                        float dis = Mathf.Clamp(Vector3.Distance(_FirePos, hit.point)*0.1f, 0f, 2f);
                       path[(int)i] = MathParabola.Parabola(_FirePos, hit.point, dis, div);
                        
                    }
                      
                } else {

                    _throwDirection = ((ray.GetPoint(throwRange) - _FirePos).normalized + (Vector3.up * aimYaxisOffset));
                    //(Dir * force) / M * time
                    velocity = ((_throwDirection * _MaxForce) / _Mass) * Time.fixedDeltaTime;

                    path = CalculateLineArray(); 
                }
                
                RenderArc(path);
                yield return null;
            }
            
            _Animator.CrossFade("Throw", .1f);
            _Inventory.throwing = false;
        //Fire throwItem
        Rigidbody rB = Instantiate(throwObject.GetThrowable, _FirePos, Quaternion.identity).GetComponent<Rigidbody>();
        Physics.IgnoreCollision(collider, rB.GetComponent<Collider>(), true);

        rB.AddTorque(UnityEngine.Random.Range(0, throwObject.GetThrowForce * 200), UnityEngine.Random.Range(0, throwObject.GetThrowForce * 200), 
            UnityEngine.Random.Range(0, throwObject.GetThrowForce * 200));
        rB.GetComponent<AbstractThrowable>().Setup(throwObject, gameObject, path);


        //If the item is consumed upon use, then subtract item from inventory
        if (throwObject.IsConsumable()) _Inventory.SubtractInventoryItem(throwObject);
        
        _Activated = false;
        line.enabled = false;

        }

        //Sets the position of the lines nodes were it bends
        //More resolution creates a less jaged line
    private void RenderArc(Vector3[] pointsArray) {
        line.positionCount = pointsArray.Length;
        line.SetPositions(pointsArray);
    }

        //Return an array of the positions of the Lines nodes
    private Vector3[] CalculateLineArray() {
        Vector3[] lineArray = new Vector3[resolution + 1];

        var lowestTimeValueX = MaxTimeX() / resolution;
        var lowestTimeValueZ = MaxTimeZ() / resolution;
        var lowestTimeValue = lowestTimeValueX > lowestTimeValueZ ? lowestTimeValueZ : lowestTimeValueX;

        for (int i = 0; i < lineArray.Length; i++) {
            var t = lowestTimeValue * i;
            lineArray[i] = CalculateLinePoint(t);
        }

        return lineArray;
    }

        //Fire a raycast to find obstacles
    private Vector3 HitPosition() {
        var lowestTimeValue = MaxTimeY() / linecastResolution;

        for (int i = 0; i < linecastResolution + 1; i++) {
            RaycastHit rayHit;

            var t = lowestTimeValue * i;
            var tt = lowestTimeValue * (i + 1);

            if (Physics.Linecast(CalculateLinePoint(t), CalculateLinePoint(tt), out rayHit, canHit))
                return rayHit.point;
        }

        return CalculateLinePoint(MaxTimeY());
    }


    private Vector3 CalculateLinePoint(float t) {
        float x = velocity.x * t;
        float z = velocity.z * t;
        float y = (velocity.y * t) - (g * Mathf.Pow(t, 2) / 2);
        return new Vector3(x + _FirePos.x, y + _FirePos.y, z + _FirePos.z);
    }

    private float MaxTimeY() {
        var v = velocity.y;
        var vv = v * v;

            float NumConverter = Math.Abs(_FirePos.y - yLimit);
        var t = (v + Mathf.Sqrt(vv + 2 * g * NumConverter)) / g;
        return t;
    }

    private float MaxTimeX() {
        if (IsValueAlmostZero(velocity.x))
            SetValueToAlmostZero(ref velocity.x);

        var x = velocity.x;

        var t = (HitPosition().x - _FirePos.x) / x;
        return t;
    }

    private float MaxTimeZ() {
        if (IsValueAlmostZero(velocity.z))
            SetValueToAlmostZero(ref velocity.z);

        var z = velocity.z;

        var t = (HitPosition().z - _FirePos.z) / z;
        return t;
    }
        
    private bool IsValueAlmostZero(float value) {
        return value < 0.0001f && value > -0.0001f;
    }

    private void SetValueToAlmostZero(ref float value) {
        value = 0.0001f;
    }


    }

}
