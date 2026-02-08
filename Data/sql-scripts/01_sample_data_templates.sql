-- =============================================
-- Sample Data: Templates
-- Description: Creates sample templates for Coding and Book Writing projects
-- =============================================
-- Note: XPO creates tables with PascalCase naming convention
-- The primary key field is 'OID' (Object ID) as GUID/VARCHAR
-- Insert Coding Template
INSERT INTO Template (
        Name,
        Description,
        Category,
        Version,
        Icon,
        IsActive,
        CreatedAt,
        UpdatedAt,
        OptimisticLockField,
        GCRecord
    )
VALUES (
        -- OID (primary key)
        'Coding Project',
        -- Name
        'A comprehensive template for software development projects with AI-assisted workflow. This template includes Director for strategic planning, Manager for task coordination, Supervisor for quality assurance, and Agent agents for actual code development.',
        -- Description
        'coding',
        -- Category
        '1.0.0',
        -- Version
        'bi bi-code-slash text-primary',
        -- Icon (Bootstrap Icons class)
        1,
        -- IsActive (boolean, 1 = true)
        NOW(),
        -- CreatedAt
        NOW(),
        -- UpdatedAt
        0,
        -- OptimisticLockField (for concurrency control)
        NULL -- GCRecord (for garbage collection, NULL means not deleted)
    );
-- Insert Book Writing Template
INSERT INTO Template (
        Name,
        Description,
        Category,
        Version,
        Icon,
        IsActive,
        CreatedAt,
        UpdatedAt,
        OptimisticLockField,
        GCRecord
    )
VALUES (
        -- OID (primary key)
        'Book Writing',
        -- Name
        'A structured template for book writing projects with AI collaboration. Features Director for outline and strategy, Manager for chapter coordination, Supervisor for editorial review, and Writer agents for content creation.',
        -- Description
        'book writing',
        -- Category
        '1.0.0',
        -- Version
        'bi bi-pen text-success',
        -- Icon (Bootstrap Icons class)
        1,
        -- IsActive (boolean, 1 = true)
        NOW(),
        -- CreatedAt
        NOW(),
        -- UpdatedAt
        0,
        -- OptimisticLockField
        NULL -- GCRecord
    );