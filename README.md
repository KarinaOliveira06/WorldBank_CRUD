# 🏦 WorldBank CRUD

This project is the evolution of my banking logic studies, moving from a local console script to a **distributed backend ecosystem**. It focuses on integrating a high-performance C# transactional core with agile Python automation workers.

---

## 🏗️ Project Roadmap

### **Phase 1: The Foundation (Backend C# & EF Core)**
Development of a headless **ASP.NET Core Web API** to manage financial data via JSON.
* **ORM:** Using Entity Framework Core for database mapping.
* **Database:** Starting with **SQLite** for development agility, with a planned migration to **PostgreSQL**.

### **Phase 2: Resilience & Security (Middlewares & JWT)**
Hardening the API for production-ready scenarios.
* **Middlewares:** Global exception handling to prevent crashes and centralized logging for auditing.
* **Authentication:** Implementing **JWT (JSON Web Tokens)** to replace simple login logic with secure, encrypted access.

### **Phase 3: The Post Office (Messaging)**
Enabling event-driven communication between different services.
* **Broker:** Integration with **RabbitMQ**.
* **Logic:** The C# API will publish events to queues whenever critical actions occur (e.g., "High-value withdrawal detected").

### **Phase 4: Automation Workers (Python, AI & Twilio)**
Independent Python scripts acting as "Smart Workers" that listen to the message queue.
* **Asynchronous Tasks:** Python consumes RabbitMQ messages to trigger background jobs.
* **Services:** Automated SMS notifications via **Twilio** and customer profile analysis using **AI APIs**.

### **Phase 5: The Interface (Frontend)**
A clean, responsive UI to interact with the API.
* **Stack:** Pure **HTML5/CSS3** (Vanilla) to master web foundations before moving into frameworks.

---

## 🐋 Infrastructure (Docker)
The entire ecosystem—PostgreSQL, RabbitMQ, the .NET API, and Python Workers—is designed to be containerized using **Docker & Docker Compose**. This ensures the environment is consistent, isolated, and can be launched with a single command.

---

## 🛠️ Tech Stack Matrix
| Component | Technology |
| :--- | :--- |
| **Core API** | .NET 10 (C#) |
| **Automation** | Python 3.12+ |
| **Persistence** | EF Core / PostgreSQL |
| **Messaging** | RabbitMQ |
| **Infrastructure** | Docker |
| **Testing** | Insomnia |
