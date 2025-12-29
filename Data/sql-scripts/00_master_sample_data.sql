-- =============================================
-- Master Sample Data Script
-- Description: Executes all sample data scripts in the correct order
-- Usage: Run this script to populate sample data for Templates, Prompts, and AI Models
-- =============================================

-- Note: This script should be run after the database schema has been created by XPO
-- Make sure the application has been run at least once in DEBUG mode to create the tables

-- Disable foreign key checks temporarily (if needed)
-- SET FOREIGN_KEY_CHECKS = 0;

-- =============================================
-- 1. Create Templates
-- =============================================
SOURCE 01_sample_data_templates.sql;

-- =============================================
-- 2. Create Prompts (depends on Templates)
-- =============================================
SOURCE 02_sample_data_prompts.sql;

-- =============================================
-- 3. Create AI Models (depends on Templates)
-- =============================================
SOURCE 03_sample_data_aimodels.sql;

-- Re-enable foreign key checks
-- SET FOREIGN_KEY_CHECKS = 1;

-- =============================================
-- Verification Queries
-- =============================================

-- Check created templates
SELECT 
    Name, Category, Version, IsActive, Description 
FROM Template 
WHERE GCRecord IS NULL;

-- Check created prompts count
SELECT 
    t.Name AS TemplateName,
    p.NodeType,
    p.MessageType,
    COUNT(*) AS PromptCount
FROM Prompt p
JOIN Template t ON p.Template = t.OID
WHERE p.GCRecord IS NULL
GROUP BY t.Name, p.NodeType, p.MessageType
ORDER BY t.Name, p.NodeType, p.MessageType;

-- Check created AI models count
SELECT 
    t.Name AS TemplateName,
    a.NodeType,
    a.MessageType,
    COUNT(*) AS AIModelCount
FROM AIModel a
JOIN Template t ON a.Template = t.OID
WHERE a.GCRecord IS NULL
GROUP BY t.Name, a.NodeType, a.MessageType
ORDER BY t.Name, a.NodeType, a.MessageType;

-- Show total counts
SELECT 
    (SELECT COUNT(*) FROM Template WHERE GCRecord IS NULL) AS TotalTemplates,
    (SELECT COUNT(*) FROM Prompt WHERE GCRecord IS NULL) AS TotalPrompts,
    (SELECT COUNT(*) FROM AIModel WHERE GCRecord IS NULL) AS TotalAIModels;
