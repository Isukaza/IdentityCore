namespace IdentityCore.DAL.PostgreSQL.Roles;

public enum UserRole
{
    /// <summary>
    /// SuperAdmin: Full control over all aspects of the system.
    /// <para>Permissions:</para>
    /// <list type="bullet">
    /// <item>Manage tracks (add/delete/edit).</item>
    /// <item>Manage albums (create/delete/edit).</item>
    /// <item>Assign/remove user roles (including Admin, Manager, Publisher).</item>
    /// <item>Change the subscription status of any user.</item>
    /// <item>Delete any user except other SuperAdmins (deletion of a SuperAdmin is only possible through the database).</item>
    /// <item>Manage subscriptions for all users.</item>
    /// <item>Perform Admin and Publisher tasks (full rights over their actions).</item>
    /// </list>
    /// </summary>
    SuperAdmin = 4,

    /// <summary>
    /// Admin: Responsible for operational tasks and user support.
    /// <para>Permissions:</para>
    /// <list type="bullet">
    /// <item>Add/delete tracks.</item>
    /// <item>Edit existing albums.</item>
    /// <item>Assign/demote Managers.</item>
    /// <item>Assist users with requests (within their permissions).</item>
    /// <item>If the Admin has a subscription, they can enable/disable it for access to premium content.</item>
    /// </list>
    /// </summary>
    Admin = 3,

    /// <summary>
    /// Manager: Involved in creating and managing content.
    /// <para>Permissions:</para>
    /// <list type="bullet">
    /// <item>Add/delete tracks.</item>
    /// <item>Edit album metadata (title, description, cover).</item>
    /// </list>
    /// <para>Restrictions:</para>
    /// <list type="bullet">
    /// <item>Cannot create or delete albums.</item>
    /// <item>Cannot manage roles or subscriptions of other users.</item>
    /// </list>
    /// </summary>
    Manager = 2,

    /// <summary>
    /// Publisher: Manages their own content.
    /// <para>Permissions:</para>
    /// <list type="bullet">
    /// <item>Create new albums.</item>
    /// <item>Delete their own albums.</item>
    /// <item>Upload, delete, and edit tracks in their albums (CRUD operations restricted to their own content).</item>
    /// </list>
    /// <para>Restrictions:</para>
    /// <list type="bullet">
    /// <item>Cannot manage other users' roles or subscriptions.</item>
    /// </list>
    /// </summary>
    Publisher = 1,

    /// <summary>
    /// User: Standard access to content.
    /// <para>Permissions:</para>
    /// <list type="bullet">
    /// <item>Listen to and download tracks.</item>
    /// </list>
    /// <para>Restrictions:</para>
    /// <list type="bullet">
    /// <item>No access to premium content without a subscription.</item>
    /// </list>
    /// </summary>
    User = 0
}