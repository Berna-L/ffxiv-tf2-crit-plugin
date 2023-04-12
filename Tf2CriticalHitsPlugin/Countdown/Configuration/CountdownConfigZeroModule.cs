﻿using System;
using System.Collections.Generic;
using System.IO;
using KamiLib.Configuration;
using KamiLib.ZoneFilterList;

namespace Tf2CriticalHitsPlugin.Countdown.Configuration;

public class CountdownConfigZeroModule
{
    private static string DefaultInterruptSound = Path.Combine(
        Path.GetDirectoryName(Service.PluginInterface.AssemblyLocation.DirectoryName + "\\"),
        @"Countdown\Sounds\record-scratch-freesounds-luffy-3536.wav");
    
    public Setting<string> Id { get; set; } = new(Guid.NewGuid().ToString()); 
    public Setting<string> Label { get; set; } = new (string.Empty);
    public Setting<bool> Enabled { get; set; } = new(true);
    public Setting<string> FilePath { get; set; } = new(string.Empty);
    public Setting<int> Volume { get; set; } = new(100);
    public Setting<bool> ApplySfxVolume { get; set; } = new(true);
    public Setting<string> InterruptedFilePath { get; set; } = new(DefaultInterruptSound);
    public Setting<int> InterruptedVolume { get; set; } = new(100);
    public Setting<bool> InterruptedApplySfxVolume { get; set; } = new(true);
    public Setting<int> MinimumCountdownTimer { get; set; } = new(5);
    public Setting<int> MaximumCountdownTimer { get; set; } = new(30);
    public Setting<bool> DelayPlay { get; set; } = new(false);
    public Setting<int> DelayUntilCountdownHits { get; set; } = new(1);
    public Setting<bool> PlayWithOtherSounds { get; set; } = new(false);
    public Setting<bool> StopWhenCountdownCompletes { get; set; } = new(false);
    public Setting<bool> AllTerritories { get; set; } = new(true);
    public Setting<List<uint>> TerritoryList { get; init; } = new(new List<uint>());

    [NonSerialized]
    [Obsolete("Use TerritoryList in its place.")]
    private IList<uint> Territories = new List<uint>();

    public Setting<ZoneFilterTypeId> TerritoryFilterType { get; init; } = new(ZoneFilterTypeId.Whitelist);

    private CountdownConfigZeroModule()
    {
    }

    public bool ValidForCountdown(double countdownValue)
    {
        return MinimumCountdownTimer.Value <= countdownValue && MaximumCountdownTimer.Value >= countdownValue;
    }
    
    public bool ValidForTerritory(ushort territoryId)
    {
        return AllTerritories || (TerritoryFilterType.Value == ZoneFilterTypeId.Whitelist &&
                                  TerritoryList.Value.Contains(territoryId))
                              || (TerritoryFilterType.Value == ZoneFilterTypeId.Blacklist &&
                                  !TerritoryList.Value.Contains(territoryId));
    }
    
    public static CountdownConfigZeroModule Create(string name)
    {
        return new CountdownConfigZeroModule
        {
            Label = new Setting<string>(name)
        };
    }
}
