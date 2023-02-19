﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Configuration;
using KamiLib.Configuration;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Tf2CriticalHitsPlugin.SeFunctions;
using static Tf2CriticalHitsPlugin.Constants;

namespace Tf2CriticalHitsPlugin.Configuration;

[Serializable]
public class ConfigOne : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public SortedDictionary<uint, JobConfig> JobConfigurations { get; set; } = new();

    public ConfigOne()
    {
        foreach (var (key, _) in CombatJobs)
        {
            JobConfigurations[key] = JobConfig.Create(key);
        }
    }

    public class JobConfig
    {
        public Setting<uint> ClassJobId { get; init; } = new(255);
        public ConfigModule DirectCriticalDamage { get; init; } = new();
        public ConfigModule CriticalDamage { get; init; } = new();
        public ConfigModule OwnCriticalHeal { get; init; } = new();

        [Obsolete("Used only to import old JSONs")]
        public ConfigModule CriticalHeal
        {
            init
            {
                // Migration code from Version 2.0.0.0 configuration.
                OwnCriticalHeal = ConfigModule.Create(value.ClassJobId.Value, ModuleType.OwnCriticalHeal);
                OwnCriticalHeal.CopySettingsFrom(value);
                OtherCriticalHeal = ConfigModule.Create(value.ClassJobId.Value, ModuleType.OtherCriticalHeal);
                OtherCriticalHeal.CopySettingsFrom(value);
                if (OtherCriticalHeal.Text.Value == OwnCriticalHeal.GetModuleDefaults().DefaultText)
                {
                    OtherCriticalHeal.Text = new Setting<string>(OtherCriticalHeal.GetModuleDefaults().DefaultText);
                }

                if (OtherCriticalHeal.GameSound.Value == OwnCriticalHeal.GetModuleDefaults().GameSound)
                {
                    OtherCriticalHeal.GameSound = new Setting<Sounds>(OtherCriticalHeal.GetModuleDefaults().GameSound);
                }
            }
        }

        public ConfigModule OtherCriticalHeal { get; init; } = new();

        public ConfigModule DirectDamage { get; init; } = new();


        public static JobConfig Create(uint classJobId)
        {
            return new JobConfig
            {
                ClassJobId = new Setting<uint>(classJobId),
                DirectCriticalDamage = ConfigModule.Create(classJobId, ModuleType.DirectCriticalDamage),
                CriticalDamage = ConfigModule.Create(classJobId, ModuleType.CriticalDamage),
                OwnCriticalHeal = ConfigModule.Create(classJobId, ModuleType.OwnCriticalHeal),
                OtherCriticalHeal = ConfigModule.Create(classJobId, ModuleType.OtherCriticalHeal),
                DirectDamage = ConfigModule.Create(classJobId, ModuleType.DirectDamage)
            };
        }


        public ClassJob GetClassJob() => CombatJobs[ClassJobId.Value];

        public IEnumerator<ConfigModule> GetEnumerator()
        {
            return new[] { DirectCriticalDamage, CriticalDamage, OwnCriticalHeal, OtherCriticalHeal, DirectDamage }
                   .ToList().GetEnumerator();
        }

        public void CopySettingsFrom(JobConfig jobConfig)
        {
            DirectCriticalDamage.CopySettingsFrom(jobConfig.DirectCriticalDamage);
            CriticalDamage.CopySettingsFrom(jobConfig.CriticalDamage);
            OwnCriticalHeal.CopySettingsFrom(jobConfig.OwnCriticalHeal);
            OtherCriticalHeal.CopySettingsFrom(jobConfig.OtherCriticalHeal);
            DirectDamage.CopySettingsFrom(jobConfig.DirectDamage);
        }
    }

    public class ConfigModule
    {
        public static ConfigModule Create(uint classJobId, ModuleType moduleType)
        {
            var configModule = new ConfigModule
            {
                ClassJobId = new Setting<uint>(classJobId),
                ModuleType = new Setting<ModuleType>(moduleType),
            };
            var moduleDefaults = ModuleDefaults.GetDefaultsFromType(moduleType);
            configModule.GameSound = new Setting<Sounds>(moduleDefaults.GameSound);
            configModule.Text = new Setting<string>(moduleDefaults.DefaultText);
            configModule.TextColor = new Setting<ushort>(moduleDefaults.FlyTextParameters.ColorKey.Value);
            configModule.TextGlowColor =
                new Setting<ushort>(moduleDefaults.FlyTextParameters.GlowColorKey.Value);
            configModule.TextItalics = new Setting<bool>(moduleDefaults.FlyTextParameters.Italics);
            return configModule;
        }

        public string GetId()
        {
            return $"{ClassJobId}{ModuleType}";
        }

        public ModuleDefaults GetModuleDefaults()
        {
            return ModuleDefaults.GetDefaultsFromType(ModuleType.Value);
        }

        public Setting<uint> ClassJobId { get; init; } = new(255);
        public Setting<ModuleType> ModuleType { get; init; } = new(Configuration.ModuleType.DirectCriticalDamage);
        public Setting<bool> UseCustomFile { get; set; } = new(false);
        public Setting<bool> SoundForActionsOnly { get; set; } = new(false);
        public Setting<Sounds> GameSound { get; set; } = new(Sounds.None);
        public Setting<string> FilePath { get; set; } = new(string.Empty);
        public Setting<int> Volume { get; set; } = new(12);
        public Setting<bool> ShowText { get; set; } = new(true);
        public Setting<string> Text { get; set; } = new(string.Empty);
        public Setting<ushort> TextColor { get; set; } = new(0);
        public Setting<ushort> TextGlowColor { get; set; } = new(0);
        public Setting<bool> TextItalics { get; set; } = new(false);

        public void CopySettingsFrom(ConfigModule other)
        {
            UseCustomFile = other.UseCustomFile with { };
            SoundForActionsOnly = other.SoundForActionsOnly with { };
            GameSound = other.GameSound with { };
            FilePath = other.FilePath with { };
            Volume = other.Volume with { };
            ShowText = other.ShowText with { };
            Text = other.Text with { };
            TextColor = other.TextColor with { };
            TextGlowColor = other.TextGlowColor with { };
            TextItalics = other.TextItalics with { };
        }
    }


    public void Save()
    {
        File.WriteAllText(Service.PluginInterface.ConfigFile.FullName,
                          JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
