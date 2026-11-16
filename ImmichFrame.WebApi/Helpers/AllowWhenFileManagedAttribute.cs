namespace ImmichFrame.WebApi.Helpers;

/// <summary>
/// Admin endpoint that stays reachable when settings come from a config file.
/// Every other admin endpoint is rejected in that mode, including anonymous ones.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AllowWhenFileManagedAttribute : Attribute;
