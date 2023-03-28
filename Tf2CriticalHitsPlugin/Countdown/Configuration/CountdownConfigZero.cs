﻿using System;
using System.Collections.Generic;
using Tf2CriticalHitsPlugin.Common.Configuration;

namespace Tf2CriticalHitsPlugin.Countdown.Configuration;

public class CountdownConfigZero
{

    public int Version { get; set; }
    public IList<CountdownConfigZeroModule> modules { get; init; } = new List<CountdownConfigZeroModule>();
    
    public CountdownConfigZero()
    {
        Version = 0;
    }

    public void Rescue(CountdownConfigZero fileCountdownJams)
    {
        modules.Clear();
        foreach (var module in fileCountdownJams.modules)
        {
            modules.Add(module);
        }
    }
}
