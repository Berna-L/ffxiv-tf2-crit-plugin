using Dalamud.Game;
using Tf2CriticalHitsPlugin.Game.SFX;

namespace Tf2CriticalHitsPlugin.SeFunctions
{
    public delegate ulong PlaySoundDelegate(Sounds id, ulong unk1, ulong unk2);

    public sealed class PlaySound : SeFunctionBase<PlaySoundDelegate>
    {
        public PlaySound(ISigScanner sigScanner)
            : base(sigScanner, "E8 ?? ?? ?? ?? 4D 39 BE")
        { }

        public void Play(Sounds id)
            => Invoke(id, 0ul, 0ul);
    }
}
