//SPDX-FileCopyrightText: 2025 MirageEexe <Mirageeexe@gmail.com>
//SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.MartialArts.Events;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ArbArmLockPerformedEvent : EntityEventArgs;


[Serializable, NetSerializable, DataDefinition]
public sealed partial class ArbSweepPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ArbChokePerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ArbElbowStrikePerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ArbKneeStrikePerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ArbHipThrowPerformedEvent : EntityEventArgs;
