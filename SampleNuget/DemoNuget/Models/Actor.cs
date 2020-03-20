﻿using DemoNuget.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DemoNuget.Models
{
    public class Actor : BaseNotify
    {
        public string PhotoUrl { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string FamousRole { get; set; }
        public string Description { get; set; }
    }
}
