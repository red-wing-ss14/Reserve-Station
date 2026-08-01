// SPDX-FileCopyrightText: 2025 Amour
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._RW.TTS;
using Content.Shared._RW.TTS;
using Content.Shared.Humanoid;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<TTSVoicePrototype> _ttsPrototypes = new();
    private Button? _ttsVoiceButton;
    private TTSVoiceMenu? _ttsVoiceMenu;
    private TTSVoicePrototype? _selectedTTSVoice;

    private void InitializeTTSVoice()
    {
        var parent = TTSVoiceButton.Parent;
        if (parent == null)
            return;

        var index = -1;
        for (var i = 0; i < parent.ChildCount; i++)
        {
            if (parent.GetChild(i) == TTSVoiceButton)
            {
                index = i;
                break;
            }
        }

        _ttsVoiceButton = new Button
        {
            HorizontalAlignment = TTSVoiceButton.HorizontalAlignment,
            HorizontalExpand = true,
            Text = Loc.GetString("humanoid-profile-editor-tts-voice-button-select")
        };

        if (index >= 0)
        {
            parent.RemoveChild(TTSVoiceButton);
            parent.AddChild(_ttsVoiceButton);
            _ttsVoiceButton.SetPositionInParent(index);
        }

        _ttsVoiceButton.OnPressed += _ => OpenTTSVoiceMenu();
        TTSVoicePlayButton.OnPressed += _ => PlayPreviewTTS();
    }

    private void OpenTTSVoiceMenu()
    {
        if (_ttsVoiceMenu is { IsOpen: true })
        {
            _ttsVoiceMenu.Close();
            return;
        }

        _ttsVoiceMenu?.Dispose();
        _ttsVoiceMenu = new TTSVoiceMenu();

        _ttsVoiceMenu.OnVoiceSelected += voice =>
        {
            _selectedTTSVoice = voice;
            SetTTSVoice(voice);
            UpdateTTSVoiceButtonText();
        };

        _ttsVoiceMenu.OnVoicePreview += voice =>
        {
            var ttsSystem = _entManager.System<TTSSystem>();
            ttsSystem.RequestGlobalTTS(VoiceRequestType.Preview, voice.ID);
        };

        if (_selectedTTSVoice != null)
        {
            _ttsVoiceMenu.SetSelectedVoice(_selectedTTSVoice.ID);
        }

        _ttsVoiceMenu.OpenCentered();
    }

    private void UpdateTTSVoice()
    {
        if (Profile is null || _ttsVoiceButton is null)
            return;

        _ttsPrototypes = _prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(o => o.RoundStart)
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();

        _selectedTTSVoice = _ttsPrototypes.FirstOrDefault(v => v.ID == Profile.Voice);

        if (_selectedTTSVoice == null && _ttsPrototypes.Count > 0)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            _selectedTTSVoice = random.Pick(_ttsPrototypes);
            SetTTSVoice(_selectedTTSVoice);
        }
        else if (_selectedTTSVoice != null)
        {
            SetTTSVoice(_selectedTTSVoice);
        }

        UpdateTTSVoiceButtonText();
    }

    private void UpdateTTSVoiceButtonText()
    {
        if (_ttsVoiceButton == null)
            return;

        if (_selectedTTSVoice != null)
        {
            var sexIcon = _selectedTTSVoice.Sex switch
            {
                Sex.Male => "♂",
                Sex.Female => "♀",
                _ => "⚥"
            };
            _ttsVoiceButton.Text = $"{sexIcon} {Loc.GetString(_selectedTTSVoice.Name)}";
        }
        else
        {
            _ttsVoiceButton.Text = Loc.GetString("humanoid-profile-editor-tts-voice-button-select");
        }
    }

    private void PlayPreviewTTS()
    {
        if (Profile is null || _selectedTTSVoice is null)
            return;

        var ttsSystem = _entManager.System<TTSSystem>();
        ttsSystem.RequestGlobalTTS(VoiceRequestType.Preview, _selectedTTSVoice.ID);
    }
}
