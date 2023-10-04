using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.FlyText;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Tf2CriticalHitsPlugin;

public class Service
{
    [PluginService]
    public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    public static IPluginLog PluginLog { get; private set; } = null!;
    
    [PluginService]
    public static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    public static IFlyTextGui FlyTextGui { get; private set; } = null!;

    [PluginService]
    public static IDataManager? DataManager { get; private set; } = null;

    [PluginService]
    public static ISigScanner SigScanner { get; private set; } = null!;

    [PluginService]
    public static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    public static ICondition Condition { get; private set; } = null!;

    [PluginService]
    public static IFramework Framework { get; private set; } = null!;

    [PluginService]
    public static IPartyList PartyList { get; private set; } = null!;
    
    [PluginService]
    public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

}
