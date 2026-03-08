using UnityEngine;

[CreateAssetMenu(fileName = "ShipData", menuName = "Icarus/Ship Data")]
public class ShipData : ScriptableObject
{
    [Header("Movement")]
    public float MoveSpeed  = 3f;
    public float JumpRange  = 10f;

    [Header("Hull")]
    public int   MaxHull    = 100;
    public float RepairRate = 1f;

    [Header("Cargo")]
    public int MaxCargo     = 50;

    [Header("Crew")]
    public int MaxCrew      = 6;

    [Header("Power")]
    public int MaxPower     = 10;
}
