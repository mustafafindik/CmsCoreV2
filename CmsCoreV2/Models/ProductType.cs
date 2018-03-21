﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CmsCoreV2.Models
{
    public enum ProductType
    {
        [Display(Name="Basit")]
        Simple = 1,
        /*
        Grouped = 2,
         
        [Display(Name="Harici")]
        External = 3
        ,*/
        [Display(Name = "Değişken")]
        Variable = 4 

    }
}
