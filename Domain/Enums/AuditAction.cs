namespace Domain.Enums;

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    BusinessAction = 3,
    Login = 4,
    Logout = 5,
    FailedLogin = 6,
    AccessDenied = 7
}
