# Dusk Of Coding – C4 Diagrams (Mermaid)

This document contains C4-style diagrams for the current solution using Mermaid syntax.

## 1) System Context

```mermaid
C4Context
	title Dusk Of Coding - System Context

	Person(student, "Student", "Submits C# solutions and receives tutor feedback")
	Person(tutor, "Tutor", "Creates tasks and requests AI-generated tests")
	Person(admin, "Admin", "Monitors platform telemetry and configuration")

	System(dusk, "Dusk Of Coding Platform", "Coding practice, evaluation, and AI tutoring")

	System_Ext(keycloak, "Keycloak", "OIDC identity and access management")
	System_Ext(openai, "LLM Provider", "OpenAI/Azure OpenAI used for tutoring and test generation")

	Rel(student, dusk, "Uses")
	Rel(tutor, dusk, "Uses")
	Rel(admin, dusk, "Operates")

	Rel(dusk, keycloak, "Authenticates users via OIDC/JWT")
	Rel(dusk, openai, "Requests AI responses and test generation")
```

## 2) Container Diagram

```mermaid
C4Container
	title Dusk Of Coding - Container Diagram

	Person(student, "Student")
	Person(tutor, "Tutor")
	Person(admin, "Admin")

	System_Ext(keycloak, "Keycloak", "Authentication and roles")
	System_Ext(openai, "LLM Provider", "OpenAI / Azure OpenAI")

	System_Boundary(dusk, "Dusk Of Coding") {
		Container(webui, "WebUi", "Blazor Server (.NET 10)", "Interactive UI, auth flow, API calls, SignalR client")
		Container(webapi, "WebApi", "ASP.NET Core Minimal API (.NET 10)", "Business endpoints, SignalR hub, RabbitMQ publish/consume")
		Container(executionapi, "ExecutionApi", "ASP.NET Core API (.NET 10)", "Compiles and executes submitted code in sandbox")
		Container(tutorworker, "TutorWorker", "Worker Service (.NET 10)", "Consumes submission/test-gen messages and performs AI workflows")
		ContainerDb(mariadb, "MariaDB", "Relational DB", "Tasks, tests, submissions, feedback, telemetry")
		ContainerQueue(rabbitmq, "RabbitMQ", "Message broker", "Async submission and tutor/test-generation pipelines")
	}

	Rel(student, webui, "Uses", "HTTPS")
	Rel(tutor, webui, "Uses", "HTTPS")
	Rel(admin, webui, "Uses", "HTTPS")

	Rel(webui, keycloak, "Login / OIDC")
	Rel(webui, webapi, "Calls REST endpoints", "HTTP")
	Rel(webui, webapi, "Receives tutor updates", "SignalR / WebSocket")

	Rel(webapi, keycloak, "Validates JWT")
	Rel(webapi, mariadb, "Reads/writes domain data", "EF Core")
	Rel(webapi, rabbitmq, "Publishes submissions and test-generation commands")
	Rel(webapi, rabbitmq, "Consumes tutor responses + test-gen results via bridges")

	Rel(tutorworker, rabbitmq, "Consumes submissions and test-generation commands")
	Rel(tutorworker, rabbitmq, "Publishes tutor responses and test-generation results")
	Rel(tutorworker, mariadb, "Reads/writes submissions, feedback, telemetry", "EF Core")
	Rel(tutorworker, executionapi, "Executes code for evaluation", "HTTP")
	Rel(tutorworker, openai, "Generates Socratic feedback and tests")

	Rel(webapi, executionapi, "Delegates code execution via infrastructure engine", "HTTP")
```

## 3) Component Diagram – WebApi Container

```mermaid
C4Component
	title Dusk Of Coding - WebApi Components

	Container_Boundary(webapi_boundary, "WebApi") {
		Component(apiEndpoints, "Minimal API Endpoints", "Program.cs", "Tasks, submissions, feedback, telemetry, tutor stats")
		Component(signalrHub, "TutorHub", "SignalR Hub", "Real-time channel for tutor and test-generation notifications")
		Component(tutorBridge, "TutorResponseBridge", "BackgroundService", "Consumes tutor response messages and forwards to SignalR groups")
		Component(testGenBridge, "TestGenerationBridge", "BackgroundService", "Consumes test-generation result messages and forwards to SignalR users")
		Component(appServices, "Application Services", "DuskOfCoding.Application", "TaskService, SubmissionService, FeedbackService")
		Component(infraServices, "Infrastructure Services", "DuskOfCoding.Infrastructure", "Repositories, execution engine, telemetry")
		Component(rabbitService, "RabbitMQService", "Messaging Adapter", "Publishes and consumes broker messages")
		Component(auth, "AuthN/AuthZ", "Keycloak JWT + ASP.NET Authorization", "Secures endpoints and hub")
	}

	Container_Ext(webui, "WebUi", "Blazor Server")
	Container_Ext(executionapi, "ExecutionApi", "Execution API")
	Container_Ext(rabbitmq, "RabbitMQ", "Message Broker")
	ContainerDb_Ext(mariadb, "MariaDB", "Relational DB")
	System_Ext(keycloak, "Keycloak", "OIDC/JWT")

	Rel(webui, apiEndpoints, "Calls REST APIs")
	Rel(webui, signalrHub, "Subscribes for real-time updates")

	Rel(apiEndpoints, auth, "Applies auth policies")
	Rel(auth, keycloak, "Validates issuer and claims")

	Rel(apiEndpoints, appServices, "Invokes use-cases")
	Rel(appServices, infraServices, "Uses repositories/execution engine")
	Rel(infraServices, mariadb, "Persists and queries data")
	Rel(infraServices, executionapi, "Calls /api/executions")

	Rel(apiEndpoints, rabbitService, "Publishes submission/test-gen events")
	Rel(tutorBridge, rabbitService, "Consumes tutor response events")
	Rel(testGenBridge, rabbitService, "Consumes test-generation result events")
	Rel(rabbitService, rabbitmq, "AMQP publish/consume")

	Rel(tutorBridge, signalrHub, "Pushes ReceiveTutorResponse")
	Rel(testGenBridge, signalrHub, "Pushes TestSuiteGenerationCompleted")
```

## 4) Component Diagram – TutorWorker Container

```mermaid
C4Component
	title Dusk Of Coding - TutorWorker Components

	Container_Boundary(worker_boundary, "TutorWorker") {
		Component(tutorWorker, "TutorWorkerService", "BackgroundService", "Processes submission messages, executes code, creates tutor responses")
		Component(testWorker, "TestGenerationWorkerService", "BackgroundService", "Processes test generation commands and stores generated tests")
		Component(chatClient, "Configurable Chat Client", "Microsoft.Extensions.AI", "Invokes OpenAI/Azure OpenAI models")
		Component(executionEngine, "HttpCodeExecutionEngine", "Infrastructure service", "Calls execution API")
		Component(repositories, "Repositories + DbContext", "Infrastructure", "Persists submissions, feedback, tests, telemetry")
		Component(rabbitService, "RabbitMQService", "Messaging Adapter", "Consumes and publishes messages")
		Component(mcpTools, "MCP Tools", "ModelContextProtocol", "Optional tooling for analysis and custom test execution")
	}

	Container_Ext(rabbitmq, "RabbitMQ", "Message Broker")
	Container_Ext(executionapi, "ExecutionApi", "Code compile/execute service")
	ContainerDb_Ext(mariadb, "MariaDB", "Relational DB")
	System_Ext(openai, "LLM Provider", "OpenAI/Azure OpenAI")

	Rel(rabbitService, rabbitmq, "Consumes: submissions, test generation commands")
	Rel(rabbitService, rabbitmq, "Publishes: tutor responses, test generation results")

	Rel(tutorWorker, rabbitService, "Consumes/Publishes pipeline messages")
	Rel(testWorker, rabbitService, "Consumes/Publishes pipeline messages")

	Rel(tutorWorker, executionEngine, "Evaluates submissions")
	Rel(executionEngine, executionapi, "POST /api/executions")

	Rel(tutorWorker, chatClient, "Generates Socratic feedback")
	Rel(testWorker, chatClient, "Generates xUnit test suites")
	Rel(chatClient, openai, "LLM inference calls")

	Rel(tutorWorker, repositories, "Loads tasks/submissions and saves feedback/status")
	Rel(testWorker, repositories, "Replaces task tests and writes telemetry")
	Rel(repositories, mariadb, "EF Core persistence")

	Rel(tutorWorker, mcpTools, "Uses optional tool invocations")
```
