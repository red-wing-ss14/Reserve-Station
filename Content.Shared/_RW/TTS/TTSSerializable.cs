using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._RW.TTS;

public enum VoiceRequestType
{
    None,
    Preview
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestGlobalTTSEvent(VoiceRequestType text, string voiceId) : EntityEventArgs
{
    public VoiceRequestType Text { get;} = text;
    public string VoiceId { get; } = voiceId;
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestPreviewTTSEvent(string voiceId) : EntityEventArgs
{
    public string VoiceId { get; } = voiceId;
}

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class AnnounceTTSEvent(byte[] data, string announcementSound, AudioParams announcementParams) : EntityEventArgs
{
    public byte[] Data { get; } = data;
    public string AnnouncementSound { get; } = announcementSound;
    public AudioParams AnnouncementParams{ get; } = announcementParams;
}
