using FundLaunch.Platform.Contracts;

namespace FundLaunch.Platform.Core;

public sealed record PolicyOverrideResult(
    RiskLimitConfig EffectiveLimits,
    IReadOnlyList<PolicyOverrideAuditEntry> AuditTrail);

public static class PolicyOverrideEngine
{
    public static PolicyOverrideResult Apply(
        RiskLimitConfig baseline,
        IReadOnlyList<PolicyOverrideRequest>? overrides,
        DateTime asOfUtc)
    {
        baseline.Validate();

        if (overrides is null || overrides.Count == 0)
        {
            return new PolicyOverrideResult(
                EffectiveLimits: baseline,
                AuditTrail: Array.Empty<PolicyOverrideAuditEntry>());
        }

        var effective = baseline;
        var audit = new List<PolicyOverrideAuditEntry>(overrides.Count);

        foreach (var request in overrides.OrderBy(x => x.RequestedAtUtc))
        {
            var key = NormalizePolicyKey(request.PolicyKey);
            var hasPolicy = TryReadPolicyValue(effective, key, out var priorValue);
            var status = "PENDING_APPROVAL";
            decimal? appliedValue = null;

            if (!hasPolicy)
            {
                status = "UNSUPPORTED_POLICY";
            }
            else if (!request.IsApproved)
            {
                status = "PENDING_APPROVAL";
            }
            else if (request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc.Value <= asOfUtc)
            {
                status = "EXPIRED";
            }
            else
            {
                try
                {
                    var candidate = ApplyPolicyValue(effective, key, request.Value);
                    candidate.Validate();
                    effective = candidate;
                    appliedValue = request.Value;
                    status = "APPLIED";
                }
                catch (ArgumentOutOfRangeException)
                {
                    status = "REJECTED_INVALID_VALUE";
                }
            }

            audit.Add(new PolicyOverrideAuditEntry(
                PolicyKey: request.PolicyKey,
                RequestedValue: request.Value,
                PriorValue: hasPolicy ? priorValue : null,
                AppliedValue: appliedValue,
                Status: status,
                Reason: request.Reason,
                RequestedBy: request.RequestedBy,
                ApprovedBy: request.ApprovedBy,
                RequestedAtUtc: request.RequestedAtUtc,
                ApprovedAtUtc: request.ApprovedAtUtc,
                EvaluatedAtUtc: asOfUtc));
        }

        effective.Validate();

        return new PolicyOverrideResult(
            EffectiveLimits: effective,
            AuditTrail: audit);
    }

    private static string NormalizePolicyKey(string key)
    {
        return key
            .Trim()
            .ToUpperInvariant()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private static bool TryReadPolicyValue(RiskLimitConfig limits, string key, out decimal value)
    {
        switch (key)
        {
            case "MAXABSWEIGHTPERSYMBOL":
                value = limits.MaxAbsWeightPerSymbol;
                return true;
            case "MAXGROSSEXPOSURE":
                value = limits.MaxGrossExposure;
                return true;
            case "MAXTURNOVER":
                value = limits.MaxTurnover;
                return true;
            case "MAXABSNETEXPOSURE":
                value = limits.MaxAbsNetExposure;
                return true;
            case "MINORDERNOTIONAL":
                value = limits.MinOrderNotional;
                return true;
            case "CAPITALBASE":
                value = limits.CapitalBase;
                return true;
            default:
                value = 0m;
                return false;
        }
    }

    private static RiskLimitConfig ApplyPolicyValue(RiskLimitConfig limits, string key, decimal value)
    {
        return key switch
        {
            "MAXABSWEIGHTPERSYMBOL" => limits with { MaxAbsWeightPerSymbol = value },
            "MAXGROSSEXPOSURE" => limits with { MaxGrossExposure = value },
            "MAXTURNOVER" => limits with { MaxTurnover = value },
            "MAXABSNETEXPOSURE" => limits with { MaxAbsNetExposure = value },
            "MINORDERNOTIONAL" => limits with { MinOrderNotional = value },
            "CAPITALBASE" => limits with { CapitalBase = value },
            _ => limits
        };
    }
}
