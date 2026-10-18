using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.General;

[Table("energy_character")]
[Index(nameof(UserId), nameof(Name), IsUnique = true)]
public class EnergyCharacter
{
    [Key] [Column("id")] public long Id { get; init; }

    [Column("user_id")] public ulong UserId { get; init; }

    [Column("name")] [MaxLength(100)] public required string Name { get; set; }

    [Column("item_level")] public int? ItemLevel { get; set; }

    [Column("is_main")] public bool IsMain { get; set; }

    [Column("energy")] public int Energy { get; set; }

    [Column("extra_energy")] public int ExtraEnergy { get; set; }

    [Column("nightmare_tickets")] public int NightmareTickets { get; set; }

    // Recharges are counted from these timestamps, so overwriting a value resets its clock
    [Column("energy_updated_at_utc")] public DateTime EnergyUpdatedAtUtc { get; set; }

    [Column("tickets_updated_at_utc")] public DateTime TicketsUpdatedAtUtc { get; set; }

    [Column("energy_alert_sent")] public bool EnergyAlertSent { get; set; }

    [Column("ticket_alert_sent")] public bool TicketAlertSent { get; set; }
}
