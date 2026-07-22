namespace Subify.Domain.Common;

/// <summary>
/// Subify OS GUID generation policy (task 2.1.10).
/// </summary>
/// <remarks>
/// <para>
/// <b>Strategy:</b> application-generated <b>UUID version 7</b> (<see cref="Guid.CreateVersion7"/>).
/// </para>
/// <list type="bullet">
/// <item>Time-ordered → better B-tree locality than random v4 for inserts.</item>
/// <item>Works in unit tests without database defaults.</item>
/// <item>Postgres stores as <c>uuid</c>; no SQL Server <c>NEWSEQUENTIALID()</c>.</item>
/// <item>Entities may assign Id in factories; empty Ids are filled in SaveChanges.</item>
/// </list>
/// <para>
/// DB-side <c>gen_random_uuid()</c> is intentionally not used as the primary strategy
/// (random v4, less sequential). It remains valid for ad-hoc SQL if needed.
/// </para>
/// </remarks>
public static class GuidGenerator
{
    /// <summary>Creates a new UUID v7 identifier.</summary>
    public static Guid NewId() => Guid.CreateVersion7();
}
