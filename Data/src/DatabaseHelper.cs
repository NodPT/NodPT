using DevExpress.Xpo;
using DevExpress.Data;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using NodPT.Data.Models;
using System.Runtime.CompilerServices;

public static class DatabaseHelper
{
    static string connectionString = string.Empty;

    public static UnitOfWork CreateUnitOfWork()
    {

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string is not set. Please set it before creating a UnitOfWork.");

        var dataStore = XpoDefault.GetConnectionProvider(connectionString, AutoCreateOption.SchemaAlreadyExists);
        var dl = new SimpleDataLayer(dataStore);

#if DEBUG
//        CreateSampleData(dl);
#endif

        return new UnitOfWork(dl);

    }

    private static void CreateSampleData(SimpleDataLayer dl)
    {
        using var session = new UnitOfWork(dl);

        // Check if data already exists
        if (session.Query<User>().Any())
        {
            return; // Data already exists
        }

        // Begin transaction
        session.BeginTransaction();

        try
        {
            // Create sample users
            var user1 = new User(session)
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

            var user2 = new User(session)
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

            // Create sample templates
            var template1 = new Template(session)
            {
                Name = "Data Processing Workflow",
                Description = "A template for data processing and analysis workflows",
                Category = "Data Science",
                Version = "1.0.0",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            };

            var template2 = new Template(session)
            {
                Name = "AI Assistant Workflow",
                Description = "A template for AI-powered assistant workflows",
                Category = "AI/ML",
                Version = "1.2.0",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-35)
            };

            // Create sample template files
            var templateFile1 = new TemplateFile(session)
            {
                Name = "workflow_config.json",
                Path = "/config/workflow_config.json",
                Extension = ".json",
                Size = 256,
                MimeType = "application/json",
                Content = "{\"workflow\": {\"type\": \"data_processing\", \"version\": \"1.0\", \"steps\": [\"load\", \"process\", \"analyze\"]}}",
                Template = template1,
                CreatedAt = DateTime.UtcNow.AddDays(-38)
            };

            var templateFile2 = new TemplateFile(session)
            {
                Name = "data_schema.yaml",
                Path = "/schemas/data_schema.yaml",
                Extension = ".yaml",
                Size = 512,
                MimeType = "text/yaml",
                Content = "schema:\n  type: object\n  properties:\n    id: {type: integer}\n    name: {type: string}\n    data: {type: array}",
                Template = template1,
                CreatedAt = DateTime.UtcNow.AddDays(-37)
            };

            var templateFile3 = new TemplateFile(session)
            {
                Name = "ai_config.json",
                Path = "/config/ai_config.json",
                Extension = ".json",
                Size = 384,
                MimeType = "application/json",
                Content = "{\"ai\": {\"model\": \"gpt-4\", \"temperature\": 0.7, \"max_tokens\": 2000, \"system_prompt\": \"You are a helpful AI assistant.\"}}",
                Template = template2,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            var templateFile4 = new TemplateFile(session)
            {
                Name = "prompt_templates.txt",
                Path = "/templates/prompt_templates.txt",
                Extension = ".txt",
                Size = 1024,
                MimeType = "text/plain",
                Content = "User Question Template:\nUser: {user_input}\nContext: {context}\nPlease provide a helpful response.\n\nSystem Response Template:\nBased on the provided context, here is my response: {response}",
                Template = template2,
                CreatedAt = DateTime.UtcNow.AddDays(-32)
            };

            // Create sample template nodes
            var templateNode1 = new Node(session)
            {
                Id = "template_node_001",
                Name = "Data Input Node",
                NodeType = NodeType.Data,
                Properties = "{\"input_type\":\"csv\",\"delimiter\":\",\",\"encoding\":\"utf-8\"}",
                Status = "template",
                Template = template1,
                CreatedAt = DateTime.UtcNow.AddDays(-36)
            };

            var templateNode2 = new Node(session)
            {
                Id = "template_node_002",
                Name = "Data Processor Node",
                NodeType = NodeType.Action,
                Properties = "{\"processing_type\":\"transform\",\"operations\":[\"clean\",\"normalize\",\"validate\"]}",
                Status = "template",
                Template = template1,
                CreatedAt = DateTime.UtcNow.AddDays(-36)
            };

            var templateNode3 = new Node(session)
            {
                Id = "template_node_003",
                Name = "Analysis Node",
                NodeType = NodeType.Action,
                Properties = "{\"analysis_type\":\"statistical\",\"metrics\":[\"mean\",\"median\",\"std_dev\"]}",
                Status = "template",
                Template = template1,
                CreatedAt = DateTime.UtcNow.AddDays(-36)
            };

            var templateNode4 = new Node(session)
            {
                Id = "template_node_004",
                Name = "AI Input Node",
                NodeType = NodeType.Data,
                Properties = "{\"input_type\":\"text\",\"max_length\":1000}",
                Status = "template",
                Template = template2,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            var templateNode5 = new Node(session)
            {
                Id = "template_node_005",
                Name = "AI Processing Node",
                NodeType = NodeType.Action,
                Properties = "{\"model\":\"gpt-4\",\"temperature\":0.7,\"max_tokens\":2000}",
                Status = "template",
                Template = template2,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            var templateNode6 = new Node(session)
            {
                Id = "template_node_006",
                Name = "Response Formatter",
                NodeType = NodeType.Panel,
                Properties = "{\"format\":\"json\",\"include_metadata\":true}",
                Status = "template",
                Template = template2,
                CreatedAt = DateTime.UtcNow.AddDays(-33)
            };

            // Create sample projects
            var project1 = new Project(session)
            {
                Name = "Customer Data Analysis",
                Description = "Analyze customer behavior data for insights",
                User = user1,
                Template = template1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            };

            var project2 = new Project(session)
            {
                Name = "AI Support Bot",
                Description = "Intelligent customer support chatbot",
                User = user2,
                Template = template2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            // Create sample folders
            var folder1 = new Folder(session)
            {
                Name = "src",
                Path = "/src",
                Project = project1,
                CreatedAt = DateTime.UtcNow.AddDays(-18)
            };

            var folder2 = new Folder(session)
            {
                Name = "data",
                Path = "/data",
                Project = project1,
                CreatedAt = DateTime.UtcNow.AddDays(-18)
            };

            var folder3 = new Folder(session)
            {
                Name = "components",
                Path = "/src/components",
                Project = project2,
                Parent = folder1,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };

            // Create sample files
            var file1 = new ProjectFile(session)
            {
                Name = "data_processor.py",
                Path = "/src/data_processor.py",
                Extension = ".py",
                MimeType = "text/x-python",
                Size = 2048,
                Content = "# Data processing module\nimport pandas as pd\n\ndef process_data(df):\n    return df.dropna()",
                Folder = folder1,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            };

            var file2 = new ProjectFile(session)
            {
                Name = "customer_data.csv",
                Path = "/data/customer_data.csv",
                Extension = ".csv",
                MimeType = "text/csv",
                Size = 10240,
                Content = "id,name,email,age\n1,John Doe,john@example.com,30\n2,Jane Smith,jane@example.com,25",
                Folder = folder2,
                CreatedAt = DateTime.UtcNow.AddDays(-16)
            };

            // Create sample nodes for projects (not users directly)
            var node1 = new Node(session)
            {
                Id = "node_001",
                Name = "Data Processing Node",
                NodeType = NodeType.Data,
                Properties = "{\"input_type\":\"csv\",\"delimiter\":\",\"}",
                Status = "active",
                Project = project1,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var node2 = new Node(session)
            {
                Id = "node_002",
                Name = "AI Analysis Node",
                NodeType = NodeType.Action,
                Properties = "{\"model\":\"gpt-4\",\"temperature\":0.7}",
                Status = "active",
                Project = project1,
                CreatedAt = DateTime.UtcNow.AddDays(-9)
            };

            var node3 = new Node(session)
            {
                Id = "node_003",
                Name = "Output Formatter",
                NodeType = NodeType.Panel,
                Properties = "{\"format\":\"json\",\"pretty\":true}",
                Status = "active",
                Project = project2,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            // Create sample chat messages
            var chatMessages = new[]
            {
                    new ChatMessage(session)
                    {
                        Sender = "User",
                        Message = "How do I configure the data processing node?",
                        Timestamp = DateTime.UtcNow.AddDays(-8),
                        User = user1,
                        Node = node1,
                        MarkedAsSolution = false,
                        Liked = false,
                        Disliked = false
                    },
                    new ChatMessage(session)
                    {
                        Sender = "AI Assistant",
                        Message = "To configure the data processing node, you need to set the input_type property to specify your data format (csv, json, xml) and configure any format-specific options like delimiter for CSV files.",
                        Timestamp = DateTime.UtcNow.AddDays(-8).AddMinutes(2),
                        User = user1,
                        Node = node1,
                        MarkedAsSolution = true,
                        Liked = true,
                        Disliked = false
                    },
                    new ChatMessage(session)
                    {
                        Sender = "User",
                        Message = "What's the best temperature setting for the AI model?",
                        Timestamp = DateTime.UtcNow.AddDays(-7),
                        User = user1,
                        Node = node2,
                        MarkedAsSolution = false,
                        Liked = false,
                        Disliked = false
                    },
                    new ChatMessage(session)
                    {
                        Sender = "AI Assistant",
                        Message = "For most tasks, a temperature between 0.3-0.7 works well. Lower values (0.1-0.3) for more deterministic outputs, higher values (0.7-0.9) for more creative responses. The current setting of 0.7 is good for balanced creativity and consistency.",
                        Timestamp = DateTime.UtcNow.AddDays(-7).AddMinutes(1),
                        User = user1,
                        Node = node2,
                        MarkedAsSolution = true,
                        Liked = true,
                        Disliked = false
                    },
                    new ChatMessage(session)
                    {
                        Sender = "User",
                        Message = "Can I export the results in different formats?",
                        Timestamp = DateTime.UtcNow.AddDays(-4),
                        User = user2,
                        Node = node3,
                        MarkedAsSolution = false,
                        Liked = false,
                        Disliked = false
                    },
                    new ChatMessage(session)
                    {
                        Sender = "AI Assistant",
                        Message = "Yes! The output formatter supports multiple formats including JSON, XML, CSV, and plain text. You can configure the format property and enable pretty printing for better readability.",
                        Timestamp = DateTime.UtcNow.AddDays(-4).AddMinutes(3),
                        User = user2,
                        Node = node3,
                        MarkedAsSolution = true,
                        Liked = true,
                        Disliked = false
                    },
                    new ChatMessage(session)
                    {
                        Sender = "User",
                        Message = "Thanks! This is very helpful.",
                        Timestamp = DateTime.UtcNow.AddDays(-4).AddMinutes(5),
                        User = user2,
                        Node = node3,
                        MarkedAsSolution = false,
                        Liked = false,
                        Disliked = false
                    }
                };

            // Save all sample data
            session.Save(user1);
            session.Save(user2);
            session.Save(template1);
            session.Save(template2);
            session.Save(templateFile1);
            session.Save(templateFile2);
            session.Save(templateFile3);
            session.Save(templateFile4);
            session.Save(templateNode1);
            session.Save(templateNode2);
            session.Save(templateNode3);
            session.Save(templateNode4);
            session.Save(templateNode5);
            session.Save(templateNode6);
            session.Save(project1);
            session.Save(project2);
            session.Save(folder1);
            session.Save(folder2);
            session.Save(folder3);
            session.Save(file1);
            session.Save(file2);
            session.Save(node1);
            session.Save(node2);
            session.Save(node3);

            foreach (var message in chatMessages)
            {
                session.Save(message);
            }

            session.CommitTransaction();
        }
        catch
        {
            session.RollbackTransaction();
            throw;
        }
    }
    public static void SetConnectionString(string _connectionString)
    {
        connectionString = _connectionString;
    }
}
