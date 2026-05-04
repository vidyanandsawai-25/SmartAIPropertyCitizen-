using SmartAIPropertyCitizen.Api.Core.Interfaces;
using SmartAIPropertyCitizen.Api.Core.Models;
using System.Text.Json;

namespace SmartAIPropertyCitizen.Api.Application.Services
{
    public class SmartAiService : ISmartAiService
    {
        private readonly ISqlRepository _sqlRepository;
        private readonly IOtpService _otpService;
        private readonly IConfiguration _config;

        private readonly Dictionary<string, Dictionary<string, string>> i18n = new()
        {
            ["en"] = new() { ["login"] = "Please verify your mobile number first.", ["notfound"] = "No property records found.", ["error"] = "Something went wrong. Please try again." },
            ["mr"] = new() { ["login"] = "कृपया आधी आपला मोबाईल नंबर पडताळून पहा.", ["notfound"] = "कोणतीही मालमत्ता नोंद सापडली नाही.", ["error"] = "काहीतरी चुकले आहे. कृपया पुन्हा प्रयत्न करा." },
            ["hi"] = new() { ["login"] = "कृपया पहले अपना मोबाइल नंबर सत्यापित करें।", ["notfound"] = "कोई संपत्ति रिकॉर्ड नहीं मिला।", ["error"] = "कुछ गलत हो गया। कृपया पुन: प्रयास करें।" }
        };

        public SmartAiService(ISqlRepository sqlRepository, IOtpService otpService, IConfiguration config)
        {
            _sqlRepository = sqlRepository;
            _otpService = otpService;
            _config = config;
        }

        public async Task<ChatResponse> ProcessChatAsync(ChatRequest request)
        {
            string l = request.Language?.ToLower() ?? "en";
            if (!i18n.ContainsKey(l)) l = "en";

            // 1. Verify Session
            if (!Guid.TryParse(request.SessionId, out Guid guidSessionId))
                return new ChatResponse { ResponseText = i18n[l]["login"] };

            var session = await _otpService.GetSessionAsync(guidSessionId);
            if (session == null || !session.IsVerified)
                return new ChatResponse { ResponseText = i18n[l]["login"] };

            int ownerId = session.OwnerId;
            string message = request.Message.ToLower();

            // 2. OpenAI Intent Detection
            string intent = "GENERAL";
            string aiReply = "";

            try 
            {
                string apiKey = _config["OpenAI:ApiKey"] ?? string.Empty;
                string model = _config["OpenAI:Model"] ?? "gpt-4o";

                var chatClient = new OpenAI.Chat.ChatClient(model, apiKey);

                string systemPrompt = $@"
You are SmartAI, an official property tax assistant for Akola Municipal Corporation.
The user is asking a question in {l}.
Categorize their intent into one of the following:
- DEMAND: user wants to see their tax demand, balance, or how much they need to pay.
- RECEIPT: user wants to see previous receipts or history.
- PAYMENT: user wants to pay online.
- NOTICE: user wants to download their tax notice.
- GENERAL: anything else (greetings, general questions).

Respond strictly in valid JSON format:
{{
  ""intent"": ""DEMAND|RECEIPT|PAYMENT|NOTICE|GENERAL"",
  ""reply"": ""A short, polite conversational reply in {l} acknowledging the request.""
}}";

                var completion = await chatClient.CompleteChatAsync(new[] {
                    OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
                    OpenAI.Chat.ChatMessage.CreateUserMessage(request.Message)
                });

                var aiResponse = JsonSerializer.Deserialize<JsonElement>(completion.Value.Content[0].Text);
                intent = aiResponse.GetProperty("intent").GetString() ?? "GENERAL";
                aiReply = aiResponse.GetProperty("reply").GetString() ?? "";
            }
            catch
            {
                // Fallback to basic keyword matching if AI fails
                if (message.Contains("demand") || message.Contains("balance") || message.Contains("tax") || message.Contains("किती")) intent = "DEMAND";
                else if (message.Contains("receipt") || message.Contains("पावती")) intent = "RECEIPT";
                else if (message.Contains("pay") || message.Contains("payment") || message.Contains("भरायचे")) intent = "PAYMENT";
                else if (message.Contains("notice") || message.Contains("सूचना")) intent = "NOTICE";
            }

            // 3. Execute Intent
            var response = new ChatResponse { Intent = intent, ResponseText = aiReply };

            switch (intent)
            {
                case "DEMAND":
                    var demands = await _sqlRepository.ExecuteStoredProcedureAsync<dynamic>("dbo.Rpt_OwnerHeadWiseTaxDetails", new { OwnerID = ownerId });
                    response.Data = demands;
                    if (!demands.Any()) response.ResponseText = i18n[l]["notfound"];
                    break;

                case "RECEIPT":
                    var receipts = await _sqlRepository.ExecuteStoredProcedureAsync<dynamic>("dbo.Rpt_OwnerReceiptDetails", new { OwnerID = ownerId });
                    response.Data = receipts;
                    if (!receipts.Any()) response.ResponseText = i18n[l]["notfound"];
                    break;

                case "PAYMENT":
                    response.PaymentUrl = $"https://akolamc.in/onlinepayment?UniqID={session.UpicNo}";
                    response.ResponseText += (l == "en" ? $" You can pay your taxes online here: {response.PaymentUrl}" : $" आपण आपला कर येथे ऑनलाइन भरू शकता: {response.PaymentUrl}");
                    break;
                
                case "NOTICE":
                    response.DownloadUrl = $"https://akolamc.in/Download/Index?B={session.UpicNo}";
                    response.ResponseText += (l == "en" ? $" You can download your latest tax notice here: {response.DownloadUrl}" : $" आपण आपली ताजी कर सूचना येथे डाउनलोड करू शकता: {response.DownloadUrl}");
                    break;

                default:
                    if (string.IsNullOrEmpty(aiReply))
                    {
                        response.ResponseText = l == "en" ? "How can I help you with your Akola Property Tax today?" : "मी तुम्हाला आज अकोला मालमत्ता कराबाबत कशी मदत करू शकतो?";
                    }
                    break;
            }

            return response;
        }
    }
}
