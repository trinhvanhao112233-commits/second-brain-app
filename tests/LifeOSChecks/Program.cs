using System.Text.Json;
using System.Text.Json.Nodes;
using LifeOS;
using Microsoft.AspNetCore.DataProtection;

var count = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; Console.WriteLine("PASS " + message); }
void Reject(Action action, string message) { try { action(); } catch (ApiError) { Check(true, message); return; } throw new Exception(message); }
var key = BankRules.NewKey(); var hash = BankRules.Hash(key);
Check(key.Length == 64 && key != hash, "Webhook key is random and stored as a hash");
Check(BankRules.Authenticate("Apikey " + key, hash), "Configured key authenticates");
Check(!BankRules.Authenticate("Bearer " + key, hash) && !BankRules.Authenticate("Apikey " + BankRules.NewKey(), hash) && !BankRules.Authenticate(null, hash), "Session tokens and wrong/missing keys cannot authenticate webhook");
var p = new SePayPayload(31, "VietinBank", "1234567890", "out", 45000, "2026-11-09 00:30:00", "SEVQR an trua", "REF-31");
var e = BankRules.Validate(p, "1234567890");
Check(e.Amount == -45000 && BankRules.Validate(p with { TransferType = "in" }, "1234567890").Amount == 45000, "In/out have correct signs");
Check(e.OccurredAt == new DateTime(2026,11,8,17,30,0,DateTimeKind.Utc), "Vietnam bank time crosses midnight to UTC correctly");
Check(e.Hash == BankRules.Validate(p with { TransferAmount = 45000.00m }, "1234567890").Hash, "Equivalent number formatting is idempotent");
Check(e.Hash != BankRules.Validate(p with { TransferAmount = 50000 }, "1234567890").Hash, "Changed amount has a different financial fingerprint");
foreach (var invalid in new[] { p with { Id = 0 }, p with { Id = -1 }, p with { TransferAmount = 0 }, p with { TransferAmount = -1 }, p with { TransferAmount = 1.5m }, p with { TransferAmount = 9007199254740992m }, p with { Gateway = "MBBank" }, p with { AccountNumber = "9999999999" }, p with { TransferType = "other" }, p with { TransactionDate = "2026-02-30 12:00:00" }, p with { Content = null } }) Reject(() => BankRules.Validate(invalid, "1234567890"), "Malformed/mismatched transaction rejected");
try { JsonSerializer.Deserialize<SePayPayload>("{\"transferAmount\":1}"); throw new Exception("Missing ID accepted"); } catch (JsonException) { Check(true, "Missing ID is not confused with SePay test ID 0"); }
var initial = StateRules.Empty();
var first = StateRules.Merge(initial, [e]); var retry = StateRules.Merge(first, [e]);
Check(((JsonArray)retry["transactions"]!).Count == 1, "Repeated ledger projection creates exactly one transaction");
var tx = retry["transactions"]![0]!;
Check(tx["date"]!.ToString() == "2026-11-09", "Display date is Vietnam date, not previous UTC date");
tx["amount"] = 1; tx["type"] = "income"; tx["category"] = "food"; tx["note"] = "Ghi chú riêng";
var canonical = StateRules.Merge(retry, [e]);
Check(canonical["transactions"]![0]!["amount"]!.GetValue<decimal>() == 45000 && canonical["transactions"]![0]!["type"]!.ToString() == "expense", "Client cannot change bank amount or direction");
Check(canonical["transactions"]![0]!["category"]!.ToString() == "food" && canonical["transactions"]![0]!["note"]!.ToString() == "Ghi chú riêng", "User categories/notes survive refresh");
canonical["transactions"] = new JsonArray();
Check(((JsonArray)StateRules.Merge(canonical, [e])["transactions"]!).Count == 0, "Removing a displayed bank entry survives replay");
var nextEvent = BankRules.Validate(p with { Id = 32, TransferType = "in" }, "1234567890");
var later = StateRules.Merge(canonical, [e,nextEvent]);
Check(((JsonArray)later["transactions"]!).Count == 1 && later["transactions"]![0]!["bankTransactionId"]!.GetValue<long>() == 32, "Events received while closed appear without restoring deleted entries");
var invalidState = StateRules.Empty(); invalidState["notes"] = new JsonArray(new JsonObject { ["id"] = "same" }, new JsonObject { ["id"] = "same" });
Reject(() => StateRules.Validate(invalidState), "Duplicate item IDs rejected");
var tokens = new SessionTokens(new EphemeralDataProtectionProvider());
var user = new AppUser(Guid.NewGuid(), "alice", "Alice", "hash", 4); var session = tokens.Issue(user);
Check(tokens.Read(session).Id == user.Id && tokens.Read(session).Version == 4, "Session binds owner and password version");
Reject(() => tokens.Read(session[..^8] + "tampered"), "Tampered session rejected");
Console.WriteLine($"{count} checks passed. No database or production financial data used.");
