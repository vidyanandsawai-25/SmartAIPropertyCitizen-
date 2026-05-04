using SmartAIPropertyCitizen.Api.Core.Interfaces;
using SmartAIPropertyCitizen.Api.Core.Domain;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartAIPropertyCitizen.Api.Application.Services
{
    public class SmartAiService : ISmartAiService
    {
        private readonly IPropertyTaxService _propertyTaxService;
        private readonly IOtpService _otpService;
        private readonly IConfiguration _config;

        private readonly Dictionary<string, Dictionary<string, string>> i18n = new()
        {
            ["mr"] = new() {
                ["DemandFound"] = "येथे आपल्या मागणीचा तपशील आहे:",
                ["ReceiptFound"] = "येथे आपल्या मागील पावत्यांची यादी आहे:",
                ["PaymentLink"] = "आपण खालील लिंकवर क्लिक करून ऑनलाइन पेमेंट करू शकता:",
                ["NoticeLink"] = "आपण खालील लिंकवरून नोटीस डाउनलोड करू शकता:",
                ["DefaultHelp"] = "क्षमस्व, मी तुम्हाला आज अकोला मालमत्ता कराबाबत कशी मदत करू शकतो?",
                ["LoginRequired"] = "कृपया प्रथम लॉगइन करा.",
                ["NoRecords"] = "कोणतीही मालमत्ता नोंद सापडली नाही."
            },
            ["hi"] = new() {
                ["DemandFound"] = "यहाँ आपकी मांग का विवरण है:",
                ["ReceiptFound"] = "यहाँ आपकी पिछली रसीदों की सूची है:",
                ["PaymentLink"] = "आप नीचे दिए गए लिंक पर क्लिक करके ऑनलाइन भुगतान कर सकते हैं:",
                ["NoticeLink"] = "आप नीचे दिए गए लिंक से नोटिस डाउनलोड कर सकते हैं:",
                ["DefaultHelp"] = "क्षमा करें, मैं आपकी अकोला संपत्ति कर में कैसे सहायता कर सकता हूँ?",
                ["LoginRequired"] = "कृपया पहले लॉगिन करें।",
                ["NoRecords"] = "कोई संपत्ति रिकॉर्ड नहीं मिला।"
            },
            ["en"] = new() {
                ["DemandFound"] = "Here are your demand details:",
                ["ReceiptFound"] = "Here is the list of your previous receipts:",
                ["PaymentLink"] = "You can make an online payment by clicking the link below:",
                ["NoticeLink"] = "You can download the notice from the link below:",
                ["DefaultHelp"] = "How can I help you with your Akola Property Tax today?",
                ["LoginRequired"] = "Please login first.",
                ["NoRecords"] = "No property records found."
            }
        };

        public SmartAiService(IPropertyTaxService propertyTaxService, IOtpService otpService, IConfiguration config)
        {
            _propertyTaxService = propertyTaxService;
            _otpService = otpService;
            _config = config;
        }

        public async Task<ChatResponse> ProcessChatAsync(ChatRequest request)
        {
            var l = request.Language?.ToLower() ?? "mr";
            if (!i18n.ContainsKey(l)) l = "mr";

            // 1. Verify Session
            if (string.IsNullOrEmpty(request.SessionId) || !Guid.TryParse(request.SessionId, out Guid guidSessionId))
                return new ChatResponse { ResponseText = i18n[l]["LoginRequired"] };

            var session = await _otpService.GetSessionAsync(guidSessionId);
            if (session == null || !session.IsVerified)
                return new ChatResponse { ResponseText = i18n[l]["LoginRequired"] };

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
  ""reply"": ""If GENERAL, provide a helpful polite response in {l}. Otherwise leave empty.""
}}";

                var completion = await chatClient.CompleteChatAsync(new OpenAI.Chat.ChatMessage[]
                {
                    OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
                    OpenAI.Chat.ChatMessage.CreateUserMessage(message)
                });

                string jsonContent = completion.Value.Content[0].Text.Trim();
                
                // Remove markdown if present
                if (jsonContent.StartsWith("```"))
                {
                    var match = Regex.Match(jsonContent, @"```(?:json)?\s*(.*?)\s*```", RegexOptions.Singleline);
                    if (match.Success) jsonContent = match.Groups[1].Value;
                }

                using var aiDoc = JsonDocument.Parse(jsonContent);
                intent = aiDoc.RootElement.GetProperty("intent").GetString()?.ToUpper() ?? "GENERAL";
                aiReply = aiDoc.RootElement.GetProperty("reply").GetString() ?? "";
            }
            catch
            {
                // Fallback to basic keyword matching if AI fails
                if (message.Contains("demand") || message.Contains("balance") || message.Contains("मागणी") || message.Contains("किती")) intent = "DEMAND";
                else if (message.Contains("receipt") || message.Contains("पावती") || message.Contains("रसीद")) intent = "RECEIPT";
                else if (message.Contains("pay") || message.Contains("payment") || message.Contains("भरणा") || message.Contains("भरायचे")) intent = "PAYMENT";
                else if (message.Contains("notice") || message.Contains("सूचना") || message.Contains("नोटीस")) intent = "NOTICE";
            }

            // 3. Execute Intent
            var response = new ChatResponse { ResponseText = aiReply, SessionId = request.SessionId };

            switch (intent)
            {
                case "DEMAND":
                    var demands = await _propertyTaxService.GetHeadwiseTaxDetailsAsync(ownerId);
                    response.Data = demands;
                    response.ResponseText = i18n[l]["DemandFound"];
                    if (!demands.Any()) response.ResponseText = i18n[l]["NoRecords"];
                    break;

                case "RECEIPT":
                    var receipts = await _propertyTaxService.GetPreviousReceiptsAsync(ownerId);
                    response.Data = receipts;
                    response.ResponseText = i18n[l]["ReceiptFound"];
                    if (!receipts.Any()) response.ResponseText = i18n[l]["NoRecords"];
                    break;

                case "PAYMENT":
                    response.PaymentUrl = $"https://akolamc.in/onlinepayment?UniqID={session.UpicNo}";
                    response.ResponseText = i18n[l]["PaymentLink"];
                    break;
                
                case "NOTICE":
                    response.DownloadUrl = $"https://akolamc.in/Download/Index?B={session.UpicNo}";
                    response.ResponseText = i18n[l]["NoticeLink"];
                    break;

                default:
                    if (string.IsNullOrEmpty(aiReply))
                    {
                        response.ResponseText = i18n[l]["DefaultHelp"];
                    }
                    break;
            }

            return response;
        }
    }
}
