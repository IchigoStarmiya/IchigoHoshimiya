using IchigoHoshimiya.Entities.Wwm;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Context;

public class WwmDbContext : DbContext
{
    public WwmDbContext()
    {
    }

    public WwmDbContext(DbContextOptions<WwmDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<WwmPlayer> Players { get; set; }

    public virtual DbSet<WwmPlayerSnapshot> Snapshots { get; set; }

    public virtual DbSet<WwmGear> Gear { get; set; }

    public virtual DbSet<WwmGearRetone> GearRetones { get; set; }

    public virtual DbSet<WwmGearBaseAttr> GearBaseAttrs { get; set; }

    public virtual DbSet<WwmGearAffix> GearAffixes { get; set; }

    public virtual DbSet<WwmSnapshotXinfa> SnapshotXinfa { get; set; }

    public virtual DbSet<WwmSnapshotSkill> SnapshotSkills { get; set; }

    public virtual DbSet<WwmTrackedPlayer> TrackedPlayers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WwmPlayer>(entity =>
        {
            entity.HasKey(e => e.NumberId);

            entity.Property(e => e.NumberId)
                  .ValueGeneratedNever();

            entity.HasMany(e => e.Snapshots)
                  .WithOne(s => s.Player)
                  .HasForeignKey(s => s.NumberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WwmPlayerSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LookupType)
                  .HasConversion<int>();

            // Microsecond precision keeps the (number_id, fetched_at_utc) key usable for rapid re-lookups.
            entity.Property(e => e.FetchedAtUtc)
                  .HasColumnType("datetime(6)");

            entity.Property(e => e.GameplayTrailJson).HasColumnType("TEXT");
            entity.Property(e => e.CurrentScenarioJson).HasColumnType("TEXT");
            entity.Property(e => e.RawBaseJson).HasColumnType("TEXT");

            entity.HasMany(e => e.Gear)
                  .WithOne(g => g.Snapshot)
                  .HasForeignKey(g => g.SnapshotId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Xinfa)
                  .WithOne(x => x.Snapshot)
                  .HasForeignKey(x => x.SnapshotId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Skills)
                  .WithOne(s => s.Snapshot)
                  .HasForeignKey(s => s.SnapshotId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WwmTrackedPlayer>(entity =>
        {
            entity.HasKey(e => e.NumberId);

            entity.Property(e => e.NumberId)
                  .ValueGeneratedNever();
        });

        modelBuilder.Entity<WwmGear>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasMany(e => e.RetoneHistory)
                  .WithOne(r => r.Gear)
                  .HasForeignKey(r => r.GearId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.BaseAttrs)
                  .WithOne(a => a.Gear)
                  .HasForeignKey(a => a.GearId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Affixes)
                  .WithOne(a => a.Gear)
                  .HasForeignKey(a => a.GearId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
