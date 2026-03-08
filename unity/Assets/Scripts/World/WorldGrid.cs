using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Fills an isometric tilemap procedurally at runtime.
/// Attach to the Grid GameObject. Assign Tilemap and Tile in Inspector (or via bootstrapper).
/// </summary>
public class WorldGrid : MonoBehaviour
{
    [SerializeField] private Tilemap  _tilemap;
    [SerializeField] private TileBase _tile;
    [SerializeField] private int      _width  = 24;
    [SerializeField] private int      _height = 24;

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (_tilemap == null || _tile == null) return;
        _tilemap.ClearAllTiles();

        int halfW = _width  / 2;
        int halfH = _height / 2;

        for (int x = -halfW; x < halfW; x++)
        for (int y = -halfH; y < halfH; y++)
            _tilemap.SetTile(new Vector3Int(x, y, 0), _tile);
    }
}
