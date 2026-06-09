using UnityEngine;

public class SpinBlock : MonoBehaviour
{
    public Vector3 maxSpeed = new Vector3(0, 800, 0);

    public float rampUpTime = 60f; // 1 minute to reach full speed

    private float timer = 0f;

    void Update()
    {
        // count up over time
        timer += Time.deltaTime;

        // convert to 0 → 1 over 60 seconds
        float t = Mathf.Clamp01(timer / rampUpTime);

        // smooth turbine curve (slow start, smoother ramp)
        t = t * t; // makes it feel more "heavy machine"

        Vector3 currentSpeed = Vector3.Lerp(Vector3.zero, maxSpeed, t);

        transform.Rotate(currentSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Hit player. Player's health reduced.");
            PlayerController pc = other.gameObject.GetComponent<PlayerController>();
            pc.SetHealth(-20);
        }
    }
}