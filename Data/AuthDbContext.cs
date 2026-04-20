using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}

public static class DbSeeder
{
    public static void Seed(AuthDbContext db)
    {
        if (db.Users.Any()) return;

        var users = new[]
        {
            new User
            {
                Id = Guid.NewGuid(),
                Username = "superadmin",
                Email = "superadmin@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "SuperAdmin",
                FullName = "Super Administrator",
                Department = "IT"
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "approver1",
                Email = "approver@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Approver@123"),
                Role = "Approver",
                FullName = "Manager HRD",
                Department = "HRD"
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "employee1",
                Email = "employee@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                Role = "Employee",
                FullName = "Budi Santoso",
                Department = "Engineering"
            }
        };

        db.Users.AddRange(users);
        db.SaveChanges();
    }
}