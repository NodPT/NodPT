using Microsoft.EntityFrameworkCore;
using NodPT.Data;
using NodPT.Data.Models;
using System.Runtime.CompilerServices;

public static class DatabaseHelper
{
    static string connectionString = string.Empty;

    public static void SetConnectionString(string connStr)
    {
        connectionString = connStr;
    }

    public static NodPTDbContext CreateDbContext()
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is not set. Please set it before creating a DbContext.");

        var optionsBuilder = new DbContextOptionsBuilder<NodPTDbContext>();
        
        // Use Pomelo MySQL provider
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new NodPTDbContext(optionsBuilder.Options);
    }

    private static void CreateSampleData()
    {
        using var context = CreateDbContext();

        // Check if data already exists
        if (context.Users.Any())
        {
            return; // Data already exists
        }

        // Begin transaction
        using var transaction = context.Database.BeginTransaction();

        try
        {
            // Create sample users
            var user1 = new User
            {
                FirebaseUid = "sample_user_001",
                Email = "john.doe@example.com",
                DisplayName = "John Doe",
                PhotoUrl = "https://example.com/photos/john.jpg",
                Active = true,
                Approved = true,
                Banned = false,
                IsAdmin = true, // Set as admin
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow.AddHours(-2)
            };

            var user2 = new User
            {
                FirebaseUid = "sample_user_002",
                Email = "jane.smith@example.com",
                DisplayName = "Jane Smith",
                PhotoUrl = "https://example.com/photos/jane.jpg",
                Active = true,
                Approved = true,
                Banned = false,
                IsAdmin = false, // Regular user
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                LastLoginAt = DateTime.UtcNow.AddMinutes(-30)
            };

            context.Users.Add(user1);
            context.Users.Add(user2);
            context.SaveChanges();

            // Create sample templates
            var template1 = new Template
            {
                Name = "Data Processing Workflow",
                Description = "A template for data processing and analysis workflows",
                Category = "Data Science",
                Version = "1.0.0",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            };

            var template2 = new Template
            {
                Name = "AI Assistant Workflow",
                Description = "A template for AI-powered assistant workflows",
                Category = "AI/ML",
                Version = "1.2.0",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-35)
            };

            context.Templates.Add(template1);
            context.Templates.Add(template2);
            context.SaveChanges();

            // Create sample template files
            var templateFile1 = new TemplateFile
            {
                Name = "workflow_config.json",
                Path = "/config/workflow_config.json",
                Extension = ".json",
                Size = 256,
                MimeType = "application/json",
                Content = "{\"workflow\": {\"type\": \"data_processing\", \"version\": \"1.0\", \"steps\": [\"load\", \"process\", \"analyze\"]}}",
                TemplateId = template1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-38)
            };

            var templateFile2 = new TemplateFile
            {
                Name = "data_schema.yaml",
                Path = "/schemas/data_schema.yaml",
                Extension = ".yaml",
                Size = 512,
                MimeType = "text/yaml",
                Content = "schema:\n  type: object\n  properties:\n    id: {type: integer}\n    name: {type: string}\n    data: {type: array}",
                TemplateId = template1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-37)
            };

            var templateFile3 = new TemplateFile
            {
                Name = "ai_config.json",
                Path = "/config/ai_config.json",
                Extension = ".json",
                Size = 384,
                MimeType = "application/json",
                Content = "{\"ai\": {\"model\": \"gpt-4\", \"temperature\": 0.7, \"max_tokens\": 2000, \"system_prompt\": \"You are a helpful AI assistant.\"}}",
                TemplateId = template2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            var templateFile4 = new TemplateFile
            {
                Name = "prompt_templates.txt",
                Path = "/templates/prompt_templates.txt",
                Extension = ".txt",
                Size = 1024,
                MimeType = "text/plain",
                Content = "User Question Template:\nUser: {user_input}\nContext: {context}\nPlease provide a helpful response.\n\nSystem Response Template:\nBased on the provided context, here is my response: {response}",
                TemplateId = template2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-32)
            };

            context.TemplateFiles.AddRange(templateFile1, templateFile2, templateFile3, templateFile4);
            context.SaveChanges();

            // Create sample template nodes
            var templateNode1 = new Node
            {
                Id = "template_node_001",
                Name = "Data Input Node",
                NodeType = NodeType.Data,
                Properties = "{\"input_type\":\"csv\",\"delimiter\":\",\",\"encoding\":\"utf-8\"}",
                Status = "template",
                TemplateId = template1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-36)
            };

            var templateNode2 = new Node
            {
                Id = "template_node_002",
                Name = "Data Processor Node",
                NodeType = NodeType.Action,
                Properties = "{\"processing_type\":\"transform\",\"operations\":[\"clean\",\"normalize\",\"validate\"]}",
                Status = "template",
                TemplateId = template1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-36)
            };

            var templateNode3 = new Node
            {
                Id = "template_node_003",
                Name = "Analysis Node",
                NodeType = NodeType.Action,
                Properties = "{\"analysis_type\":\"statistical\",\"metrics\":[\"mean\",\"median\",\"std_dev\"]}",
                Status = "template",
                TemplateId = template1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-36)
            };

            var templateNode4 = new Node
            {
                Id = "template_node_004",
                Name = "AI Input Node",
                NodeType = NodeType.Data,
                Properties = "{\"input_type\":\"text\",\"max_length\":1000}",
                Status = "template",
                TemplateId = template2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            var templateNode5 = new Node
            {
                Id = "template_node_005",
                Name = "AI Processing Node",
                NodeType = NodeType.Action,
                Properties = "{\"model\":\"gpt-4\",\"temperature\":0.7,\"max_tokens\":2000}",
                Status = "template",
                TemplateId = template2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            var templateNode6 = new Node
            {
                Id = "template_node_006",
                Name = "Response Formatter",
                NodeType = NodeType.Panel,
                Properties = "{\"format\":\"json\",\"include_metadata\":true}",
                Status = "template",
                TemplateId = template2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            context.Nodes.AddRange(templateNode1, templateNode2, templateNode3, templateNode4, templateNode5, templateNode6);
            context.SaveChanges();

            // Create sample projects
            var project1 = new Project
            {
                Name = "Customer Data Analysis",
                Description = "Analyze customer behavior data for insights",
                UserId = user1.Id,
                TemplateId = template1.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            };

            var project2 = new Project
            {
                Name = "AI Support Bot",
                Description = "Intelligent customer support chatbot",
                UserId = user2.Id,
                TemplateId = template2.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            context.Projects.Add(project1);
            context.Projects.Add(project2);
            context.SaveChanges();

            // Create sample folders
            var folder1 = new Folder
            {
                Name = "src",
                Path = "/src",
                ProjectId = project1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-18)
            };

            var folder2 = new Folder
            {
                Name = "data",
                Path = "/data",
                ProjectId = project1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-18)
            };

            var folder3 = new Folder
            {
                Name = "components",
                Path = "/src/components",
                ProjectId = project2.Id,
                ParentId = folder1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };

            context.Folders.AddRange(folder1, folder2, folder3);
            context.SaveChanges();

            // Create sample files
            var file1 = new ProjectFile
            {
                Name = "data_processor.py",
                Path = "/src/data_processor.py",
                Extension = ".py",
                MimeType = "text/x-python",
                Size = 2048,
                Content = "# Data processing module\nimport pandas as pd\n\ndef process_data(df):\n    return df.dropna()",
                FolderId = folder1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            };

            var file2 = new ProjectFile
            {
                Name = "customer_data.csv",
                Path = "/data/customer_data.csv",
                Extension = ".csv",
                MimeType = "text/csv",
                Size = 10240,
                Content = "id,name,email,age\n1,John Doe,john@example.com,30\n2,Jane Smith,jane@example.com,25",
                FolderId = folder2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-16)
            };

            context.ProjectFiles.AddRange(file1, file2);
            context.SaveChanges();

            // Create sample nodes for projects
            var node1 = new Node
            {
                Id = "node_001",
                Name = "Data Processing Node",
                NodeType = NodeType.Data,
                Properties = "{\"input_type\":\"csv\",\"delimiter\":\",\"}",
                Status = "active",
                ProjectId = project1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var node2 = new Node
            {
                Id = "node_002",
                Name = "AI Analysis Node",
                NodeType = NodeType.Action,
                Properties = "{\"model\":\"gpt-4\",\"temperature\":0.7}",
                Status = "active",
                ProjectId = project1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-9)
            };

            var node3 = new Node
            {
                Id = "node_003",
                Name = "Output Formatter",
                NodeType = NodeType.Panel,
                Properties = "{\"format\":\"json\",\"pretty\":true}",
                Status = "active",
                ProjectId = project2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            context.Nodes.AddRange(node1, node2, node3);
            context.SaveChanges();

            // Create sample chat messages
            var chatMessages = new[]
            {
                new ChatMessage
                {
                    Sender = "User",
                    Message = "How do I configure the data processing node?",
                    Timestamp = DateTime.UtcNow.AddDays(-8),
                    UserId = user1.Id,
                    NodeId = node1.Id,
                    MarkedAsSolution = false,
                    Liked = false,
                    Disliked = false
                },
                new ChatMessage
                {
                    Sender = "AI Assistant",
                    Message = "To configure the data processing node, you need to set the input_type property to specify your data format (csv, json, xml) and configure any format-specific options like delimiter for CSV files.",
                    Timestamp = DateTime.UtcNow.AddDays(-8).AddMinutes(2),
                    UserId = user1.Id,
                    NodeId = node1.Id,
                    MarkedAsSolution = true,
                    Liked = true,
                    Disliked = false
                },
                new ChatMessage
                {
                    Sender = "User",
                    Message = "What's the best temperature setting for the AI model?",
                    Timestamp = DateTime.UtcNow.AddDays(-7),
                    UserId = user1.Id,
                    NodeId = node2.Id,
                    MarkedAsSolution = false,
                    Liked = false,
                    Disliked = false
                },
                new ChatMessage
                {
                    Sender = "AI Assistant",
                    Message = "For most tasks, a temperature between 0.3-0.7 works well. Lower values (0.1-0.3) for more deterministic outputs, higher values (0.7-0.9) for more creative responses. The current setting of 0.7 is good for balanced creativity and consistency.",
                    Timestamp = DateTime.UtcNow.AddDays(-7).AddMinutes(1),
                    UserId = user1.Id,
                    NodeId = node2.Id,
                    MarkedAsSolution = true,
                    Liked = true,
                    Disliked = false
                },
                new ChatMessage
                {
                    Sender = "User",
                    Message = "Can I export the results in different formats?",
                    Timestamp = DateTime.UtcNow.AddDays(-4),
                    UserId = user2.Id,
                    NodeId = node3.Id,
                    MarkedAsSolution = false,
                    Liked = false,
                    Disliked = false
                },
                new ChatMessage
                {
                    Sender = "AI Assistant",
                    Message = "Yes! The output formatter supports multiple formats including JSON, XML, CSV, and plain text. You can configure the format property and enable pretty printing for better readability.",
                    Timestamp = DateTime.UtcNow.AddDays(-4).AddMinutes(3),
                    UserId = user2.Id,
                    NodeId = node3.Id,
                    MarkedAsSolution = true,
                    Liked = true,
                    Disliked = false
                },
                new ChatMessage
                {
                    Sender = "User",
                    Message = "Thanks! This is very helpful.",
                    Timestamp = DateTime.UtcNow.AddDays(-4).AddMinutes(5),
                    UserId = user2.Id,
                    NodeId = node3.Id,
                    MarkedAsSolution = false,
                    Liked = false,
                    Disliked = false
                }
            };

            context.ChatMessages.AddRange(chatMessages);
            context.SaveChanges();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
