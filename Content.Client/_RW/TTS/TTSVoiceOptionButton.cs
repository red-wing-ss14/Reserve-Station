// SPDX-FileCopyrightText: 2025 Amour
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RW.TTS;

public sealed class TTSVoiceOptionButton : OptionButton
{
    private static readonly Color MaleVoiceColor = Color.FromHex("#9BD1E5");
    private static readonly Color FemaleVoiceColor = Color.FromHex("#FFB6C1");
    private static readonly Color UnsexedVoiceColor = Color.FromHex("#808080");

    private readonly Dictionary<int, Sex> _voiceSexMap = new();
    private Sex? _pendingSex;

    public void AddVoiceItem(string label, Sex sex, int? id = null)
    {
        var actualId = id ?? ItemCount;

        _voiceSexMap[actualId] = sex;
        _pendingSex = sex;
        AddItem(label, actualId);
        _pendingSex = null;
    }

    public override void ButtonOverride(Button button)
    {
        base.ButtonOverride(button);

        if (_pendingSex.HasValue)
        {
            var color = _pendingSex.Value switch
            {
                Sex.Male => MaleVoiceColor,
                Sex.Female => FemaleVoiceColor,
                _ => UnsexedVoiceColor
            };
            button.Modulate = color;
        }
    }

    public new void Clear()
    {
        base.Clear();
        _voiceSexMap.Clear();
    }
}
