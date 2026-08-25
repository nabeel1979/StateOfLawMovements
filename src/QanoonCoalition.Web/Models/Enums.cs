namespace QanoonCoalition.Web.Models;

public enum UserRole
{
    Admin = 1,
    MovementManager = 2
}

public enum Gender
{
    Male = 1,
    Female = 2
}

public enum RequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum AuditAction
{
    CreateMovement,
    UpdateMovement,
    DeleteMovement,
    CreateUser,
    UpdateUser,
    DeleteUser,
    AddMember,
    UpdateMember,
    DeleteMember,
    SubmitRequest,
    ApproveRequest,
    RejectRequest,
    UpdateRequestStatus,
    UpdateConstants,
    Login,
    Logout
}
