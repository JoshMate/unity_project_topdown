using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Project-wide NPC thought configuration stored as an asset in a Resources folder. Holds one entry per enumNpcThought
/// (except None) with its sprite and how long it stays shown. Missing entries are added automatically, so a new thought
/// only needs a new enumNpcThought value and a sprite assigned here.
/// </summary>
[CreateAssetMenu(fileName = "OB_Config_NpcThoughts", menuName = "Config/Npc Thoughts")]
public class F_Utility_Config_NpcThoughts : ScriptableObject
{
    /// <summary>The sprite and display duration for one thought.</summary>
    [Serializable]
    public class NpcThoughtSettings
    {
        public enumNpcThought thought;
        public Sprite thoughtSprite;
        // Seconds the thought stays shown before hiding itself; 0 keeps it shown until the NPC clears or replaces it
        [Min(cfgNpcThoughtDisplayDurationUntilCleared)]
        public float thoughtDisplayDuration;
    }

    [Header("Constants Public")]
    // Display duration meaning the thought stays shown until the NPC clears or replaces it
    public const float cfgNpcThoughtDisplayDurationUntilCleared = 0f;
    // Gap in world units between the top of the NPC's body and the bottom of the thought icon
    public const float cfgNpcThoughtOffsetY = 0.2f;
    // Uniform scale applied to the thought icon sprite
    public const float cfgNpcThoughtScale = 0.65f;
    // Sorting order added on top of the NPC's highest sprite (between health bars and damage numbers)
    public const int cfgNpcThoughtSortingOrderOffset = 95;

    [Header("Constants Private")]
    private const string cfgNpcThoughtsResourcePath = "OB_Config_NpcThoughts";

    [Header("Npc Thought Config")]
    // One entry per enumNpcThought except None
    public List<NpcThoughtSettings> cfgNpcThoughtSettings = new List<NpcThoughtSettings>();

    [Header("Privates")]
    private static F_Utility_Config_NpcThoughts instance;

    private void Reset()
    {
        EnsureThoughtEntries();
    }

    private void OnValidate()
    {
        EnsureThoughtEntries();
    }

    // Keeps one entry per thought (None excluded), adding missing ones with no sprite and the until-cleared duration.
    private void EnsureThoughtEntries()
    {
        foreach (enumNpcThought thought in Enum.GetValues(typeof(enumNpcThought)))
        {
            bool isMissing = thought != enumNpcThought.None && !cfgNpcThoughtSettings.Exists(entry => entry != null && entry.thought == thought);
            if (isMissing)
            {
                cfgNpcThoughtSettings.Add(new NpcThoughtSettings
                {
                    thought = thought,
                    thoughtDisplayDuration = cfgNpcThoughtDisplayDurationUntilCleared
                });
            }
        }
    }

    /// <summary>Loads (and caches) the config asset from Resources.</summary>
    /// <returns>The config asset, or null if it does not exist.</returns>
    public static F_Utility_Config_NpcThoughts Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<F_Utility_Config_NpcThoughts>(cfgNpcThoughtsResourcePath);
        }

        return instance;
    }

    /// <summary>Returns the settings configured for a thought.</summary>
    /// <param name="thought">The thought to look up.</param>
    /// <returns>The matching settings, or null when the asset or entry is missing.</returns>
    public static NpcThoughtSettings GetThoughtSettings(enumNpcThought thought)
    {
        F_Utility_Config_NpcThoughts config = Get();
        return config == null ? null : config.cfgNpcThoughtSettings.Find(entry => entry != null && entry.thought == thought);
    }
}
