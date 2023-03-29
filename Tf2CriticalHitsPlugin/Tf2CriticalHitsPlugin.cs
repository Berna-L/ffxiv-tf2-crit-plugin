using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
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
using PluginVersion = Tf2CriticalHitsPlugin.Configuration.PluginVersion;

namespace Tf2CriticalHitsPlugin
{
    public sealed class Tf2CriticalHitsPlugin : IDalamudPlugin
    {
        public string Name => PluginName;
        public const string PluginName = "Hit it, Joe!";
        private const string CommandName = "/joeconfig";
        private const string RescueCommandName = "/rescuemejoe";
        private const string LegacyCommandName = "/critconfig";

        public ConfigTwo Configuration { get; private set; }
        
        
        public readonly WindowSystem WindowSystem = new("TF2CriticalHitsPlugin");
        
        private readonly CriticalHitsModule criticalHitsModule;
        private readonly CountdownModule countdownModule;
        private static string BackupsFolderName = "backups";
        private static readonly Regex BackupFileNameFormat = new Regex(Regex.Escape(Path.Combine(BackupsFolder, Path.GetFileNameWithoutExtension(Service.PluginInterface.ConfigFile.Name))) + @"\.(\d+)\.json");

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

            Service.CommandManager.AddHandler(RescueCommandName, new CommandInfo(OnRescueCommand)
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

                var filesLength = Directory.GetFiles(BackupsFolder).Length;
                LogDebug($"{filesLength} files in the backups folder");
                if ((config.PluginVersion.Equals(PluginVersion.From(3, 0, 1)) ||
                     config.PluginVersion.Equals(PluginVersion.From(3, 0, 0)))
                    && filesLength != 0)
                {
                    Chat.PrintError("We detected you may have been impacted by configuration loss in version 3.0.1.0 of this plugin. " +
                                    "If you want to restore the last configuration saved before updating to 3.0.1.0, use this command in chat: " +
                                    RescueCommandName);
                }

                TriggerChatAlertsForEarlierVersions(config);

                Directory.CreateDirectory(BackupsFolder);
                
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

        private static string BackupFileName => $"{Path.Combine(BackupsFolder, Path.GetFileNameWithoutExtension(Service.PluginInterface.ConfigFile.Name))}.{DateTimeOffset.Now.ToUnixTimeSeconds()}.json";

        private static string BackupsFolder => Path.Combine(Service.PluginInterface.ConfigDirectory.FullName, BackupsFolderName);

        private static void CleanUpOldFiles()
        {
            var timeSpan = TimeSpan.FromDays(5);
            var regexOldFormat = new Regex(Regex.Escape(Service.PluginInterface.ConfigFile.FullName) + @"\.(\d+)\.old");


            bool FileTooOld(FileInfo f, Regex regex)
            {
                var match = regex.Match(f.FullName);
                if (!match.Success) return false;
                var value = match.Groups[1].Value;
                var date = DateTime.UnixEpoch.AddSeconds(long.Parse(value));
                return DateTime.Now - date > timeSpan;
            }
            
            
            // Deletes all files backed up the old way that are more than five days old
            foreach (var file in Directory.GetParent(Service.PluginInterface.ConfigFile.FullName)?
                                          .GetFiles()
                                          .Where( f => FileTooOld(f, regexOldFormat))
                                 ?? ArraySegment<FileInfo>.Empty)
            {
                File.Delete(file.FullName);
            }

            var previousFileVersion = PluginVersion.From(0, 0, 0);
            
            // Deletes all files backed up the new way that are more than five days old 
            // and is the first backup for a given plugin version
            foreach (var file in Directory.GetFiles(BackupsFolder)
                                          .Select(f => new FileInfo(f))
                                          .Where(f => BackupFileNameFormat.IsMatch(f.FullName))
                                          .Where(f => FileTooOld(f, BackupFileNameFormat)))
            {
                var baseConfig = JsonConvert.DeserializeObject<BaseConfiguration>(File.ReadAllText(file.FullName));
                if (baseConfig is null) continue;
                if (previousFileVersion.Before(baseConfig.PluginVersion)) continue;
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
            Service.CommandManager.RemoveHandler(RescueCommandName);
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

        private void OnRescueCommand(string command, string arguments)
        {
            var firstBackup = Directory.GetFiles(BackupsFolder)
                                       .Where(path => BackupFileNameFormat.IsMatch(path))
                                       .Order()
                                       .FirstOrDefault();
            if (firstBackup is null)
            {
                Chat.PrintError("No backups files found. If you are sure you lost your configuration in 3.0.1.0, please contact us in the Dalamud Discord, #plugin-dev-forum -> Hit it, Joe");
                return;
            }

            var rescuedConfig = JsonConvert.DeserializeObject<ConfigTwo>(File.ReadAllText(firstBackup));
            if (rescuedConfig is null)
            {
                Chat.PrintError("Error while rescuing your previous configuration. Please contact us in the Dalamud Discord, #plugin-dev-forum -> Hit it, Joe");
                return;
            }

            Configuration.Rescue(rescuedConfig);
            KamiCommon.SaveConfiguration();
            Chat.Print("Rescue", "The configuration was rescued. Sorry for the inconvenience! If you notice any issues, please contact us in the Dalamud Discord, #plugin-dev-forum -> Hit it, Joe");
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
