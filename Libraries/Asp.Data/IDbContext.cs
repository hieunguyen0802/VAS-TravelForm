using System;
using System.Threading;
using System.Threading.Tasks;
using src.Core.Domains;
using Microsoft.EntityFrameworkCore;


namespace src.Data
{
    public interface IDbContext
    {
        DbSet<Domain> Domains { get; set; }
        DbSet<EmailTemplate> EmailTemplates { get; set; }
        DbSet<Log> Logs { get; set; }
        DbSet<Role> Roles { get; set; }
        DbSet<Setting> Settings { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<UserRole> UserRoles { get; set; }
        DbSet<parents_activities> parents_activities { get; set; }

        DbSet<TravelDeclaration> travel_declaration { get; set; }
        DbSet<TravellingRoute> travelling_routes { get; set; }
        DbSet<IncidentReport> incidentReport { get; set; }
        DbSet<routesAtRedZones> routesAtRedZones { get; set; }
        DbSet<routesOutsizeRedZones> routesOutsizeRedZones { get; set; }
        DbSet<informationContactSuspectCaseCovid> informationContactSuspectCaseCovid { get; set; }
        DbSet<routesOfContactingWithColleagues> routesOfContactingWithColleagues { get; set; }
        DbSet<HeadOfDepartment> head_of_department { get; set; }
        DbSet<configs_email> email_configs { get; set; }
        DbSet<Province> province { get; set; }
        DbSet<District> district { get; set; }
        DbSet<Ward> ward { get; set; }
        DbSet<TravelDeclarationDashboardModel> Dashboards { get; set; }

        DbSet<Country>  country { get; set; }
        DbSet<redZone> RedZone { get; set; }
        DbSet<RedZoneFollowUp> RedZoneFollowUp { get; set; }
        DbSet<updateHistory> updateHistory { get; set; } 

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

    }
}
