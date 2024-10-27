using StackExchange.Redis;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"當前工作目錄: {Directory.GetCurrentDirectory()}");

        // Redis 連接字串
        string redisConnectionString = "localhost:6379"; // 根據你的 Redis 設定進行修改
        string redisKey = "downloaded-logs-20241027-111115";

        try
        {
            // 建立 Redis 連接
            ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            IDatabase db = redis.GetDatabase();

            // 讀取 Redis key 的值
            string? jsonString = await db.StringGetAsync(redisKey);

            if (!string.IsNullOrEmpty(jsonString))
            {
                // 解析 JSON 內容
                var jsonDocument = JsonDocument.Parse(jsonString);

                // 確認根元素是否為 JSON 陣列
                if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // 將根元素轉換成陣列
                    JsonElement.ArrayEnumerator arrayEnumerator = jsonDocument.RootElement.EnumerateArray();

                    // 遍歷並輸出每個元素的 jsonPayload -> properties -> body 欄位
                    foreach (var element in arrayEnumerator)
                    {
                        if (element.ValueKind == JsonValueKind.Object &&
                            element.TryGetProperty("jsonPayload", out JsonElement jsonPayloadElement) &&
                            jsonPayloadElement.ValueKind == JsonValueKind.Object &&
                            jsonPayloadElement.TryGetProperty("message", out JsonElement messageElement))
                        {
                            string message = messageElement.GetString() ?? string.Empty;
                            string truncatedMessage = message.Length > 25 ? message.Substring(0, 25) : message;
                            Console.WriteLine(truncatedMessage);
                        }
                        else
                        {
                            Console.WriteLine("元素中沒有 jsonPayload -> message 欄位或元素不是物件。");
                        }

                        if (element.ValueKind == JsonValueKind.Object &&
                            element.TryGetProperty("jsonPayload", out JsonElement jsonPayloadElement2) &&
                            jsonPayloadElement2.ValueKind == JsonValueKind.Object &&
                            jsonPayloadElement2.TryGetProperty("properties", out JsonElement propertiesElement) &&
                            propertiesElement.ValueKind == JsonValueKind.Object &&
                            propertiesElement.TryGetProperty("body", out JsonElement bodyElement))
                        {
                            Console.WriteLine(bodyElement.ToString());
                        }
                        else
                        {
                            Console.WriteLine("元素中沒有 jsonPayload -> properties -> body 欄位或元素不是物件。");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("根元素不是 JSON 陣列。");
                }
            }
            else
            {
                Console.WriteLine("指定的 Redis key 沒有值或不存在。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"讀取或解析 Redis key 時發生錯誤: {ex.Message}");
        }
    }
}