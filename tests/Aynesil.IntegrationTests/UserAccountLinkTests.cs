using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Eğitmen/veli ↔ kullanıcı hesabı bağı (V48) gerçek HTTP hattında, API en az yetkili aynesil_app
/// rolüyle (RLS etkin) bağlıyken. Kurumlar devops/new_tenant.py ile açılır (admin + guardian rolü).
///   - bağla / bağlı kaydı göster / kaldır; aynı kullanıcıyı ikinci eğitmene bağlama 409;
///   - başka kurumun kullanıcısını bağlama 404 (RLS + TenantUserGuard + composite FK);
///   - bağsız kullanıcı seans notu yazamaz (422, yöneticiye yol gösteren kod); bağlandıktan sonra yazar;
///   - veli kaydından portal hesabı açılır, geçici şifreyle giriş yapılır, portalda öğrenci görünür;
///     portal:access içermeyen rolle veli hesabı açılamaz.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserAccountLinkTests : IDisposable
{
    private const string AdminPassword = "Gecici#Sifre1";
    /// <summary>new_tenant.py --dry-run masks the password in its SQL; that literal becomes the admin password.</summary>
    private const string DryRunPassword = "********";
    private static readonly string[] EnvKeys =
        ["ConnectionStrings__DefaultConnection", "ConnectionStrings__Redis", "Jwt__Secret"];

    private readonly MigratedDatabaseFixture _db;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public UserAccountLinkTests(MigratedDatabaseFixture db)
    {
        _db = db;
        var appConnection = new NpgsqlConnectionStringBuilder(db.ConnectionString) { Username = "aynesil_app", Password = "app_it" };
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", appConnection.ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "");
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-test-secret-integration-test-secret-0123456789");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var key in EnvKeys) Environment.SetEnvironmentVariable(key, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>Opens a tenant with devops/new_tenant.py (dry-run SQL executed as owner); returns the corporation id.</summary>
    private async Task<Guid> NewTenantAsync(string code, string adminUsername)
    {
        var root = RepoRoot();
        var psi = new ProcessStartInfo("python3")
        {
            ArgumentList =
            {
                Path.Combine(root, "devops", "new_tenant.py"), "--dry-run", "--code", code, "--name", $"Kurum {code}",
                "--admin-username", adminUsername, "--admin-name", "Yönetici", "--admin-password", AdminPassword,
                "--enable-modules", "all", // kamera akışları yeni kurumda varsayılan kapalı modüle bağlı (V53)
            },
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
        };
        using var process = Process.Start(psi)!;
        var sql = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0, await process.StandardError.ReadToEndAsync());

        var statements = string.Join('\n', sql.Split('\n').Where(l => !l.TrimStart().StartsWith('\\')));
        await using var owner = await _db.OpenAsync();
        await using (var cmd = new NpgsqlCommand(statements, owner)) await cmd.ExecuteNonQueryAsync();
        return await ScalarAsync<Guid>(owner, "select id from core.corporation where code = @c", ("c", code));
    }

    private async Task<Guid> RoleIdAsync(Guid corporationId, string code)
    {
        await using var owner = await _db.OpenAsync();
        return await ScalarAsync<Guid>(owner,
            "select id from iam.role where corporation_id = @c and code = @r", ("c", corporationId), ("r", code));
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(
        HttpMethod method, string url, string? token = null, object? body = null)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Accept-Language", "tr");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password });
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        var token = body.GetProperty("data").GetProperty("accessToken").GetString()!;
        if (!body.GetProperty("data").GetProperty("mustChangePassword").GetBoolean()) return token;

        // Accounts opened by an admin (new_tenant.py, user-account create) start with must_change_password (V50).
        var changed = password + "-x9";
        (await SendAsync(HttpMethod.Post, "/api/auth/change-password", token, new { currentPassword = password, newPassword = changed }))
            .Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        (status, body) = await SendAsync(HttpMethod.Post, "/api/auth/login", body: new { username, password = changed });
        status.Should().Be(HttpStatusCode.OK, body.ToString());
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    /// <summary>Every string value in the body, unescaped (raw JSON escapes non-ASCII characters).</summary>
    private static IEnumerable<string> Strings(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString()!],
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Strings),
        _ => [],
    };

    private static Guid Id(JsonElement body) => body.GetProperty("data").GetProperty("id").GetGuid();

    private async Task<Guid> CreateEducatorAsync(string token, string firstName)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/educators", token, new { firstName, lastName = "Eğitmen" });
        status.Should().Be(HttpStatusCode.Created, body.ToString());
        return Id(body);
    }

    private async Task<Guid> CreateUserAsync(string token, string username)
    {
        var (status, body) = await SendAsync(HttpMethod.Post, "/api/users", token,
            new { username, fullName = "Bağlanacak Kullanıcı", password = "Kullanici#Sifre1", activateImmediately = true });
        status.Should().Be(HttpStatusCode.Created, body.ToString());
        return Id(body);
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Aynesil.sln"))) return dir.FullName;
        throw new InvalidOperationException("Repository root (Aynesil.sln) not found.");
    }

    // ── Educator ↔ user ───────────────────────────────────────────────────────

    [Fact]
    public async Task Link_show_and_unlink_an_existing_user_and_reject_a_second_educator()
    {
        var admin = $"admin-la-{_suffix}@t.test";
        await NewTenantAsync($"la_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        var first = await CreateEducatorAsync(token, "Birinci");
        var second = await CreateEducatorAsync(token, "İkinci");
        var user = await CreateUserAsync(token, $"egitmen-la-{_suffix}@t.test");

        var (linkStatus, linked) = await SendAsync(HttpMethod.Put, $"/api/educators/{first}/user-account", token, new { userId = user });
        linkStatus.Should().Be(HttpStatusCode.OK, linked.ToString());
        linked.GetProperty("data").GetProperty("id").GetGuid().Should().Be(user);

        var (_, account) = await SendAsync(HttpMethod.Get, $"/api/educators/{first}/user-account", token);
        account.GetProperty("data").GetProperty("username").GetString().Should().Be($"egitmen-la-{_suffix}@t.test");

        var (_, byUser) = await SendAsync(HttpMethod.Get, $"/api/educators/by-user/{user}", token);
        byUser.GetProperty("data").GetProperty("id").GetGuid().Should().Be(first);
        byUser.GetProperty("data").GetProperty("fullName").GetString().Should().Be("Birinci Eğitmen");

        // Linking the same user again is idempotent; a second educator is refused.
        (await SendAsync(HttpMethod.Put, $"/api/educators/{first}/user-account", token, new { userId = user })).Status
            .Should().Be(HttpStatusCode.OK);
        var (dupStatus, dup) = await SendAsync(HttpMethod.Put, $"/api/educators/{second}/user-account", token, new { userId = user });
        dupStatus.Should().Be(HttpStatusCode.Conflict, dup.ToString());
        dup.GetProperty("code").GetString().Should().Be("Rule.UserAlreadyLinkedToEducator");

        var (unlinkStatus, unlinked) = await SendAsync(HttpMethod.Delete, $"/api/educators/{first}/user-account", token);
        unlinkStatus.Should().Be(HttpStatusCode.OK, unlinked.ToString());
        unlinked.GetProperty("code").GetString().Should().Be("Success.UserAccountUnlinked");
        var (_, after) = await SendAsync(HttpMethod.Get, $"/api/educators/{first}/user-account", token);
        after.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);

        // Once free, the user can back the other educator.
        (await SendAsync(HttpMethod.Put, $"/api/educators/{second}/user-account", token, new { userId = user })).Status
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_user_of_another_corporation_cannot_be_linked()
    {
        var adminA = $"admin-xa-{_suffix}@t.test";
        var adminB = $"admin-xb-{_suffix}@t.test";
        await NewTenantAsync($"xa_{_suffix}", adminA);
        await NewTenantAsync($"xb_{_suffix}", adminB);
        var tokenA = await LoginAsync(adminA, DryRunPassword);
        var tokenB = await LoginAsync(adminB, DryRunPassword);

        var educatorA = await CreateEducatorAsync(tokenA, "Kurum");
        var userB = await CreateUserAsync(tokenB, $"kullanici-xb-{_suffix}@t.test");

        var (status, body) = await SendAsync(HttpMethod.Put, $"/api/educators/{educatorA}/user-account", tokenA, new { userId = userB });
        status.Should().Be(HttpStatusCode.NotFound, body.ToString());
        body.GetProperty("code").GetString().Should().Be("Rule.UserNotInCorporation");

        await using var owner = await _db.OpenAsync();
        (await ScalarAsync<long>(owner, "select count(*) from educators.educator where id = @e and user_id is not null", ("e", educatorA)))
            .Should().Be(0);
    }

    [Fact]
    public async Task Only_a_linked_educator_can_write_a_session_note()
    {
        var admin = $"admin-sn-{_suffix}@t.test";
        var corp = await NewTenantAsync($"sn_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        Guid session;
        await using (var owner = await _db.OpenAsync())
            session = await ScalarAsync<Guid>(owner, """
                insert into scheduling.session(corporation_id, session_type_id, starts_at, ends_at)
                select @c, v.id, now(), now() + interval '40 minutes'
                  from ref.ref_value v join ref.ref_type t on t.id = v.ref_type_id
                 where t.code = 'session_type' and v.corporation_id is null
                 order by v.sort_order limit 1
                returning id
                """, ("c", corp));

        // The tenant admin has session_note:write but no educator record → 422 that tells the admin what to do.
        var (unlinkedStatus, unlinked) = await SendAsync(HttpMethod.Post, $"/api/scheduling/sessions/{session}/notes", token,
            new { body = "Not", parentVisible = false });
        unlinkedStatus.Should().Be(HttpStatusCode.UnprocessableEntity, unlinked.ToString());
        Strings(unlinked).Should().Contain("Rule.ActingEducatorRequired");
        Strings(unlinked).Should().Contain(m => m.Contains("Kullanıcı hesabı") && m.Contains("yönetici"));

        // New account opened from the educator record, with a role that can write notes.
        var educator = await CreateEducatorAsync(token, "Notçu");
        var (createStatus, created) = await SendAsync(HttpMethod.Post, $"/api/educators/{educator}/user-account", token, new
        {
            username = $"notcu-{_suffix}@t.test", fullName = "Notçu Eğitmen", email = (string?)null, phone = (string?)null,
            roleId = await RoleIdAsync(corp, "admin"), campusId = (Guid?)null,
        });
        createStatus.Should().Be(HttpStatusCode.Created, created.ToString());
        var temporaryPassword = created.GetProperty("data").GetProperty("temporaryPassword").GetString()!;
        temporaryPassword.Should().HaveLength(14);

        var educatorToken = await LoginAsync($"notcu-{_suffix}@t.test", temporaryPassword);
        var (noteStatus, note) = await SendAsync(HttpMethod.Post, $"/api/scheduling/sessions/{session}/notes", educatorToken,
            new { body = "Seans iyi geçti.", parentVisible = true });
        noteStatus.Should().Be(HttpStatusCode.Created, note.ToString());
        note.GetProperty("data").GetProperty("authoredBy").GetGuid().Should().Be(educator);
    }

    // ── Guardian ↔ portal user ────────────────────────────────────────────────

    [Fact]
    public async Task A_guardian_account_opened_from_the_record_logs_in_to_the_portal()
    {
        var admin = $"admin-gp-{_suffix}@t.test";
        var corp = await NewTenantAsync($"gp_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        Guid student;
        await using (var owner = await _db.OpenAsync())
            student = await ScalarAsync<Guid>(owner,
                "insert into students.student(corporation_id, first_name, last_name) values (@c, 'Deniz', 'Portal') returning id", ("c", corp));

        var (gStatus, g) = await SendAsync(HttpMethod.Post, "/api/guardians", token, new { firstName = "Veli", lastName = "Portal" });
        gStatus.Should().Be(HttpStatusCode.Created, g.ToString());
        var guardian = Id(g);
        (await SendAsync(HttpMethod.Post, $"/api/students/{student}/guardians", token, new
        {
            guardianId = guardian, relationshipId = (Guid?)null, isPrimary = true, hasCustody = true,
            portalAccess = false, financialResponsible = true,
        })).Status.Should().Be(HttpStatusCode.Created);
        var (grantStatus, grant) = await SendAsync(HttpMethod.Post, $"/api/guardians/{guardian}/students/{student}/portal-access", token, new
        {
            canViewSessions = true, canViewAttendance = true, canViewReports = true,
            canViewPlan = true, canViewFinance = false, canViewCamera = false,
        });
        grantStatus.Should().Be(HttpStatusCode.OK, grant.ToString());

        var username = $"veli-{_suffix}@t.test";
        object NewAccount(Guid roleId) => new
        {
            username, fullName = "Veli Portal", email = username, phone = (string?)null, roleId, campusId = (Guid?)null,
        };

        // A role without portal:access would give an account that cannot open the portal.
        Guid noPortalRole;
        await using (var owner = await _db.OpenAsync())
            noPortalRole = await ScalarAsync<Guid>(owner,
                "insert into iam.role(corporation_id, code, name) values (@c, 'no_portal', 'Portalsız') returning id", ("c", corp));
        var (badStatus, bad) = await SendAsync(HttpMethod.Post, $"/api/guardians/{guardian}/user-account", token, NewAccount(noPortalRole));
        badStatus.Should().Be(HttpStatusCode.UnprocessableEntity, bad.ToString());
        Strings(bad).Should().Contain("Rule.RoleLacksPermission");

        var (createStatus, created) = await SendAsync(HttpMethod.Post, $"/api/guardians/{guardian}/user-account", token,
            NewAccount(await RoleIdAsync(corp, "guardian")));
        createStatus.Should().Be(HttpStatusCode.Created, created.ToString());
        var temporaryPassword = created.GetProperty("data").GetProperty("temporaryPassword").GetString()!;

        var (_, detail) = await SendAsync(HttpMethod.Get, $"/api/guardians/{guardian}", token);
        detail.GetProperty("data").GetProperty("hasPortalAccount").GetBoolean().Should().BeTrue();

        var parentToken = await LoginAsync(username, temporaryPassword);
        var (portalStatus, portal) = await SendAsync(HttpMethod.Get, "/api/portal/my-students", parentToken);
        portalStatus.Should().Be(HttpStatusCode.OK, portal.ToString());
        portal.GetProperty("data").EnumerateArray().Select(s => s.GetProperty("studentId").GetGuid())
            .Should().ContainSingle().Which.Should().Be(student);

        // The parent account is portal-only: staff screens stay closed.
        (await SendAsync(HttpMethod.Get, "/api/students", parentToken)).Status.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Camera consent is per guardian ────────────────────────────────────────

    [Fact]
    public async Task Withdrawing_the_mothers_camera_consent_stops_her_viewing_even_if_the_fathers_consent_stands()
    {
        var admin = $"admin-cam-{_suffix}@t.test";
        var corp = await NewTenantAsync($"cm_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        Guid student, mother, father, consentType, motherConsent, motherAuth, fatherAuth;
        await using (var owner = await _db.OpenAsync())
        {
            student = await ScalarAsync<Guid>(owner,
                "insert into students.student(corporation_id, first_name, last_name) values (@c, 'Çocuk', 'Kamera') returning id", ("c", corp));
            mother = await ScalarAsync<Guid>(owner,
                "insert into students.guardian(corporation_id, first_name, last_name) values (@c, 'Anne', 'Kamera') returning id", ("c", corp));
            father = await ScalarAsync<Guid>(owner,
                "insert into students.guardian(corporation_id, first_name, last_name) values (@c, 'Baba', 'Kamera') returning id", ("c", corp));
            consentType = await ScalarAsync<Guid>(owner,
                "select id from ref.ref_value where code = 'camera_viewing' and deleted_at is null limit 1");
            Task<Guid> Consent(Guid g) => ScalarAsync<Guid>(owner,
                "insert into legal.student_consent(corporation_id, student_id, guardian_id, consent_type_id, state, granted_at) values (@c, @s, @g, @t, 'granted', now()) returning id",
                ("c", corp), ("s", student), ("g", g), ("t", consentType));
            Task<Guid> Auth(Guid g, Guid consent) => ScalarAsync<Guid>(owner,
                "insert into media.viewing_authorization(corporation_id, guardian_id, student_id, consent_id, valid_from, valid_to) values (@c, @g, @s, @k, now() - interval '1 hour', now() + interval '30 days') returning id",
                ("c", corp), ("g", g), ("s", student), ("k", consent));
            motherConsent = await Consent(mother);
            var fatherConsent = await Consent(father);
            motherAuth = await Auth(mother, motherConsent);
            fatherAuth = await Auth(father, fatherConsent);
        }

        // Both can start while both consents stand.
        (await SendAsync(HttpMethod.Post, "/api/cameras/viewing/start", token, new { authorizationId = motherAuth, sessionId = (Guid?)null, cameraId = (Guid?)null }))
            .Status.Should().Be(HttpStatusCode.OK);

        var (wStatus, w) = await SendAsync(HttpMethod.Post, $"/api/legal/consents/{motherConsent}/withdraw", token);
        wStatus.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // The mother's open authorization is revoked with the reason; the father's is untouched.
        await using (var owner = await _db.OpenAsync())
        {
            (await ScalarAsync<bool>(owner, "select is_revoked from media.viewing_authorization where id = @a", ("a", motherAuth))).Should().BeTrue();
            (await ScalarAsync<string>(owner, "select revoked_reason from media.viewing_authorization where id = @a", ("a", motherAuth))).Should().Be("Rıza geri alındı");
            (await ScalarAsync<bool>(owner, "select is_revoked from media.viewing_authorization where id = @a", ("a", fatherAuth))).Should().BeFalse();
        }

        var (mStatus, m) = await SendAsync(HttpMethod.Post, "/api/cameras/viewing/start", token, new { authorizationId = motherAuth, sessionId = (Guid?)null, cameraId = (Guid?)null });
        mStatus.Should().NotBe(HttpStatusCode.OK, m.ToString());

        (await SendAsync(HttpMethod.Post, "/api/cameras/viewing/start", token, new { authorizationId = fatherAuth, sessionId = (Guid?)null, cameraId = (Guid?)null }))
            .Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Viewing_start_requires_the_authorized_guardians_own_consent_not_a_siblings()
    {
        var admin = $"admin-cm2-{_suffix}@t.test";
        var corp = await NewTenantAsync($"c2_{_suffix}", admin);
        var token = await LoginAsync(admin, DryRunPassword);

        Guid mother, motherAuth;
        await using (var owner = await _db.OpenAsync())
        {
            var student = await ScalarAsync<Guid>(owner,
                "insert into students.student(corporation_id, first_name, last_name) values (@c, 'Çocuk', 'Kamera2') returning id", ("c", corp));
            mother = await ScalarAsync<Guid>(owner,
                "insert into students.guardian(corporation_id, first_name, last_name) values (@c, 'Anne', 'İki') returning id", ("c", corp));
            var father = await ScalarAsync<Guid>(owner,
                "insert into students.guardian(corporation_id, first_name, last_name) values (@c, 'Baba', 'İki') returning id", ("c", corp));
            var type = await ScalarAsync<Guid>(owner, "select id from ref.ref_value where code = 'camera_viewing' and deleted_at is null limit 1");
            // Only the father has a consent; the mother's authorization has none of her own.
            await ScalarAsync<Guid>(owner,
                "insert into legal.student_consent(corporation_id, student_id, guardian_id, consent_type_id, state, granted_at) values (@c, @s, @g, @t, 'granted', now()) returning id",
                ("c", corp), ("s", student), ("g", father), ("t", type));
            motherAuth = await ScalarAsync<Guid>(owner,
                "insert into media.viewing_authorization(corporation_id, guardian_id, student_id, valid_from, valid_to) values (@c, @g, @s, now() - interval '1 hour', now() + interval '30 days') returning id",
                ("c", corp), ("g", mother), ("s", student));
        }

        var (status, body) = await SendAsync(HttpMethod.Post, "/api/cameras/viewing/start", token, new { authorizationId = motherAuth, sessionId = (Guid?)null, cameraId = (Guid?)null });
        status.Should().NotBe(HttpStatusCode.OK, body.ToString());
        Strings(body).Should().Contain("Rule.CameraConsentWithdrawn");
    }
}
