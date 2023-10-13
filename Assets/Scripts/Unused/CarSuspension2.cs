using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSuspension2 : MonoBehaviour
{
    public GameObject wheelPrefab;
    public Vector2 wheelDistance = new Vector2(2, 2);

    GameObject[] wheelPrefabs = new GameObject[4];
    Vector3[] wheels = new Vector3[4];
    float rpm = 0;

    public float maxSuspentionLength = 1f;
    public float suspentionMultiplier = 320f;
    public float maxTraction = 240f;
    public float traction = 120f;

    Rigidbody rb;

    Vector3[] suspensionForce = new Vector3[4];
    Vector3[] tractionForce = new Vector3[4];

    private void Awake()
    {
        suspentionMultiplier = suspentionMultiplier / maxSuspentionLength;
        for (int i = 0; i < 4; i++)
        {
            wheelPrefabs[i] = Instantiate(wheelPrefab, wheels[i], Quaternion.identity);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        wheels[0] = transform.right * wheelDistance.x + transform.forward * wheelDistance.y; //front right
        wheels[1] = transform.right * -wheelDistance.x + transform.forward * wheelDistance.y; //front left
        wheels[2] = transform.right * wheelDistance.x + transform.forward * -wheelDistance.y; //back right
        wheels[3] = transform.right * -wheelDistance.x + transform.forward * -wheelDistance.y; //back left

        for (int i = 0; i < 4; i++)
        {
            if (i == 0 || i == 1) wheelPrefabs[i].transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + Input.GetAxisRaw("Horizontal") * 45f, transform.eulerAngles.z);
            else wheelPrefabs[i].transform.rotation = transform.rotation;

            rpm += Input.GetAxisRaw("Vertical") * Time.deltaTime;

            rpm -= ((rpm * Mathf.PI) + -transform.InverseTransformDirection(rb.velocity).z) * Time.deltaTime * 0.1f;

            RaycastHit hit;
            Physics.Raycast(transform.position + wheels[i], -transform.up, out hit, maxSuspentionLength);
            if (hit.collider != null)
            {
                wheelPrefabs[i].transform.position = hit.point + transform.up * 0.5f;

                wheelPrefabs[i].transform.GetChild(0).Rotate(0, -rpm, 0, Space.Self);

                suspensionForce[i] = transform.up * (Mathf.Clamp(maxSuspentionLength - hit.distance, 0, 3) * suspentionMultiplier);

                tractionForce[i] = Vector3.ClampMagnitude( traction * (transform.right * -transform.InverseTransformDirection(rb.velocity).x + transform.forward * ((rpm * Mathf.PI) + -transform.InverseTransformDirection(rb.velocity).z)), maxTraction );
                if (i == 0 || i == 1) tractionForce[i] = Quaternion.AngleAxis(45 * Input.GetAxisRaw("Horizontal"), Vector3.up) * tractionForce[i];

                rb.AddForceAtPosition((tractionForce[i] + suspensionForce[i]) * Time.deltaTime, hit.point);
            }
            else
            {
                wheelPrefabs[i].transform.position = transform.position + wheels[i] - transform.up * (maxSuspentionLength - 0.5f);
            }
        }
        
    }

}