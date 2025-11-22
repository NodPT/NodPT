using Microsoft.EntityFrameworkCore;
using NodPT.Data.Models;

namespace NodPT.Data
{
    public class NodPTDbContext : DbContext
    {
        public NodPTDbContext(DbContextOptions<NodPTDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Node> Nodes { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateFile> TemplateFiles { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<ProjectFile> ProjectFiles { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<UserAccessLog> UserAccessLogs { get; set; }
        public DbSet<AIModel> AIModels { get; set; }
        public DbSet<Prompt> Prompts { get; set; }
        public DbSet<ChatResponse> ChatResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FirebaseUid).IsUnique();
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Active);
                entity.HasIndex(e => e.Approved);
                entity.HasIndex(e => e.Banned);
                entity.HasIndex(e => e.IsAdmin);
                entity.Property(e => e.FirebaseUid).HasMaxLength(128);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.DisplayName).HasMaxLength(255);
            });

            // Project entity configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Projects)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Template)
                    .WithMany(e => e.Projects)
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Node entity configuration
            modelBuilder.Entity<Node>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(450);
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Project)
                    .WithMany(e => e.Nodes)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Template)
                    .WithMany(e => e.Nodes)
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.AIModel)
                    .WithMany(e => e.Nodes)
                    .HasForeignKey(e => e.AIModelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Template entity configuration
            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Version).HasMaxLength(50);
            });

            // TemplateFile entity configuration
            modelBuilder.Entity<TemplateFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Path).HasMaxLength(500);
                entity.Property(e => e.Extension).HasMaxLength(50);
                entity.Property(e => e.MimeType).HasMaxLength(100);
                entity.HasOne(e => e.Template)
                    .WithMany(e => e.TemplateFiles)
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Folder entity configuration
            modelBuilder.Entity<Folder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Path).HasMaxLength(500);
                entity.HasOne(e => e.Project)
                    .WithMany(e => e.Folders)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ProjectFile entity configuration
            modelBuilder.Entity<ProjectFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Path).HasMaxLength(500);
                entity.Property(e => e.Extension).HasMaxLength(50);
                entity.Property(e => e.MimeType).HasMaxLength(100);
                entity.HasOne(e => e.Folder)
                    .WithMany(e => e.Files)
                    .HasForeignKey(e => e.FolderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ChatMessage entity configuration
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Sender).HasMaxLength(100);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.ChatMessages)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Node)
                    .WithMany(e => e.ChatMessages)
                    .HasForeignKey(e => e.NodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Log entity configuration
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).HasMaxLength(50);
                entity.Property(e => e.Logger).HasMaxLength(255);
                entity.Property(e => e.Source).HasMaxLength(255);
            });

            // UserAccessLog entity configuration
            modelBuilder.Entity<UserAccessLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.AccessLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AIModel entity configuration
            modelBuilder.Entity<AIModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.ModelIdentifier).HasMaxLength(255);
                entity.HasIndex(e => e.IsActive);
                entity.HasOne(e => e.Template)
                    .WithMany(e => e.AIModels)
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Prompt entity configuration
            modelBuilder.Entity<Prompt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Template)
                    .WithMany(e => e.Prompts)
                    .HasForeignKey(e => e.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ChatResponse entity configuration
            modelBuilder.Entity<ChatResponse>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).HasMaxLength(50);
            });
        }
    }
}
