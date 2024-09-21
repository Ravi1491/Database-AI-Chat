// using Azure;
// using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json;
using YourOwnData;

namespace StoryCreator.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string UserPrompt { get; set; } = string.Empty;
        public List<List<string>> RowData { get; set; }
        public string Summary { get; set; }
        public string Query { get; set; }
        public string Error { get; set; }

        public async Task OnPost()
        {
            await RunQuery(UserPrompt);
        }

        public async Task RunQuery(string userPrompt)
        {
            // Configure Groq client
            string groqApiKey = "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";
            if (string.IsNullOrEmpty(groqApiKey))
            {
                Error = "Groq API key is missing. Please set the GROQ_API_KEY environment variable.";
                return;
            }

            string groqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {groqApiKey}");

            // Use the SchemaLoader project to export your db schema and then paste the schema in the placeholder below
            var systemMessage = @"Your are a helpful, cheerful database assistant. 
            Use the following database schema when creating your answers:

            - comments (id, type, content, discussion_id, parent_comment_id, user_id, created_at, updated_at, deleted_at)
            - discussion_hashtags (id, discussion_id, hashtag_id, created_at, updated_at, deleted_at)
            - discussion_likes (id, type, entity_type, discussion_id, comment_id, user_id, created_at, updated_at, deleted_at)
            - discussions (id, title, text, image_url, user_id, created_at, updated_at, deleted_at)
            - hashtags (id, name, created_at, updated_at, deleted_at)
            - user_followers (id, follower_id, following_id, created_at, updated_at, deleted_at)
            - users (id, name, email, password, mobile_number, created_at, updated_at, deleted_at)

            Include column name headers in the query results.

            Always provide your answer in the JSON format below:
            
            { ""summary"": ""your-summary"", ""query"":  ""your-query"" }
            
            Output ONLY JSON.
            In the preceding JSON response, substitute ""your-query"" with Microsoft SQL Server Query to retrieve the requested data.
            In the preceding JSON response, substitute ""your-summary"" with a summary of the query.
            Always include all columns in the table.
            If the resulting query is non-executable, replace ""your-query"" with NA, but still substitute ""your-query"" with a summary of the query.
            Do not use MySQL syntax.
            Always limit the SQL Query to 100 rows.";

            var requestBody = new
            {
                model = "llama3-70b-8192",
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 1024
            };

            try
            {
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(groqApiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Error = $"API Error: {response.StatusCode} - {errorContent}";
                    return;
                }

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var groqResponse = JsonSerializer.Deserialize<GroqResponse>(responseContent);

                var aiQuery = JsonSerializer.Deserialize<AIQuery>(groqResponse.choices[0].message.content);
                
                Summary = aiQuery.summary;
                Query = aiQuery.query;
                RowData = DataService.GetDataTable(aiQuery.query);
            }
            catch (Exception e)
            {
                Error = $"Exception: {e.Message}";
            }
        }
    }

    public class AIQuery
    {
        public string summary { get; set; }
        public string query { get; set; }
    }

    // Add this class to handle Claude's response structure
    public class GroqResponse
    {
        public List<GroqChoice> choices { get; set; }
    }

    public class GroqChoice
    {
        public GroqMessage message { get; set; }
    }

    public class GroqMessage
    {
        public string content { get; set; }
    }
}