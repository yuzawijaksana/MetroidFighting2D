using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapPlatformController : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TilemapCollider2D tilemapCollider;
    [SerializeField] private Collider2D playerCollider;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private LayerMask noCollisionLayerMask;

    private void Start()
    {
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
            if (tilemap == null)
            {
                Debug.LogError("Tilemap is not assigned or missing.");
            }
        }

        if (tilemapCollider == null)
        {
            tilemapCollider = GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
            {
                Debug.LogError("TilemapCollider2D is not assigned or missing.");
            }
        }

        if (playerCollider == null)
        {
            Debug.LogError("PlayerCollider is not assigned.");
        }
    }

    private void Update()
    {
        if (playerCollider == null || tilemapCollider == null) return;

        if (playerCollider.bounds.max.y < transform.position.y)
        {
            gameObject.layer = LayerMaskToLayer(noCollisionLayerMask);
        }
        else
        {
            gameObject.layer = LayerMaskToLayer(platformLayerMask);
        }
    }

    private int LayerMaskToLayer(LayerMask layerMask)
    {
        int layer = 0;
        int mask = layerMask.value;
        while (mask > 1)
        {
            mask >>= 1;
            layer++;
        }
        return layer;
    }
}
