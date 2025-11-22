using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace NodPT.Data.Models
{
    public class Node
    {
        [Key]
        [MaxLength(450)]
        public string? Id { get; set; }

        public string? Name { get; set; }

        public NodeType NodeType { get; set; }

        public string? Properties { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? Status { get; set; }

        public string? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        [JsonIgnore]
        public virtual Node? Parent { get; set; }

        [JsonIgnore]
        public virtual ICollection<Node> Children { get; set; } = new List<Node>();

        public int? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [JsonIgnore]
        public virtual Project? Project { get; set; }

        /// <summary>
        /// Many-to-one relationship: Node can belong to a Template (nullable)
        /// </summary>
        public int? TemplateId { get; set; }

        [ForeignKey(nameof(TemplateId))]
        [JsonIgnore]
        public virtual Template? Template { get; set; }

        /// <summary>
        /// Type of message: Discussion or Decision
        /// </summary>
        public MessageTypeEnum MessageType { get; set; }

        /// <summary>
        /// Level: Brain, Manager, Inspector, or Worker
        /// </summary>
        public LevelEnum Level { get; set; }

        /// <summary>
        /// Many-to-one relationship: Node can have an AIModel (nullable)
        /// </summary>
        public int? AIModelId { get; set; }

        [ForeignKey(nameof(AIModelId))]
        [JsonIgnore]
        public virtual AIModel? AIModel { get; set; }

        [JsonIgnore]
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

        // Helper property to work with Properties as Dictionary
        [NotMapped]
        [Browsable(false)]
        public Dictionary<string, string> PropertiesDictionary
        {
            get
            {
                if (string.IsNullOrEmpty(Properties))
                    return new Dictionary<string, string>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Properties)
                           ?? new Dictionary<string, string>();
                }
                catch
                {
                    return new Dictionary<string, string>();
                }
            }
            set
            {
                Properties = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }

        /// <summary>
        /// Readonly property that returns the AIModel from Project.Template that matches this Node's MessageType and Level
        /// </summary>
        [NotMapped]
        [Browsable(false)]
        public AIModel? MatchingAIModel
        {
            get
            {
                if (Project?.Template == null) return null;

                return Project.Template.AIModels
                    .FirstOrDefault(am => am.MessageType == MessageType && am.Level == Level && am.IsActive);
            }
        }

        /// <summary>
        /// Readonly property that returns the list of Prompts from Project.Template that match this Node's MessageType and Level
        /// </summary>
        [NotMapped]
        [Browsable(false)]
        public List<Prompt> MatchingPrompts
        {
            get
            {
                if (Project?.Template == null) return new List<Prompt>();

                return Project.Template.Prompts
                    .Where(p => p.MessageType == MessageType && p.Level == Level)
                    .ToList();
            }
        }
    }
}
