using Microsoft.EntityFrameworkCore;
using PineSms.Core.Contracts;
using PineSms.Core.Entities;
using PineSms.Core.Features.Customer;
using PineSms.Persistence.Services;

namespace PineSms.Persistence.Repositories;

public class CustomerRepository : ICustomerService
{
    private readonly PineSmsDbContext dbContext;

    public CustomerRepository(PineSmsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<(bool success, string message)> InsertCustomer(InsertCustomerCommand command, string userId)
    {
        if (!IsValidPhoneNumber(command.PhoneNumber))
            return (false, "شماره موبایل معتبر نیست");

        bool exists = await dbContext.Customer.AnyAsync(c => c.PhoneNumber == command.PhoneNumber);
        if (exists)
            return (false, "این شماره موبایل قبلاً ثبت شده است");

        DateTime? birthDate = null;
        if (!string.IsNullOrEmpty(command.BirthDate))
        {
            var pc = new System.Globalization.PersianCalendar();
            var parts = command.BirthDate.Split('/');
            if (parts.Length == 3 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m) && int.TryParse(parts[2], out int d))
                birthDate = pc.ToDateTime(y, m, d, 0, 0, 0, 0);
        }

        var customer = new Customer
        {
            PhoneNumber = command.PhoneNumber,
            Name = command.Name,
            Gender = command.Gender,
            BirthYear = command.BirthYear,
            BirthDate = birthDate,
            SaveDate = DateTime.Now,
            SaveUserId = userId,
            SaveType = 1,
            IsTester = command.IsTester
        };

        dbContext.Customer.Add(customer);
        await dbContext.SaveChangesAsync();
        return (true, "مشتری با موفقیت ثبت شد");
    }

    public async Task<ImportCustomersResult> ImportCustomers(ImportCustomersCommand command, string userId)
    {
        var result = new ImportCustomersResult();
        var validNumbers = new List<string>();
        var invalidNumbers = new List<string>();
        var duplicateNumbers = new List<string>();

        foreach (var phone in command.PhoneNumbers)
        {
            if (!IsValidPhoneNumber(phone))
                invalidNumbers.Add(phone);
            else
                validNumbers.Add(phone);
        }

        var distinctValid = validNumbers.Distinct().ToList();
        var internalDuplicates = validNumbers.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var dup in internalDuplicates)
            if (!duplicateNumbers.Contains(dup))
                duplicateNumbers.Add(dup);

        var existingNumbers = await dbContext.Customer
            .Where(c => distinctValid.Contains(c.PhoneNumber))
            .Select(c => c.PhoneNumber)
            .ToListAsync();
        
        foreach (var existing in existingNumbers)
            if (!duplicateNumbers.Contains(existing))
                duplicateNumbers.Add(existing);

        result.InvalidNumbers = invalidNumbers;
        result.DuplicateNumbers = duplicateNumbers;

        bool hasIssues = invalidNumbers.Count > 0 || duplicateNumbers.Count > 0;

        if (hasIssues && !command.IgnoreInvalid)
        {
            result.Success = false;
            result.Message = "اعداد نامعتبر یا تکراری وجود دارد. برای ادامه تأیید کنید.";
            return result;
        }

        var toInsert = distinctValid.Except(existingNumbers).ToList();
        
        DateTime saveDate = FromPersianDate(command.SaveDate) ?? DateTime.Now;
        
        foreach (var phone in toInsert)
        {
            dbContext.Customer.Add(new Customer
            {
                PhoneNumber = phone,
                SaveDate = saveDate,
                SaveUserId = userId,
                SaveType = 2
            });
        }

        var duplicateEntities = await dbContext.Customer
            .Where(c => existingNumbers.Contains(c.PhoneNumber))
            .ToListAsync();
        
        foreach (var entity in duplicateEntities)
            entity.LastUsageDate = DateTime.Now;

        await dbContext.SaveChangesAsync();
        
        result.Success = true;
        result.InsertedCount = toInsert.Count;
        result.Message = $"{toInsert.Count} شماره با موفقیت ثبت شد";
        return result;
    }

    public async Task<List<Customer>> GetCustomersByDateRange(DateTime from, DateTime to)
    {
        return await dbContext.Customer
            .Where(c => c.SaveDate >= from && c.SaveDate <= to)
            .OrderBy(c => c.PhoneNumber)
            .ToListAsync();
    }

    private static bool IsValidPhoneNumber(string phone)
    {
        return !string.IsNullOrEmpty(phone) 
            && phone.Length == 10 
            && phone.All(char.IsDigit) 
            && phone.StartsWith("9");
    }

    /// <summary>Converts a Persian date string (yyyy/MM/dd) to UTC DateTime. Returns null on failure.</summary>
    private static DateTime? FromPersianDate(string? persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate)) return null;
        var parts = persianDate.Split('/');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out int y) &&
            int.TryParse(parts[1], out int m) &&
            int.TryParse(parts[2], out int d))
        {
            try { return new System.Globalization.PersianCalendar().ToDateTime(y, m, d, 0, 0, 0, 0); }
            catch { return null; }
        }
        return null;
    }
}
