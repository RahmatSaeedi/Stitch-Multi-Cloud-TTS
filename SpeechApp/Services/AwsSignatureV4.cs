using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SpeechApp.Services;

public class AwsSignatureV4
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string ContentType = "application/json";

    public static HttpRequestMessage SignRequest(
        HttpRequestMessage request,
        string accessKey,
        string secretKey,
        string region,
        string service,
        byte[]? payload = null)
    {
        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);

        // Add required headers
        request.Headers.TryAddWithoutValidation("X-Amz-Date", amzDate);
        request.Headers.Host = request.RequestUri!.Host;

        // Calculate payload hash
        var payloadHash = payload != null
            ? ToHex(SHA256.HashData(payload))
            : ToHex(SHA256.HashData(Array.Empty<byte>()));

        request.Headers.TryAddWithoutValidation("X-Amz-Content-Sha256", payloadHash);

        // Create canonical request
        var canonicalUri = request.RequestUri.AbsolutePath;
        var canonicalQueryString = GetCanonicalQueryString(request.RequestUri.Query);
        var canonicalHeaders = GetCanonicalHeaders(request.Headers);
        var signedHeaders = GetSignedHeaders(request.Headers);

        var canonicalRequest = string.Join("\n",
            request.Method.Method,
            canonicalUri,
            canonicalQueryString,
            canonicalHeaders,
            signedHeaders,
            payloadHash
        );

        // Debug logging
        Console.WriteLine("=== AWS Signature V4 Debug ===");
        Console.WriteLine($"Method: {request.Method.Method}");
        Console.WriteLine($"URI: {canonicalUri}");
        Console.WriteLine($"Query: '{canonicalQueryString}'");
        Console.WriteLine($"Headers:\n{canonicalHeaders}");
        Console.WriteLine($"Signed Headers: {signedHeaders}");
        Console.WriteLine($"Payload Hash: {payloadHash}");
        Console.WriteLine($"Canonical Request:\n{canonicalRequest}");
        Console.WriteLine($"Canonical Request Hash: {ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}");

        // Create string to sign
        var credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
        var stringToSign = string.Join("\n",
            Algorithm,
            amzDate,
            credentialScope,
            ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))
        );

        Console.WriteLine($"String to Sign:\n{stringToSign}");

        // Calculate signature
        var signingKey = GetSignatureKey(secretKey, dateStamp, region, service);
        var signature = ToHex(HMACSHA256Hash(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        Console.WriteLine($"Signature: {signature}");

        // Add authorization header
        var authorizationHeader = $"{Algorithm} Credential={accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        Console.WriteLine($"Authorization: {authorizationHeader}");
        Console.WriteLine("=== End Debug ===\n");

        request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

        return request;
    }

    private static string GetCanonicalQueryString(string query)
    {
        if (string.IsNullOrEmpty(query) || query == "?")
            return string.Empty;

        query = query.TrimStart('?');
        var parameters = query.Split('&')
            .Select(p =>
            {
                var parts = p.Split('=');
                return new { Key = Uri.EscapeDataString(parts[0]), Value = parts.Length > 1 ? Uri.EscapeDataString(parts[1]) : "" };
            })
            .OrderBy(p => p.Key)
            .ThenBy(p => p.Value);

        return string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
    }

    private static string GetCanonicalHeaders(System.Net.Http.Headers.HttpRequestHeaders headers)
    {
        var canonicalHeaders = new SortedDictionary<string, string>();

        // AWS requires signing host and all x-amz-* headers
        // Other headers are optional but if present and signed, must be lowercase
        foreach (var header in headers)
        {
            var headerName = header.Key.ToLowerInvariant();

            // Skip headers that shouldn't be included in AWS signature
            // These are typically added by HttpClient and can vary
            var skipHeaders = new HashSet<string> { "connection", "user-agent", "accept-encoding" };

            if (!skipHeaders.Contains(headerName))
            {
                // Trim and normalize header values
                var headerValues = header.Value.Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v));
                var headerValue = string.Join(",", headerValues);

                if (!string.IsNullOrEmpty(headerValue))
                {
                    canonicalHeaders[headerName] = headerValue;
                }
            }
        }

        return string.Join("\n", canonicalHeaders.Select(h => $"{h.Key}:{h.Value}")) + "\n";
    }

    private static string GetSignedHeaders(System.Net.Http.Headers.HttpRequestHeaders headers)
    {
        // Skip headers that shouldn't be included in AWS signature
        var skipHeaders = new HashSet<string> { "connection", "user-agent", "accept-encoding" };

        return string.Join(";",
            headers.Select(h => h.Key.ToLowerInvariant())
                   .Where(h => !skipHeaders.Contains(h))
                   .OrderBy(h => h));
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var kSecret = Encoding.UTF8.GetBytes($"AWS4{key}");
        var kDate = HMACSHA256Hash(kSecret, Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HMACSHA256Hash(kDate, Encoding.UTF8.GetBytes(regionName));
        var kService = HMACSHA256Hash(kRegion, Encoding.UTF8.GetBytes(serviceName));
        var kSigning = HMACSHA256Hash(kService, Encoding.UTF8.GetBytes("aws4_request"));

        return kSigning;
    }

    private static byte[] HMACSHA256Hash(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string ToHex(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
    }
}
