using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MasterProject.Models
{
    public class Inquiry
    {
        [Key]
        public Guid InquiryId { get; set; }
        public string Content { get; set; }
        public string Response { get; set; }
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser User { get; set; }
        public string ResponseBy { get; set; }
    }
}
