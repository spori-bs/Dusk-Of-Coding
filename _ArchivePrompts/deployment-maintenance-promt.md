# Role: Senior Solutions Architect & Technical Writer
# Task: Create a comprehensive Deployment and Operations Documentation for "Dusk of Coding"

## Language Requirements:
- Bilingual: English and Hungarian.
- The Hungarian version must be highly professional ("kiváló magyarság"), but keep technical industry terms in English (e.g., Deployment, Middleware, Containerization, On-Premise, Throughput, Deadlock).

## Content Structure:

1. **Project Goal & Overview**: Explain the "Dusk of Coding" philosophy as an AI-awareness and code mastery platform.
2. **Actors**:
   - Students (Learning & submitting code).
   - Tutors (Managing tasks & reviewing analytics).
   - System Administrators (Infrastructure & IAM management).
3. **C4 Architecture (Level 1 & 2)**:
   - Provide a Mermaid.js C4 Context and Container diagram.
   - Describe the interactions between Blazor UI, WebAPI, Keycloak, RabbitMQ, and the TutorWorker.
4. **Operations & Deployment Strategies (Detailed Guides)**:
   - **Scenario A: Azure Cloud**: Focus on Container Apps or App Service, Azure SQL, and Azure Service Bus.
   - **Scenario B: On-Premise Windows (IIS/WAS)**: Document the hosting of Blazor Server on IIS, Windows Services for workers, and SQL Server setup. Include "Reverse Proxy" configuration for Keycloak.
   - **Scenario C: Fully Containerized (Docker/K8s)**: Document Docker Compose for staging and Kubernetes (Helm charts) for production.
5. **Monitoring & Maintenance**:
   - Observability using the LlmTelemetryLog.
   - Log rotation and health check endpoints.