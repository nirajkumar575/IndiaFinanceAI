using IndiaFinanceAI.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndiaFinanceAI.Infrastructure.Persistence;

public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
{
    public DbSet<CustomerLead> CustomerLeads => Set<CustomerLead>();
    public DbSet<WhatsAppCommunicationEvent> WhatsAppCommunicationEvents => Set<WhatsAppCommunicationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var lead = modelBuilder.Entity<CustomerLead>();
        lead.ToTable("CustomerLeads");
        lead.HasKey(item => item.Id);
        lead.Property(item => item.FullName).HasMaxLength(100).IsRequired();
        lead.Property(item => item.MobileNumber).HasMaxLength(10).IsRequired();
        lead.Property(item => item.Email).HasMaxLength(254);
        lead.Property(item => item.State).HasMaxLength(100).IsRequired();
        lead.Property(item => item.City).HasMaxLength(100).IsRequired();
        lead.Property(item => item.LoanType).HasMaxLength(40).IsRequired();
        lead.Property(item => item.RequiredLoanAmount).HasPrecision(18, 2);
        lead.Property(item => item.MonthlyIncome).HasPrecision(18, 2);
        lead.Property(item => item.EmploymentType).HasMaxLength(30).IsRequired();
        lead.Property(item => item.ConsentGiven).IsRequired();
        lead.Property(item => item.CallbackRequested).IsRequired();
        lead.Property(item => item.PreferredContactTime).HasMaxLength(20);
        lead.Property(item => item.LeadSource).HasMaxLength(30).HasDefaultValue("Website").IsRequired();
        lead.Property(item => item.CreatedAt).IsRequired();
        lead.Property(item => item.LeadStatus).HasMaxLength(20).IsRequired();
        lead.Property(item => item.FollowUpDate);
        lead.Property(item => item.FollowUpNotes).HasMaxLength(2000);
        lead.Property(item => item.WhatsAppConsentGiven).IsRequired();
        lead.Property(item => item.WhatsAppConsentAt);
        lead.Property(item => item.WhatsAppOptedOutAt);
        lead.Property(item => item.LastWhatsAppActivityAt);
        lead.Property(item => item.LastWhatsAppStatus).HasMaxLength(40);
        lead.HasIndex(item => item.CreatedAt);
        lead.HasIndex(item => new { item.LoanType, item.LeadStatus });

        var communicationEvent = modelBuilder.Entity<WhatsAppCommunicationEvent>();
        communicationEvent.ToTable("WhatsAppCommunicationEvents");
        communicationEvent.HasKey(item => item.Id);
        communicationEvent.Property(item => item.EventType).HasMaxLength(40).IsRequired();
        communicationEvent.Property(item => item.Status).HasMaxLength(30).IsRequired();
        communicationEvent.Property(item => item.MessageId).HasMaxLength(150);
        communicationEvent.Property(item => item.TemplateName).HasMaxLength(100);
        communicationEvent.Property(item => item.ErrorCode).HasMaxLength(50);
        communicationEvent.Property(item => item.CreatedAt).IsRequired();
        communicationEvent.HasOne(item => item.CustomerLead)
            .WithMany()
            .HasForeignKey(item => item.CustomerLeadId)
            .OnDelete(DeleteBehavior.Cascade);
        communicationEvent.HasIndex(item => new { item.CustomerLeadId, item.CreatedAt });
        communicationEvent.HasIndex(item => item.MessageId);
    }
}
