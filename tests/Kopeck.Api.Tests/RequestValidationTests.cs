using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Kopeck.Api.Controllers;
using Kopeck.Api.Filters;
using Kopeck.Application.Validation;
using Kopeck.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Tests;

public class RequestValidationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public RequestValidationTests(ApiFactory f) => _f = f;

    /// <summary>A validator only runs when the action has the matching filter, so a forgotten attribute would silently skip it.</summary>
    [Fact]
    public void Every_action_with_a_validated_request_has_the_validation_filter()
    {
        var validated = typeof(IRequestValidator<>).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestValidator<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToHashSet();

        var actions = typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ApiControllerBase)) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        var missing = new List<string>();
        foreach (var action in actions)
        {
            var filters = action.GetCustomAttributes<ServiceFilterAttribute>().Select(a => a.ServiceType).ToHashSet();
            foreach (var request in action.GetParameters().Select(p => p.ParameterType).Where(validated.Contains))
            {
                if (!filters.Contains(typeof(RequestValidationFilter<>).MakeGenericType(request)))
                    missing.Add($"{action.DeclaringType!.Name}.{action.Name}({request.Name})");
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public async Task All_failures_are_reported_in_one_error_message()
    {
        var response = await _f.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest("not-an-email", "short", " "), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<ApiError>(Json.Options))!;
        Assert.Contains("valid email", error.Error);
        Assert.Contains("display name", error.Error);
    }

    [Fact]
    public async Task Login_with_a_well_formed_but_wrong_password_is_still_unauthorized()
    {
        var response = await _f.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@test.dev", "Wrong-pass1"), Json.Options);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
