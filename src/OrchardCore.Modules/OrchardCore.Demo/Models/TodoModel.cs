using System;
using System.ComponentModel.DataAnnotations;
using OrchardCore.DisplayManagement.Views;

namespace OrchardCore.Demo.Models
{
    public class TodoModel : ShapeViewModel
    {
        public TodoModel()
        {
            Metadata.Type = "Todo";
        }


        [Required]
        public string Text { get; set; }
        [Required]
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }


        public string DisplayMode
        {
            get
            {
                return Metadata.DisplayType;
            }
            set
            {
                var alternate = $"Todo_{value}";
                if (Metadata.Alternates.Contains(alternate))
                {
                    if (Metadata.Alternates.Last == alternate)
                    {
                        return;
                    }

                    Metadata.Alternates.Remove(alternate);
                }
                Metadata.Alternates.Add(alternate);
                Metadata.DisplayType = value;
            }
        }
    }
}
