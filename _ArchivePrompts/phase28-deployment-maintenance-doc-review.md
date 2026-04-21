# Role: Principal Site Reliability Engineer (SRE) / Red Team Auditor
# Task: Architecture & Runbook Stress-Test
# Language: Hungarian (Professional/Technical)

**Context:**
Below is the final Deployment and Operations Documentation for "Dusk of Coding," an AI-assisted code evaluation platform (Blazor Server, Keycloak OIDC, RabbitMQ, Worker Service, Roslyn Sandbox). It includes a C4 Architecture overview and step-by-step Runbooks for Azure, On-Premise, and Docker Compose environments.

**Your Mission:**
Do NOT praise the document. Do NOT rewrite the document. Your job is to act as an aggressive but constructive SRE Auditor. You must actively search for Single Points of Failure (SPOF), security vulnerabilities, and missing operational steps in the provided Runbook.

**Execute the following 3-Phase Audit:**

### Phase 1: Security & Identity Audit (The Keycloak/OIDC Threat Model)
Analyze the On-Premise (IIS) and Azure deployment steps specifically regarding the Keycloak integration.
1. Are there specific missing steps in the IIS Reverse Proxy setup that will break OIDC token validation or callback URLs (e.g., specific missing Forwarded Headers)?
2. In the Azure Container Apps setup, is the connection between the WebAPI and Keycloak secure? What happens if the `Keycloak__Authority` URL is misconfigured between internal/external networks?

### Phase 2: System Resilience & Deadlock Audit (The RabbitMQ/Worker Flow)
Analyze the decoupled architecture (WebAPI -> RabbitMQ -> TutorWorker -> Gemini LLM).
1. The Azure Runbook mentions setting `--min-replicas 1` for the Worker. What happens to the RabbitMQ queues if the LLM provider (Gemini) suffers a total outage? Is there a missing Dead Letter Queue (DLQ) or Circuit Breaker configuration mentioned in the Runbook?
2. In the Docker Compose environment, what happens if the Roslyn execution Sandbox (which runs inside the WebAPI/Worker) causes a memory leak? Does the Runbook address container memory limits?

### Phase 3: The "What's Missing" Report
Identify exactly 3 **critical operational commands or configurations** that are completely missing from the Step-by-Step Runbooks, which a Junior DevOps engineer would immediately stumble upon during a real 3:00 AM deployment.

**Output Format:**
Provide your audit report in a structured, direct, and highly technical Hungarian format. Do not use fluff. Use bullet points and focus strictly on vulnerabilities and missing steps.

---
**[INSERT THE COMPLETE DUSK OF CODING DOCUMENTATION HERE]**