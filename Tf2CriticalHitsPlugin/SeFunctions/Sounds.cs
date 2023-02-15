using System;
using System.Collections.Generic;
using System.Linq;

namespace Tf2CriticalHitsPlugin.SeFunctions
{
    public enum Sounds : byte
    {
        None    = 0x00,
        Unknown = 0x01,
        Sound01 = 0x25,
        Sound02 = 0x26,
        Sound03 = 0x27,
        Sound04 = 0x28,
        Sound05 = 0x29,
        Sound06 = 0x2A,
        Sound07 = 0x2B,
        Sound08 = 0x2C,
        Sound09 = 0x2D,
        Sound10 = 0x2E,
        Sound11 = 0x2F,
        Sound12 = 0x30,
        Sound13 = 0x31,
        Sound14 = 0x32,
        Sound15 = 0x33,
        Sound16 = 0x34,
    }

    public static class SoundsExtensions
    {
        public static string ToName(this Sounds value)
        {
            return value switch
            {
                Sounds.None    => "None",
                Sounds.Sound01 => "Sound Effect 1",
                Sounds.Sound02 => "Sound Effect 2",
                Sounds.Sound03 => "Sound Effect 3",
                Sounds.Sound04 => "Sound Effect 4",
                Sounds.Sound05 => "Sound Effect 5",
                Sounds.Sound06 => "Sound Effect 6",
                Sounds.Sound07 => "Sound Effect 7",
                Sounds.Sound08 => "Sound Effect 8",
                Sounds.Sound09 => "Sound Effect 9",
                Sounds.Sound10 => "Sound Effect 10",
                Sounds.Sound11 => "Sound Effect 11",
                Sounds.Sound12 => "Sound Effect 12",
                Sounds.Sound13 => "Sound Effect 13",
                Sounds.Sound14 => "Sound Effect 14",
                Sounds.Sound15 => "Sound Effect 15",
                Sounds.Sound16 => "Sound Effect 16",
                _              => "Unknown",
            };
        }

        public static int ToIdx(this Sounds value)
        {
            return value switch
            {
                Sounds.None    => 0,
                Sounds.Sound01 => 1,
                Sounds.Sound02 => 2,
                Sounds.Sound03 => 3,
                Sounds.Sound04 => 4,
                Sounds.Sound05 => 5,
                Sounds.Sound06 => 6,
                Sounds.Sound07 => 7,
                Sounds.Sound08 => 8,
                Sounds.Sound09 => 9,
                Sounds.Sound10 => 10,
                Sounds.Sound11 => 11,
                Sounds.Sound12 => 12,
                Sounds.Sound13 => 13,
                Sounds.Sound14 => 14,
                Sounds.Sound15 => 15,
                Sounds.Sound16 => 16,
                _              => -1,
            };
        }

        public static Sounds FromIdx(int idx)
        {
            return idx switch
            {
                0  => Sounds.None,
                1  => Sounds.Sound01,
                2  => Sounds.Sound02,
                3  => Sounds.Sound03,
                4  => Sounds.Sound04,
                5  => Sounds.Sound05,
                6  => Sounds.Sound06,
                7  => Sounds.Sound07,
                8  => Sounds.Sound08,
                9  => Sounds.Sound09,
                10 => Sounds.Sound10,
                11 => Sounds.Sound11,
                12 => Sounds.Sound12,
                13 => Sounds.Sound13,
                14 => Sounds.Sound14,
                15 => Sounds.Sound15,
                16 => Sounds.Sound16,
                _  => Sounds.Unknown,
            };
        }

        public static IEnumerable<Sounds> Values()
        {
            return Enum.GetValues<Sounds>().Where(s => s != Sounds.Unknown);
        }
    }
}
