using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using KamiLib.Configuration;
using KamiLib.Drawing;
using KamiLib.Interfaces;
using KamiLib.ZoneFilterList;
using Tf2CriticalHitsPlugin.Common.Windows;
using Tf2CriticalHitsPlugin.Countdown.Configuration;

namespace Tf2CriticalHitsPlugin.Countdown.Windows;

public class CountdownOption : ISelectable, IDrawable
{
    private readonly CountdownConfigZero configZero;
    internal readonly CountdownConfigZeroModule Module;
    private readonly FileDialogManager dialogManager;

    public CountdownOption(CountdownConfigZero configZero, CountdownConfigZeroModule module, FileDialogManager dialogManager)
    {
        this.configZero = configZero;
        this.Module = module;
        this.dialogManager = dialogManager;
        this.anywhereOrSelect = new Setting<Option>(Module.AllTerritories ? Option.Anywhere : Option.SelectTerritories);
    }

    public IDrawable Contents => this;

    public void DrawLabel()
    {
        using var color = ImRaii.PushColor(ImGuiCol.Text, Module.Enabled ? Colors.Green : Colors.Red);
        ImGui.Text(Module.Label.Value);
    }

    public string ID => Module.Id.Value;

    private readonly Setting<Option> anywhereOrSelect;

    public void Draw()
    {
        if (configZero.modules.Contains(Module))
        {
            DrawDetailPane();
        }
    }

    private void DrawDetailPane()
    {
        new SimpleDrawList()
            .AddConfigCheckbox("Enabled", Module.Enabled)
            .AddInputString("Name", Module.Label, 40)
            .AddString("What to play")
            .AddIndent(2)
            .AddString("When countdown starts, play:")
            .AddIndent(2)
            .AddSoundFileConfiguration(Module.Id.Value + "start", Module.FilePath, Module.Volume, Module.ApplySfxVolume,
                                       dialogManager, showPlayButton: true)
            .AddIndent(-2)
            .AddString("If the countdown is cancelled by anyone, play:")
            .AddIndent(2)
            .AddSoundFileConfiguration(Module.Id.Value + "cancel", Module.InterruptedFilePath, Module.InterruptedVolume,
                                       Module.InterruptedApplySfxVolume, dialogManager, showPlayButton: true)
            .AddIndent(-2)
            .AddIndent(-2)
            .AddString("When to play")
            .AddIndent(2)
            .AddString("Enable for countdowns between")
            .SameLine()
            .AddInputInt($"##{Module.Id}MinCD", Module.MinimumCountdownTimer, 5, Module.MaximumCountdownTimer.Value, width: GetSecondsInputWidth())
            .SameLine()
            .AddString("and")
            .SameLine()
            .AddInputInt($"##{Module.Id}MaxCD", Module.MaximumCountdownTimer, Module.MinimumCountdownTimer.Value, 30, width: GetSecondsInputWidth())
            .SameLine()
            .AddString("seconds long")
            .AddConfigCheckbox("Play when the countdown hits a specific mark (and not when it starts)", Module.DelayPlay)
            .StartConditional(Module.DelayPlay)
            .AddIndent(2)
            .AddString("Start playing when it hits")
            .SameLine()
            .AddInputInt($"seconds##{Module.Id}PlayWhen", Module.DelayUntilCountdownHits, 1, Module.MaximumCountdownTimer.Value, width: GetSecondsInputWidth())
            .AddConfigCheckbox("Play with other Jams", Module.PlayWithOtherSounds, "If enabled, this Jam will be played for any applicable countdowns,\nno matter if another Countdown Jam is also valid for it.\n\nWarning: this Jam's cancel sound will only be played\nif it's the only or first valid Jam for the countdown.")
            .AddIndent(-2)
            .EndConditional()
            .AddConfigCheckbox($"Stop when the countdown completes", Module.StopWhenCountdownCompletes, "Disabled: this Jam will keep playing after the countdown hits \"Start!\"." +
                                   "\n(It'll be interrupted only if the countdown itself is interrupted)" +
                                   "\n\nEnabled: this Jam will be interrupted when the countdown hits \"Start!\".", Module.Id.Value)
            .AddIndent(-2)
            .AddString("Where to play")
            .AddIndent(2)
            .AddConfigRadio("Anywhere", anywhereOrSelect, Option.Anywhere)
            .AddConfigRadio("Select territories", anywhereOrSelect, Option.SelectTerritories)
            .SameLine()
            .AddHelpMarker("To know the name of a Trial/Raid arena, check its description in the Duty Finder.")
            .StartConditional(!Module.AllTerritories)
            .AddAction(() => ZoneFilterListDraw.DrawFilterTypeRadio(Module.TerritoryFilterType))
            .StartConditional(Service.ClientState.TerritoryType != 0)
            .AddAction(() => ZoneFilterListDraw.DrawAddRemoveHere(Module.TerritoryList))
            .EndConditional()
            .AddAction(() => ZoneFilterListDraw.DrawTerritorySearch(
                           Module.TerritoryList, ZoneFilterType.FromId(Module.TerritoryFilterType.Value)!))
            .AddAction(() => ZoneFilterListDraw.DrawZoneList(
                           Module.TerritoryList, ZoneFilterType.FromId(Module.TerritoryFilterType.Value)!))
            .EndConditional()
            .AddIndent(-2)
            .BeginDisabled(!ImGui.GetIO().KeyShift)
            .AddButton("Delete configuration", () => configZero.modules.Remove(Module))
            .EndDisabled()
            .SameLine()
            .AddHelpMarker("Hold \"Shift\" to enable this button.")
            .Draw();

        Module.AllTerritories.Value = anywhereOrSelect.Value == Option.Anywhere;
    }

    private static float GetSecondsInputWidth()
    {
        return 5F * ImGui.GetFontSize();
    }
}

public enum Option
{
    Anywhere = 0,
    SelectTerritories = 1
}
