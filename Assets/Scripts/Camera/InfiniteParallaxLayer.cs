using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class InfiniteParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    [Range(0f, 1f)]
    [SerializeField] private float parallaxAmount = 0.1f;

    [SerializeField] private float seamOverlap = 0.01f;

    private Transform[] tiles = new Transform[3];

    private float tileWidth;
    private float startingX;
    private float cameraStartingX;

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        tileWidth = spriteRenderer.bounds.size.x - seamOverlap;
        startingX = transform.position.x;
        cameraStartingX = cameraTransform.position.x;

        tiles[1] = transform;

        tiles[0] = CreateCopy("Left Copy");
        tiles[2] = CreateCopy("Right Copy");
    }

    private Transform CreateCopy(string copyName)
    {
        GameObject copy = Instantiate(gameObject, transform.parent);
        copy.name = gameObject.name + " - " + copyName;

        InfiniteParallaxLayer copyScript =
            copy.GetComponent<InfiniteParallaxLayer>();

        copyScript.enabled = false;

        return copy.transform;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || tileWidth <= 0f)
        {
            return;
        }

        float cameraDistance =
            cameraTransform.position.x - cameraStartingX;

        float parallaxX =
            startingX + cameraDistance * parallaxAmount;

        float repeatNumber = Mathf.Round(
            (cameraTransform.position.x - parallaxX) / tileWidth
        );

        float centreX =
            parallaxX + repeatNumber * tileWidth;

        PositionTile(tiles[0], centreX - tileWidth);
        PositionTile(tiles[1], centreX);
        PositionTile(tiles[2], centreX + tileWidth);
    }

    private void PositionTile(Transform tile, float xPosition)
    {
        Vector3 position = tile.position;
        position.x = xPosition;
        tile.position = position;
    }
}
