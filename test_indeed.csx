using System.Net;
var handler = new SocketsHttpHandler {
    AutomaticDecompression = DecompressionMethods.All,
    UseCookies = true,
    CookieContainer = new CookieContainer()
};
using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
var warmup = new HttpRequestMessage(HttpMethod.Get, "https://www.indeed.com");
warmup.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
warmup.Headers.Add("Accept", "text/html");
warmup.Headers.Add("Sec-Ch-Ua", "\"Chromium\";v=\"131\"");
warmup.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
warmup.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
warmup.Headers.Add("Sec-Fetch-Dest", "document");
warmup.Headers.Add("Sec-Fetch-Mode", "navigate");
warmup.Headers.Add("Sec-Fetch-Site", "none");
warmup.Headers.Add("Upgrade-Insecure-Requests", "1");
var warmupRes = await client.SendAsync(warmup);
Console.WriteLine($"Warmup: {(int)warmupRes.StatusCode}");

var req = new HttpRequestMessage(HttpMethod.Get, "https://www.indeed.com/jobs?q=software+engineer&l=New+York&start=0");
req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
req.Headers.Add("Accept", "text/html");
req.Headers.Add("Sec-Ch-Ua", "\"Chromium\";v=\"131\"");
req.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
req.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
req.Headers.Add("Sec-Fetch-Dest", "document");
req.Headers.Add("Sec-Fetch-Mode", "navigate");
req.Headers.Add("Sec-Fetch-Site", "same-origin");
req.Headers.Add("Upgrade-Insecure-Requests", "1");
var res = await client.SendAsync(req);
Console.WriteLine($"Search: {(int)res.StatusCode}");
var html = await res.Content.ReadAsStringAsync();
Console.WriteLine($"HTML length: {html.Length}");
var matches = System.Text.RegularExpressions.Regex.Matches(html, @"data-jk=""([^""]+)""");
Console.WriteLine($"Job cards: {matches.Count}");
