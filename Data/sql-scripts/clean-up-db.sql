update Folder
set Parent = NULL;
delete from TemplateFile;
DELETE FROM ProjectFile;
DELETE from Folder;
DELETE from Project;
DELETE from ChatMessage;
DELETE from Node;
delete from AIModel;
delete from Template;
delete from Prompt;