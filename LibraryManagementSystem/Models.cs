using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LibraryManagementSystem;

public sealed class LibraryData
{
    public int SchemaVersion { get; set; } = 1;
    public List<UserAccount> Users { get; set; } = new();
    public List<Book> Books { get; set; } = new();
    public List<Member> Members { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();
}

public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Administrator";
}

public sealed class Book
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccessionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public int PublishYear { get; set; }
    public string Shelf { get; set; } = string.Empty;
    public int TotalCopies { get; set; } = 1;
    public int AvailableCopies { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public string Availability => AvailableCopies > 0 ? "Available" : "Out of stock";
}

public sealed class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MemberNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;
}

public sealed class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LoanNumber { get; set; } = string.Empty;
    public Guid BookId { get; set; }
    public Guid MemberId { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
    public DateTime? ReturnDate { get; set; }
    public decimal FinePaid { get; set; }

    [JsonIgnore]
    public bool IsReturned => ReturnDate.HasValue;
}

public sealed record DashboardStats(
    int BookTitles,
    int TotalCopies,
    int Members,
    int ActiveLoans,
    int OverdueLoans);
