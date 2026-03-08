using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Procedurally fills an isometric tilemap with a flat grid of tiles.
/// Attach to the Grid GameObject. Assign the Tilemap and a Tile asset in the Inspector.
/// </summary>
public class WorldGrid : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TileBase _tile;
    [SerializeField] private int _width  = 20;
    [SerializeField] private int _height = 20;

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        _tilemap.ClearAllTiles();

        int halfW = _width  / 2;
        int halfH = _height / 2;

        for (int x = -halfW; x < halfW; x++)
        {
            for (int y = -halfH; y < halfH; y++)
            {
                _tilemap.SetTile(new Vector3Int(x, y, 0), _tile);
            }
        }
    }
}
