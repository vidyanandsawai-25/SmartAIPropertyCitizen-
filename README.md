# SmartAI Property Citizen Portal 🏛️🤖

A next-generation, AI-powered multilingual property tax management portal designed for **Akola Municipal Corporation (AMC)**. This application allows citizens to securely log in via OTP, view their property tax demands, download receipts, make online payments, and chat with an intelligent AI assistant in Marathi, Hindi, and English.

![SmartAI Portal](https://akolamc.in/images/councilLogo/akola.png)

## 🚀 Features

- **True AI Intent Detection**: Powered by OpenAI (`gpt-4o`), the chatbot dynamically understands citizen queries in natural language, automatically categorizing intents (Demand, Receipts, Payments) regardless of typos or phrasing.
- **Multilingual UI**: Seamless, instant switching between Marathi (मराठी), Hindi (हिंदी), and English.
- **Secure OTP Verification**: Integrated with an SMS gateway to ensure strict access control using the citizen's registered mobile number and Property/UPIC code.
- **Mathematical Tax Tables**: Highly optimized, mobile-responsive tables that display live financial data directly from the AMC SQL Database.
- **Unified Deployment**: The Next.js static frontend is elegantly bundled inside the ASP.NET Core API for a seamless, single-port IIS deployment.

## 🏗️ Architecture Stack

This project is divided into two heavily integrated architectures:

1. **Frontend (`/smartai-property-web`)**
   - **Framework**: Next.js 16 / React
   - **Styling**: TailwindCSS & Framer Motion (micro-animations)
   - **Build**: Static HTML/JS Export for zero-latency hosting.

2. **Backend (`/SmartAIPropertyCitizen.Api`)**
   - **Framework**: .NET 10.0 (ASP.NET Core Web API)
   - **Database**: SQL Server (via Dapper)
   - **AI Engine**: Official OpenAI ChatClient SDK
   - **Structure**: Clean Architecture (Core, Application, Infrastructure, API).

## ⚙️ Running Locally

### Prerequisites
- Node.js (v18+)
- .NET 10.0 SDK
- SQL Server (with AMC Database schema)
- OpenAI API Key

### Backend Setup
1. Navigate to `SmartAIPropertyCitizen.Api/appsettings.json`.
2. Configure your `DefaultConnection` string.
3. Insert your OpenAI API Key inside the `"OpenAI"` block.
4. Run the API:
   ```bash
   cd SmartAIPropertyCitizen.Api
   dotnet run
   ```

### Frontend Setup
1. Navigate to `smartai-property-web`.
2. Install dependencies: `npm install`.
3. Run the development server:
   ```bash
   npm run dev
   ```

## 🌍 IIS Production Deployment

This project is built to deploy as a unified single-folder application.

1. Navigate to the frontend directory and build the static export:
   ```bash
   npm run build
   ```
2. Copy the contents of the generated `out` folder into the Backend's `wwwroot` directory.
3. Publish the .NET Backend:
   ```bash
   dotnet publish -c Release -o ./IIS_Publish
   ```
4. Point your IIS Application's **Physical Path** to the `IIS_Publish` folder.
5. Set the IIS Application Pool to **No Managed Code**.

## 🔒 Security & AI Configuration
- The OpenAI API Key is completely hidden from the frontend and secured within the ASP.NET Core environment.
- The AI is strictly instructed via a **System Prompt** (`SmartAiService.cs`) to act purely as a municipal assistant. It intercepts intents and maps them securely to Dapper SQL queries without allowing arbitrary data manipulation.

---
**Design & Developed by Sthapatya Consultants**
