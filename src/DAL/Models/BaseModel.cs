using System;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreateDate { get; set; }
    }
}