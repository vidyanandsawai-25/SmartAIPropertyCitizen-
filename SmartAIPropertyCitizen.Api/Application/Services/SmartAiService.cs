using SmartAIPropertyCitizen.Api.Core.Domain;
using SmartAIPropertyCitizen.Api.Core.Interfaces;

namespace SmartAIPropertyCitizen.Api.Application.Services
{
    public class SmartAiService : ISmartAiService
    {
        private readonly IPropertyTaxService _propertyTaxService;
        private readonly IOtpService _otpService;
        private readonly IConfiguration _config;

        public SmartAiService(IPropertyTaxService propertyTaxService, IOtpService otpService, IConfiguration config)
        {
            _propertyTaxService = propertyTaxService;
            _otpService = otpService;
            _config = config;
        }

        public async Task<ChatResponse> ProcessChatAsync(ChatRequest request)
        {
            var l = request.Language;
            var i18n = GetI18nDictionary();

            if (!i18n.ContainsKey(l)) l = "mr";

            // 1. Verify Session
            if (string.IsNullOrEmpty(request.SessionId) || !Guid.TryParse(request.SessionId, out Guid guidSessionId))
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
                string apiKey = _config["OpenAI:ApiKey"];
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
  ""reply"": ""If GENERAL, provide a helpful polite response in the requested language. Otherwise leave empty.""
}}";

                var completion = await chatClient.CompleteChatAsync(new OpenAI.Chat.ChatMessage[]
                {
                    new OpenAI.Chat.SystemChatMessage(systemPrompt),
                    new OpenAI.Chat.UserChatMessage(message)
                });

                string jsonContent = completion.Value.Content[0].Text.Trim();
                if (jsonContent.StartsWith("```json")) 
                {
                    jsonContent = jsonContent.Substring(7);
                    jsonContent = jsonContent.Substring(0, jsonContent.LastIndexOf("```"));
                }

                var aiDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                intent = aiDoc.RootElement.GetProperty("intent").GetString()?.ToUpper() ?? "GENERAL";
                aiReply = aiDoc.RootElement.GetProperty("reply").GetString() ?? "";
            }
            catch (Exception)
            {
                // Fallback to static matching if AI fails
                if (message.Contains("मागणी") || message.Contains("demand") || message.Contains("मांग")) intent = "DEMAND";
                else if (message.Contains("पावती") || message.Contains("receipt") || message.Contains("रसीद")) intent = "RECEIPT";
                else if (message.Contains("पेमेंट") || message.Contains("payment") || message.Contains("भरणा")) intent = "PAYMENT";
                else if (message.Contains("नोटीस") || message.Contains("notice") || message.Contains("डाउनलोड")) intent = "NOTICE";
            }

            // 3. Logic Mapping
            if (intent == "DEMAND")
            {
                var data = await _propertyTaxService.GetHeadwiseTaxDetailsAsync(ownerId);
                return new ChatResponse { ResponseText = i18n[l]["dem"], Data = data, SessionId = request.SessionId };
            }
            else if (intent == "RECEIPT")
            {
                var receipts = await _propertyTaxService.GetPreviousReceiptsAsync(ownerId);
                return new ChatResponse { ResponseText = i18n[l]["rec"], Data = receipts, SessionId = request.SessionId };
            }
            else if (intent == "PAYMENT")
            {
                return new ChatResponse { 
                    ResponseText = i18n[l]["pay"], 
                    PaymentUrl = $"https://akolamc.in/onlinepayment?UniqID={session.UpicNo}", 
                    SessionId = request.SessionId 
                };
            }
            else if (intent == "NOTICE")
            {
                return new ChatResponse { 
                    ResponseText = i18n[l]["not"], 
                    DownloadUrl = $"https://akolamc.in/Download/Index?B={session.UpicNo}", 
                    SessionId = request.SessionId 
                };
            }

            // General AI response
            if (!string.IsNullOrEmpty(aiReply))
            {
                return new ChatResponse { ResponseText = aiReply, SessionId = request.SessionId };
            }

            return new ChatResponse { ResponseText = i18n[l]["error"] };
        }

        private Dictionary<string, Dictionary<string, string>> GetI18nDictionary()
        {
            return new Dictionary<string, Dictionary<string, string>>
            {
                ["mr"] = new() {
                    ["dem"] = "येथे आपल्या मागणीचा तपशील आहे:",
                    ["rec"] = "येथे आपल्या मागील पावत्यांची यादी आहे:",
                    ["pay"] = "आपण खालील लिंकवर क्लिक करून ऑनलाइन पेमेंट करू शकता:",
                    ["not"] = "आपण खालील लिंकवरून नोटीस डाउनलोड करू शकता:",
                    ["error"] = "क्षमस्व, कृपया पुन्हा विचारा. (उदा. माझी मागणी किती आहे?)",
                    ["login"] = "कृपया प्रथम लॉगइन करा."
                },
                ["hi"] = new() {
                    ["dem"] = "यहाँ आपकी मांग का विवरण है:",
                    ["rec"] = "यहाँ आपकी पिछली रसीदों की सूची है:",
                    ["pay"] = "आप नीचे दिए गए लिंक पर क्लिक करके ऑनलाइन भुगतान कर सकते हैं:",
                    ["not"] = "आप नीचे दिए गए लिंक से नोटिस डाउनलोड कर सकते हैं:",
                    ["error"] = "क्षमा करें, कृपया फिर से पूछें। (जैसे: मेरी मांग कितनी है?)",
                    ["login"] = "कृपया पहले लॉगिन करें।"
                },
                ["en"] = new() {
                    ["dem"] = "Here are your demand details:",
                    ["rec"] = "Here is the list of your previous receipts:",
                    ["pay"] = "You can make an online payment by clicking the link below:",
                    ["not"] = "You can download the notice from the link below:",
                    ["error"] = "Sorry, please ask again. (e.g. What is my demand?)",
                    ["login"] = "Please login first."
                }
            };
        }
    }
}
