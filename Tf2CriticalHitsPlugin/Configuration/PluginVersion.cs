using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Logging;

namespace Tf2CriticalHitsPlugin.Configuration;

public class PluginVersion : IComparable<PluginVersion>
{
    public static readonly PluginVersion Current;

    public int Major { get; init; }
    public int Minor { get; init; }
    public int Patch { get; init; }

    public static PluginVersion From(int major, int minor, int patch)
    {
        return new PluginVersion
        {
            Major = major,
            Minor = minor,
            Patch = patch
        };
    }

    static PluginVersion()
    {
        var fullVersionText = Assembly.GetExecutingAssembly().FullName!.Split(',')[1];
        var version = fullVersionText[(fullVersionText.IndexOf('=') + 1)..].Split(".");
        Current = From(int.Parse(version[0]), int.Parse(version[1]), int.Parse(version[2]));
    }

    public int CompareTo(PluginVersion? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;
        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;
        return Patch.CompareTo(other.Patch);
    }

    public bool Before(int major, int minor, int patch)
    {
        return this.CompareTo(From(major, minor, patch)) < 0;
    }

    protected bool Equals(PluginVersion other)
    {
        return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PluginVersion)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch);
    }
}
