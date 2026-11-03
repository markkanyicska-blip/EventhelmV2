using System.Reflection;
using System.Text.RegularExpressions;
using EventHelm.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace EventHelm.Api.Tests;

public sealed class ApiAuthorizationInventoryTests
{
    [Fact]
    public void Every_controller_verb_and_route_has_exactly_one_complete_inventory_row()
    {
        var rows = ReadRows();
        Assert.Equal(82, rows.Length);
        Assert.Equal(Enumerable.Range(1, 82).Select(id => $"E{id:000}"), rows.Select(row => row[1]));
        Assert.All(rows, row =>
        {
            Assert.Equal(11, row.Length);
            Assert.All(row.Skip(1).SkipLast(1), cell => Assert.False(string.IsNullOrWhiteSpace(cell)));
        });
        var lines = ReadInventory();
        for (var index = 1; index < lines.Length; index++)
        {
            if (Regex.IsMatch(lines[index], @"^\|[-:| ]+\|$"))
                Assert.Equal(lines[index - 1].Split('|').Length, lines[index].Split('|').Length);
        }

        var actions = ControllerActions().ToArray();
        Assert.Equal(80, actions.Length);
        var expected = actions.Select(action => action.Endpoint).Order().ToArray();
        var actual = rows.Where(row => row[1] is not ("E004" or "E082"))
            .Select(Endpoint).Order().ToArray();
        Assert.Equal(expected, actual);
        Assert.Equal("GET /api/health", Endpoint(rows.Single(row => row[1] == "E004")));
        Assert.Equal("GET /openapi/{documentname}.json", Endpoint(rows.Single(row => row[1] == "E082")));
    }

    [Fact]
    public void Existing_anonymous_exceptions_and_shared_gate_are_described_not_replaced()
    {
        var rows = ReadRows();
        foreach (var action in ControllerActions())
        {
            var row = rows.Single(row => Endpoint(row) == action.Endpoint);
            Assert.StartsWith(action.Anonymous ? "ANON" : "L", row[8]);
        }
    }

    [Fact]
    public void All_additional_scope_blockers_are_explicit_and_counted()
    {
        var expected = new[] { 5 }
            .Concat(Enumerable.Range(10, 5))
            .Concat(Enumerable.Range(25, 45))
            .Concat([80, 81])
            .Select(id => $"E{id:000}")
            .ToArray();
        var blocked = ReadRows().Where(row => row[9].StartsWith("SCOPE-BLOCKED:", StringComparison.Ordinal))
            .Select(row => row[1]).ToArray();
        Assert.Equal(53, blocked.Length);
        Assert.Equal(expected, blocked);
    }

    private static string[] ReadInventory() => File.ReadAllLines(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "api-authorization-inventory.md"));

    private static string[][] ReadRows() => ReadInventory()
        .Where(line => Regex.IsMatch(line, @"^\| E\d{3} \|"))
        .Select(line => line.Split('|', StringSplitOptions.TrimEntries))
        .ToArray();

    private static string Endpoint(string[] row) =>
        $"{row[2].Trim('`')} {row[3].Trim('`').ToLowerInvariant()}";

    private static IEnumerable<(string Endpoint, bool Anonymous)> ControllerActions()
    {
        foreach (var controller in typeof(AccessController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ControllerBase))))
        {
            var prefix = Assert.Single(controller.GetCustomAttributes<RouteAttribute>()).Template
                .Replace("[controller]", controller.Name.Replace("Controller", "", StringComparison.Ordinal),
                    StringComparison.Ordinal);
            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var route in method.GetCustomAttributes<HttpMethodAttribute>())
                {
                    var path = "/" + string.Join("/", new[] { prefix, route.Template }
                        .Where(part => !string.IsNullOrEmpty(part)));
                    foreach (var verb in route.HttpMethods)
                        yield return ($"{verb} {path.ToLowerInvariant()}",
                            method.IsDefined(typeof(AllowAnonymousAttribute)));
                }
            }
        }
    }
}
