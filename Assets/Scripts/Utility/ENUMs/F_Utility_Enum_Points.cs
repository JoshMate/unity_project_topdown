/// <summary>What a placed point is used for. Game logic looks points up by this type.</summary>
/// <remarks>Serialised by index: only append new values at the end, never reorder or remove existing ones.</remarks>
public enum enumPointType
{
    None,
    FunctionPlayerSpawnPoint,
    Spawn
}
