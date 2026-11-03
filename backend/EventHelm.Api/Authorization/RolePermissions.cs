namespace EventHelm.Api.Authorization;

public static class RolePermissions
{
    public static Permission? For(ResourceKind resource, ResourceAction action) => (resource, action) switch
    {
        (ResourceKind.System, ResourceAction.Manage) => Permission.ManageSystem,
        (ResourceKind.Event, ResourceAction.Create) => Permission.CreateEvent,
        (ResourceKind.Event, ResourceAction.Read) => Permission.ReadEvent,
        (ResourceKind.Event, ResourceAction.Manage) => Permission.ManageEvent,
        (ResourceKind.Event, ResourceAction.Plan) => Permission.PlanEvent,
        (ResourceKind.Officials, ResourceAction.Read) => Permission.ReadOfficials,
        (ResourceKind.Officials, ResourceAction.Manage) => Permission.ManageOfficials,
        (ResourceKind.Boats, ResourceAction.Read) => Permission.ReadBoats,
        (ResourceKind.Boats, ResourceAction.Allocate) => Permission.AllocateBoats,
        (ResourceKind.Boats, ResourceAction.Report) => Permission.ReportDefect,
        (ResourceKind.Boats, ResourceAction.Resolve) => Permission.ResolveDefect,
        (ResourceKind.Boats, ResourceAction.UpdateOwn) => Permission.UpdateOwnUnit,
        (ResourceKind.Assignments, ResourceAction.Read) => Permission.ReadAssignments,
        (ResourceKind.Assignments, ResourceAction.Manage) => Permission.ManageAssignments,
        (ResourceKind.Shifts, ResourceAction.Read) => Permission.ReadShifts,
        (ResourceKind.Shifts, ResourceAction.Manage) => Permission.ManageShifts,
        (ResourceKind.Tasks, ResourceAction.Read) => Permission.ReadTasks,
        (ResourceKind.Tasks, ResourceAction.Manage) => Permission.ManageTasks,
        (ResourceKind.Tasks, ResourceAction.UpdateOwn) => Permission.UpdateOwnTask,
        (ResourceKind.Communications, ResourceAction.Read) => Permission.ReadCommunications,
        (ResourceKind.Communications, ResourceAction.Operate) => Permission.SendCommunications,
        (ResourceKind.Documents, ResourceAction.Read) => Permission.ReadDocuments,
        (ResourceKind.Documents, ResourceAction.Manage) => Permission.ManageDocuments,
        (ResourceKind.RaceControl, ResourceAction.Read) => Permission.ReadRaceControl,
        (ResourceKind.RaceControl, ResourceAction.Operate) => Permission.OperateRaceControl,
        (ResourceKind.PersonalInformation, ResourceAction.Read) => Permission.ReadPersonalInformation,
        (ResourceKind.PersonalInformation, ResourceAction.Manage) => Permission.ManagePersonalInformation,
        (ResourceKind.MedicalInformation, ResourceAction.Read) => Permission.ReadMedicalInformation,
        (ResourceKind.MedicalInformation, ResourceAction.Manage) => Permission.ManageMedicalInformation,
        (ResourceKind.EventRoles, ResourceAction.GrantRole) => Permission.GrantEventRole,
        (ResourceKind.GlobalInventory, ResourceAction.Manage) => Permission.ManageGlobalInventory,
        _ => null
    };

    public static bool IsSensitive(Permission permission) => permission is
        Permission.ReadPersonalInformation or Permission.ManagePersonalInformation or
        Permission.ReadMedicalInformation or Permission.ManageMedicalInformation or
        Permission.ReadPrivateDocuments or Permission.ManagePrivateDocuments;

    public static bool Allows(EventRole role, Permission permission) => role switch
    {
        EventRole.EventOrganizer => permission is
            Permission.ReadEvent or Permission.ManageEvent or Permission.PlanEvent or
            Permission.ReadOfficials or Permission.ManageOfficials or
            Permission.ReadBoats or Permission.AllocateBoats or Permission.ReportDefect or Permission.UpdateOwnUnit or
            Permission.ReadAssignments or Permission.ManageAssignments or Permission.ReadShifts or Permission.ManageShifts or
            Permission.ReadTasks or Permission.ManageTasks or Permission.UpdateOwnTask or
            Permission.ReadCommunications or Permission.SendCommunications or
            Permission.ReadDocuments or Permission.ManageDocuments or Permission.GrantEventRole,
        EventRole.RaceOfficer => permission is
            Permission.ReadEvent or Permission.PlanEvent or Permission.ReadOfficials or
            Permission.ReadBoats or Permission.AllocateBoats or Permission.ReportDefect or Permission.UpdateOwnUnit or
            Permission.ReadAssignments or Permission.ManageAssignments or Permission.ReadShifts or Permission.ManageShifts or
            Permission.ReadTasks or Permission.ManageTasks or Permission.UpdateOwnTask or
            Permission.ReadCommunications or Permission.SendCommunications or
            Permission.ReadDocuments or Permission.ReadRaceControl or Permission.OperateRaceControl,
        EventRole.TechnicalStaff => permission is
            Permission.ReadBoats or Permission.AllocateBoats or Permission.ReportDefect or Permission.ResolveDefect or
            Permission.UpdateOwnUnit or Permission.ReadAssignments or Permission.ReadShifts or Permission.ReadTasks or
            Permission.UpdateOwnTask or Permission.ReadCommunications or Permission.SendCommunications,
        EventRole.OnWaterPersonnel => permission is
            Permission.ReadBoats or Permission.ReportDefect or Permission.UpdateOwnUnit or
            Permission.ReadAssignments or Permission.ReadShifts or Permission.ReadTasks or
            Permission.UpdateOwnTask or Permission.ReadCommunications or Permission.SendCommunications,
        _ => false
    };
}
