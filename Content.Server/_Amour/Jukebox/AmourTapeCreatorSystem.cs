using System;
using System.Linq;
using Content.Server.Popups;
using Content.Shared._Amour;
using Content.Shared._Amour.Jukebox;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server._Amour.Jukebox;

public sealed class AmourTapeCreatorSystem : EntitySystem
{
    [Dependency] private readonly ServerAmourJukeboxSongsSyncManager _songsSyncManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private const string TapeCreatorContainerName = "amour_tape_creator_container";
    private const string CoinTag = "AmourTapeRecorderCoin";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AmourJukeboxSongUploadRequest>(OnSongUploaded);
        SubscribeLocalEvent<AmourTapeCreatorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AmourTapeCreatorComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<AmourTapeCreatorComponent, GetVerbsEvent<Verb>>(OnTapeCreatorGetVerb);
        SubscribeLocalEvent<AmourTapeCreatorComponent, ComponentGetState>(OnTapeCreatorStateChanged);
        SubscribeLocalEvent<AmourTapeComponent, ComponentGetState>(OnTapeStateChanged);
    }

    private void OnTapeCreatorGetVerb(EntityUid uid, AmourTapeCreatorComponent component, GetVerbsEvent<Verb> ev)
    {
        if (component.Recording)
            return;
        if (ev.Hands == null)
            return;
        if (component.TapeContainer.ContainedEntities.Count == 0)
            return;

        var removeTapeVerb = new Verb
        {
            Text = "Вытащить кассету",
            Priority = 10000,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/_Amour/Interface/VerbIcons/remove_tape.png")),
            Act = () =>
            {
                var tapes = component.TapeContainer.ContainedEntities.ToList();
                _container.EmptyContainer(component.TapeContainer, true);

                foreach (var tape in tapes)
                {
                    _hands.PickupOrDrop(ev.User, tape);
                }

                component.InsertedTape = null;
                Dirty(uid, component);
            }
        };

        ev.Verbs.Add(removeTapeVerb);
    }

    private void OnTapeStateChanged(EntityUid uid, AmourTapeComponent component, ref ComponentGetState args)
    {
        args.State = new AmourTapeComponentState
        {
            Songs = component.Songs
        };
    }

    private void OnTapeCreatorStateChanged(EntityUid uid, AmourTapeCreatorComponent component, ref ComponentGetState args)
    {
        args.State = new AmourTapeCreatorComponentState
        {
            Recording = component.Recording,
            CoinBalance = component.CoinBalance,
            InsertedTape = component.InsertedTape
        };
    }

    private void OnComponentInit(EntityUid uid, AmourTapeCreatorComponent component, ComponentInit args)
    {
        component.TapeContainer = _container.EnsureContainer<Container>(uid, TapeCreatorContainerName);
    }

    private void OnInteract(EntityUid uid, AmourTapeCreatorComponent component, InteractUsingEvent args)
    {
        if (component.Recording)
            return;

        if (HasComp<AmourTapeComponent>(args.Used))
        {
            var containedEntities = component.TapeContainer.ContainedEntities;

            if (containedEntities.Count >= 1)
            {
                var removedTapes = _container.EmptyContainer(component.TapeContainer, true).ToList();

                foreach (var tape in removedTapes)
                {
                    _hands.PickupOrDrop(args.User, tape);
                }
            }

            _container.Insert(args.Used, component.TapeContainer);

            component.InsertedTape = GetNetEntity(args.Used);
            Dirty(uid, component);
            return;
        }
        if (_tag.HasTag(args.Used, CoinTag))
        {
            Del(args.Used);
            component.CoinBalance += 1;
            Dirty(uid, component);
        }
    }

    private void OnSongUploaded(AmourJukeboxSongUploadRequest ev, EntitySessionEventArgs args)
    {
        var tapeCreator = GetEntity(ev.TapeCreatorUid);
        if (!Exists(tapeCreator) || !TryComp<AmourTapeCreatorComponent>(tapeCreator, out var tapeCreatorComponent))
        {
            SendUploadResponse(args, ev.TapeCreatorUid, false, "Записывающее устройство не найдено.");
            return;
        }

        var session = args.SenderSession;
        if (session.AttachedEntity is not { } sender || !Exists(sender))
        {
            SendUploadResponse(args, ev.TapeCreatorUid, false, "Нет привязанной сущности.");
            return;
        }

        if (!_uiSystem.IsUiOpen(tapeCreator, AmourTapeCreatorUIKey.Key, sender))
        {
            SendUploadResponse(args, ev.TapeCreatorUid, false, "Интерфейс не открыт.");
            return;
        }

        if (!_interaction.InRangeUnobstructed(sender, tapeCreator))
        {
            SendUploadResponse(args, ev.TapeCreatorUid, false, "Слишком далеко.");
            return;
        }

        var maxMb = _cfg.GetCVar(AmourCVars.MaxJukeboxSongSizeInMb);
        var maxBytes = (int) (maxMb * 1024 * 1024);
        if (ev.SongBytes.Count > maxBytes || ev.SongBytes.Count > AmourJukeboxSongUploadNetMessage.MaxDataLength)
        {
            SendUploadResponse(args, ev.TapeCreatorUid, false, $"Файл слишком большой. Лимит: {maxMb} МБ.");
            return;
        }

        if (!tapeCreatorComponent.InsertedTape.HasValue || tapeCreatorComponent.CoinBalance <= 0)
        {
            _popup.PopupEntity("Запись была прервана: нет кассеты или жетонов.", tapeCreator);
            SendUploadResponse(args, ev.TapeCreatorUid, false, "Нет кассеты или жетонов.");
            return;
        }

        var insertedTape = GetEntity(tapeCreatorComponent.InsertedTape.Value);
        if (!TryComp<AmourTapeComponent>(insertedTape, out var tapeComponent))
        {
            SendUploadResponse(args, ev.TapeCreatorUid, false, "Кассета недоступна.");
            return;
        }

        if (tapeCreatorComponent.Recording)
        {
            SendUploadResponse(args, ev.TapeCreatorUid, false, "Уже идёт запись.");
            return;
        }

        tapeCreatorComponent.Recording = true;
        Dirty(tapeCreator, tapeCreatorComponent);

        try
        {
            (string SongName, ResPath Path) songData;
            try
            {
                songData = _songsSyncManager.SyncSongData(ev.SongName, ev.SongBytes);
            }
            catch (Exception e)
            {
                Log.Error($"AmourTapeCreator: SyncSongData failed: {e}");
                SendUploadResponse(args, ev.TapeCreatorUid, false, "Не удалось сохранить песню.");
                return;
            }

            var durationSeconds = 0f;
            try
            {
                durationSeconds = (float) _audio.GetAudioLength(songData.Path.ToString()).TotalSeconds;
            }
            catch (Exception e)
            {
                Log.Warning($"AmourTapeCreator: failed to resolve audio length for {songData.Path}: {e}");
            }

            tapeCreatorComponent.CoinBalance -= 1;

            var song = new AmourJukeboxSong
            {
                SongName = songData.SongName,
                SongPath = songData.Path,
                SongDurationSeconds = durationSeconds
            };

            tapeComponent.Songs.Add(song);

            DirtyEntity(tapeCreator);
            Dirty(insertedTape, tapeComponent);

            FinishRecording(tapeCreator, tapeCreatorComponent);
            SendUploadResponse(args, ev.TapeCreatorUid, true, "Запись на кассету завершена.");
        }
        finally
        {
            if (tapeCreatorComponent.Recording)
            {
                tapeCreatorComponent.Recording = false;
                Dirty(tapeCreator, tapeCreatorComponent);
            }
        }
    }

    private void SendUploadResponse(EntitySessionEventArgs args, NetEntity tapeCreatorUid, bool success, string? message)
    {
        RaiseNetworkEvent(new AmourJukeboxSongUploadResponse
        {
            TapeCreatorUid = tapeCreatorUid,
            Success = success,
            Message = message
        }, args.SenderSession);
    }

    private void FinishRecording(EntityUid uid, AmourTapeCreatorComponent component)
    {
        _container.EmptyContainer(component.TapeContainer, force: true);

        component.Recording = false;
        component.InsertedTape = null;

        _popup.PopupEntity("Запись на кассету завершена.", uid);
        Dirty(uid, component);
    }
}
