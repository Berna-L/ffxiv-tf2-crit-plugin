using System.Collections.Generic;
using System.Collections.Immutable;
using Dalamud.Game.Gui.FlyText;

namespace Tf2CriticalHitsPlugin.Configuration;

public class FlyTextType
{
    public ISet<FlyTextKind> AutoAttack { get; }

    public ISet<FlyTextKind> Action { get; }

    public FlyTextType(ISet<FlyTextKind> autoAttack, ISet<FlyTextKind> action)
    {
        AutoAttack = autoAttack;
        Action = action;
    }

    public static readonly ISet<FlyTextKind> AutoDirectCriticalDamage =
        new[] { FlyTextKind.AutoAttackOrDotCritDh }.ToImmutableHashSet();

    public static readonly ISet<FlyTextKind> ActionDirectCriticalDamage =
        new[] { FlyTextKind.DamageCritDh }.ToImmutableHashSet();

    public static readonly ISet<FlyTextKind> AutoCriticalDamage = new[]
            { FlyTextKind.AutoAttackOrDotCrit, FlyTextKind.CriticalHit4 }
        .ToImmutableHashSet();

    public static readonly ISet<FlyTextKind> ActionCriticalDamage = new[]
            { FlyTextKind.DamageCrit, FlyTextKind.NamedCriticalHitWithMp, FlyTextKind.NamedCriticalHitWithTp }
        .ToImmutableHashSet();

    public static readonly ISet<FlyTextKind> ActionCriticalHeal =
        new[] { FlyTextKind.HealingCrit }.ToImmutableHashSet();

    public static readonly ISet<FlyTextKind> AutoDirectDamage =
        new[] { FlyTextKind.AutoAttackOrDotDh }.ToImmutableHashSet();

    public static readonly ISet<FlyTextKind> ActionDirectDamage =
        new[] { FlyTextKind.DamageDh }.ToImmutableHashSet();
}
