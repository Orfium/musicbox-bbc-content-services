using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MusicManager.Core.Models
{
    public partial class MLContext : DbContext
    {
      
        public MLContext(DbContextOptions<MLContext> options)
            : base(options)
        {
        }

        public virtual DbSet<album_org> album_org { get; set; }
        public virtual DbSet<c_tag> c_tag { get; set; }
        public virtual DbSet<c_tag_extended> c_tag_extended { get; set; }
        public virtual DbSet<c_tag_index_status> c_tag_index_status { get; set; }
        public virtual DbSet<chart_master_albums> chart_master_albums { get; set; }
        public virtual DbSet<chart_master_tracks> chart_master_tracks { get; set; }
        public virtual DbSet<chart_sync_summary> chart_sync_summary { get; set; }
        public virtual DbSet<cleansed_tag_track> cleansed_tag_track { get; set; }
        public virtual DbSet<ctag_extended_search> ctag_extended_search { get; set; }
        public virtual DbSet<ctag_search> ctag_search { get; set; }
        public virtual DbSet<dh_status> dh_status { get; set; }
        public virtual DbSet<elastic_album_change> elastic_album_change { get; set; }
        public virtual DbSet<elastic_track_change> elastic_track_change { get; set; }
        public virtual DbSet<isrc_tunecode> isrc_tunecode { get; set; }
        public virtual DbSet<library> library { get; set; }
        public virtual DbSet<library_org> library_org { get; set; }
        public virtual DbSet<library_search> library_search { get; set; }
        public virtual DbSet<library_search_ml_admin> library_search_ml_admin { get; set; }
        public virtual DbSet<log_album_api_calls> log_album_api_calls { get; set; }
        public virtual DbSet<log_album_api_results> log_album_api_results { get; set; }
        public virtual DbSet<log_album_sync_session> log_album_sync_session { get; set; }
        public virtual DbSet<log_elastic_track_changes> log_elastic_track_changes { get; set; }
        public virtual DbSet<log_library_change> log_library_change { get; set; }
        public virtual DbSet<log_prs_search_time> log_prs_search_time { get; set; }
        public virtual DbSet<log_sync_time> log_sync_time { get; set; }
        public virtual DbSet<log_track_api_calls> log_track_api_calls { get; set; }
        public virtual DbSet<log_track_api_results> log_track_api_results { get; set; }
        public virtual DbSet<log_track_index_error> log_track_index_error { get; set; }
        public virtual DbSet<log_track_sync_session> log_track_sync_session { get; set; }
        public virtual DbSet<log_user_action> log_user_action { get; set; }
        public virtual DbSet<log_workspace_change> log_workspace_change { get; set; }
        public virtual DbSet<log_ws_lib_change> log_ws_lib_change { get; set; }
        public virtual DbSet<log_ws_lib_status_change> log_ws_lib_status_change { get; set; }
        public virtual DbSet<member_label> member_label { get; set; }
        public virtual DbSet<ml_master_album> ml_master_album { get; set; }
        public virtual DbSet<ml_master_track> ml_master_track { get; set; }
        public virtual DbSet<ml_status> ml_status { get; set; }
        public virtual DbSet<org_exclude> org_exclude { get; set; }
        public virtual DbSet<org_track_version> org_track_version { get; set; }
        public virtual DbSet<org_user> org_user { get; set; }
        public virtual DbSet<org_workspace> org_workspace { get; set; }
        public virtual DbSet<playout_response> playout_response { get; set; }
        public virtual DbSet<playout_response_status> playout_response_status { get; set; }
        public virtual DbSet<playout_session> playout_session { get; set; }
        public virtual DbSet<playout_session_search> playout_session_search { get; set; }
        public virtual DbSet<playout_session_search1> playout_session_search1 { get; set; }
        public virtual DbSet<playout_session_tracks> playout_session_tracks { get; set; }
        public virtual DbSet<playout_tracks_search> playout_tracks_search { get; set; }
        public virtual DbSet<playout_tracks_search1> playout_tracks_search1 { get; set; }
        public virtual DbSet<ppl_label_search> ppl_label_search { get; set; }
        public virtual DbSet<prior_approval_work> prior_approval_work { get; set; }
        public virtual DbSet<radio_categories> radio_categories { get; set; }
        public virtual DbSet<radio_stations> radio_stations { get; set; }
        public virtual DbSet<record_label> record_label { get; set; }
        public virtual DbSet<staging_library> staging_library { get; set; }
        public virtual DbSet<staging_tag_track> staging_tag_track { get; set; }
        public virtual DbSet<staging_workspace> staging_workspace { get; set; }
        public virtual DbSet<sync_info> sync_info { get; set; }
        public virtual DbSet<sync_status> sync_status { get; set; }
        public virtual DbSet<tag> tag { get; set; }
        public virtual DbSet<tag_code> tag_code { get; set; }
        public virtual DbSet<tag_track> tag_track { get; set; }
        public virtual DbSet<tag_type> tag_type { get; set; }
        public virtual DbSet<track_org> track_org { get; set; }
        public virtual DbSet<upload_album> upload_album { get; set; }
        public virtual DbSet<upload_album_search> upload_album_search { get; set; }
        public virtual DbSet<upload_session> upload_session { get; set; }
        public virtual DbSet<upload_session_search> upload_session_search { get; set; }
        public virtual DbSet<upload_track> upload_track { get; set; }
        public virtual DbSet<upload_tracks_search> upload_tracks_search { get; set; }
        public virtual DbSet<workspace> workspace { get; set; }
        public virtual DbSet<workspace_org> workspace_org { get; set; }
        public virtual DbSet<workspace_pause> workspace_pause { get; set; }
        public virtual DbSet<workspace_search> workspace_search { get; set; }
        public virtual DbSet<workspace_search_ml_admin> workspace_search_ml_admin { get; set; }
        public virtual DbSet<ws_lib_tracks_to_be_synced> ws_lib_tracks_to_be_synced { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("uuid-ossp");

            modelBuilder.Entity<album_org>(entity =>
            {
                entity.HasKey(e => new { e.original_album_id, e.org_id })
                    .HasName("album_org_pkey");

                entity.HasIndex(e => e.original_album_id)
                    .HasName("idx_original_album_id");
            });

            modelBuilder.Entity<c_tag_index_status>(entity =>
            {
                entity.HasKey(e => e.type)
                    .HasName("c_tag_index_status_pkey");
            });

            modelBuilder.Entity<chart_master_albums>(entity =>
            {
                entity.HasKey(e => e.master_id)
                    .HasName("chart_master_albums_pkey");

                entity.Property(e => e.master_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<chart_master_tracks>(entity =>
            {
                entity.HasKey(e => e.master_id)
                    .HasName("chart_master_tracks_pkey");

                entity.Property(e => e.master_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<chart_sync_summary>(entity =>
            {
                entity.Property(e => e.id).HasDefaultValueSql("nextval('charts.sync_summary_id_seq'::regclass)");
            });

            modelBuilder.Entity<ctag_extended_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ctag_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<dh_status>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<elastic_album_change>(entity =>
            {
                entity.HasKey(e => e.document_id)
                    .HasName("elastic_album_change_pkey");

                entity.Property(e => e.document_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<elastic_track_change>(entity =>
            {
                entity.HasIndex(e => e.org_workspace_id)
                    .HasName("idx_org_workspace_id");

                entity.HasIndex(e => new { e.original_track_id, e.org_id })
                    .HasName("idx_original_track_id_org_id");

                entity.Property(e => e.archived).HasDefaultValueSql("false");
            });

            modelBuilder.Entity<isrc_tunecode>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<library>(entity =>
            {
                entity.HasKey(e => e.library_id)
                    .HasName("library_pkey");

                entity.HasIndex(e => e.workspace_id)
                    .HasName("library_ws_idx");

                entity.Property(e => e.library_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<library_org>(entity =>
            {
                entity.HasKey(e => e.org_library_id)
                    .HasName("library_org_pkey");

                entity.Property(e => e.org_library_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<library_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<library_search_ml_admin>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<log_album_sync_session>(entity =>
            {
                entity.HasKey(e => e.session_id)
                    .HasName("log_album_sync_session_pkey");
            });

            modelBuilder.Entity<log_library_change>(entity =>
            {
                entity.HasKey(e => e.libch_id)
                    .HasName("log_library_change_pkey");
            });

            modelBuilder.Entity<log_track_sync_session>(entity =>
            {
                entity.HasKey(e => e.session_id)
                    .HasName("log_track_sync_session_pkey");
            });

            modelBuilder.Entity<log_workspace_change>(entity =>
            {
                entity.HasKey(e => e.wsch_id)
                    .HasName("log_workspace_change_pkey");
            });

            modelBuilder.Entity<log_ws_lib_change>(entity =>
            {
                entity.HasKey(e => e.ws_lib_change_id)
                    .HasName("ws_lib_change_pkey");

                entity.Property(e => e.ws_lib_change_id).HasDefaultValueSql("nextval('log.ws_lib_change_ws_lib_change_id_seq'::regclass)");
            });

            modelBuilder.Entity<ml_master_album>(entity =>
            {
                entity.HasKey(e => e.album_id)
                    .HasName("ml_master_album_pkey");

                entity.HasIndex(e => e.album_id)
                    .HasName("ml_master_album_id_idx")
                    .IsUnique();

                entity.HasIndex(e => new { e.workspace_id, e.library_id, e.api_result_id })
                    .HasName("ml_master_album_workspace_id_library_id_api_result_id");

                entity.Property(e => e.album_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<ml_master_track>(entity =>
            {
                entity.HasKey(e => e.track_id)
                    .HasName("ml_master_track_pkey");

                entity.HasIndex(e => e.album_id)
                    .HasName("ml_master_track_album_id_idx");

                entity.HasIndex(e => e.library_id)
                    .HasName("ml_master_track_library_id_idx");

                entity.HasIndex(e => e.track_id)
                    .HasName("ml_master_track_id_idx")
                    .IsUnique();

                entity.HasIndex(e => new { e.workspace_id, e.api_result_id })
                    .HasName("ml_master_track_workspace_id_api_result_id");

                entity.Property(e => e.track_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<ml_status>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<org_track_version>(entity =>
            {
                entity.HasKey(e => e.ml_version_id)
                    .HasName("org_track_version_pkey");

                entity.Property(e => e.ml_version_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<org_user>(entity =>
            {
                entity.HasKey(e => e.user_id)
                    .HasName("org_user_pkey");

                entity.Property(e => e.user_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<playout_response>(entity =>
            {
                entity.HasKey(e => e.response_id)
                    .HasName("playout_response_pkey");
            });

            modelBuilder.Entity<playout_session_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<playout_session_search1>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<playout_tracks_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<playout_tracks_search1>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<ppl_label_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<prior_approval_work>(entity =>
            {
                entity.Property(e => e.id).HasDefaultValueSql("nextval('prior_approval_id_seq'::regclass)");
            });

            modelBuilder.Entity<radio_categories>(entity =>
            {
                entity.HasKey(e => e.category_id)
                    .HasName("radio_categories_pk");

                entity.Property(e => e.category_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<radio_stations>(entity =>
            {
                entity.Property(e => e.id).ValueGeneratedNever();
            });

            modelBuilder.Entity<record_label>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<staging_library>(entity =>
            {
                entity.HasKey(e => e.library_id)
                    .HasName("library_pkey");

                entity.Property(e => e.library_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<staging_workspace>(entity =>
            {
                entity.HasKey(e => e.workspace_id)
                    .HasName("workspace_pkey");

                entity.Property(e => e.workspace_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<sync_status>(entity =>
            {
                entity.HasKey(e => e.status_code)
                    .HasName("sync_status_pkey");
            });

            modelBuilder.Entity<tag>(entity =>
            {
                entity.HasKey(e => e.tag_id)
                    .HasName("tag_pkey");

                entity.Property(e => e.tag_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<tag_code>(entity =>
            {
                entity.HasKey(e => e.tag_code_id)
                    .HasName("tag_code_pkey");

                entity.Property(e => e.tag_code_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<tag_type>(entity =>
            {
                entity.HasKey(e => e.tag_type_id)
                    .HasName("tag_type_pkey");

                entity.Property(e => e.tag_type_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<track_org>(entity =>
            {
                entity.HasKey(e => new { e.original_track_id, e.org_id })
                    .HasName("track_org_pkey");

                entity.HasIndex(e => e.album_id)
                    .HasName("idx_album_id");

                entity.HasIndex(e => e.original_track_id)
                    .HasName("idx_original_track_id");
            });

            modelBuilder.Entity<upload_album>(entity =>
            {
                entity.Property(e => e.id).ValueGeneratedNever();
            });

            modelBuilder.Entity<upload_album_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<upload_session_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<upload_track>(entity =>
            {
                entity.HasIndex(e => e.session_id)
                    .HasName("idx_upload_id");

                entity.Property(e => e.id).ValueGeneratedNever();

                entity.Property(e => e.asset_uploaded).HasDefaultValueSql("false");
            });

            modelBuilder.Entity<upload_tracks_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<workspace>(entity =>
            {
                entity.HasKey(e => e.workspace_id)
                    .HasName("workspace_pkey");

                entity.Property(e => e.workspace_id).ValueGeneratedNever();

                entity.Property(e => e.priority_sync).HasDefaultValueSql("0");
            });

            modelBuilder.Entity<workspace_org>(entity =>
            {
                entity.HasKey(e => e.org_workspace_id)
                    .HasName("workspace_org_pkey");

                entity.HasIndex(e => e.workspace_id)
                    .HasName("idx_workspace_id");

                entity.Property(e => e.org_workspace_id).ValueGeneratedNever();
            });

            modelBuilder.Entity<workspace_pause>(entity =>
            {
                entity.Property(e => e.id).ValueGeneratedNever();
            });

            modelBuilder.Entity<workspace_search>(entity =>
            {
                entity.HasNoKey();
            });

            modelBuilder.Entity<workspace_search_ml_admin>(entity =>
            {
                entity.HasNoKey();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
