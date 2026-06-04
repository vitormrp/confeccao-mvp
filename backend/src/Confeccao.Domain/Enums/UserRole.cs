namespace Confeccao.Domain.Enums;

/// <summary>
/// Role of a user in the system. Operator roles map 1:1 to a <see cref="StageCode"/>;
/// the manager role is administrative.
/// </summary>
public enum UserRole
{
    Manager = 1,
    Cutting = 2,
    Interfacing = 3,
    Sewing = 4,
    Washing = 5,
    Buttoning = 6,
    Labeling = 7,
    Pressing = 8,
}

public static class UserRoleExtensions
{
    /// <summary>
    /// Returns the stage this operator role works on, or null for non-operator roles (manager).
    /// </summary>
    public static StageCode? ToStageCode(this UserRole role) => role switch
    {
        UserRole.Cutting => StageCode.Cutting,
        UserRole.Interfacing => StageCode.Interfacing,
        UserRole.Sewing => StageCode.Sewing,
        UserRole.Washing => StageCode.Washing,
        UserRole.Buttoning => StageCode.Buttoning,
        UserRole.Labeling => StageCode.Labeling,
        UserRole.Pressing => StageCode.Pressing,
        _ => null,
    };
}
