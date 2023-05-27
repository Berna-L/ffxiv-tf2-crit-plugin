using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using KamiLib;
using KamiLib.ChatCommands;
using KamiLib.Configuration;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Tf2CriticalHitsPlugin.Common.Configuration;
using Tf2CriticalHitsPlugin.Configuration;
using Tf2CriticalHitsPlugin.SeFunctions;
using static Tf2CriticalHitsPlugin.Constants;
using Configuration_ModuleType = Tf2CriticalHitsPlugin.Configuration.ModuleType;

namespace Tf2CriticalHitsPlugin.CriticalHits.Configuration;

[Serializable]
public class CriticalHitsConfigOne
{

    public int Version { get; set; }

    public SortedDictionary<uint, JobConfig> JobConfigurations { get; set; } = new();

    public CriticalHitsConfigOne()
    {
        Version = 1;
        foreach (var (key, _) in CombatJobs)
        {
            JobConfigurations[key] = JobConfig.Create(key);
        }
    }

    public class JobConfig
    {
        public Setting<uint> ClassJobId { get; init; } = new(255);
        public Setting<int> TimeBetweenSounds = new(0);
        public ConfigModule DirectCriticalDamage { get; init; } = new();
        public ConfigModule CriticalDamage { get; init; } = new();
        public ConfigModule DirectDamage { get; init; } = new();
        public ConfigModule OwnCriticalHeal { get; init; } = new();
        public ConfigModule OwnFairyCriticalHeal { get; init; } = new();
        public ConfigModule OtherCriticalHeal { get; init; } = new();
        public ConfigModule OtherFairyCriticalHeal { get; init; } = new();



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


        public static JobConfig Create(uint classJobId)
        {
            return new JobConfig
            {
                ClassJobId = new Setting<uint>(classJobId),
                TimeBetweenSounds = new Setting<int>(0),
                DirectCriticalDamage = ConfigModule.Create(classJobId, ModuleType.DirectCriticalDamage),
                CriticalDamage = ConfigModule.Create(classJobId, ModuleType.CriticalDamage),
                DirectDamage = ConfigModule.Create(classJobId, ModuleType.DirectDamage),
                OwnCriticalHeal = ConfigModule.Create(classJobId, ModuleType.OwnCriticalHeal),
                OwnFairyCriticalHeal = ConfigModule.Create(classJobId, ModuleType.OwnFairyCriticalHeal),
                OtherCriticalHeal = ConfigModule.Create(classJobId, ModuleType.OtherCriticalHeal),
                OtherFairyCriticalHeal = ConfigModule.Create(classJobId, ModuleType.OtherFairyCriticalHeal),
            };
        }


        public ClassJob GetClassJob() => CombatJobs[ClassJobId.Value];
        
        public void CopySettingsFrom(JobConfig jobConfig)
        {
            TimeBetweenSounds = jobConfig.TimeBetweenSounds with { };
            DirectCriticalDamage.CopySettingsFrom(jobConfig.DirectCriticalDamage);
            CriticalDamage.CopySettingsFrom(jobConfig.CriticalDamage);
            DirectDamage.CopySettingsFrom(jobConfig.DirectDamage);
            OwnCriticalHeal.CopySettingsFrom(jobConfig.OwnCriticalHeal);
            OwnFairyCriticalHeal.CopySettingsFrom(jobConfig.OwnFairyCriticalHeal);
            OtherCriticalHeal.CopySettingsFrom(jobConfig.OtherCriticalHeal);
            OtherFairyCriticalHeal.CopySettingsFrom(jobConfig.OtherFairyCriticalHeal);
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
        public Setting<ModuleType> ModuleType { get; init; } = new(Configuration_ModuleType.DirectCriticalDamage);
        public Setting<bool> ApplyInPvP { get; set; } = new(false);
        public Setting<bool> UseCustomFile { get; set; } = new(false);
        public Setting<bool> SoundForActionsOnly { get; set; } = new(false);
        public Setting<Sounds> GameSound { get; set; } = new(Sounds.None);
        public Setting<string> FilePath { get; set; } = new(string.Empty);
        public Setting<int> Volume { get; set; } = new(12);
        public Setting<bool> ApplySfxVolume { get; set; } = new(true);
        public Setting<bool> ShowText { get; set; } = new(true);
        public Setting<string> Text { get; set; } = new(string.Empty);
        public Setting<ushort> TextColor { get; set; } = new(0);
        public Setting<ushort> TextGlowColor { get; set; } = new(0);
        public Setting<bool> TextItalics { get; set; } = new(false);

        public void CopySettingsFrom(ConfigModule other)
        {
            ApplyInPvP = other.ApplyInPvP with { };
            UseCustomFile = other.UseCustomFile with { };
            SoundForActionsOnly = other.SoundForActionsOnly with { };
            GameSound = other.GameSound with { };
            FilePath = other.FilePath with { };
            Volume = other.Volume with { };
            ApplySfxVolume = other.ApplySfxVolume with { };
            ShowText = other.ShowText with { };
            Text = other.Text with { };
            TextColor = other.TextColor with { };
            TextGlowColor = other.TextGlowColor with { };
            TextItalics = other.TextItalics with { };
        }
    }
    
    public static IEnumerable<ConfigModule> GetModules(JobConfig jobConfig)
        => new[] { jobConfig.DirectCriticalDamage, jobConfig.CriticalDamage, jobConfig.DirectDamage, jobConfig.OwnCriticalHeal, jobConfig.OwnFairyCriticalHeal, jobConfig.OtherCriticalHeal, jobConfig.OtherFairyCriticalHeal };

    
    public void CreateZip(string path)
    {
        var actualPath = Path.HasExtension(path) ? path : $"{path}.zip";
        try
        {
            var stagingPath = Path.Combine(Path.GetTempPath(), "critplugin");
            if (Directory.Exists(stagingPath))
            {
                foreach (var file in Directory.GetFiles(stagingPath))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(stagingPath);
            }
            var files = JobConfigurations.Select(c => c.Value)
                                         .SelectMany(GetModules)
                                         .Where(c => c.UseCustomFile)
                                         .Select(c => c.FilePath.ToString())
                                         .Distinct()
                                         .Where(File.Exists)
                                         .ToDictionary(s => s, s => CopyAndRenameFile(s, stagingPath));
            var zippedConfig = this.Clone();
            foreach (var configModule in zippedConfig.JobConfigurations.Select(c => c.Value)
                                          .SelectMany(GetModules)
                                          .Where(c => File.Exists(c.FilePath.Value)))
            {
                configModule.FilePath = new Setting<string>(files[configModule.FilePath.Value]);
            }
            File.WriteAllText(Path.Combine(stagingPath, "config.json"), JsonConvert.SerializeObject(zippedConfig, Formatting.Indented));
                ZipFile.CreateFromDirectory(stagingPath, actualPath);
                Chat.Print("Sharing", $"The ZIP was created successfully at {actualPath}. Use it to share with your friends!");
        }
        catch (IOException exception)
        {
            if (exception.Message.EndsWith("already exists."))
            {
                Chat.PrintError($"The file \"{Path.GetFileName(actualPath)}\" already exists in the chosen folder.");
            }
            throw;
        }
    }

    public static CriticalHitsConfigOne? GenerateFrom(string zipPath)
    {
        var zipArchive = ZipFile.OpenRead(zipPath);
        var configFile = zipArchive.GetEntry("config.json");
        if (configFile is null) return null;
        var newConfig = JsonConvert.DeserializeObject<CriticalHitsConfigOne>(new StreamReader(configFile.Open()).ReadToEnd());
        return newConfig;
    }

    public void ImportFrom(string zipPath, string soundsPath, CriticalHitsConfigOne newCriticalHitsConfig)
    {
        var zipArchive = ZipFile.OpenRead(zipPath);
        foreach (var entry in zipArchive.Entries.Where(e => e.Name is not "config.json"))
        {
            entry.ExtractToFile(Path.Combine(soundsPath, entry.Name), true);
        }
        foreach (var module in newCriticalHitsConfig.JobConfigurations.Select(c => c.Value)
                                        .SelectMany(GetModules))
        {
            module.FilePath = new Setting<string>(Path.Combine(soundsPath, module.FilePath.Value));
        }
        foreach (var jobConfig in JobConfigurations.Select(c => c.Value))
        {
            jobConfig.CopySettingsFrom(newCriticalHitsConfig.JobConfigurations[jobConfig.ClassJobId.Value]);
        }
        KamiCommon.SaveConfiguration();
    }

    private CriticalHitsConfigOne Clone()
    {
        var newInstance = new CriticalHitsConfigOne();
        newInstance.JobConfigurations.ToList()
                   .ForEach(kv => kv.Value.CopySettingsFrom(JobConfigurations[kv.Key]));
        return newInstance;
    }

    private static string CopyAndRenameFile(string originalFile, string destDirectory)
    {
        var fileName = Guid.NewGuid() + Path.GetExtension(originalFile);
        File.Copy(originalFile, Path.Combine(destDirectory, fileName));
        return fileName;
    }

    public ConfigTwo MigrateToTwo(PluginVersion pluginVersion)
    {
        return new ConfigTwo
        {
            PluginVersion = pluginVersion,
            criticalHits = this
        };
    }

    public void Rescue(CriticalHitsConfigOne fileCriticalHits)
    {
        foreach (var (key, _) in fileCriticalHits.JobConfigurations)
        {
            JobConfigurations[key] = fileCriticalHits.JobConfigurations[key];
        }
    }

    public void MigrateToFairyHealing()
    {
        foreach (var (_, job) in JobConfigurations)
        {
            if (job.OwnFairyCriticalHeal.ClassJobId.Value == 255)
            {
                job.OwnFairyCriticalHeal.ClassJobId.Value = job.ClassJobId.Value;
                job.OwnFairyCriticalHeal.CopySettingsFrom(job.OwnCriticalHeal);
                if (job.OwnFairyCriticalHeal.Text.Value == ModuleDefaults.GetDefaultsFromType(ModuleType.OwnCriticalHeal).DefaultText)
                {
                    job.OwnFairyCriticalHeal.Text.Value =
                        ModuleDefaults.GetDefaultsFromType(ModuleType.OwnFairyCriticalHeal).DefaultText;
                }
                job.OwnFairyCriticalHeal.ModuleType.Value = ModuleType.OwnFairyCriticalHeal;
            }

            if (job.OtherFairyCriticalHeal.ClassJobId.Value == 255)
            {
                job.OtherFairyCriticalHeal.ClassJobId.Value = job.ClassJobId.Value;
                job.OtherFairyCriticalHeal.CopySettingsFrom(job.OtherCriticalHeal);
                if (job.OtherFairyCriticalHeal.Text.Value == ModuleDefaults.GetDefaultsFromType(ModuleType.OtherCriticalHeal).DefaultText)
                {
                    job.OtherFairyCriticalHeal.Text.Value =
                        ModuleDefaults.GetDefaultsFromType(ModuleType.OtherFairyCriticalHeal).DefaultText;
                }
                job.OtherFairyCriticalHeal.ModuleType.Value = ModuleType.OtherFairyCriticalHeal;
            }
        }
    }
}
