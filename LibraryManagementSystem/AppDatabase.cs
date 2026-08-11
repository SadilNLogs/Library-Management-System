using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LibraryManagementSystem;

public sealed class AppDatabase
{
    private readonly object _sync = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string DataDirectory { get; }
    public string DatabasePath { get; }
    public LibraryData Data { get; private set; }

    public AppDatabase()
    {
        DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LibraryManagementSystem");
        DatabasePath = Path.Combine(DataDirectory, "library-data.json");
        Directory.CreateDirectory(DataDirectory);
        Data = LoadOrCreate();
    }

    private LibraryData LoadOrCreate()
    {
        if (!File.Exists(DatabasePath))
        {
            LibraryData seeded = CreateSeedData();
            SaveInternal(seeded);
            return seeded;
        }

        try
        {
            string json = File.ReadAllText(DatabasePath);
            LibraryData data = JsonSerializer.Deserialize<LibraryData>(json, _jsonOptions)
                ?? throw new InvalidDataException("The data file is empty.");
            ValidateData(data);
            return data;
        }
        catch (Exception ex)
        {
            string backupPath = Path.Combine(
                DataDirectory,
                $"library-data-corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(DatabasePath, backupPath, true);
            throw new InvalidDataException(
                $"The local data file is damaged. A copy was saved as '{Path.GetFileName(backupPath)}'. " +
                "Restore a valid backup or rename the damaged file, then start the app again.", ex);
        }
    }

    public UserAccount? Authenticate(string username, string password)
    {
        lock (_sync)
        {
            UserAccount? account = Data.Users.FirstOrDefault(user =>
                string.Equals(user.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
            return account is not null && PasswordHasher.Verify(password, account.PasswordHash)
                ? account
                : null;
        }
    }

    public void ChangePassword(Guid userId, string currentPassword, string newPassword)
    {
        lock (_sync)
        {
            UserAccount account = Data.Users.First(user => user.Id == userId);
            if (!PasswordHasher.Verify(currentPassword, account.PasswordHash))
                throw new InvalidOperationException("The current password is incorrect.");
            if (newPassword.Length < 8)
                throw new InvalidOperationException("The new password must contain at least eight characters.");
            if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
                throw new InvalidOperationException("Choose a new password that is different from the current password.");

            account.PasswordHash = PasswordHasher.Hash(newPassword);
            Save();
        }
    }

    public DashboardStats GetStats()
    {
        lock (_sync)
        {
            return new DashboardStats(
                Data.Books.Count,
                Data.Books.Sum(book => book.TotalCopies),
                Data.Members.Count(member => member.IsActive),
                Data.Loans.Count(loan => !loan.IsReturned),
                Data.Loans.Count(loan => !loan.IsReturned && loan.DueDate.Date < DateTime.Today));
        }
    }

    public string NextAccessionNumber() => NextNumber("BK", Data.Books.Select(book => book.AccessionNumber));
    public string NextMemberNumber() => NextNumber("MEM", Data.Members.Select(member => member.MemberNumber));
    public string NextLoanNumber() => NextNumber("LN", Data.Loans.Select(loan => loan.LoanNumber));

    public void AddBook(Book book)
    {
        lock (_sync)
        {
            EnsureBookIsValid(book);
            if (Data.Books.Any(existing => string.Equals(existing.AccessionNumber, book.AccessionNumber, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That accession number is already in use.");
            if (!string.IsNullOrWhiteSpace(book.Isbn) && Data.Books.Any(existing => string.Equals(existing.Isbn, book.Isbn, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That ISBN is already in use.");

            book.Id = Guid.NewGuid();
            book.CreatedAt = DateTime.Now;
            book.AvailableCopies = book.TotalCopies;
            Data.Books.Add(book);
            Save();
        }
    }

    public void UpdateBook(Book changed)
    {
        lock (_sync)
        {
            EnsureBookIsValid(changed);
            Book current = Data.Books.First(book => book.Id == changed.Id);
            if (Data.Books.Any(book => book.Id != changed.Id && string.Equals(book.AccessionNumber, changed.AccessionNumber, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That accession number is already in use.");
            if (!string.IsNullOrWhiteSpace(changed.Isbn) && Data.Books.Any(book => book.Id != changed.Id && string.Equals(book.Isbn, changed.Isbn, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That ISBN is already in use.");

            int issuedCopies = current.TotalCopies - current.AvailableCopies;
            if (changed.TotalCopies < issuedCopies)
                throw new InvalidOperationException($"At least {issuedCopies} copies are currently issued. Total copies cannot be lower than that.");

            current.AccessionNumber = changed.AccessionNumber.Trim();
            current.Title = changed.Title.Trim();
            current.Author = changed.Author.Trim();
            current.Isbn = changed.Isbn.Trim();
            current.Category = changed.Category.Trim();
            current.Publisher = changed.Publisher.Trim();
            current.PublishYear = changed.PublishYear;
            current.Shelf = changed.Shelf.Trim();
            current.TotalCopies = changed.TotalCopies;
            current.AvailableCopies = changed.TotalCopies - issuedCopies;
            Save();
        }
    }

    public void DeleteBook(Guid id)
    {
        lock (_sync)
        {
            if (Data.Loans.Any(loan => loan.BookId == id && !loan.IsReturned))
                throw new InvalidOperationException("This book has an active loan and cannot be deleted.");
            Data.Books.RemoveAll(book => book.Id == id);
            Save();
        }
    }

    public void AddMember(Member member)
    {
        lock (_sync)
        {
            EnsureMemberIsValid(member);
            if (Data.Members.Any(existing => string.Equals(existing.MemberNumber, member.MemberNumber, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That member number is already in use.");
            if (!string.IsNullOrWhiteSpace(member.Email) && Data.Members.Any(existing => string.Equals(existing.Email, member.Email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That email address is already in use.");

            member.Id = Guid.NewGuid();
            Data.Members.Add(member);
            Save();
        }
    }

    public void UpdateMember(Member changed)
    {
        lock (_sync)
        {
            EnsureMemberIsValid(changed);
            Member current = Data.Members.First(member => member.Id == changed.Id);
            if (Data.Members.Any(member => member.Id != changed.Id && string.Equals(member.MemberNumber, changed.MemberNumber, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That member number is already in use.");
            if (!string.IsNullOrWhiteSpace(changed.Email) && Data.Members.Any(member => member.Id != changed.Id && string.Equals(member.Email, changed.Email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("That email address is already in use.");

            current.MemberNumber = changed.MemberNumber.Trim();
            current.FullName = changed.FullName.Trim();
            current.Email = changed.Email.Trim();
            current.Phone = changed.Phone.Trim();
            current.Address = changed.Address.Trim();
            current.JoinedDate = changed.JoinedDate.Date;
            current.IsActive = changed.IsActive;
            Save();
        }
    }

    public void DeleteMember(Guid id)
    {
        lock (_sync)
        {
            if (Data.Loans.Any(loan => loan.MemberId == id && !loan.IsReturned))
                throw new InvalidOperationException("This member has an active loan and cannot be deleted.");
            Data.Members.RemoveAll(member => member.Id == id);
            Save();
        }
    }

    public void IssueBook(Guid bookId, Guid memberId, DateTime issueDate, DateTime dueDate)
    {
        lock (_sync)
        {
            Book book = Data.Books.First(item => item.Id == bookId);
            Member member = Data.Members.First(item => item.Id == memberId);

            if (book.AvailableCopies < 1)
                throw new InvalidOperationException("No copy of this book is currently available.");
            if (!member.IsActive)
                throw new InvalidOperationException("The selected member is inactive.");
            if (dueDate.Date <= issueDate.Date)
                throw new InvalidOperationException("The due date must be after the issue date.");
            if (Data.Loans.Count(loan => loan.MemberId == memberId && !loan.IsReturned) >= 5)
                throw new InvalidOperationException("A member can have a maximum of five active loans.");
            if (Data.Loans.Any(loan => loan.MemberId == memberId && loan.BookId == bookId && !loan.IsReturned))
                throw new InvalidOperationException("This member already has an active loan for that book.");

            Data.Loans.Add(new Loan
            {
                LoanNumber = NextLoanNumber(),
                BookId = bookId,
                MemberId = memberId,
                IssueDate = issueDate.Date,
                DueDate = dueDate.Date
            });
            book.AvailableCopies--;
            Save();
        }
    }

    public decimal CalculateFine(Loan loan, DateTime returnDate)
    {
        int lateDays = Math.Max(0, (returnDate.Date - loan.DueDate.Date).Days);
        return lateDays * 10m;
    }

    public void ReturnBook(Guid loanId, DateTime returnDate, decimal finePaid)
    {
        lock (_sync)
        {
            Loan loan = Data.Loans.First(item => item.Id == loanId);
            if (loan.IsReturned)
                throw new InvalidOperationException("This loan has already been returned.");
            if (returnDate.Date < loan.IssueDate.Date)
                throw new InvalidOperationException("Return date cannot be before the issue date.");

            Book book = Data.Books.First(item => item.Id == loan.BookId);
            loan.ReturnDate = returnDate.Date;
            loan.FinePaid = Math.Max(0, finePaid);
            book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);
            Save();
        }
    }

    public string CreateBackup(string destinationDirectory)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(destinationDirectory);
            string destination = Path.Combine(destinationDirectory, $"library-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            Save();
            File.Copy(DatabasePath, destination, false);
            return destination;
        }
    }

    public void Save()
    {
        lock (_sync)
        {
            ValidateData(Data);
            SaveInternal(Data);
        }
    }

    private void SaveInternal(LibraryData data)
    {
        string temporaryPath = DatabasePath + ".tmp";
        string json = JsonSerializer.Serialize(data, _jsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, DatabasePath, true);
    }

    private static void EnsureBookIsValid(Book book)
    {
        if (string.IsNullOrWhiteSpace(book.AccessionNumber))
            throw new InvalidOperationException("Accession number is required.");
        if (string.IsNullOrWhiteSpace(book.Title))
            throw new InvalidOperationException("Book title is required.");
        if (string.IsNullOrWhiteSpace(book.Author))
            throw new InvalidOperationException("Author is required.");
        if (book.TotalCopies < 1)
            throw new InvalidOperationException("Total copies must be at least one.");
        if (book.PublishYear < 0 || book.PublishYear > DateTime.Today.Year + 1)
            throw new InvalidOperationException("Enter a valid publication year.");
    }

    private static void EnsureMemberIsValid(Member member)
    {
        if (string.IsNullOrWhiteSpace(member.MemberNumber))
            throw new InvalidOperationException("Member number is required.");
        if (string.IsNullOrWhiteSpace(member.FullName))
            throw new InvalidOperationException("Member name is required.");
        if (!string.IsNullOrWhiteSpace(member.Email) && !member.Email.Contains('@'))
            throw new InvalidOperationException("Enter a valid email address.");
    }

    private static void ValidateData(LibraryData data)
    {
        data.Users ??= new List<UserAccount>();
        data.Books ??= new List<Book>();
        data.Members ??= new List<Member>();
        data.Loans ??= new List<Loan>();

        if (data.Books.Any(book => book.TotalCopies < 0 || book.AvailableCopies < 0 || book.AvailableCopies > book.TotalCopies))
            throw new InvalidDataException("One or more book copy counts are invalid.");
    }

    private static string NextNumber(string prefix, IEnumerable<string> values)
    {
        int max = values
            .Select(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value[prefix.Length..], out int number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"{prefix}{max + 1:0000}";
    }

    private static LibraryData CreateSeedData()
    {
        Book cleanCode = new()
        {
            AccessionNumber = "BK0001",
            Title = "Clean Code",
            Author = "Robert C. Martin",
            Isbn = "9780132350884",
            Category = "Software Engineering",
            Publisher = "Prentice Hall",
            PublishYear = 2008,
            Shelf = "A-01",
            TotalCopies = 3,
            AvailableCopies = 3
        };
        Book databaseSystems = new()
        {
            AccessionNumber = "BK0002",
            Title = "Database System Concepts",
            Author = "Abraham Silberschatz",
            Isbn = "9780078022159",
            Category = "Database",
            Publisher = "McGraw-Hill",
            PublishYear = 2019,
            Shelf = "A-02",
            TotalCopies = 2,
            AvailableCopies = 1
        };
        Member member = new()
        {
            MemberNumber = "MEM0001",
            FullName = "Demo Student",
            Email = "student@example.com",
            Phone = "0770000000",
            Address = "Sri Lanka",
            JoinedDate = DateTime.Today.AddMonths(-2)
        };

        return new LibraryData
        {
            Users = new List<UserAccount>
            {
                new()
                {
                    Username = "admin",
                    PasswordHash = PasswordHasher.Hash("admin123"),
                    FullName = "System Administrator",
                    Role = "Administrator"
                }
            },
            Books = new List<Book> { cleanCode, databaseSystems },
            Members = new List<Member> { member },
            Loans = new List<Loan>
            {
                new()
                {
                    LoanNumber = "LN0001",
                    BookId = databaseSystems.Id,
                    MemberId = member.Id,
                    IssueDate = DateTime.Today.AddDays(-5),
                    DueDate = DateTime.Today.AddDays(9)
                }
            }
        };
    }
}
