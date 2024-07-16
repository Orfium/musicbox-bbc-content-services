using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MusicManager.Core.PrsModels
{
    public partial class MLPrsContext : DbContext
    {
        public MLPrsContext()
        {
        }

        public MLPrsContext(DbContextOptions<MLPrsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<track_prs_master> track_prs_master { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<track_prs_master>(entity =>
            {
                entity.HasKey(e => e.track_id)
                    .HasName("track_prs_master_pkey");

                entity.Property(e => e.track_id).ValueGeneratedNever();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
