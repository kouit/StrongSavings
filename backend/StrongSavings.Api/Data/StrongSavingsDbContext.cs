
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StrongSavings.Api.Auth;
using StrongSavings.Api.Enums;
using StrongSavings.Api.Models;

namespace StrongSavings.Api.Data;

public class StrongSavingsDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public StrongSavingsDbContext(
        DbContextOptions<StrongSavingsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<Person> People => Set<Person>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Meeting> Meetings => Set<Meeting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Group>(entity =>
        {
            entity.Property(group => group.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasMany(group => group.People)
                .WithOne(person => person.Group)
                .HasForeignKey(person => person.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.Property(person => person.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(person => person.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasMany(person => person.Accounts)
                .WithOne(account => account.Person)
                .HasForeignKey(account => account.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(account => account.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(account => account.Type)
                .HasConversion<int>();

            entity.HasMany(account => account.Transactions)
                .WithOne(transaction => transaction.Account)
                .HasForeignKey(transaction => transaction.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(account => account.PersonId)
                .IsUnique()
                .HasFilter("[PersonId] IS NOT NULL AND [Type] = 1");

            entity.HasIndex(account => account.Type)
                .IsUnique()
                .HasFilter("[Type] = 2");

            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_Accounts_Owner",
                    """
                    (
                        (Type = 1 AND PersonId IS NOT NULL)
                        OR
                        (Type = 2 AND PersonId IS NULL)
                    )
                    """));
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(transaction => transaction.Amount)
                .HasPrecision(18, 2);

            entity.Property(transaction => transaction.Type)
                .HasConversion<int>();

            entity.Property(transaction => transaction.Description)
                .HasMaxLength(500);

            entity.HasOne(transaction => transaction.SubmittedByPerson)
                .WithMany()
                .HasForeignKey(transaction => transaction.SubmittedByPersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.Property(meeting => meeting.Notes)
                .HasMaxLength(5000);
        });

        // A login can be linked to at most one club member.
        // A club member can have at most one linked login.
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(user => user.PersonId)
                .IsUnique()
                .HasFilter("[PersonId] IS NOT NULL");

            entity.HasOne<Person>()
                .WithOne()
                .HasForeignKey<ApplicationUser>(user => user.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
