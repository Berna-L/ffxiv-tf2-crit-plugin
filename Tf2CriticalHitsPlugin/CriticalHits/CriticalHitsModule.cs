using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Tf2CriticalHitsPlugin.Configuration;
using Tf2CriticalHitsPlugin.CriticalHits.Configuration;
using Tf2CriticalHitsPlugin.SeFunctions;
using static Dalamud.Logging.PluginLog;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Tf2CriticalHitsPlugin.CriticalHits;

public unsafe class CriticalHitsModule: IDisposable
{
    private readonly CriticalHitsConfigOne config;
    internal static PlaySound? GameSoundPlayer;
    private int petHeal;
    private int otherPlayerHeal;

    private delegate void AddToScreenLogWithScreenLogKindDelegate(
        Character* target,
        Character* source,
        FlyTextKind logKind,
        byte option,
        byte actionKind,
        int actionId,
        int val1,
        int val2,
        byte damageType);
    private readonly Hook<AddToScreenLogWithScreenLogKindDelegate>? addToScreenLogWithScreenLogKindHook;
    

    public CriticalHitsModule(CriticalHitsConfigOne config)
    {
        this.config = config;
        GameSoundPlayer = new PlaySound(Service.SigScanner);

        Service.FlyTextGui.FlyTextCreated += this.FlyTextCreate;

        try {
            var addToScreenLogWithScreenLogKindAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? BF ?? ?? ?? ?? EB 39");
            this.addToScreenLogWithScreenLogKindHook = Hook<AddToScreenLogWithScreenLogKindDelegate>.FromAddress(addToScreenLogWithScreenLogKindAddress, this.AddToScreenLogWithScreenLogKindDetour);
            this.addToScreenLogWithScreenLogKindHook.Enable();

        }
        catch (Exception)
        {
            // ignored
        }

    }
    

    private void AddToScreenLogWithScreenLogKindDetour(
        Character* target,
        Character* source,
        FlyTextKind flyTextKind,
        byte option, // 0 = DoT / 1 = % increase / 2 = blocked / 3 = parried / 4 = resisted / 5 = default
        byte actionKind,
        int actionId,
        int val1,
        int val2,
        byte damageType)
    {
        if (FlyTextType.ActionCriticalHeal.Contains(flyTextKind))
        {
            if (IsPlayerPet(source))
            {
                petHeal = val1;
            } else if (!IsPlayer(source))
            {
                otherPlayerHeal = val1;
            }
            
        }
        
        this.addToScreenLogWithScreenLogKindHook!.Original(target, source, flyTextKind, option, actionKind, actionId, val1, val2, damageType);
    }
    
    private bool IsPlayer(Character* source)
    {
        return source->GameObject.ObjectID == Service.ClientState.LocalPlayer?.ObjectId;
    }

    private static bool IsPlayerPet(Character* source)
    {
        LogDebug("cheguei aqui");
        return source->GameObject.SubKind == (int)BattleNpcSubKind.Pet && source->CompanionOwnerID == Service.ClientState.LocalPlayer?.ObjectId;
    }

    public void FlyTextCreate(
            ref FlyTextKind kind,
            ref int val1,
            ref int val2,
            ref SeString text1,
            ref SeString text2,
            ref uint color,
            ref uint icon,
            ref uint damageTypeIcon,
            ref float yOffset,
            ref bool handled)
        {
            var currentText2 = text2.ToString();
            var currentClassJobId = currentText2.StartsWith("TF2TEST##")
                                        ? byte.Parse(
                                            currentText2[
                                                (currentText2.LastIndexOf("#", StringComparison.Ordinal) + 1)..])
                                        : GetCurrentClassJobId();
            if (currentClassJobId is null) return;

            foreach (var config in config.JobConfigurations[currentClassJobId.Value])
            {
                if (ShouldTriggerInCurrentMode(config) &&
                    (IsAutoAttack(config, kind) ||
                     IsEnabledAction(config, kind, text1, val1, currentClassJobId)))
                {
                    LogDebug($"{config.GetId()} registered!");
                    if (config.ShowText)
                    {
                        text2 = GenerateText(config);
                    }

                    if (!config.SoundForActionsOnly ||
                        config.GetModuleDefaults().FlyTextType.Action.Contains(kind))
                    {
                        if (config.UseCustomFile)
                        {
                            SoundEngine.PlaySound(config.FilePath.Value, config.ApplySfxVolume, config.Volume.Value);
                        }
                        else
                        {
                            GameSoundPlayer?.Play(config.GameSound.Value);
                        }
                    }
                    break;
                }
            }
        }
        
                private static bool ShouldTriggerInCurrentMode(CriticalHitsConfigOne.ConfigModule config)
        {
            return !IsPvP() || config.ApplyInPvP;
        }
        
        private static bool IsAutoAttack(CriticalHitsConfigOne.ConfigModule config, FlyTextKind kind)
        {
            return config.GetModuleDefaults().FlyTextType.AutoAttack.Contains(kind);
        }

        private bool IsEnabledAction(
            CriticalHitsConfigOne.ConfigModule config, FlyTextKind kind, SeString text, int val, [DisallowNull] byte? currentClassJobId)
        {
            // If it's not a FlyText for an action, return false
            if (!config.GetModuleDefaults().FlyTextType.Action.Contains(kind)) return false;
            // If we're checking the Own Critical Heals section, check if it's an action of the current job
            if (config.ModuleType == ModuleType.OwnCriticalHeal)
            {
                if (petHeal == val)
                {
                    petHeal = -1;
                    return true;
                }
                return Constants.ActionsPerJob[currentClassJobId.Value].Contains(text.TextValue) && otherPlayerHeal != val;
            }
            // If we're checking the Other Critical Heals section, check if it's NOT an action of the current job
            if (config.ModuleType == ModuleType.OtherCriticalHeal)
            {
                LogDebug($"a = {otherPlayerHeal} | val = {val}");
                if (otherPlayerHeal == val)
                {
                    otherPlayerHeal = -1;
                    return true;
                }
                return !Constants.ActionsPerJob[currentClassJobId.Value].Contains(text.TextValue);
            }
            // If it's any other configuration section, it's enabled.
            return true;
        }

        private static unsafe byte? GetCurrentClassJobId()
        {
            var classJobId = PlayerState.Instance()->CurrentClassJobId;
            return Constants.CombatJobs.ContainsKey(classJobId) ? classJobId : null;
        }

        private static bool IsPvP()
        {
            return Service.ClientState.IsPvP;
        }
        
        public static void GenerateTestFlyText(CriticalHitsConfigOne.ConfigModule config)
        {
            var kind = config.GetModuleDefaults().FlyTextType.Action.FirstOrDefault();
            LogDebug($"Kind: {kind}, Config ID: {config.GetId()}");
            var text = GetTestText(config);
            Service.FlyTextGui.AddFlyText(kind, 1, 3333, 0, new SeStringBuilder().AddText(text).Build(),
                                          new SeStringBuilder().AddText($"TF2TEST##{config.ClassJobId}").Build(),
                                          config.GetModuleDefaults().FlyTextColor, 0, 60012);
        }

        private static string GetTestText(CriticalHitsConfigOne.ConfigModule configModule)
        {
            var array = configModule.ModuleType.Value == ModuleType.OwnCriticalHeal
                            ? Constants.ActionsPerJob[configModule.ClassJobId.Value].ToArray()
                            : Constants.TestFlavorText;
            return array[(int)Math.Floor(Random.Shared.NextSingle() * array.Length)];
        }

        private static SeString GenerateText(CriticalHitsConfigOne.ConfigModule config)
        {
            LogDebug(
                $"Generating text with color {config.TextColor} and glow {config.TextGlowColor}");
            var stringBuilder = new SeStringBuilder()
                                .AddUiForeground(config.TextColor.Value)
                                .AddUiGlow(config.TextGlowColor.Value);
            if (config.TextItalics)
            {
                stringBuilder.AddItalicsOn();
            }

            return stringBuilder
                   .AddText(config.Text.Value)
                   .AddItalicsOff()
                   .AddUiForegroundOff()
                   .AddUiGlowOff()
                   .Build();
        }



        public void Dispose()
        {
            addToScreenLogWithScreenLogKindHook?.Dispose();
            Service.FlyTextGui.FlyTextCreated -= FlyTextCreate;
        }
}
