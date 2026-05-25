using Microsoft.EntityFrameworkCore;

namespace admin.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<SensorEntity> Sensors => Set<SensorEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e => {
            e.ToTable("USERS");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Username).HasColumnName("username");
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.PasswordHash).HasColumnName("password_hash");
            e.Property(u => u.FullName).HasColumnName("full_name");
            e.Property(u => u.CreatedAt).HasColumnName("created_at");
            e.HasOne(u => u.Device)
             .WithOne(d => d.User)
             .HasForeignKey<DeviceEntity>(d => d.UserId);
        });

        modelBuilder.Entity<DeviceEntity>(e => {
            e.ToTable("DEVICES");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id");
            e.Property(d => d.UserId).HasColumnName("user_id");
            e.Property(d => d.Name).HasColumnName("name");
            e.Property(d => d.Location).HasColumnName("location");
            e.Property(d => d.Status).HasColumnName("status");
            e.Property(d => d.LastSeen).HasColumnName("last_seen");
            e.Property(d => d.CreatedAt).HasColumnName("created_at");
            e.HasMany(d => d.Sensors)
             .WithOne(s => s.Device)
             .HasForeignKey(s => s.DeviceId);
        });

        modelBuilder.Entity<SensorEntity>(e => {
            e.ToTable("SENSORS");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.DeviceId).HasColumnName("device_id");
            e.Property(s => s.Type).HasColumnName("type");
            e.Property(s => s.Label).HasColumnName("label");
            e.Property(s => s.Unit).HasColumnName("unit");
            e.Property(s => s.MinThreshold).HasColumnName("min_threshold");
            e.Property(s => s.MaxThreshold).HasColumnName("max_threshold");
        });
    }
}