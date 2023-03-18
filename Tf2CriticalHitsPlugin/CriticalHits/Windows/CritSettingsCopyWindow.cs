﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using KamiLib;
using Lumina.Excel.GeneratedSheets;
using Tf2CriticalHitsPlugin.Configuration;
using Tf2CriticalHitsPlugin.CriticalHits.Configuration;

namespace Tf2CriticalHitsPlugin.CriticalHits.Windows;

public class CritSettingsCopyWindow : Dalamud.Interface.Windowing.Window
{
    private readonly ConfigOne configOne;

    private static readonly ClassJob[] Jobs = Constants.CombatJobs.Values
                                                .OrderBy(j => j.Role)
                                                .ThenBy(j => j.NameEnglish.ToString()).ToArray();

    private const string Title = "TF2-ish Critical Hits — Settings Copy";

    private int sourceJobIdx;
    private readonly ISet<int> destJobs = new HashSet<int>();

    public CritSettingsCopyWindow(
        ConfigOne configOne, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(
        Title, flags, forceMainWindow)
    {
        this.configOne = configOne;
        Size = new Vector2(300, 670);
    }

    public void Open()
    {
        sourceJobIdx = 0;
        destJobs.Clear();
        IsOpen = true;
    }

    public override void Draw()
    {
        DrawSourceCombo();
        DrawDestCheckboxes();
        DrawSelectAll();
        ImGui.SameLine();
        DrawSelectNone();
        DrawCopyButton();
    }

    private void DrawSourceCombo()
    {
        ImGui.Text("Copy from:");
        ImGui.Indent();
        ImGui.Combo("", ref sourceJobIdx, Jobs.Select(j => j.NameEnglish.ToString()).ToArray(), Jobs.Length);
        ImGui.Unindent();
    }

    private void DrawDestCheckboxes()
    {
        ImGui.Text("Copy to:");
        ImGui.Indent();
        for (var idx = 0; idx < Jobs.Length; idx++)
        {
            var job = Jobs[idx];
            var selected = destJobs.Contains(idx);
            if (idx == sourceJobIdx) continue;
            if (ImGui.Checkbox(job.NameEnglish.ToString(), ref selected))
            {
                if (selected)
                {
                    destJobs.Add(idx);
                }
                else
                {
                    destJobs.Remove(idx);
                }
            }
        }

        ImGui.Unindent();
    }

    private void DrawSelectAll()
    {
        if (ImGui.Button("Select all"))
        {
            for (var idx = 0; idx < Jobs.Length; idx++)
            {
                if (idx == sourceJobIdx) continue;
                destJobs.Add(idx);
            }
        }
    }

    private void DrawSelectNone()
    {
        if (ImGui.Button("Select none"))
        {
            for (var idx = 0; idx < Jobs.Length; idx++)
            {
                destJobs.Clear();
            }
        }
    }

    private void DrawCopyButton()
    {
        if (ImGui.Button("Copy"))
        {
            var sourceJob = Jobs[sourceJobIdx];
            var sourceJobSettings = configOne.JobConfigurations[sourceJob.RowId];
            foreach (var destJobIdx in destJobs)
            {
                var destJob = Jobs[destJobIdx];
                configOne.JobConfigurations[destJob.RowId].CopySettingsFrom(sourceJobSettings);
            }
            KamiCommon.SaveConfiguration();
            this.IsOpen = false;
        }
    }
}
