using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Tf2CriticalHitsPlugin.Configuration;
using Tf2CriticalHitsPlugin.CriticalHits.Configuration;
using Tf2CriticalHitsPlugin.SeFunctions;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Tf2CriticalHitsPlugin.CriticalHits;

public unsafe class CriticalHitsModule: IDisposable
{
    private readonly CriticalHitsConfigOne config;
    internal static PlaySound? GameSoundPlayer;
    private int myHeal;
    private int myPetHeal;
    private int otherPlayerHeal;
    private int otherPetHeal;

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
            myHeal = -1;
            myPetHeal = -1;
            otherPlayerHeal = -1;
            otherPetHeal = -1;
            if (IsPlayer(source))
            {
                myHeal = val1;
            }
            if (IsPlayerPet(source))
            {
                if (IsOwnerScholar(source))
                {
                    myPetHeal = val1;
                }
                else
                {
                    myHeal = val1;
                }
            } else if (!IsPlayer(source))
            {
                otherPlayerHeal = val1;
            } else if (IsOtherPlayerPet(source))
            {
                if (IsOwnerScholar(source))
                {
                    otherPetHeal = val1;
                }
                else
                {
                    otherPlayerHeal = val1;
                }
            }
            
        }
        
        this.addToScreenLogWithScreenLogKindHook!.Original(target, source, flyTextKind, option, actionKind, actionId, val1, val2, damageType);
    }
    
    private static bool IsPlayer(Character* source) => source->GameObject.ObjectID == Service.ClientState.LocalPlayer?.ObjectId;

    private static bool IsPlayerPet(Character* source) => source->GameObject.SubKind == (int)BattleNpcSubKind.Pet &&
                                                          source->CompanionOwnerID ==
                                                          Service.ClientState.LocalPlayer?.ObjectId;
    
    private static bool IsOtherPlayerPet(Character* source) =>
        source->GameObject.SubKind == (int)BattleNpcSubKind.Pet &&
        source->CompanionOwnerID != Service.ClientState.LocalPlayer?.ObjectId;

    private static bool IsOwnerScholar(Character* source)
    {
        var owner = source->CompanionOwnerID == Service.ClientState.LocalPlayer?.ObjectId ?
                        Service.ClientState.LocalPlayer :
                        Service.PartyList.FirstOrDefault(pm => pm.ObjectId == source->CompanionOwnerID)?.GameObject;
        return (owner as BattleChara)?.ClassJob.Id ==
               Constants.CombatJobs.FirstOrDefault(kv => kv.Value.Abbreviation == "SCH").Key;
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
            var currentClassJobId = GetCurrentClassJobId();
            if (currentText2.StartsWith("TF2TEST##"))
            {
                myHeal = -1;
                myPetHeal = 1;
                otherPlayerHeal = 1;
                otherPetHeal = 1;
                var testFlyText = currentText2.Split("##");
                currentClassJobId = byte.Parse(testFlyText[1]);
                switch (Enum.Parse<ModuleType>(testFlyText[2]))
                {
                    case ModuleType.OwnCriticalHeal:
                        myHeal = val1;
                        break;
                    case ModuleType.OwnFairyCriticalHeal:
                        myPetHeal = val1;
                        break;
                    case ModuleType.OtherCriticalHeal:
                        otherPlayerHeal = val1;
                        break;
                    case ModuleType.OtherFairyCriticalHeal:
                        otherPetHeal = val1;
                        break;
                }
            }
            if (currentClassJobId is null) return;

            foreach (var module in CriticalHitsConfigOne.GetModules(config.JobConfigurations[currentClassJobId.Value]))
            {
                if (ShouldTriggerInCurrentMode(module) &&
                    (IsAutoAttack(module, kind) ||
                     IsEnabledAction(module, kind, text1, val1, currentClassJobId)))
                {
                    if (module.ShowText)
                    {
                        text2 = GenerateText(module);
                    }

                    if (!module.SoundForActionsOnly ||
                        module.GetModuleDefaults().FlyTextType.Action.Contains(kind))
                    {
                        if (module.UseCustomFile)
                        {
                            SoundEngine.PlaySound(module.FilePath.Value, module.ApplySfxVolume, module.Volume.Value);
                        }
                        else
                        {
                            GameSoundPlayer?.Play(module.GameSound.Value);
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
            return config.ModuleType.Value switch
            {
                ModuleType.OwnCriticalHeal => myHeal == val,
                ModuleType.OwnFairyCriticalHeal => myPetHeal == val,
                ModuleType.OtherCriticalHeal => otherPlayerHeal == val,
                ModuleType.OtherFairyCriticalHeal => otherPetHeal == val,
                _ => true
            };

            // If it's any other configuration section, it's enabled.
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
            var text = GetTestText();
            Service.FlyTextGui.AddFlyText(kind, 1, 3333, 0, new SeStringBuilder().AddText(text).Build(),
                                          new SeStringBuilder().AddText($"TF2TEST##{config.ClassJobId}##{config.ModuleType}").Build(),
                                          config.GetModuleDefaults().FlyTextColor, 0, 60012);
        }

        private static string GetTestText()
        {
            return Constants.TestFlavorText[(int)Math.Floor(Random.Shared.NextSingle() * Constants.TestFlavorText.Length)];
        }

        private static SeString GenerateText(CriticalHitsConfigOne.ConfigModule config)
        {
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
