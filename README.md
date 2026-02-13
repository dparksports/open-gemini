# Super Agent 🦸‍♂️

![Super Agent Dashboard](Assets/app_dashboard.png)

**Super Agent** is an autonomous AI assistant for Windows that bridges the gap between local execution and cloud intelligence. It lives in your system tray, manages its own Python environment, and can extend its capabilities dynamically through a plugin-based skill system.

It is designed to be **proactive**, **private**, and **powerful**—capable of browsing the web, running code, seeing your screen, and hearing your voice.

## 🌟 Key Capabilities

### 🧠 Autonomous Execution
- **Run Python Code**: Automatically writes and executes Python scripts in a managed, isolated environment (`venv`).
- **Browse the Web**: Uses headless Chromium (via Playwright) to research topics, scrape data, and summarize web pages.
- **File Operations**: Reads, writes, and manages files on your local system with permission.

### 👁️ Vision & Perception
- **Vision Language Model**: Extracts text and understands images using local VLM (offline).
- **Screen Awareness**: Can "see" your screen contents to assist with on-screen tasks.
- **Computer Vision**: Analyzes images for object detection and scene understanding.

### 🗣️ Voice & Communications
- **Batch Attention 2 Layer Fusion**: Transcribes audio and video files locally using advanced fusion models.
- **VoIP Calling**: Capable of placing SIP calls to interact with real-world phone systems.
- **Messaging**: Sends notifications and alerts via Apprise integration.

### 🧩 Dynamic Skill System
- **Hot-Swappable Skills**: Drop Python or PowerShell scripts into `%APPDATA%\OpenClaw\Skills` to instantly teach the agent new tricks.
- **Self-Improving**: The agent can write its own skills to solve recurring problems.

### 💾 Long-Term Memory
- **Vector Database**: Memorizes conversations and facts using local SQLite with vector embeddings.
- **Context-Aware**: Recalls past interactions to provide continuity across sessions.

## 🛠️ Technology Stack

- **Core**: Windows App SDK (WinUI 3) / .NET 10 (C#)
- **AI Orchestration**: Microsoft Semantic Kernel
- **Local Inference**: ONNX Runtime GenAI, Whisper.net
- **Scripting Engine**: Python 3.10+ (Managed Venv), PowerShell Core
- **Database**: SQLite (Data) + Qdrant/Faiss (Vector Memory)

## 🚀 Getting Started

### Prerequisites
- **OS**: Windows 10 (Build 19041) or Windows 11.
- **Python**: Python 3.10+ installed and added to `PATH`.
- **API Keys**: A Gemini API key (for reasoning) or OpenAI key.

### Installation
1.  **Download** the latest release (`super-agent-win-x64.zip`) from GitHub.
2.  **Extract** to a folder of your choice.
3.  **Run** `super-agent.exe`.
4.  **Setup**:
    - The agent will automatically initialize its Python environment on first run (this may take a few minutes).
    - Enter your API keys when prompted.
5.  **Usage**:
    - Look for the 🦸‍♂️ icon in your system tray.
    - Click to open the chat interface.
    - Type or speak your request!

## 🔒 Privacy & Security

- **Local First**: Transcription, OCR, and Memory storage happen locally on your device.
- **Sandboxed Execution**: Python code runs in a dedicated virtual environment.
- **User Control**: You permit sensitive actions (like file deletion or network calls).

---
*Made with ❤️ in California*
