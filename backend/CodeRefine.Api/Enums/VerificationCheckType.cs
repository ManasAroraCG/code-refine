namespace CodeRefine.Api.Enums;

/// <summary>
/// Verification checks executed by the sandbox. Owned as a contract with
/// Developer 3; values are mapped from the sandbox response.
/// </summary>
public enum VerificationCheckType
{
    Syntax = 0,
    Lint = 1,
    TypeCheck = 2,
    Tests = 3,
    Build = 4,
    Security = 5
}
