-- =============================================
-- Sample Data: AI Models with Ollama Configuration
-- Description: Creates AI model configurations for different node levels
-- =============================================

-- Note: NodeType enum values: Director=0, Manager=1, Inspector=2, Worker=3, Compiler=4, Tester=5, Runner=6
-- Note: MessageTypeEnum values: Discussion=0, Decision=1

-- Get Template OIDs
SET @coding_template_oid = (SELECT OID FROM Template WHERE Name = 'Coding Project' LIMIT 1);
SET @writing_template_oid = (SELECT OID FROM Template WHERE Name = 'Book Writing' LIMIT 1);

-- =============================================
-- CODING TEMPLATE AI MODELS
-- =============================================

-- Director AI Model for Coding (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Director Discussion Model - Coding',
    'codellama:13b',  -- A good model for strategic thinking and architecture
    0,  -- Discussion
    0,  -- Director
    'Strategic AI model for high-level architectural discussions and planning in coding projects. Optimized for deep reasoning and long-form responses.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',  -- Ollama endpoint
    0.7,  -- Temperature (moderately creative)
    2048,  -- NumPredict (longer responses for detailed planning)
    40,  -- TopK
    0.9,  -- TopP
    0,  -- Seed (random)
    4096,  -- NumCtx (large context for understanding full scope)
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,  -- Stop sequences
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- Director AI Model for Coding (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Director Decision Model - Coding',
    'codellama:13b',
    1,  -- Decision
    0,  -- Director
    'Decision-focused AI model for architectural approvals and strategic choices. Configured for clear, authoritative responses.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.3,  -- Temperature (low for consistent, decisive answers)
    512,  -- NumPredict (concise decisions)
    20,  -- TopK
    0.8,  -- TopP
    0,  -- Seed
    4096,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- Manager AI Model for Coding (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Manager Discussion Model - Coding',
    'codellama:7b',  -- Lighter model for task coordination
    0,  -- Discussion
    1,  -- Manager
    'Task coordination AI model for breaking down work and managing development workflow. Balanced for practical, actionable responses.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.5,  -- Temperature (moderate for balanced responses)
    1024,  -- NumPredict
    30,  -- TopK
    0.85,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- Manager AI Model for Coding (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Manager Decision Model - Coding',
    'codellama:7b',
    1,  -- Decision
    1,  -- Manager
    'Task approval and prioritization AI model. Optimized for quick, practical decisions on work assignments and progress.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.3,  -- Temperature (low for consistent decisions)
    512,  -- NumPredict
    20,  -- TopK
    0.8,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- Inspector AI Model for Coding (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Inspector Discussion Model - Coding',
    'codellama:13b',  -- Needs good understanding for code review
    0,  -- Discussion
    2,  -- Inspector
    'Code review and quality assurance AI model. Configured for thorough analysis and constructive feedback on code quality.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.4,  -- Temperature (low-moderate for objective analysis)
    1536,  -- NumPredict (detailed feedback)
    30,  -- TopK
    0.85,  -- TopP
    0,  -- Seed
    4096,  -- NumCtx (needs full context for code review)
    1,  -- NumGpu
    4,  -- NumThread
    1.2,  -- RepeatPenalty (slightly higher to avoid repetitive feedback)
    NULL,
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- Inspector AI Model for Coding (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Inspector Decision Model - Coding',
    'codellama:13b',
    1,  -- Decision
    2,  -- Inspector
    'Quality gate decision AI model. Optimized for clear pass/fail decisions on code quality and standards compliance.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.2,  -- Temperature (very low for objective decisions)
    512,  -- NumPredict
    20,  -- TopK
    0.75,  -- TopP
    0,  -- Seed
    4096,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- Worker AI Model for Coding (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Worker Discussion Model - Coding',
    'codellama:7b',  -- Fast, efficient for code generation
    0,  -- Discussion
    3,  -- Worker
    'Code implementation AI model. Optimized for generating clean, efficient code with good documentation and error handling.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.4,  -- Temperature (low-moderate for reliable code)
    2048,  -- NumPredict (can generate longer code blocks)
    40,  -- TopK
    0.9,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.15,  -- RepeatPenalty (higher to avoid repetitive code patterns)
    NULL,
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- Worker AI Model for Coding (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Worker Decision Model - Coding',
    'codellama:7b',
    1,  -- Decision
    3,  -- Worker
    'Implementation decision AI model. Configured for making technical choices about algorithms, libraries, and approaches.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.4,  -- Temperature (moderate for balanced technical decisions)
    512,  -- NumPredict
    30,  -- TopK
    0.85,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @coding_template_oid,
    0,
    NULL
);

-- =============================================
-- BOOK WRITING TEMPLATE AI MODELS
-- =============================================

-- Director AI Model for Writing (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Director Discussion Model - Writing',
    'llama2:13b',  -- Good for creative and strategic thinking
    0,  -- Discussion
    0,  -- Director
    'Strategic AI model for book vision and structure. Optimized for creative thinking and high-level content planning.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.8,  -- Temperature (higher for creative strategic thinking)
    2048,  -- NumPredict
    50,  -- TopK (higher for more creative options)
    0.95,  -- TopP
    0,  -- Seed
    4096,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);

-- Director AI Model for Writing (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Director Decision Model - Writing',
    'llama2:13b',
    1,  -- Decision
    0,  -- Director
    'Editorial decision AI model for book structure and content direction. Configured for clear, authoritative decisions.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.4,  -- Temperature (moderate for balanced decisions)
    512,  -- NumPredict
    30,  -- TopK
    0.85,  -- TopP
    0,  -- Seed
    4096,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);

-- Manager AI Model for Writing (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Manager Discussion Model - Writing',
    'llama2:7b',
    0,  -- Discussion
    1,  -- Manager
    'Content organization AI model for managing the writing workflow. Balanced for practical content planning and assignment.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.6,  -- Temperature (moderate for organizational thinking)
    1024,  -- NumPredict
    40,  -- TopK
    0.9,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);

-- Manager AI Model for Writing (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Manager Decision Model - Writing',
    'llama2:7b',
    1,  -- Decision
    1,  -- Manager
    'Content approval AI model for managing writing assignments and progress. Optimized for practical editorial decisions.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.4,  -- Temperature (low-moderate for consistent decisions)
    512,  -- NumPredict
    30,  -- TopK
    0.85,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);

-- Inspector AI Model for Writing (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Inspector Discussion Model - Writing',
    'llama2:13b',  -- Better understanding for editorial review
    0,  -- Discussion
    2,  -- Inspector
    'Editorial review AI model for quality control and style consistency. Configured for thorough content analysis and feedback.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.5,  -- Temperature (moderate for balanced critique)
    1536,  -- NumPredict (detailed feedback)
    40,  -- TopK
    0.9,  -- TopP
    0,  -- Seed
    4096,  -- NumCtx (needs full context)
    1,  -- NumGpu
    4,  -- NumThread
    1.2,  -- RepeatPenalty (avoid repetitive feedback)
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);

-- Inspector AI Model for Writing (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Inspector Decision Model - Writing',
    'llama2:13b',
    1,  -- Decision
    2,  -- Inspector
    'Editorial approval AI model for publication readiness. Optimized for clear editorial judgments on content quality.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.3,  -- Temperature (low for objective decisions)
    512,  -- NumPredict
    20,  -- TopK
    0.8,  -- TopP
    0,  -- Seed
    4096,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);

-- Worker AI Model for Writing (Discussion)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Worker Discussion Model - Writing',
    'llama2:7b',  -- Fast, efficient for content generation
    0,  -- Discussion
    3,  -- Worker
    'Content creation AI model for writing book sections. Optimized for engaging, well-structured prose.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.7,  -- Temperature (moderate-high for creative writing)
    2048,  -- NumPredict (longer content generation)
    50,  -- TopK (more creative options)
    0.95,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.15,  -- RepeatPenalty (avoid repetitive writing)
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);

-- Worker AI Model for Writing (Decision)
INSERT INTO AIModel (
    OID, Name, ModelIdentifier, MessageType, NodeType, Description, IsActive,
    EndpointAddress, Temperature, NumPredict, TopK, TopP, Seed, NumCtx,
    NumGpu, NumThread, RepeatPenalty, Stop,
    CreatedAt, UpdatedAt, Template, OptimisticLockField, GCRecord
)
VALUES (
    UUID(),
    'Worker Decision Model - Writing',
    'llama2:7b',
    1,  -- Decision
    3,  -- Worker
    'Writing decision AI model for content choices and structure. Configured for practical decisions on examples, depth, and style.',
    1,  -- IsActive
    'http://ollama:11434/api/generate',
    0.5,  -- Temperature (moderate for balanced decisions)
    512,  -- NumPredict
    40,  -- TopK
    0.9,  -- TopP
    0,  -- Seed
    3072,  -- NumCtx
    1,  -- NumGpu
    4,  -- NumThread
    1.1,  -- RepeatPenalty
    NULL,
    NOW(),
    NOW(),
    @writing_template_oid,
    0,
    NULL
);
