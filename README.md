# SAP Azure Integration Functions

## Overview

This project is an Azure Functions application that provides integration capabilities between SAP systems and various external systems. It's designed to handle data extraction, transformation, and communication with multiple services including SAP SuccessFactors, databases, FTP servers, and message queues.

## Architecture

The solution consists of several Azure Functions that work together to provide a comprehensive integration platform:

### Core Functions

1. **RcaCafFunction** (`ExtraerYEncolarDatosFiscales`)
   - Extracts fiscal data from various sources
   - Processes HTTP requests with company, restaurant, and establishment codes
   - Integrates with SQL Server databases
   - Queues messages to Azure Service Bus

2. **IntegrationFunction** (`FtpConnection`)
   - Manages FTP connections and file operations
   - Downloads and processes files from external FTP servers
   - Handles file parsing and data transformation

3. **SuccessFactorsFunction**
   - Integrates with SAP SuccessFactors HR system
   - Handles employee data synchronization
   - Processes HR-related business logic

4. **ExtraccionAsincronaRcaCaf**
   - Asynchronous data extraction processes
   - Background processing capabilities

5. **GestionarColasServiceBus**
   - Service Bus queue management
   - Message routing and processing

6. **GetEntitiesInStatePending**
   - Retrieves entities with pending status
   - Database state management

7. **UpdateStateAndExecuteSP**
   - Updates entity states
   - Executes stored procedures

## Technology Stack

- **.NET 6.0** - Runtime framework
- **Azure Functions v4** - Serverless compute platform
- **Azure Service Bus** - Message queuing service
- **Azure Table Storage** - NoSQL data storage
- **Azure Key Vault** - Secrets management
- **SQL Server** - Relational database
- **RabbitMQ** - Additional message queuing
- **SAP NetWeaver RFC** - SAP system connectivity

## Key Dependencies

```xml
<PackageReference Include="Azure.Identity" Version="1.9.0" />
<PackageReference Include="Microsoft.Azure.WebJobs.Extensions.ServiceBus" Version="5.11.0" />
<PackageReference Include="Microsoft.Azure.WebJobs.Extensions.Storage" Version="4.0.5" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.2.0-beta.5" />
<PackageReference Include="SapNwRfc" Version="1.1.0" />
<PackageReference Include="System.Data.SqlClient" Version="4.8.5" />
```

## Configuration

The application uses Azure Functions configuration through `local.settings.json` (for development) and Azure App Settings (for production). Key configuration areas include:

### Database Connections
- RTF Database connection
- Business system database connections
- Table storage connections

### External Systems
- SAP system credentials and connection strings
- FTP server configurations
- SuccessFactors API settings

### Message Queues
- Azure Service Bus connection strings
- RabbitMQ configurations
- Queue names and routing

### Security
- Azure Key Vault integration
- Managed Identity authentication
- API keys and subscription keys

## Features

### Data Integration
- **Multi-source data extraction**: Supports various data sources including databases, FTP servers, and web services
- **Real-time processing**: HTTP-triggered functions for immediate data processing
- **Batch processing**: Timer-triggered functions for scheduled operations

### SAP Integration
- **SuccessFactors connectivity**: Direct integration with SAP SuccessFactors for HR data
- **RFC connectivity**: Native SAP NetWeaver RFC support for real-time SAP data access
- **Data transformation**: Automatic data mapping and transformation between systems

### Message Processing
- **Asynchronous processing**: Service Bus integration for reliable message handling
- **Dead letter handling**: Error handling and retry mechanisms
- **Message routing**: Intelligent message routing based on content and type

### File Processing
- **FTP integration**: Automated file download and processing from FTP servers
- **Multiple file formats**: Support for various file formats (XML, JSON, CSV)
- **File validation**: Data validation and error handling

## Security Features

- **Azure Managed Identity**: Secure authentication without storing credentials
- **Azure Key Vault**: Centralized secrets management
- **Connection string encryption**: Secure storage of sensitive configuration
- **RBAC integration**: Role-based access control for Azure resources

## Deployment

### Prerequisites
- Azure subscription
- Azure Functions runtime
- SQL Server databases
- SAP system access
- Appropriate Azure services (Service Bus, Key Vault, Storage Account)

### Local Development
1. Clone the repository
2. Configure `local.settings.json` with your development settings
3. Install required NuGet packages
4. Run using Azure Functions Core Tools or Visual Studio

### Production Deployment
- Deploy to Azure Functions using Azure DevOps, GitHub Actions, or Visual Studio
- Configure application settings in Azure portal
- Set up managed identity and Key Vault access
- Configure monitoring and logging

## Monitoring and Logging

The application includes comprehensive logging using:
- Azure Application Insights
- ILogger framework
- Custom telemetry and metrics
- Error tracking and alerting

## Data Models

The project includes several data models for different integration scenarios:

- **BuxisResponse**: Business system response models
- **RTFResponse**: Tax/fiscal data response models  
- **TableStorageBuxis**: Azure Table Storage entities
- **SuccessFactors entities**: HR data models

## Error Handling

- Comprehensive exception handling
- Retry mechanisms for transient failures
- Dead letter queue support
- Detailed error logging and monitoring

## Performance Considerations

- Connection pooling for database operations
- Asynchronous processing patterns
- Efficient memory management
- Scalable architecture design

## Contributing

When contributing to this project:

1. Follow .NET coding conventions
2. Include unit tests for new functionality
3. Update documentation for any changes
4. Ensure security best practices are followed
5. Test integration with all dependent systems

## License

This project is licensed under the terms specified in the LICENSE file.

## Support

For support and questions, please refer to the project documentation or contact the development team.

---

**Note**: This is a sanitized version of the original project. All sensitive information including credentials, server names, and company-specific details have been replaced with example values for security purposes.
