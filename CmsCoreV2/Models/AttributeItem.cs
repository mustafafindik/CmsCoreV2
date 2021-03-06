﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CmsCoreV2.Models
{
    public class AttributeItem:BaseEntity
    {
        public AttributeItem() {
            ProductAttributeItems = new HashSet<ProductAttributeItem>();
        }

        [Required]
        [Display(Name = "Ad")]
        [StringLength(200)]
        public string Name { get; set; }

        [Display(Name = "Bağlantı")]
        [StringLength(200)]
        public string Slug { get; set; }

        [Display(Name = "Açıklama")]
        public string Description { get; set; }

        [Display(Name = "Özellik")]
        public long? AttributeId { get; set; }
        [Display(Name = "Özellik")]
        [ForeignKey("AttributeId")]
        public Attribute Attribute { get; set; }

        public virtual ICollection<ProductAttributeItem> ProductAttributeItems {get; set;}

        
    }
}
