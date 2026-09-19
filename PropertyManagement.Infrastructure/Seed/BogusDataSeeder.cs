using Bogus;
using Microsoft.AspNetCore.Identity;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Seed;

/// <summary>Generates the bulk of the demo data (extra managers/applicants, properties, units,
/// and applications in every status) with Bogus for .NET. Uses a fixed randomizer seed so a
/// fresh database always gets the same reviewable data set.</summary>
internal static class BogusDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ApplicationUser manager1,
        ApplicationUser manager2,
        ApplicationUser applicant1,
        ApplicationUser applicant2,
        List<UnitType> unitTypes)
    {
        Randomizer.Seed = new Random(8675309);
        var faker = new Faker("en_US");
        var activeTypes = unitTypes.Where(t => t.IsActive).ToList();
        var now = DateTime.UtcNow;

        var extraManagers = await CreateUsersAsync(userManager, faker, Roles.PropertyManager, count: 3, prefix: "mgr");
        var extraApplicants = await CreateUsersAsync(userManager, faker, Roles.Applicant, count: 8, prefix: "app");

        var manager1Properties = CreateProperties(db, faker, manager1.Id, activeTypes, count: 2);
        var manager2Properties = CreateProperties(db, faker, manager2.Id, activeTypes, count: 2);
        foreach (var mgr in extraManagers)
        {
            CreateProperties(db, faker, mgr.Id, activeTypes, count: faker.Random.Int(1, 2));
        }

        await db.SaveChangesAsync();

        var allUnits = manager1Properties.Concat(manager2Properties).SelectMany(p => p.Units).ToList();
        var allApplicants = new List<ApplicationUser> { applicant1, applicant2 }.Concat(extraApplicants).ToList();
        var unitQueue = new Queue<Unit>(allUnits.OrderBy(_ => faker.Random.Int()));

        // One application per required status, using the fixed demo applicants first so the
        // video walkthrough logs in as applicant1/applicant2 and immediately sees every state.
        CreateApplication(db, faker, NextUnit(), [applicant1.Id], ApplicationStatus.Draft, manager1.Id, now);
        CreateApplication(db, faker, NextUnit(), [applicant2.Id], ApplicationStatus.Submitted, manager1.Id, now);
        CreateApplication(db, faker, NextUnit(), [applicant1.Id], ApplicationStatus.Returned, manager2.Id, now);
        CreateApplication(db, faker, NextUnit(), [applicant2.Id], ApplicationStatus.Approved, manager2.Id, now);
        CreateApplication(db, faker, NextUnit(), [applicant1.Id], ApplicationStatus.Denied, manager1.Id, now);
        CreateApplication(db, faker, NextUnit(), [applicant2.Id], ApplicationStatus.Withdrawn, manager1.Id, now);
        // Review-queue demo (bonus 2): one application claimed by each fixed manager, so either
        // manager sees a claim of their own (Release) and one held by the other (read-only).
        CreateApplication(db, faker, NextUnit(), [applicant1.Id], ApplicationStatus.UnderReview, manager1.Id, now);
        CreateApplication(db, faker, NextUnit(), [applicant2.Id], ApplicationStatus.UnderReview, manager2.Id, now);
        // Co-applicant demo (bonus 5): both fixed applicants on one Submitted application.
        CreateApplication(db, faker, NextUnit(), [applicant1.Id, applicant2.Id], ApplicationStatus.Submitted, manager2.Id, now);

        // A handful more with the Bogus-generated applicants for a less sparse demo list.
        var statuses = new[] { ApplicationStatus.Draft, ApplicationStatus.Submitted, ApplicationStatus.Approved, ApplicationStatus.Denied };
        foreach (var applicant in allApplicants.Skip(2).Take(6))
        {
            if (unitQueue.Count == 0)
            {
                break;
            }

            var status = faker.PickRandom(statuses);
            var reviewer = faker.PickRandom(manager1.Id, manager2.Id);
            CreateApplication(db, faker, NextUnit(), [applicant.Id], status, reviewer, now);
        }

        await db.SaveChangesAsync();

        Unit NextUnit() => unitQueue.Count > 0 ? unitQueue.Dequeue() : allUnits[faker.Random.Int(0, allUnits.Count - 1)];
    }

    private static async Task<List<ApplicationUser>> CreateUsersAsync(UserManager<ApplicationUser> userManager, Faker faker, string role, int count, string prefix)
    {
        var users = new List<ApplicationUser>();
        for (var i = 0; i < count; i++)
        {
            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();
            var email = $"{prefix}{i}.{firstName}.{lastName}@demo.local".ToLowerInvariant();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await userManager.CreateAsync(user, DbInitializer.DemoPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                users.Add(user);
            }
        }

        return users;
    }

    private static List<Property> CreateProperties(ApplicationDbContext db, Faker faker, string ownerUserId, List<UnitType> activeTypes, int count)
    {
        var properties = new List<Property>();

        for (var i = 0; i < count; i++)
        {
            var property = new Property
            {
                OwnerUserId = ownerUserId,
                Name = $"{faker.Address.StreetName()} {faker.PickRandom("Apartments", "Residences", "Flats", "Court")}",
                AddressLine1 = faker.Address.StreetAddress(),
                City = faker.Address.City(),
                State = faker.Address.StateAbbr(),
                ZipCode = faker.Address.ZipCode()
            };

            var unitCount = faker.Random.Int(3, 6);
            for (var u = 0; u < unitCount; u++)
            {
                var type = faker.PickRandom(activeTypes);
                property.Units.Add(new Unit
                {
                    UnitNumber = $"{faker.Random.Int(1, 9)}{faker.Random.Char('A', 'F')}",
                    Bedrooms = faker.Random.Int(0, 4),
                    RentAmount = faker.Random.Decimal(900, 3500),
                    UnitTypeId = type.Id
                });
            }

            db.Properties.Add(property);
            properties.Add(property);
        }

        return properties;
    }

    private static void CreateApplication(
        ApplicationDbContext db, Faker faker, Unit unit, string[] applicantUserIds,
        ApplicationStatus targetStatus, string reviewerUserId, DateTime now)
    {
        var application = new RentalApplication
        {
            UnitId = unit.Id,
            CreatedAtUtc = now.AddDays(-faker.Random.Int(5, 60)),
            CurrentStep = ApplicationStep.Summary
        };

        foreach (var userId in applicantUserIds)
        {
            application.Applicants.Add(new ApplicationApplicant { UserId = userId, IsPrimary = userId == applicantUserIds[0] });
        }

        application.ApplicantInformation = new ApplicantInformation
        {
            FullLegalName = faker.Name.FullName(),
            PhoneNumber = faker.Phone.PhoneNumber("###-###-####"),
            Email = faker.Internet.Email(),
            CurrentAddress = faker.Address.FullAddress(),
            IsComplete = true
        };

        application.ResidenceHistoryEntries.Add(new ResidenceHistoryEntry
        {
            AddressLine1 = faker.Address.StreetAddress(),
            City = faker.Address.City(),
            State = faker.Address.StateAbbr(),
            ZipCode = faker.Address.ZipCode(),
            LandlordName = faker.Name.FullName(),
            LandlordPhone = faker.Phone.PhoneNumber("###-###-####"),
            MoveInDate = DateOnly.FromDateTime(now.AddYears(-faker.Random.Int(1, 4)))
        });

        var draftAt = application.CreatedAtUtc;
        AddHistory(application, null, ApplicationStatus.Draft, applicantUserIds[0], draftAt, null);

        if (targetStatus == ApplicationStatus.Draft)
        {
            application.Status = ApplicationStatus.Draft;
            db.RentalApplications.Add(application);
            return;
        }

        var submittedAt = draftAt.AddDays(faker.Random.Int(1, 5));
        application.SubmittedAtUtc = submittedAt;
        AddHistory(application, ApplicationStatus.Draft, ApplicationStatus.Submitted, applicantUserIds[0], submittedAt, null);

        if (targetStatus == ApplicationStatus.Withdrawn)
        {
            var withdrawnAt = submittedAt.AddDays(1);
            application.Status = ApplicationStatus.Withdrawn;
            AddHistory(application, ApplicationStatus.Submitted, ApplicationStatus.Withdrawn, applicantUserIds[0], withdrawnAt, null);
            db.RentalApplications.Add(application);
            return;
        }

        if (targetStatus == ApplicationStatus.Submitted)
        {
            application.Status = ApplicationStatus.Submitted;
            db.RentalApplications.Add(application);
            return;
        }

        if (targetStatus == ApplicationStatus.UnderReview)
        {
            var claimedAt = submittedAt.AddDays(1);
            application.Status = ApplicationStatus.UnderReview;
            application.ClaimedByUserId = reviewerUserId;
            application.ClaimedAtUtc = claimedAt;
            AddHistory(application, ApplicationStatus.Submitted, ApplicationStatus.UnderReview, reviewerUserId, claimedAt, null);
            db.RentalApplications.Add(application);
            return;
        }

        var decidedAt = submittedAt.AddDays(faker.Random.Int(1, 4));
        application.DecidedAtUtc = decidedAt;

        switch (targetStatus)
        {
            case ApplicationStatus.Returned:
                application.Status = ApplicationStatus.Returned;
                application.CurrentStep = ApplicationStep.ApplicantInformation;
                AddHistory(application, ApplicationStatus.Submitted, ApplicationStatus.Returned, reviewerUserId, decidedAt, "Please clarify your current employer on the Applicant Information section.");
                break;

            case ApplicationStatus.Denied:
                application.Status = ApplicationStatus.Denied;
                AddHistory(application, ApplicationStatus.Submitted, ApplicationStatus.Denied, reviewerUserId, decidedAt, "Income did not meet the minimum requirement for this unit.");
                break;

            case ApplicationStatus.Approved:
                application.Status = ApplicationStatus.Approved;
                AddHistory(application, ApplicationStatus.Submitted, ApplicationStatus.Approved, reviewerUserId, decidedAt, null);
                var start = DateOnly.FromDateTime(decidedAt);
                application.Lease = new Lease
                {
                    UnitId = unit.Id,
                    StartDate = start,
                    EndDate = start.AddMonths(12).AddDays(-1),
                    MonthlyRent = unit.RentAmount,
                    CreatedAtUtc = decidedAt
                };
                break;
        }

        db.RentalApplications.Add(application);
    }

    private static void AddHistory(RentalApplication application, ApplicationStatus? from, ApplicationStatus to, string changedByUserId, DateTime at, string? comment) =>
        application.StatusHistory.Add(new ApplicationStatusHistoryEntry
        {
            FromStatus = from,
            ToStatus = to,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = at,
            Comment = comment
        });
}
