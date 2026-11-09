using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform background;

        [Range(0f, 1f)]
        public float parallaxAmount;
    }

    [SerializeField] private ParallaxLayer[] layers;

    private Transform cameraTransform;
    private Vector3 cameraStartPosition;
    private Vector3[] layerStartPositions;

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        cameraStartPosition = cameraTransform.position;

        layerStartPositions = new Vector3[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].background != null)
            {
                layerStartPositions[i] = layers[i].background.position;
            }
        }
    }

    private void LateUpdate()
    {
        float cameraMovement =
            cameraTransform.position.x - cameraStartPosition.x;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].background == null)
            {
                continue;
            }

            Vector3 startingPosition = layerStartPositions[i];

            layers[i].background.position = new Vector3(
                startingPosition.x +
                cameraMovement * layers[i].parallaxAmount,
                startingPosition.y,
                startingPosition.z
            );
        }
    }
}