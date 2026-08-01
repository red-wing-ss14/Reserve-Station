using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("amour_client_cache")]
[Index(nameof(ClientId), IsUnique = true)]
public sealed class AmourClientRecord
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid ClientId { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public string RecordedBy { get; set; } = string.Empty;

    public string? Note { get; set; }
}

[Table("amour_boosters")]
public sealed class RwBooster
{
    [Key]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public int? OocColor { get; set; }

    public bool IsActive { get; set; } = false;

    public string? TierName { get; set; }

    public int TierLevel { get; set; } = 0;
}
