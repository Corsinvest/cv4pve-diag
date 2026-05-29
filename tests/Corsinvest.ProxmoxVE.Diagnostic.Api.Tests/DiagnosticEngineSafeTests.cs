/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Dynamic;
using System.Net;
using Corsinvest.ProxmoxVE.Api;
using Xunit;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api.Tests;

/// <summary>
/// The format of <see cref="DiagnosticSafeExtensions.BuildApiErrorMessage"/> is what users see
/// in WG0042 findings. These tests pin the format so it does not regress silently.
/// </summary>
public class DiagnosticEngineSafeTests
{
    [Fact]
    public void BuildApiErrorMessage_status_reason_and_path()
    {
        var r = MakeResult(HttpStatusCode.Forbidden, "Forbidden", "/cluster/firewall/options", responseError: null);
        var msg = DiagnosticSafeExtensions.BuildApiErrorMessage(r);
        Assert.Equal("403 Forbidden — Get /cluster/firewall/options", msg);
    }

    [Fact]
    public void BuildApiErrorMessage_includes_api_error_body_when_present()
    {
        var r = MakeResult(HttpStatusCode.Forbidden, "Forbidden", "/cluster/firewall/options",
                           responseError: "Permission check failed (/cluster, Sys.Audit)");
        var msg = DiagnosticSafeExtensions.BuildApiErrorMessage(r);
        // Result.GetError() prefixes each error entry with its key ("root : ...") — the
        // formatter just embeds it verbatim, so the assertion mirrors the real PVE output shape.
        Assert.Contains("403 Forbidden", msg);
        Assert.Contains("Permission check failed (/cluster, Sys.Audit)", msg);
        Assert.Contains("Get /cluster/firewall/options", msg);
    }

    [Fact]
    public void BuildApiErrorMessage_omits_path_when_empty()
    {
        var r = MakeResult(HttpStatusCode.InternalServerError, "Internal Server Error", requestResource: "", responseError: null);
        var msg = DiagnosticSafeExtensions.BuildApiErrorMessage(r);
        Assert.Equal("500 Internal Server Error", msg);
    }

    [Fact]
    public void BuildApiErrorMessage_handles_501_status()
    {
        // 501 is silenced upstream by the catch filter, but the formatter must still produce
        // a sensible string in case any code path calls it for a 501.
        var r = MakeResult(HttpStatusCode.NotImplemented, "Not Implemented", "/cluster/ha/groups", responseError: null);
        var msg = DiagnosticSafeExtensions.BuildApiErrorMessage(r);
        Assert.Equal("501 Not Implemented — Get /cluster/ha/groups", msg);
    }

    [Fact]
    public void ApiErrorCode_is_stable()
    {
        // The WG0042 code is published in the README check table; flag accidental renames.
        Assert.Equal("WG0042", DiagnosticSafeExtensions.ApiErrorCode);
    }

    // -------- helper --------

    private static Result MakeResult(HttpStatusCode status, string reason, string requestResource, string? responseError)
    {
        dynamic response = new ExpandoObject();
        if (responseError != null)
        {
            dynamic errors = new ExpandoObject();
            ((IDictionary<string, object>)errors)["root"] = responseError;
            ((IDictionary<string, object>)response)["errors"] = errors;
        }

        return new Result(response: response,
                          statusCode: status,
                          reasonPhrase: reason,
                          isSuccessStatusCode: (int)status >= 200 && (int)status < 300,
                          requestResource: requestResource,
                          requestParameters: new Dictionary<string, object>(),
                          methodType: MethodType.Get,
                          responseType: ResponseType.Json,
                          duration: TimeSpan.Zero);
    }
}
