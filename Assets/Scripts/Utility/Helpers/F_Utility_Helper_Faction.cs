using UnityEngine;

/// <summary>
/// Faction checks between ents. Only characters belong to a faction; any other ent (crates, walls) is neither friendly
/// nor hostile. Relationships are one-way: each faction's view of another is read from F_Utility_Config_Factions.
/// </summary>
public static class F_Utility_Helper_Faction
{
    /// <summary>Returns how the judging ent's faction treats the other ent's faction, or neutral when either is not a character.</summary>
    /// <param name="judgingEnt">The ent doing the judging, such as an attacker.</param>
    /// <param name="otherEnt">The ent being judged, such as a possible target.</param>
    public static enumFactionRelationship GetRelationship(F_Ent judgingEnt, F_Ent otherEnt)
    {
        if (!(judgingEnt is F_Character judgingCharacter) || !(otherEnt is F_Character otherCharacter))
        {
            return enumFactionRelationship.Neutral;
        }

        return F_Utility_Config_Factions.GetRelationship(judgingCharacter.characterFaction, otherCharacter.characterFaction);
    }

    /// <summary>Whether the judging ent treats the other ent as an ally.</summary>
    /// <param name="judgingEnt">The ent doing the judging.</param>
    /// <param name="otherEnt">The ent being judged.</param>
    public static bool IsFriendly(F_Ent judgingEnt, F_Ent otherEnt)
    {
        return GetRelationship(judgingEnt, otherEnt) == enumFactionRelationship.Friendly;
    }

    /// <summary>Whether the judging ent treats the other ent as an enemy to attack.</summary>
    /// <param name="judgingEnt">The ent doing the judging.</param>
    /// <param name="otherEnt">The ent being judged.</param>
    public static bool IsHostile(F_Ent judgingEnt, F_Ent otherEnt)
    {
        return GetRelationship(judgingEnt, otherEnt) == enumFactionRelationship.Hostile;
    }

    /// <summary>Finds the character behind a damage source, such as the holder of a weapon or the shooter of a projectile.</summary>
    /// <param name="damageSource">The object credited with the damage, may be null.</param>
    /// <returns>The character, or null when the source is not part of a character.</returns>
    public static F_Character GetSourceCharacter(GameObject damageSource)
    {
        return damageSource != null ? damageSource.GetComponentInParent<F_Character>() : null;
    }
}
