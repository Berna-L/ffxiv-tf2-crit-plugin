﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using KamiLib;
using KamiLib.ChatCommands;
using Newtonsoft.Json;
using Tf2CriticalHitsPlugin.Common.Configuration;
using Tf2CriticalHitsPlugin.Countdown;
using Tf2CriticalHitsPlugin.Countdown.Status;
using Tf2CriticalHitsPlugin.Countdown.Windows;
using Tf2CriticalHitsPlugin.CriticalHits;
using Tf2CriticalHitsPlugin.CriticalHits.Configuration;
using Tf2CriticalHitsPlugin.CriticalHits.Windows;
using Tf2CriticalHitsPlugin.Windows;
using static Dalamud.Logging.PluginLog;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tf2CriticalHitsPlugin
{
    public sealed class Tf2CriticalHitsPlugin : IDalamudPlugin
    {
        public string Name => PluginName;
        public const string PluginName = "Hit it, Joe!";
        private const string CommandName = "/joeconfig";
        private const string LegacyCommandName = "/critconfig";

        public ConfigTwo Configuration { get; init; }
        
        
        public readonly WindowSystem WindowSystem = new("TF2CriticalHitsPlugin");
        
        private readonly CriticalHitsModule criticalHitsModule;
        private readonly CountdownModule countdownModule;

        public Tf2CriticalHitsPlugin(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();
            KamiCommon.Initialize(pluginInterface, Name, () => Configuration?.Save());
            
            Configuration = InitConfig();
            Configuration.Save();
            

            KamiCommon.WindowManager.AddWindow(new ConfigWindow(Configuration));
            KamiCommon.WindowManager.AddWindow(new CriticalHitsCopyWindow(Configuration.criticalHits));
            KamiCommon.WindowManager.AddWindow(new CriticalHitsImportWindow(Configuration.criticalHits));
            KamiCommon.WindowManager.AddWindow(new CountdownNewSettingWindow(Configuration.countdownJams));

            criticalHitsModule = new CriticalHitsModule(Configuration.criticalHits);
            countdownModule = new CountdownModule(State.Instance(), Configuration.countdownJams);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Opens the Hit it, Joe! configuration window",
            });
            
            Service.CommandManager.AddHandler(LegacyCommandName, new CommandInfo(OnConfigCommand)
            {
                ShowInHelp = false
            });

            Service.PluginInterface.UiBuilder.Draw += DrawUserInterface;
            Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigWindow;
        }

        private static ConfigTwo InitConfig()
        {
            var configFile = Service.PluginInterface.ConfigFile.FullName;
            if (!File.Exists(configFile))
            {
                return new ConfigTwo();
            }

            var configText = File.ReadAllText(configFile);
            try
            {
                var versionCheck = JsonSerializer.Deserialize<BaseConfiguration>(configText);
                if (versionCheck is null)
                {
                    return new ConfigTwo();
                }

                var version = versionCheck.Version;
                var config = version switch
                {
                    0 => JsonSerializer.Deserialize<CriticalHitsConfigZero>(configText)?.MigrateToOne().MigrateToTwo(versionCheck.PluginVersion) ?? new ConfigTwo(),
                    1 => JsonConvert.DeserializeObject<CriticalHitsConfigOne>(configText)?.MigrateToTwo(versionCheck.PluginVersion) ?? new ConfigTwo(),
                    2 => JsonConvert.DeserializeObject<ConfigTwo>(configText) ?? new ConfigTwo(),
                    _ => new ConfigTwo()
                };

                TriggerChatAlertsForEarlierVersions(config);

                Directory.CreateDirectory(Path.GetDirectoryName(BackupFileName));
                
                CleanUpOldFiles();
                
                if (Service.PluginInterface.IsDev)
                {
                    Service.PluginInterface.ConfigFile.MoveTo(BackupFileName, true);    
                }


                return config;
            }
            catch (Exception e)
            {
                if (e.StackTrace is not null) LogError(e.StackTrace);
                Service.PluginInterface.ConfigFile.MoveTo(BackupFileName, true);
                Chat.PrintError(
                    $"There was an error while reading your configuration file and it was reset. The old file is available here: {BackupFileName}");
                return new ConfigTwo();
            }

        }

        private static string BackupFileName => $"{Path.Combine(Service.PluginInterface.ConfigDirectory.FullName, "backups", Path.GetFileNameWithoutExtension(Service.PluginInterface.ConfigFile.Name))}.{DateTimeOffset.Now.ToUnixTimeSeconds()}.json";

        private static string BackupFolder => Path.Combine(Service.PluginInterface.ConfigDirectory.FullName, "backups");

        private static void CleanUpOldFiles()
        {
            var timeSpan = TimeSpan.FromDays(5);
            var regexOldFormat = new Regex(Regex.Escape(Service.PluginInterface.ConfigFile.FullName) + @"\.(\d+)\.old");
            var regexNewFormat =
                new Regex(Path.Combine(Regex.Escape(BackupFolder), Path.GetFileNameWithoutExtension(Service.PluginInterface.ConfigFile.Name)) + @"\.(\d+).json");

            bool FileOldFormatTooOld(FileInfo f)
            {
                var match = regexOldFormat.Match(f.FullName);
                if (!match.Success) return false;
                var date = DateTime.UnixEpoch.AddSeconds(long.Parse(match.Groups[1].Value));
                return DateTime.Now - date > timeSpan;
            }

            bool FileNewFormatTooOld(FileInfo f)
            {
                var match = regexNewFormat.Match(f.FullName);
                if (!match.Success) return false;
                var value = match.Groups[1].Value;
                var date = DateTime.UnixEpoch.AddSeconds(long.Parse(value));
                return DateTime.Now - date > timeSpan;
            }

            foreach (var file in Directory.GetParent(Service.PluginInterface.ConfigFile.FullName)?
                                          .GetFiles()
                                          .Where(FileOldFormatTooOld) ?? ArraySegment<FileInfo>.Empty)
            {
                File.Delete(file.FullName);
            }

            foreach (var file in Directory.GetFiles(BackupFolder)
                                          .Select(f => new FileInfo(f))
                                          .Where(FileNewFormatTooOld))
            {
                File.Delete(file.FullName);
            }
        }

        private static void TriggerChatAlertsForEarlierVersions(BaseConfiguration config)
        {
            if (config.PluginVersion.Before(2, 0, 0))
            {
                Chat.Print("Update 2.0.0.0", "Long time no see! Now you can configure Critical Heals, use the game's sound effects and separate configurations per job. Open /critconfig to check!");
            }
            if (config.PluginVersion.Before(2, 1, 0))
            {
                Chat.Print("Update 2.1.0.0", "Now you can configure Critical Heals from your job separately from Critical Heals done by other players' jobs!");
            }
            if (config.PluginVersion.Before(2, 2, 0))
            {
                Chat.Print("Update 2.2.0.0", "New volume settings have been added for v2.2.0.0, which are enabled by default. If you're using a custom sound and it's too low, open /critconfig and adjust.");
            }
            if (config.PluginVersion.Before(3, 0, 0))
            {
                Chat.Print("Update 3.0.0.0", "TF2-ish Critical Hits has been renamed to Hit it Joe, and comes with a new module: Countdown Jams! Configure a sound to be played when a countdown begins and if it's cancelled.");
            }
        }
        
        public void Dispose()
        {
            KamiCommon.Dispose();
            countdownModule.Dispose();
            criticalHitsModule.Dispose();
            this.WindowSystem.RemoveAllWindows();
            Service.CommandManager.RemoveHandler(LegacyCommandName);
            Service.CommandManager.RemoveHandler(CommandName);
        }

        private static void OnConfigCommand(string command, string args)
        {
            if (command.Equals(LegacyCommandName))
            {
                Chat.Print("Deprecated Command", "The command /critconfig is deprecated and will be removed in the future. Use /joeconfig from now on.");
            }
            if (KamiCommon.WindowManager.GetWindowOfType<ConfigWindow>() is { } window)
            {
                window.IsOpen = true;
            }
        }

        private void DrawUserInterface()
        {
            this.WindowSystem.Draw();
        }

        public static void DrawConfigWindow()
        {
            if (KamiCommon.WindowManager.GetWindowOfType<ConfigWindow>() is { } window)
            {
                window.IsOpen = true;
            }
        }
    }
}
