using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data.Configurations;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Movement> Movements => Set<Movement>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();
    public DbSet<MovementConstant> MovementConstants => Set<MovementConstant>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemConstant> SystemConstants => Set<SystemConstant>();

    // ─── وحدة طلبات المواطنين ─────────────────────────────────────────────────
    public DbSet<CitizenRequest> CitizenRequests => Set<CitizenRequest>();
    public DbSet<RequestDestination> RequestDestinations => Set<RequestDestination>();
    public DbSet<CitizenRequestStatus> CitizenRequestStatuses => Set<CitizenRequestStatus>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<CitizenRequestAttachment> CitizenRequestAttachments => Set<CitizenRequestAttachment>();
    public DbSet<CitizenRequestStatusHistory> CitizenRequestStatusHistory => Set<CitizenRequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MovementConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new MemberConfiguration());
        modelBuilder.ApplyConfiguration(new JoinRequestConfiguration());
        modelBuilder.ApplyConfiguration(new MovementConstantConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new SystemConstantConfiguration());

        // وحدة طلبات المواطنين
        modelBuilder.ApplyConfiguration(new CitizenRequestConfiguration());
        modelBuilder.ApplyConfiguration(new RequestDestinationConfiguration());
        modelBuilder.ApplyConfiguration(new CitizenRequestStatusConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CitizenRequestAttachmentConfiguration());
        modelBuilder.ApplyConfiguration(new CitizenRequestStatusHistoryConfiguration());
    }
}
