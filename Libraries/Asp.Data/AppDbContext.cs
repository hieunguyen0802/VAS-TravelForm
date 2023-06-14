using src.Core.Domains;
using Microsoft.EntityFrameworkCore;

namespace src.Data
{
    public partial class AppDbContext : DbContext, IDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Database.SetCommandTimeout(6000);
        }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<BaseCategory> BaseCategories { get; set; }
        public DbSet<parents_activities> parents_activities { get; set; }
        public DbSet<TravelDeclaration> travel_declaration { get; set; }
        public DbSet<TravellingRoute> travelling_routes { get; set; }
        public DbSet<HeadOfDepartment> head_of_department { get; set; }
        public DbSet<configs_email> email_configs { get; set; }
        public DbSet<Province> province { get; set; }
        public DbSet<District> district { get; set; }
        public DbSet<Ward> ward { get; set; }
        public DbSet<IncidentReport> incidentReport { get; set; }
        public DbSet<routesAtRedZones> routesAtRedZones { get; set; }
        public DbSet<routesOutsizeRedZones> routesOutsizeRedZones { get; set; }
        public DbSet<informationContactSuspectCaseCovid> informationContactSuspectCaseCovid { get; set; }
        public DbSet<routesOfContactingWithColleagues> routesOfContactingWithColleagues { get; set; }
        public DbSet<TravelDeclarationDashboardModel> Dashboards { get; set; }
        public DbSet<Country> country { get; set; }


        //Redzone
        public DbSet<redZone> RedZone { get; set; }
        public DbSet<RedZoneFollowUp> RedZoneFollowUp { get; set; }
        public DbSet<updateHistory> updateHistory { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            

            modelBuilder.Entity<Domain>(entity =>
            {
                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.Property(e => e.Id).HasColumnType("uniqueidentifier");
                entity.Property(e => e.Body).IsRequired();

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(200);

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
                entity.Property(e => e.Campus).IsRequired();
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Subject)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<Log>(entity =>
            {
                entity.Property(e => e.Action).HasMaxLength(50);

                entity.Property(e => e.Application)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Controller).HasMaxLength(50);

                entity.Property(e => e.Identity).HasMaxLength(50);

                entity.Property(e => e.Level)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Logged).HasColumnType("datetime");

                entity.Property(e => e.Logger).HasMaxLength(250);

                entity.Property(e => e.Message).IsRequired();

                entity.Property(e => e.Referrer).HasMaxLength(250);

                entity.Property(e => e.Url).HasMaxLength(500);

                entity.Property(e => e.UserAgent).HasMaxLength(250);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("varchar(50)");
            });

            modelBuilder.Entity<BaseCategory>(entity =>
                {
                    entity.Property(b => b.Code);
                    entity.Property(b => b.CategoryName);
                });
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Value).IsRequired();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId })
                    .HasName("PK_UserRoles");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_UserRoles_Roles");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_UserRoles_Users");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedOn).HasColumnType("datetime");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LastLoginDate).HasColumnType("datetime");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ModifiedOn).HasColumnType("datetime");

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(50);
            });
        }
    }
}
