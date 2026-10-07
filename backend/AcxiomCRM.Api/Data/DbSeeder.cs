using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Data;

/// <summary>
/// Demo data. Runs on start up: roles are always ensured, the rest only when there are no users yet.
///
/// Demo logins (password policy compliant):
///   admin@acxiomcrm.com     Admin@123    (Admin)
///   manager@acxiomcrm.com   Manager@123  (Manager - team: sales1, sales2)
///   manager2@acxiomcrm.com  Manager@123  (Manager - team: sales3)
///   sales1@acxiomcrm.com    Sales@123    (Sales Executive)
///   sales2@acxiomcrm.com    Sales@123    (Sales Executive)
///   sales3@acxiomcrm.com    Sales@123    (Sales Executive)
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        await SeedRolesAsync(roleManager);

        if (await db.Users.AnyAsync()) return;

        logger.LogInformation("Empty database found, adding demo data...");
        await SeedDemoDataAsync(db, userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            new ApplicationRole(Roles.Admin, "Full administration: users, roles, audit logs, all CRM records and reports"),
            new ApplicationRole(Roles.Manager, "Manages team customers, leads, opportunities, follow-ups and team reports"),
            new ApplicationRole(Roles.SalesExecutive, "Works on assigned customers, leads, opportunities and follow-ups"),
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    // ---------- date helpers (all relative to today, so the demo always looks current) ----------

    // A moment "days" days ago at the given local hour, stored as UTC
    private static DateTime DaysAgo(int days, int hour = 11) =>
        DateHelper.LocalDateToUtc(DateHelper.Today().AddDays(-days)).AddHours(hour);

    // positive = future, negative = past
    private static DateOnly DateFromToday(int days) => DateHelper.Today().AddDays(days);

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string name, string email,
        string password, string role, int? managerId, int lastLoginDaysAgo)
    {
        var user = new ApplicationUser
        {
            FullName = name,
            Email = email,
            UserName = email,
            ManagerId = managerId,
            IsActive = true,
            LockoutEnabled = true,
            EmailConfirmed = true,
            LastLoginAt = DaysAgo(lastLoginDaysAgo, 10),
            CreatedDate = DaysAgo(200),
        };

        // Identity hashes the password - plain text is never stored
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        // ---------- Users ----------
        var admin = await CreateUserAsync(userManager, "Rahul Verma", "admin@acxiomcrm.com", "Admin@123", Roles.Admin, null, 0);
        var manager = await CreateUserAsync(userManager, "Priya Sharma", "manager@acxiomcrm.com", "Manager@123", Roles.Manager, null, 0);
        var manager2 = await CreateUserAsync(userManager, "Vikram Singh", "manager2@acxiomcrm.com", "Manager@123", Roles.Manager, null, 2);
        var sales1 = await CreateUserAsync(userManager, "Amit Kumar", "sales1@acxiomcrm.com", "Sales@123", Roles.SalesExecutive, manager.Id, 0);
        var sales2 = await CreateUserAsync(userManager, "Sneha Reddy", "sales2@acxiomcrm.com", "Sales@123", Roles.SalesExecutive, manager.Id, 1);
        var sales3 = await CreateUserAsync(userManager, "Kavya Nair", "sales3@acxiomcrm.com", "Sales@123", Roles.SalesExecutive, manager2.Id, 3);

        // ---------- Customers ----------
        // (name, company, city, state, status, owner, created days ago)
        var customerData = new (string Name, string Company, string City, string State, string Status, ApplicationUser Owner, int CreatedDays)[]
        {
            ("Rajesh Gupta", "Gupta Textiles", "Surat", "Gujarat", "Active", sales1, 170),
            ("Anita Desai", "Desai Pharma", "Ahmedabad", "Gujarat", "Active", sales1, 160),
            ("Suresh Iyer", "Iyer Logistics", "Chennai", "Tamil Nadu", "Active", sales2, 150),
            ("Meena Pillai", "Coastal Foods", "Kochi", "Kerala", "Active", sales3, 140),
            ("Karan Malhotra", "Malhotra Motors", "New Delhi", "Delhi", "Active", sales1, 130),
            ("Deepa Joshi", "Joshi Electronics", "Pune", "Maharashtra", "Active", sales2, 120),
            ("Arvind Rao", "Rao Constructions", "Hyderabad", "Telangana", "Active", sales1, 110),
            ("Neha Kapoor", "Kapoor Fashion House", "Mumbai", "Maharashtra", "Prospect", sales2, 100),
            ("Sanjay Patel", "Patel Agro Industries", "Vadodara", "Gujarat", "Active", sales3, 95),
            ("Lakshmi Menon", "Menon Healthcare", "Thiruvananthapuram", "Kerala", "Active", sales3, 90),
            ("Vivek Choudhary", "Choudhary Steel", "Jaipur", "Rajasthan", "Inactive", sales1, 85),
            ("Pooja Bansal", "Bansal Retail", "Chandigarh", "Punjab", "Active", sales2, 75),
            ("Manoj Tiwari", "Tiwari Packaging", "Lucknow", "Uttar Pradesh", "Active", manager, 70),
            ("Divya Krishnan", "Krishnan Software Labs", "Bengaluru", "Karnataka", "Active", sales1, 60),
            ("Harish Naidu", "Naidu Hotels", "Visakhapatnam", "Andhra Pradesh", "Prospect", sales2, 50),
            ("Ritu Agarwal", "Agarwal Jewellers", "Kolkata", "West Bengal", "Active", sales3, 40),
            ("Nikhil Saxena", "Saxena Furniture", "Indore", "Madhya Pradesh", "Active", sales1, 30),
            ("Swati Kulkarni", "Kulkarni Infotech", "Nagpur", "Maharashtra", "Prospect", sales2, 20),
            ("Gaurav Sethi", "Sethi Exports", "Ludhiana", "Punjab", "Active", manager, 10),
            ("Farhan Qureshi", "Qureshi Travels", "Bhopal", "Madhya Pradesh", "Active", sales3, 5),
        };

        var customers = new List<Customer>();
        for (var i = 0; i < customerData.Length; i++)
        {
            var c = customerData[i];
            var nameParts = c.Name.ToLower().Split(' ');
            var domain = new string(c.Company.ToLower().Where(char.IsLetter).ToArray());
            if (domain.Length > 14) domain = domain[..14];
            var created = DaysAgo(c.CreatedDays);

            customers.Add(new Customer
            {
                CustomerName = c.Name,
                Email = $"{nameParts[0]}.{nameParts[1]}@{domain}.in",
                Phone = $"98765{(10001 + i).ToString()[^5..]}",
                CompanyName = c.Company,
                Address = $"{12 + i * 3}, Main Road",
                City = c.City,
                State = c.State,
                Status = c.Status,
                AssignedTo = c.Owner.Id,
                CreatedBy = c.Owner.Id,
                CreatedDate = created,
                ModifiedDate = created,
            });
        }
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();
        foreach (var customer in customers) customer.CustomerCode = $"CUS-{customer.CustomerId:D5}";
        await db.SaveChangesAsync();

        // ---------- Leads ----------
        // The first 5 converted leads match customers 0-4
        // (name, company, source, status, priority, expected value, owner, created days ago, converted customer index)
        var leadData = new (string Name, string Company, string Source, string Status, string Priority, decimal Value, ApplicationUser Owner, int CreatedDays, int? CustomerIndex)[]
        {
            ("Rajesh Gupta", "Gupta Textiles", "Referral", "Converted", "High", 450000, sales1, 175, 0),
            ("Anita Desai", "Desai Pharma", "Website", "Converted", "High", 600000, sales1, 165, 1),
            ("Suresh Iyer", "Iyer Logistics", "Event", "Converted", "Medium", 300000, sales2, 155, 2),
            ("Meena Pillai", "Coastal Foods", "Social Media", "Converted", "Medium", 250000, sales3, 145, 3),
            ("Karan Malhotra", "Malhotra Motors", "Cold Call", "Converted", "High", 800000, sales1, 135, 4),
            ("Rohit Bhatia", "Bhatia Enterprises", "Website", "New", "Medium", 150000, sales1, 3, null),
            ("Shalini Mishra", "Mishra Consultants", "Email Campaign", "New", "Low", 80000, sales2, 2, null),
            ("Aditya Jain", "Jain Hardware", "Referral", "New", "High", 500000, sales3, 1, null),
            ("Preeti Yadav", "Yadav Dairy", "Social Media", "New", "Medium", 120000, sales2, 6, null),
            ("Sunil Shetty", "Shetty Caterers", "Event", "Contacted", "Medium", 200000, sales1, 12, null),
            ("Kiran Bedi", "Bedi Security Services", "Cold Call", "Contacted", "Low", 90000, sales2, 15, null),
            ("Mohan Das", "Das Printers", "Website", "Contacted", "Medium", 175000, sales3, 18, null),
            ("Ayesha Khan", "Khan Interiors", "Referral", "Qualified", "High", 650000, sales1, 22, null),
            ("Ramesh Pandey", "Pandey Pharmaceuticals", "Website", "Qualified", "High", 900000, sales2, 25, null),
            ("Geeta Bhatt", "Bhatt Organics", "Email Campaign", "Qualified", "Medium", 350000, sales3, 28, null),
            ("Tarun Chawla", "Chawla Auto Parts", "Cold Call", "Unqualified", "Low", 60000, sales1, 40, null),
            ("Nandini Rao", "Rao Boutique", "Social Media", "Unqualified", "Low", 50000, sales2, 45, null),
            ("Ajay Thakur", "Thakur Cement", "Event", "Lost", "Medium", 400000, sales1, 60, null),
            ("Sunita Ghosh", "Ghosh Publications", "Website", "Lost", "Low", 70000, sales3, 70, null),
            ("Imran Sheikh", "Sheikh Leather Works", "Referral", "Lost", "Medium", 220000, sales2, 80, null),
            ("Vandana Sinha", "Sinha Education", "Email Campaign", "New", "Medium", 130000, manager, 4, null),
            ("Prakash Hegde", "Hegde Coffee Estates", "Other", "Contacted", "High", 550000, sales3, 9, null),
            ("Ankita Dubey", "Dubey Clinics", "Website", "Qualified", "Medium", 280000, sales1, 14, null),
            ("Manish Arora", "Arora Solar", "Cold Call", "New", "High", 750000, sales2, 0, null),
            ("Rekha Nambiar", "Nambiar Spices", "Event", "Contacted", "Medium", 190000, manager, 7, null),
        };

        var leads = new List<Lead>();
        for (var i = 0; i < leadData.Length; i++)
        {
            var l = leadData[i];
            var customer = l.CustomerIndex.HasValue ? customers[l.CustomerIndex.Value] : null;
            var nameParts = l.Name.ToLower().Split(' ');
            var created = DaysAgo(l.CreatedDays);

            leads.Add(new Lead
            {
                LeadName = l.Name,
                Email = customer?.Email ?? $"{nameParts[0]}.{nameParts[1]}@example.in",
                Phone = customer?.Phone ?? $"91234{(10001 + i).ToString()[^5..]}",
                CompanyName = l.Company,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.Value,
                AssignedTo = l.Owner.Id,
                CreatedBy = l.Owner.Id,
                ConvertedCustomerId = customer?.CustomerId,
                ConvertedDate = customer != null ? DaysAgo(l.CreatedDays - 5) : null,
                CreatedDate = created,
                ModifiedDate = created,
            });
        }
        db.Leads.AddRange(leads);
        await db.SaveChangesAsync();
        foreach (var lead in leads) lead.LeadCode = $"LEAD-{lead.LeadId:D5}";
        await db.SaveChangesAsync();

        // ---------- Opportunities ----------
        // (name, customer index, lead index, amount, stage, probability, close offset days, created days ago, closed days ago)
        var oppData = new (string Name, int CustomerIndex, int? LeadIndex, decimal Amount, string Stage, int Probability, int CloseOffset, int CreatedDays, int? ClosedDays)[]
        {
            // Won - spread over the last 6 months for the monthly sales chart
            ("Gupta Textiles - ERP Rollout", 0, 0, 420000, "Won", 100, -150, 170, 150),
            ("Desai Pharma - CRM Licenses", 1, 1, 580000, "Won", 100, -125, 160, 125),
            ("Iyer Logistics - Fleet Tracking", 2, 2, 310000, "Won", 100, -95, 150, 95),
            ("Malhotra Motors - Dealer Portal", 4, 4, 760000, "Won", 100, -65, 130, 65),
            ("Joshi Electronics - Annual Support", 5, null, 180000, "Won", 100, -35, 115, 35),
            ("Patel Agro - Inventory System", 8, null, 260000, "Won", 100, -12, 90, 12),
            ("Tiwari Packaging - Website Revamp", 12, null, 95000, "Won", 100, -3, 60, 3),
            // Lost
            ("Coastal Foods - POS Upgrade", 3, 3, 220000, "Lost", 0, -110, 140, 110),
            ("Choudhary Steel - Analytics Suite", 10, null, 450000, "Lost", 0, -55, 85, 55),
            ("Bansal Retail - Loyalty App", 11, null, 150000, "Lost", 0, -20, 70, 20),
            // Open
            ("Rao Constructions - Project Tracker", 6, null, 520000, "Negotiation", 75, 15, 100, null),
            ("Kapoor Fashion - E-commerce Store", 7, null, 340000, "Proposal", 50, 25, 95, null),
            ("Menon Healthcare - Patient Portal", 9, null, 680000, "Negotiation", 80, 10, 85, null),
            ("Krishnan Software - Cloud Migration", 13, null, 900000, "Proposal", 40, 40, 55, null),
            ("Naidu Hotels - Booking Engine", 14, null, 410000, "Qualification", 20, 60, 45, null),
            ("Agarwal Jewellers - Billing System", 15, null, 230000, "Qualification", 25, 45, 35, null),
            ("Saxena Furniture - Online Catalogue", 16, null, 160000, "Proposal", 55, 20, 25, null),
            ("Kulkarni Infotech - HR Module", 17, null, 275000, "Qualification", 15, 75, 15, null),
            ("Sethi Exports - Shipment Dashboard", 18, null, 380000, "Negotiation", 70, 12, 8, null),
            ("Qureshi Travels - Mobile App", 19, null, 290000, "Qualification", 30, 50, 4, null),
        };

        var opportunities = new List<Opportunity>();
        foreach (var o in oppData)
        {
            var customer = customers[o.CustomerIndex];
            var created = DaysAgo(o.CreatedDays);
            var opportunity = new Opportunity
            {
                OpportunityName = o.Name,
                CustomerId = customer.CustomerId,
                LeadId = o.LeadIndex.HasValue ? leads[o.LeadIndex.Value].LeadId : null,
                Amount = o.Amount,
                Probability = o.Probability,
                ExpectedCloseDate = DateFromToday(o.CloseOffset),
                Source = o.LeadIndex.HasValue ? leads[o.LeadIndex.Value].Source : "Referral",
                AssignedTo = customer.AssignedTo,
                CreatedBy = customer.AssignedTo,
                // closed date must be set before SetStage so it is not replaced with "now"
                ClosedDate = o.ClosedDays.HasValue ? DaysAgo(o.ClosedDays.Value, 16) : null,
                CreatedDate = created,
                ModifiedDate = created,
            };
            opportunity.SetStage(o.Stage);
            opportunities.Add(opportunity);
        }
        db.Opportunities.AddRange(opportunities);
        await db.SaveChangesAsync();

        // ---------- Follow-ups ----------
        // (subject, type, date offset, status, customer index, lead index, opportunity index, owner)
        var followUpData = new (string Subject, string Type, int Offset, string Status, int? CustomerIndex, int? LeadIndex, int? OppIndex, ApplicationUser Owner)[]
        {
            ("Demo of project tracker", "Meeting", 1, "Planned", 6, null, 10, sales1),
            ("Send revised proposal", "Email", 2, "Planned", 7, null, 11, sales2),
            ("Discuss contract terms", "Call", 0, "Planned", 9, null, 12, sales3),
            ("Cloud migration scoping call", "Call", 3, "Planned", 13, null, 13, sales1),
            ("Introductory call", "Call", 1, "Planned", null, 5, null, sales1),
            ("Share product brochure", "Email", 4, "Planned", null, 6, null, sales2),
            ("Site visit for requirement study", "Visit", 6, "Planned", null, 7, null, sales3),
            ("Pricing discussion", "Meeting", 5, "Planned", 18, null, 18, manager),
            ("Qualification call", "Call", 2, "Planned", null, 22, null, sales1),
            ("Follow up on booking engine", "Call", 9, "Planned", 14, null, 14, sales2),
            ("Check proposal feedback", "Call", -2, "Planned", 16, null, 16, sales1), // overdue
            ("Collect requirement document", "Email", -1, "Planned", null, 11, null, sales3), // overdue
            ("Kick-off meeting", "Meeting", -150, "Completed", 0, null, 0, sales1),
            ("Contract signing", "Visit", -126, "Completed", 1, null, 1, sales1),
            ("Implementation review", "Meeting", -60, "Completed", 4, null, 3, sales1),
            ("Support renewal reminder", "Call", -36, "Completed", 5, null, 4, sales2),
            ("Requirement clarification", "Call", -15, "Completed", null, 9, null, sales1),
            ("Product demo", "Meeting", -30, "Missed", 15, null, 15, sales3),
            ("Budget approval check", "Call", -8, "Missed", null, 13, null, sales2),
            ("Quarterly business review", "Meeting", -20, "Cancelled", 12, null, null, manager),
        };

        foreach (var f in followUpData)
        {
            var created = DaysAgo(Math.Max(-f.Offset, 0) + 3);
            db.FollowUps.Add(new FollowUp
            {
                Subject = f.Subject,
                FollowUpType = f.Type,
                FollowUpDate = DateFromToday(f.Offset),
                Status = f.Status,
                Remarks = f.Status == "Completed" ? "Discussion completed, next steps agreed." : null,
                CustomerId = f.CustomerIndex.HasValue ? customers[f.CustomerIndex.Value].CustomerId : null,
                LeadId = f.LeadIndex.HasValue ? leads[f.LeadIndex.Value].LeadId : null,
                OpportunityId = f.OppIndex.HasValue ? opportunities[f.OppIndex.Value].OpportunityId : null,
                AssignedTo = f.Owner.Id,
                CreatedBy = f.Owner.Id,
                CompletedDate = f.Status == "Completed" ? DaysAgo(-f.Offset, 17) : null,
                CreatedDate = created,
                ModifiedDate = created,
            });
        }

        // ---------- Activities ----------
        // (type, subject, days ago (negative = future), status, customer index, lead index, owner)
        var activityData = new (string Type, string Subject, int Days, string Status, int? CustomerIndex, int? LeadIndex, ApplicationUser Owner)[]
        {
            ("Call", "Intro call with Rajesh", 172, "Completed", 0, null, sales1),
            ("Meeting", "Requirement workshop at Desai Pharma", 158, "Completed", 1, null, sales1),
            ("Email", "Sent fleet tracking proposal", 148, "Completed", 2, null, sales2),
            ("Task", "Prepare POS upgrade quote", 138, "Completed", 3, null, sales3),
            ("Meeting", "Dealer portal demo", 128, "Completed", 4, null, sales1),
            ("Call", "Support renewal discussion", 40, "Completed", 5, null, sales2),
            ("Email", "Shared project tracker brochure", 30, "Completed", 6, null, sales1),
            ("Task", "Draft e-commerce proposal", 25, "Completed", 7, null, sales2),
            ("Meeting", "Patient portal walkthrough", 14, "Completed", 9, null, sales3),
            ("Call", "Negotiation call with Vivek", 56, "Cancelled", 10, null, sales1),
            ("Email", "Welcome mail to Rohit Bhatia", 2, "Completed", null, 5, sales1),
            ("Call", "Discovery call with Sunil", 10, "Completed", null, 9, sales1),
            ("Meeting", "Office visit - Pandey Pharmaceuticals", 20, "Completed", null, 13, sales2),
            ("Task", "Research Bhatt Organics", 26, "Completed", null, 14, sales3),
            ("Call", "Check-in with Ayesha Khan", 5, "Completed", null, 12, sales1),
            ("Meeting", "Pricing meeting with Sethi Exports", -5, "Planned", 18, null, manager),
            ("Task", "Prepare cloud migration estimate", -2, "Planned", 13, null, sales1),
            ("Call", "Follow-up call with Manish Arora", -1, "Planned", null, 23, sales2),
            ("Email", "Send HR module brochure", -3, "Planned", 17, null, sales2),
            ("Meeting", "Mobile app requirement meeting", -4, "Planned", 19, null, sales3),
        };

        foreach (var a in activityData)
        {
            var created = DaysAgo(Math.Max(a.Days, 0) + 1);
            db.Activities.Add(new Activity
            {
                ActivityType = a.Type,
                Subject = a.Subject,
                ActivityDate = DaysAgo(a.Days, 12),
                Status = a.Status,
                CustomerId = a.CustomerIndex.HasValue ? customers[a.CustomerIndex.Value].CustomerId : null,
                LeadId = a.LeadIndex.HasValue ? leads[a.LeadIndex.Value].LeadId : null,
                AssignedTo = a.Owner.Id,
                CreatedBy = a.Owner.Id,
                CreatedDate = created,
                ModifiedDate = created,
            });
        }

        // ---------- A few audit entries so the audit screen is not empty ----------
        var auditData = new (ApplicationUser User, string Action, string Entity, int RecordId, string Result, string Details, int Days)[]
        {
            (admin, AuditActions.Login, "Auth", admin.Id, "Success", "User logged in successfully", 1),
            (manager, AuditActions.Login, "Auth", manager.Id, "Success", "User logged in successfully", 1),
            (sales1, AuditActions.LoginFailed, "Auth", sales1.Id, "Failure", "Invalid password (attempt 1 of 5)", 1),
            (sales1, AuditActions.Login, "Auth", sales1.Id, "Success", "User logged in successfully", 1),
            (sales1, AuditActions.Create, "Customer", customers[16].CustomerId, "Success", "Customer created", 30),
            (sales2, AuditActions.Create, "Lead", leads[23].LeadId, "Success", "Lead created", 0),
            (sales3, AuditActions.StatusChange, "Lead", leads[11].LeadId, "Success", "Status changed from New to Contacted", 2),
            (admin, AuditActions.Create, "User", sales3.Id, "Success", "User Kavya Nair created", 200),
            (sales2, AuditActions.Login, "Auth", sales2.Id, "Success", "User logged in successfully", 0),
        };

        foreach (var entry in auditData)
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId = entry.User.Id,
                UserName = entry.User.FullName,
                Action = entry.Action,
                EntityName = entry.Entity,
                RecordId = entry.RecordId.ToString(),
                Result = entry.Result,
                Details = entry.Details,
                IpAddress = "127.0.0.1",
                CreatedDate = DaysAgo(entry.Days, 9),
            });
        }

        await db.SaveChangesAsync();
    }
}
