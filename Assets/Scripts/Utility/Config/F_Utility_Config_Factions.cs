using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Project-wide faction configuration stored as an asset in a Resources folder. Holds one entry per enumFaction with its
/// display name and how it treats every faction (itself included). Missing entries are added automatically, so a new
/// faction only needs a new enumFaction value.
/// </summary>
[CreateAssetMenu(fileName = "OB_Config_Factions", menuName = "Config/Factions")]
public class F_Utility_Config_Factions : ScriptableObject
{
    /// <summary>How the owning faction treats one other faction.</summary>
    [Serializable]
    public class FactionRelationship
    {
        public enumFaction relationshipFaction;
        public enumFactionRelationship relationshipType;
    }

    /// <summary>A faction's display name and its relationships with every faction.</summary>
    [Serializable]
    public class FactionSettings
    {
        public string factionName;
        public enumFaction faction;
        public List<FactionRelationship> factionRelationships = new List<FactionRelationship>();
    }

    [Header("Constants Public")]
    // Faction given to characters that have not had one chosen
    public const enumFaction cfgFactionCharacterDefault = enumFaction.Civilian;
    // Relationship a faction starts with towards itself
    public const enumFactionRelationship cfgFactionRelationshipDefaultSelf = enumFactionRelationship.Friendly;
    // Relationship a faction starts with towards every other faction
    public const enumFactionRelationship cfgFactionRelationshipDefaultOther = enumFactionRelationship.Hostile;

    [Header("Constants Private")]
    private const string cfgFactionsResourcePath = "OB_Config_Factions";

    [Header("Faction Config")]
    // One entry per enumFaction; each lists how that faction treats every faction
    public List<FactionSettings> cfgFactionSettings = new List<FactionSettings>();

    [Header("Privates")]
    private static F_Utility_Config_Factions instance;
    // Relationship lookup indexed [from, to], rebuilt whenever the asset changes
    private enumFactionRelationship[,] factionRelationshipCache;

    private void Reset()
    {
        EnsureFactionEntries();
    }

    private void OnValidate()
    {
        EnsureFactionEntries();
        factionRelationshipCache = null;
    }

    // Keeps one entry per faction, each with one relationship per faction, adding missing ones with the default relationships.
    private void EnsureFactionEntries()
    {
        Array allFactions = Enum.GetValues(typeof(enumFaction));
        foreach (enumFaction faction in allFactions)
        {
            FactionSettings settings = cfgFactionSettings.Find(entry => entry != null && entry.faction == faction);
            if (settings == null)
            {
                settings = new FactionSettings { factionName = faction.ToString(), faction = faction };
                cfgFactionSettings.Add(settings);
            }

            foreach (enumFaction otherFaction in allFactions)
            {
                if (!settings.factionRelationships.Exists(relationship => relationship != null && relationship.relationshipFaction == otherFaction))
                {
                    settings.factionRelationships.Add(new FactionRelationship
                    {
                        relationshipFaction = otherFaction,
                        relationshipType = GetDefaultRelationship(faction, otherFaction)
                    });
                }
            }
        }
    }

    /// <summary>Loads (and caches) the config asset from Resources.</summary>
    /// <returns>The config asset, or null if it does not exist.</returns>
    public static F_Utility_Config_Factions Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<F_Utility_Config_Factions>(cfgFactionsResourcePath);
        }
        return instance;
    }

    /// <summary>Returns how one faction treats another, falling back to the defaults when the asset or entry is missing.</summary>
    /// <param name="fromFaction">The faction doing the judging.</param>
    /// <param name="toFaction">The faction being judged.</param>
    public static enumFactionRelationship GetRelationship(enumFaction fromFaction, enumFaction toFaction)
    {
        F_Utility_Config_Factions config = Get();
        return config == null ? GetDefaultRelationship(fromFaction, toFaction) : config.GetCachedRelationship(fromFaction, toFaction);
    }

    /// <summary>Returns the display name configured for a faction, or the enum name when none is set.</summary>
    /// <param name="faction">The faction to look up.</param>
    public static string GetFactionName(enumFaction faction)
    {
        F_Utility_Config_Factions config = Get();
        FactionSettings settings = config == null ? null : config.cfgFactionSettings.Find(entry => entry != null && entry.faction == faction);
        return settings != null && !string.IsNullOrEmpty(settings.factionName) ? settings.factionName : faction.ToString();
    }

    // The relationship a faction starts with: friendly to itself, hostile to everyone else.
    private static enumFactionRelationship GetDefaultRelationship(enumFaction fromFaction, enumFaction toFaction)
    {
        return fromFaction == toFaction ? cfgFactionRelationshipDefaultSelf : cfgFactionRelationshipDefaultOther;
    }

    private enumFactionRelationship GetCachedRelationship(enumFaction fromFaction, enumFaction toFaction)
    {
        if (factionRelationshipCache == null)
        {
            BuildRelationshipCache();
        }

        int fromIndex = (int)fromFaction;
        int toIndex = (int)toFaction;
        bool isInCache = fromIndex >= 0 && toIndex >= 0 &&
                         fromIndex < factionRelationshipCache.GetLength(0) && toIndex < factionRelationshipCache.GetLength(1);
        return isInCache ? factionRelationshipCache[fromIndex, toIndex] : GetDefaultRelationship(fromFaction, toFaction);
    }

    // Fills the [from, to] lookup with defaults, then overwrites them with the configured relationships.
    private void BuildRelationshipCache()
    {
        int factionCount = Enum.GetValues(typeof(enumFaction)).Length;
        factionRelationshipCache = new enumFactionRelationship[factionCount, factionCount];
        for (int fromIndex = 0; fromIndex < factionCount; fromIndex++)
        {
            for (int toIndex = 0; toIndex < factionCount; toIndex++)
            {
                factionRelationshipCache[fromIndex, toIndex] = GetDefaultRelationship((enumFaction)fromIndex, (enumFaction)toIndex);
            }
        }

        foreach (FactionSettings settings in cfgFactionSettings)
        {
            if (settings == null || (int)settings.faction < 0 || (int)settings.faction >= factionCount)
            {
                continue;
            }

            foreach (FactionRelationship relationship in settings.factionRelationships)
            {
                if (relationship != null && (int)relationship.relationshipFaction >= 0 && (int)relationship.relationshipFaction < factionCount)
                {
                    factionRelationshipCache[(int)settings.faction, (int)relationship.relationshipFaction] = relationship.relationshipType;
                }
            }
        }
    }
}
