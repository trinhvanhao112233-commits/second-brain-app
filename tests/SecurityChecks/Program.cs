using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Controllers;
using PersonalFinance.API.Data;
using PersonalFinance.API.Models;
using PersonalFinance.API.Security;

var checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; Console.WriteLine($"PASS {message}"); }
var provider = new EphemeralDataProtectionProvider();
var tokens = new SessionTokens(provider);
var alice = new User { Id = Guid.NewGuid(), PasswordHash = "password-version-one" };
var token = tokens.Issue(alice);
var ticket = tokens.Read(token);
Check(ticket.UserId == alice.Id && ticket.PasswordHash == alice.PasswordHash, "Session binds user and password version");
Check(ticket.ExpiresAt > DateTimeOffset.UtcNow && ticket.ExpiresAt < DateTimeOffset.UtcNow.AddDays(8), "Session has bounded lifetime");
try { tokens.Read(token[..^10] + "tampered00"); throw new Exception("Tampered session accepted"); } catch (System.Security.Cryptography.CryptographicException) { Check(true,"Tampered session rejected"); }
var proposals = new AiProposals(provider);
var proposal = proposals.Read(proposals.Issue(alice.Id, new() { ActionType = "add_task", Title = "Test" }));
Check(proposal.UserId == alice.Id && proposal.Action.Title == "Test", "AI confirmation binds action and owner");
var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
var current = new CurrentUser(accessor);
var options = new DbContextOptionsBuilder<AppDbContext>().UseMySql("Server=127.0.0.1;Database=unused;User=unused;Password=unused", new MySqlServerVersion(new Version(8,0,30))).Options;
using var db = new AppDbContext(options,current);
void Login(Guid id) => accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier,id.ToString()) },"test"));
Login(alice.Id);
foreach (var query in new[] { db.Wallets.ToQueryString(), db.Transactions.ToQueryString(), db.DebtRecords.ToQueryString(), db.Events.ToQueryString(), db.Tasks.ToQueryString(), db.Notes.ToQueryString(), db.PushSubscriptions.ToQueryString() })
    Check(query.Contains(alice.Id.ToString()), "SQL applies authenticated owner filter");
var bob = Guid.NewGuid(); Login(bob);
Check(db.Wallets.ToQueryString().Contains(bob.ToString()) && !db.Wallets.ToQueryString().Contains(alice.Id.ToString()), "Owner filter changes per request, not cached model");
foreach (var type in typeof(AuthController).Assembly.GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(OwnedController)) && x != typeof(AuthController)))
    Check(type.GetCustomAttributes(typeof(AuthorizeAttribute),true).Length > 0, $"{type.Name} requires authentication");
foreach (var entity in db.Model.GetEntityTypes())
    foreach (var property in entity.GetProperties().Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(Guid))) {
        var mapping = property.GetRelationalTypeMapping();
        Check(mapping.GetDataReaderMethod().Name == "GetGuid", $"{entity.ClrType.Name}.{property.Name} reads Guid directly instead of GetString");
    }
Console.WriteLine($"{checks} checks passed. No database connection or production data used.");
