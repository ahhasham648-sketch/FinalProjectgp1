using UnityEngine;

public class MoveNoPhysics : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(x, y, 0f).normalized;

        transform.Translate(movement * speed * Time.deltaTime);
    }
}