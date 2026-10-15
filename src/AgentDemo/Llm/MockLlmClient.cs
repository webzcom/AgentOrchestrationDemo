namespace AgentDemo.Llm;

/// <summary>
/// A fake model with scripted answers, so the demo runs offline, free and the same every time.
///
/// It deliberately writes a BUGGY first version of the code (it forgets to ignore
/// case, spaces and punctuation). The Tester catches it, the orchestrator feeds the
/// failures back, and the "model" fixes it on attempt 2. That retry loop is the
/// thing worth watching.
/// </summary>
public sealed class MockLlmClient : ILlmClient
{
    public string DisplayName => "Mock model (offline, scripted)";

    public async Task<string> CompleteAsync(string role, string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        await Task.Delay(400, ct); // pretend to "think" so the trace is easy to follow

        return role switch
        {
            "Planner" => Plan,
            "Coder" => userPrompt.Contains("FEEDBACK FROM TESTS") ? FixedCode : BuggyCode,
            "Reviewer" => Review,
            _ => throw new ArgumentException($"Mock has no script for role '{role}'"),
        };
    }

    private const string Plan = """
        1. Create a public static class `Solution` with `public static bool IsPalindrome(string? input)`.
        2. Treat null or empty input as a palindrome (return true).
        3. Normalise: keep only letters and digits, compare case-insensitively.
        4. Compare characters from both ends moving inward (O(n), no extra string allocations needed).
        Acceptance criteria:
        - "racecar" -> true, "hello" -> false
        - "A man, a plan, a canal: Panama" -> true (ignores case + punctuation)
        - "" and null -> true
        """;

    // Attempt 1: looks reasonable, but a straight string reverse doesn't ignore case or punctuation.
    private const string BuggyCode = """
        ```csharp
        public static class Solution
        {
            public static bool IsPalindrome(string? input)
            {
                if (string.IsNullOrEmpty(input)) return true;
                var reversed = new string(input.Reverse().ToArray());
                return input == reversed;
            }
        }
        ```
        """;

    // Attempt 2: after seeing the failing test, skips non-alphanumerics and compares case-insensitively.
    private const string FixedCode = """
        ```csharp
        public static class Solution
        {
            /// <summary>True if the input reads the same both ways, ignoring case, spaces and punctuation.</summary>
            public static bool IsPalindrome(string? input)
            {
                if (string.IsNullOrEmpty(input)) return true;

                int left = 0, right = input.Length - 1;
                while (left < right)
                {
                    if (!char.IsLetterOrDigit(input[left])) { left++; continue; }
                    if (!char.IsLetterOrDigit(input[right])) { right--; continue; }

                    if (char.ToLowerInvariant(input[left]) != char.ToLowerInvariant(input[right]))
                        return false;

                    left++;
                    right--;
                }
                return true;
            }
        }
        ```
        """;

    private const string Review = """
        APPROVE
        - Two-pointer approach is O(n) with no extra allocations. Good.
        - Null/empty handling matches the plan.
        - Nice to have: an overload taking ReadOnlySpan<char> for hot paths.
        """;
}
