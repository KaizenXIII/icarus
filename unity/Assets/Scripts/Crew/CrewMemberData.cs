using UnityEngine;

[CreateAssetMenu(fileName = "CrewMember", menuName = "Icarus/Crew Member")]
public class CrewMemberData : ScriptableObject
{
    [Header("Identity")]
    public string CrewName;
    public Sprite Portrait;

    [Header("Station Skills")]
    [Range(1, 10)] public int Navigation  = 1;
    [Range(1, 10)] public int Engineering = 1;
    [Range(1, 10)] public int Combat      = 1;
    [Range(1, 10)] public int Mining      = 1;
    [Range(1, 10)] public int Medical     = 1;

    [Header("State")]
    [Range(0, 100)] public int Morale     = 100;
    [Range(0, 100)] public int Health     = 100;
}
