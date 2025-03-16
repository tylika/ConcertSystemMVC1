using Microsoft.EntityFrameworkCore;
using ConcertSystemDomain.Model; // Імпорт моделей

namespace ConcertSystemInfrastructure
{
    public partial class ConcertTicketSystemContext : DbContext
    {
        public ConcertTicketSystemContext()
        {
        }

        public ConcertTicketSystemContext(DbContextOptions<ConcertTicketSystemContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Artist> Artists { get; set; }
        public virtual DbSet<Concert> Concerts { get; set; }
        public virtual DbSet<Genre> Genres { get; set; }
        public virtual DbSet<Purchase> Purchases { get; set; }
        public virtual DbSet<PurchaseItem> PurchaseItems { get; set; }
        public virtual DbSet<Spectator> Spectators { get; set; }
        public virtual DbSet<Ticket> Tickets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#pragma warning disable CS1030 // #warning directive
            => optionsBuilder.UseSqlServer("Server=DESKTOP-D2I193F\\SQLEXPRESS; Database=ConcertTicketSystem; Trusted_Connection=True; TrustServerCertificate=True;");
#pragma warning restore CS1030

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Artist>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Artists__3214EC07DD45FB32");
                entity.Property(e => e.FullName).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.SocialMedia).HasMaxLength(255).IsUnicode(false);
            });

            modelBuilder.Entity<Concert>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Concerts__3214EC07C4BC870B");
                entity.HasIndex(e => e.ArtistId, "IDX_Concerts_ArtistId");
                entity.Property(e => e.ConcertDate).HasColumnType("datetime");
                entity.Property(e => e.Location).HasMaxLength(200).IsUnicode(false);

                entity.HasOne(d => d.Artist).WithMany(p => p.Concerts)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Concerts__Artist__3D5E1FD2");

                entity.HasMany(c => c.Tickets)
                    .WithOne(t => t.Concert)
                    .HasForeignKey(t => t.ConcertId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__Tickets__Concert__4316F928");

                entity.HasMany(d => d.Genres).WithMany(p => p.Concerts)
                    .UsingEntity<Dictionary<string, object>>(
                        "ConcertGenre",
                        r => r.HasOne<Genre>().WithMany()
                            .HasForeignKey("GenreId")
                            .OnDelete(DeleteBehavior.Cascade)
                            .HasConstraintName("FK__ConcertGe__Genre__5535A963"),
                        l => l.HasOne<Concert>().WithMany()
                            .HasForeignKey("ConcertId")
                            .OnDelete(DeleteBehavior.Cascade)
                            .HasConstraintName("FK__ConcertGe__Conce__5441852A"),
                        j =>
                        {
                            j.HasKey("ConcertId", "GenreId").HasName("PK__ConcertG__F6D567FB08B181CC");
                            j.ToTable("ConcertGenres");
                        });
            });

            modelBuilder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Genres__3214EC07A29F00AA");
                entity.Property(e => e.Name).HasMaxLength(100).IsUnicode(false);
            });

            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Purchase__3214EC07F4E859D5");
                entity.HasIndex(e => e.SpectatorId, "IDX_Purchases_SpectatorId");
                entity.Property(e => e.PurchaseDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
                entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Completed");

                entity.HasOne(d => d.Spectator).WithMany(p => p.Purchases)
                    .HasForeignKey(d => d.SpectatorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Purchases__Spect__4AB81AF0");
            });

            modelBuilder.Entity<PurchaseItem>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Purchase__3214EC079E3880FE");
                entity.HasIndex(e => e.PurchaseId, "IDX_PurchaseItems_PurchaseId");
                entity.HasIndex(e => e.TicketId, "IDX_PurchaseItems_TicketId");
                entity.HasIndex(e => new { e.PurchaseId, e.TicketId }, "UC_PurchaseItem").IsUnique();
                entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.Quantity).HasDefaultValue(1);

                entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseItems)
                    .HasForeignKey(d => d.PurchaseId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__PurchaseI__Purch__5070F446");

                entity.HasOne(d => d.Ticket).WithMany(p => p.PurchaseItems)
                    .HasForeignKey(d => d.TicketId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__PurchaseI__Ticke__5165187F");
            });

            modelBuilder.Entity<Spectator>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Spectato__3214EC07D4053371");
                entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.FullName).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false);
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PK__Tickets__3214EC073F3B5C03");
                entity.HasIndex(e => e.ConcertId, "IDX_Tickets_ConcertId");
                entity.HasIndex(e => new { e.ConcertId, e.Row, e.SeatNumber }, "UC_Ticket").IsUnique();
                entity.Property(e => e.BasePrice).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.Row).HasMaxLength(10).IsUnicode(false);
                entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Available");
            });
        }
    }
}